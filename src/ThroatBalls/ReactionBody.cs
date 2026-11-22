using ThroatBalls.Logic;
using ThunderRoad;
using ThunderRoad.Skill;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>
    /// What a reaction does to the NPC's body, each step through the game's own calls (ThunderRoad 1.3.2):
    /// - out of the fight: SkillDiscombobulate.BrainToggle, the game's stun (brain modules stop, AI tree stops);
    /// - throat: the game's stun clip "HumanStun" (start, loop, end), as the stun skill plays it;
    /// - hands to the spot: the NPC's IK hand targets (IkController.SetHandAnchor), as the game's bow and defense AI place hands;
    /// - bent over and down: the animator's Height and Injured floats, as the game's burning NPC drops;
    /// - ragdoll fall: Ragdoll.SetState(Destabilized) plus the brain's no-stand-up hold;
    /// - voice: BrainModuleSpeak.Play with the voice's own dialog ids.
    /// </summary>
    internal static class ReactionBody
    {
        public const string StunAnimationId = "HumanStun";

        public static void KnockOut(Reaction r)
        {
            var brain = r.Creature.brain;
            if (r.BrainOff || brain == null || brain.instance == null || !brain.instance.isActive)
            {
                return;
            }
            r.TreeWasRunning = brain.instance.updateTree;
            SkillDiscombobulate.BrainToggle(r.Creature, conscious: false);
            r.BrainOff = true;
        }

        /// <summary>Brain back on now (despawn, death: the game's stun wakes a stunned NPC that died, too), with its tree as it was.</summary>
        public static void WakeNow(Reaction r)
        {
            if (!r.BrainOff)
            {
                return;
            }
            r.BrainOff = false;
            if (r.Creature != null && r.Creature.brain != null && r.Creature.brain.instance != null)
            {
                SkillDiscombobulate.BrainToggle(r.Creature, conscious: true);
                // BrainToggle sets updateTree = true; give back what the brain had (HumanDummy runs no tree).
                r.Creature.brain.instance.updateTree = r.TreeWasRunning;
            }
        }

        /// <summary>
        /// The throat's stun clip on or off. Played after the brain toggle, which stops any running clip.
        /// Groin and combo need the clip off: a full-body clip hides the animator's Height and Injured.
        /// </summary>
        public static void SetStunClip(Reaction r, bool on)
        {
            if (on && !r.PlayedClip)
            {
                r.Creature.PlayAnimation(StunAnimationId);
                r.PlayedClip = true;
            }
            else if (!on && r.PlayedClip)
            {
                r.Creature.StopAnimation(interrupt: true);
                r.PlayedClip = false;
            }
        }

        public static void DropWeapons(Reaction r)
        {
            Drop(r.Creature.handLeft);
            Drop(r.Creature.handRight);
        }

        private static void Drop(RagdollHand hand)
        {
            if (hand != null && hand.grabbedHandle != null)
            {
                hand.TryRelease();
            }
        }

        /// <summary>Puts the free hands on the spot; a hand that still holds something stays in the animation.</summary>
        public static void SetHands(Reaction r, BodyTarget target, LowBlowSettings zones)
        {
            if (r.HandsAt == target)
            {
                return;
            }
            ClearHands(r);
            var creature = r.Creature;
            var ik = creature.ragdoll != null ? creature.ragdoll.ik : null;
            var part = target == BodyTarget.Throat ? GameAdapters.NeckOf(creature) : GameAdapters.HipsOf(creature);
            if (target == BodyTarget.None || ik == null || part == null)
            {
                if (target != BodyTarget.None)
                {
                    ModLog.Verbose("HANDS | " + GameAdapters.Label(creature) + " | no " + (ik == null ? "IK" : "body part") + ", hands stay");
                }
                return;
            }
            var forward = GameAdapters.ToVec3(creature.transform.forward);
            var center = target == BodyTarget.Throat
                ? LowBlowZones.ThroatCenter(GameAdapters.ToVec3(part.transform.position), forward, zones)
                : LowBlowZones.GroinCenter(GameAdapters.ToVec3(part.transform.position), forward, zones);
            r.LeftAnchor = PlaceHand(creature, ik, part, Side.Left, target, center);
            r.RightAnchor = PlaceHand(creature, ik, part, Side.Right, target, center);
            r.HandsAt = target;
            ModLog.Verbose("HANDS | " + GameAdapters.Label(creature) + " | to the " + target.ToString().ToLowerInvariant()
                + " | left " + (r.LeftAnchor != null ? "yes" : "holds something") + ", right " + (r.RightAnchor != null ? "yes" : "holds something"));
        }

        private static GameObject PlaceHand(Creature creature, IkController ik, RagdollPart part, Side side, BodyTarget target, Vec3 center)
        {
            var hand = creature.GetHand(side);
            if (hand == null || hand.grabbedHandle != null)
            {
                return null;
            }
            var offset = PoseRules.HandOffset(target, side == Side.Left);
            var body = creature.transform;
            var position = GameAdapters.ToVector3(center) + body.right * offset.X + body.up * offset.Y + body.forward * offset.Z;
            var anchor = new GameObject("ThroatBalls.Hand." + side);
            anchor.transform.SetPositionAndRotation(position, body.rotation);
            anchor.transform.SetParent(part.transform, worldPositionStays: true);
            ik.SetHandAnchor(side, anchor.transform);
            // Position only: the arm solver turns the wrist itself (which hand axis is the palm is not known offline).
            ik.SetHandWeight(side, 1f, 0f);
            return anchor;
        }

        public static void ClearHands(Reaction r)
        {
            var ik = r.Creature != null && r.Creature.ragdoll != null ? r.Creature.ragdoll.ik : null;
            r.LeftAnchor = Release(ik, Side.Left, r.LeftAnchor);
            r.RightAnchor = Release(ik, Side.Right, r.RightAnchor);
            r.HandsAt = BodyTarget.None;
        }

        private static GameObject Release(IkController ik, Side side, GameObject anchor)
        {
            if (anchor == null)
            {
                return null;
            }
            if (ik != null)
            {
                ik.SetHandAnchor(side, null);
            }
            Object.Destroy(anchor);
            return null;
        }

        public static void SetHeight(Reaction r, float height)
        {
            r.Creature.SetAnimatorHeightRatio(height);
            r.HeightSet = height < 1f;
        }

        public static void SetInjured(Reaction r, bool injured)
        {
            if (injured || r.InjuredSet)
            {
                r.Creature.animator.SetFloat(Brain.hashInjured, injured ? 1f : 0f);
                r.InjuredSet = injured;
            }
        }

        public static void KnockDown(Reaction r, object holder)
        {
            var creature = r.Creature;
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            creature.brain.AddNoStandUpModifier(holder);
            r.KnockedDown = true;
        }

        public static void ReleaseHold(Reaction r, object holder)
        {
            if (r.KnockedDown && r.Creature != null && r.Creature.brain != null)
            {
                r.Creature.brain.RemoveNoStandUpModifier(holder);
            }
            r.KnockedDown = false;
        }

        /// <summary>
        /// Plays one of the voice's dialogs. BrainModuleSpeak.Play refuses while the brain counts as unconscious,
        /// which the stun sets; it reads that flag once at its start, so it is lifted around the call when this mod set it.
        /// </summary>
        public static void Speak(Reaction r, string dialogId)
        {
            var brain = r.Creature.brain;
            var speak = dialogId != null && brain != null && brain.instance != null
                ? brain.instance.GetModule<BrainModuleSpeak>(autoAddModuleIfMissing: false)
                : null;
            if (speak == null)
            {
                return;
            }
            var wasUnconscious = brain.isUnconscious;
            if (wasUnconscious && r.BrainOff)
            {
                brain.isUnconscious = false;
            }
            try
            {
                speak.Play(dialogId, true);
            }
            finally
            {
                brain.isUnconscious = wasUnconscious;
            }
            ModLog.Verbose("SOUND | " + GameAdapters.Label(r.Creature) + " | " + dialogId);
        }
    }
}
