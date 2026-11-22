using System;
using System.Globalization;
using ThroatBalls.Logic;
using ThunderRoad;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>
    /// Entry point. The game's ModManager creates one instance of every ThunderScript subclass in a mod DLL,
    /// calls ScriptLoaded, then Enable; ScriptUpdate runs every frame (after the creatures' own updates).
    ///
    /// A player's hit on an NPC is judged by <see cref="LowBlowClassifier"/>; a throat or groin hit starts or
    /// extends a reaction, the second spot in time makes the combo (<see cref="ReactionController"/>).
    /// </summary>
    public sealed class ThroatBallsScript : ThunderScript
    {
        private readonly ReactionController reactions = new ReactionController();
        private readonly ZoneMarkers markers = new ZoneMarkers();
        private readonly LowBlowSettings hitSettings = new LowBlowSettings();
        private bool hooked;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            ModLog.Info("Loaded " + modData.Name + " " + modData.ModVersion + " (folder " + modData.folderName + ")");
        }

        public override void ScriptEnable()
        {
            Hook(true);
            ModLog.Info("Enabled, listening to EventManager.onCreatureHit");
        }

        public override void ScriptDisable()
        {
            Hook(false);
            reactions.ReleaseAll();
            markers.Clear();
            ModLog.Info("Disabled");
        }

        public override void ScriptUnload()
        {
            Hook(false);
            reactions.ReleaseAll();
            markers.Clear();
        }

        public override void ScriptUpdate()
        {
            try
            {
                reactions.Update(Time.time);
            }
            catch (Exception ex)
            {
                ModLog.Error("Reaction update failed: " + ex);
            }
            try
            {
                markers.Update();
            }
            catch (Exception ex)
            {
                ModLog.Error("Zone markers failed: " + ex);
                markers.Clear();
            }
        }

        private void Hook(bool on)
        {
            if (on == hooked)
            {
                return;
            }
            if (on)
            {
                EventManager.onCreatureHit += OnCreatureHit;
                EventManager.onCreatureKill += OnCreatureKill;
                EventManager.onCreatureDespawn += OnCreatureDespawn;
            }
            else
            {
                EventManager.onCreatureHit -= OnCreatureHit;
                EventManager.onCreatureKill -= OnCreatureKill;
                EventManager.onCreatureDespawn -= OnCreatureDespawn;
            }
            hooked = on;
        }

        // OnEnd: Creature.Damage raises it only for hits that did not kill, after the damage is applied.
        private void OnCreatureHit(Creature creature, CollisionInstance hit, EventTime eventTime)
        {
            if (eventTime != EventTime.OnEnd || !ModOptions.ModEnabled || creature == null || hit == null)
            {
                return;
            }
            try
            {
                Judge(creature, hit);
            }
            catch (Exception ex)
            {
                ModLog.Error("Hit check failed: " + ex);
            }
        }

        private void Judge(Creature creature, CollisionInstance hit)
        {
            var attacker = GameAdapters.Attacker(hit);
            var input = GameAdapters.BuildInput(creature, hit, attacker);
            var result = LowBlowClassifier.Classify(input, ModOptions.Fill(hitSettings));
            if (result.Rejection == LowBlowRejection.SelfHit)
            {
                return;
            }
            if (result.Rejection == LowBlowRejection.NotPlayerVsNpc)
            {
                ModLog.Verbose("SKIP | " + GameAdapters.Label(creature) + " | NotPlayerVsNpc");
                return;
            }
            markers.AddHitMarker(hit.contactPoint, result.IsHit);
            if (!result.IsHit)
            {
                ModLog.Verbose("SKIP | " + GameAdapters.Label(creature) + " | " + result.Rejection + Details(input, result) + Armor(creature, result));
                return;
            }
            ModLog.Verbose("LOW BLOW | " + GameAdapters.Label(creature) + " | " + result.HitZone + Details(input, result) + Armor(creature, result));
            reactions.OnLowBlow(creature, result.HitZone, Time.time);
        }

        private static string Details(LowBlowInput input, LowBlowResult result)
        {
            var c = CultureInfo.InvariantCulture;
            return " | " + input.DamageType + (input.IsUnarmed ? " fist/foot" : string.Empty)
                + " | speed " + input.ImpactSpeed.ToString("0.0", c) + " m/s"
                + (float.IsNaN(result.ZoneDistance) ? string.Empty : " | nearest " + result.NearestZone + " " + result.ZoneDistance.ToString("0.000", c) + " m")
                + (float.IsNaN(result.AngleFromFront) ? string.Empty : " | angle " + result.AngleFromFront.ToString("0", c) + " deg")
                + (input.TargetIsMale ? " | male" : " | not male");
        }

        /// <summary>What is worn where, once a hit reached the zones (for checking the armor rule in the game).</summary>
        private static string Armor(Creature creature, LowBlowResult result)
        {
            if (!ModOptions.DebugLogging || result.NearestZone == HitZone.None)
            {
                return string.Empty;
            }
            return result.NearestZone == HitZone.Throat
                ? " | neck: " + GameAdapters.DescribeArmor(creature, GameAdapters.NeckOf(creature))
                : " | thigh: " + GameAdapters.DescribeArmor(creature, GameAdapters.ThighOf(creature.ragdoll, RagdollPart.Type.LeftLeg))
                    + " | hips: " + GameAdapters.DescribeArmor(creature, GameAdapters.HipsOf(creature));
        }

        private void OnCreatureKill(Creature creature, Player player, CollisionInstance hit, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                reactions.OnKilled(creature);
            }
        }

        private void OnCreatureDespawn(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
            {
                reactions.OnDespawn(creature);
            }
        }
    }
}
