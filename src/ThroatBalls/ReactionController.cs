using System.Collections.Generic;
using ThroatBalls.Logic;
using ThunderRoad;

namespace ThroatBalls
{
    /// <summary>
    /// Runs every NPC's reaction: a counted hit goes through <see cref="ReactionRules"/>, every frame the body
    /// follows <see cref="PoseRules"/>, and the end undoes exactly what was changed (<see cref="ReactionBody"/>).
    /// Per frame it only touches NPCs that react or wait for a second hit; nothing is allocated.
    /// </summary>
    internal sealed class ReactionController
    {
        private readonly Dictionary<int, Reaction> reactions = new Dictionary<int, Reaction>();
        private readonly List<int> done = new List<int>();
        private readonly ReactionSettings reactionSettings = new ReactionSettings();
        private readonly PoseSettings poseSettings = new PoseSettings();
        private readonly LowBlowSettings zoneSettings = new LowBlowSettings();

        // [0, 1), so a 100 % chance always hits (UnityEngine.Random.value can return 1).
        private readonly System.Random dice = new System.Random();

        /// <summary>The player's own sound files and the game clips; null: the voice's own lines only.</summary>
        public SoundBank Sounds { get; set; }

        public int Count => reactions.Count;

        public bool IsReacting(Creature creature)
        {
            return creature != null && reactions.TryGetValue(creature.GetInstanceID(), out var r) && r.State.IsActive;
        }

        public void OnLowBlow(Creature creature, HitZone zone, float impactSpeed, float now)
        {
            var id = creature.GetInstanceID();
            if (!reactions.TryGetValue(id, out var r))
            {
                r = new Reaction(creature);
                reactions[id] = r;
            }
            var before = r.State.Kind;
            var rolls = new ReactionRolls((float)dice.NextDouble(), (float)dice.NextDouble(), (float)dice.NextDouble());
            var step = ReactionRules.OnHit(r.State, zone, now, ModOptions.Fill(reactionSettings), rolls);
            r.State = step.Next;
            r.HitSpeed = impactSpeed;
            switch (step.Action)
            {
                case ReactionAction.Start:
                    Begin(r, now, before, string.Empty);
                    break;
                case ReactionAction.StartCombo:
                    Begin(r, now, before, " (both hits, roll " + F2(rolls.Combo) + " < " + Percent(reactionSettings.ComboKnockdownChance) + ")");
                    break;
                case ReactionAction.StartGroinDown:
                    Begin(r, now, before, " (groin, roll " + F2(rolls.Knockdown) + " < " + Percent(reactionSettings.GroinKnockdownChance) + ")");
                    break;
                case ReactionAction.Extend:
                    ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | " + r.State.Kind + " longer, until +" + F(r.State.Until - now) + " s");
                    break;
                case ReactionAction.Shrugged:
                    ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | " + zone + " hit shrugged off (roll " + F2(rolls.Reaction) + ", chance "
                        + Percent(zone == HitZone.Throat ? reactionSettings.ThroatReactionChance : reactionSettings.GroinReactionChance) + ")");
                    break;
                case ReactionAction.IgnoredWhileDown:
                    ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | " + zone + " hit ignored, he is down");
                    break;
            }
        }

        public void Update(float now)
        {
            if (reactions.Count == 0)
            {
                return;
            }
            var enabled = ModOptions.ModEnabled;
            ModOptions.Fill(reactionSettings);
            ModOptions.Fill(poseSettings);
            done.Clear();
            foreach (var pair in reactions)
            {
                if (!Tick(pair.Value, now, enabled))
                {
                    done.Add(pair.Key);
                }
            }
            foreach (var id in done)
            {
                SoundBank.Release(reactions[id]);
                reactions.Remove(id);
            }
        }

        /// <summary>One NPC, one frame. False when there is nothing left to keep.</summary>
        private bool Tick(Reaction r, float now, bool enabled)
        {
            var creature = r.Creature;
            if (creature == null || creature.isKilled)
            {
                Undo(r, creature != null ? "died" : "gone", wake: false);
                return false;
            }
            var step = ReactionRules.OnTick(r.State, now, enabled);
            if (step.Action == ReactionAction.End)
            {
                var kind = r.State.Kind;
                r.State = step.Next;
                End(r, kind, enabled ? "time up" : "mod turned off");
            }
            if (!r.State.IsActive)
            {
                return !ReactionRules.Forgettable(r.State, now, reactionSettings);
            }
            Hold(r, now);
            return true;
        }

        /// <summary>The body each frame while it reacts.</summary>
        private void Hold(Reaction r, float now)
        {
            var kind = r.State.Kind;
            var elapsed = now - r.State.StartedAt;
            var creature = r.Creature;
            // The throat keeps the standing height; after a groin reaction (combo off) it straightens up at once.
            if (kind != ReactionKind.Throat || r.HeightSet)
            {
                ReactionBody.SetHeight(r, PoseRules.BodyHeight(kind, elapsed, r.State.Until - now, poseSettings));
            }
            var curled = kind == ReactionKind.Down && poseSettings.FallStyle == FallStyle.CurlUp && elapsed > PoseRules.BendTime;
            ReactionBody.SetInjured(r, curled);
            if (kind == ReactionKind.Down && poseSettings.FallStyle == FallStyle.Ragdoll && PoseRules.IsDown(kind, elapsed, poseSettings))
            {
                if (!r.KnockedDown)
                {
                    ReactionBody.KnockDown(r, this);
                    ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | down (ragdoll)");
                }
                creature.brain.AddNoStandUpModifier(this);
            }
            // As the game's own stun does every frame: no walking on while out of it.
            creature.locomotion.moveDirection = UnityEngine.Vector3.zero;
            if (!ModOptions.Sounds)
            {
                return;
            }
            if (kind == ReactionKind.Throat)
            {
                // The throat sound again Choke Repeats times, each after the last one and a pause; he holds his throat until the last ends.
                var repeats = (int)ModOptions.ChokeRepeats;
                if (PoseRules.ChokeDue(r.Chokes, repeats, now - r.LastGroanAt, r.LastSoundLength))
                {
                    r.LastGroanAt = now;
                    r.Chokes++;
                    Voice(r, kind, repeat: true);
                }
                r.State.Until = System.Math.Max(r.State.Until, PoseRules.ChokesEnd(r.LastGroanAt, r.Chokes, repeats, r.LastSoundLength));
            }
            else if (PoseRules.GroanDue(r.LastGroanAt, now, ModOptions.GroanInterval))
            {
                r.LastGroanAt = now;
                Voice(r, kind, repeat: true);
            }
        }

        private void Begin(Reaction r, float now, ReactionKind before, string cause)
        {
            var kind = r.State.Kind;
            var creature = r.Creature;
            r.Generation++;
            r.LastSoundLength = PoseRules.UnknownChokeLength;
            if (ModOptions.Sounds)
            {
                Voice(r, kind, repeat: false);
            }
            r.Chokes = 1;
            r.LastGroanAt = now;
            var drops = PoseRules.DropsWeapons(kind, ModOptions.DropWeapons);
            if (drops)
            {
                ReactionBody.DropWeapons(r);
            }
            ReactionBody.KnockOut(r);
            ReactionBody.SetStunClip(r, kind == ReactionKind.Throat);
            ReactionBody.SetHands(r, PoseRules.HandsTo(kind), ModOptions.Fill(zoneSettings));
            ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | " + kind + " start" + cause
                + (before != ReactionKind.None ? " (was " + before + ")" : string.Empty)
                + " | " + F(r.State.Until - now) + " s | weapons " + (drops ? "dropped" : "kept")
                + (kind == ReactionKind.Down ? " | fall " + ModOptions.FallStyle : string.Empty));
        }

        /// <summary>
        /// One sound: an own file or a game clip (<see cref="SoundBank"/>), else the voice's own line. The first choke
        /// also gets one of the game's punches (<see cref="GameSounds.PunchDue"/>).
        /// </summary>
        private void Voice(Reaction r, ReactionKind kind, bool repeat)
        {
            var slot = OwnSounds.SlotFor(kind, repeat);
            var source = Sounds != null ? Sounds.Play(r, slot, ModOptions.PlayOwnSounds, ModOptions.ThroatSound) : SoundSource.Dialog;
            if (source == SoundSource.Dialog)
            {
                ReactionBody.Speak(r, repeat ? PoseRules.RepeatSound(kind) : PoseRules.StartSound(kind));
            }
            if (Sounds != null && GameSounds.PunchDue(kind, repeat, ModOptions.PunchSound))
            {
                Sounds.PlayPunch(r, GameSounds.HeavyPunch(r.HitSpeed, ModOptions.HeavyPunchSpeed));
            }
        }

        /// <summary>Normal end: everything back, the brain after the stun clip's end (as the game's stun ends).</summary>
        private void End(Reaction r, ReactionKind kind, string why)
        {
            var creature = r.Creature;
            ReactionBody.ClearHands(r);
            ReactionBody.SetHeight(r, 1f);
            ReactionBody.SetInjured(r, false);
            ReactionBody.ReleaseHold(r, this);
            if (r.PlayedClip)
            {
                creature.StopAnimation();
                r.PlayedClip = false;
                var generation = r.Generation;
                var endClip = creature.GetOverrideClip(Creature.clipIndex.dynamicEndClip);
                var delay = endClip != null ? endClip.length * 0.75f : 0f;
                creature.DelayedAction(delay, () =>
                {
                    if (r.Generation == generation && !r.State.IsActive && !creature.isKilled)
                    {
                        ReactionBody.WakeNow(r);
                    }
                });
            }
            else
            {
                ReactionBody.WakeNow(r);
            }
            ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | " + kind + " end (" + why + ")");
        }

        /// <summary>
        /// Death, despawn or unload: undo at once, no clip end. The brain is only woken for a living NPC (unload):
        /// on death and despawn the game's Brain.Stop already clears the unconscious flag and stops the brain, and
        /// waking it would start the modules of a dead body again (a pooled NPC gets a fresh brain on reuse).
        /// </summary>
        private void Undo(Reaction r, string why, bool wake)
        {
            var creature = r.Creature;
            if (creature != null)
            {
                ReactionBody.ClearHands(r);
                if (r.HeightSet)
                {
                    ReactionBody.SetHeight(r, 1f);
                }
                ReactionBody.SetInjured(r, false);
                ReactionBody.ReleaseHold(r, this);
                if (wake && !creature.isKilled)
                {
                    ReactionBody.WakeNow(r);
                }
                r.BrainOff = false;
                if (r.State.IsActive)
                {
                    ModLog.Verbose("REACTION | " + GameAdapters.Label(creature) + " | " + r.State.Kind + " stopped (" + why + ")");
                }
            }
            SoundBank.Release(r);
            r.PlayedClip = false;
            r.Generation++;
            r.State = ReactionState.Idle;
        }

        public void OnKilled(Creature creature)
        {
            Forget(creature, "died");
        }

        public void OnDespawn(Creature creature)
        {
            // Pooled NPCs come back as new spawns: nothing of a reaction may stay on them.
            Forget(creature, "despawned");
        }

        private void Forget(Creature creature, string why)
        {
            if (creature != null && reactions.TryGetValue(creature.GetInstanceID(), out var r))
            {
                Undo(r, why, wake: false);
                reactions.Remove(creature.GetInstanceID());
            }
        }

        public void ReleaseAll()
        {
            foreach (var r in reactions.Values)
            {
                Undo(r, "mod unloaded", wake: true);
            }
            reactions.Clear();
        }

        private static string F(float value) => value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        private static string F2(float value) => value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        private static string Percent(float chance) => (chance * 100f).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " %";
    }
}
