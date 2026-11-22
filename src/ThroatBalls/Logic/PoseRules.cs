using System;

namespace ThroatBalls.Logic
{
    /// <summary>How the combo brings the NPC down. Keep <see cref="CurlUp"/> first (enum default).</summary>
    public enum FallStyle
    {
        /// <summary>Animated: it sinks to the ground in the game's injured lying pose, hands kept at the groin.</summary>
        CurlUp = 0,

        /// <summary>Physics: it drops as a ragdoll (the game's own knock-down) and gets up by itself after the time.</summary>
        Ragdoll = 1,
    }

    /// <summary>When the NPC lets its weapons fall. Keep <see cref="WhenTheyGoDown"/> first (enum default).</summary>
    public enum DropWeapons
    {
        WhenTheyGoDown = 0,
        Always = 1,
        Never = 2,
    }

    /// <summary>Where the hands go.</summary>
    public enum BodyTarget
    {
        None,
        Throat,
        Groin,
    }

    /// <summary>Tuning values, all of them Mod Menu settings. Heights are the game's animator height (1 = standing).</summary>
    public sealed class PoseSettings
    {
        public float GroinCrouchHeight;
        public float LyingHeight;
        public float FallTime;
        public float GetUpTime;
        public FallStyle FallStyle;
        public DropWeapons DropWeapons;
    }

    /// <summary>The body over the course of a reaction: height, the moment of the fall, hands, weapons, voice.</summary>
    public static class PoseRules
    {
        /// <summary>Seconds to bend over after the hit (a reflex, not a setting).</summary>
        public const float BendTime = 0.3f;

        /// <summary>
        /// The animator height at <paramref name="elapsed"/> seconds into a reaction with
        /// <paramref name="remaining"/> seconds left: down by the bend and the fall, up over the get-up time at the end.
        /// </summary>
        public static float BodyHeight(ReactionKind kind, float elapsed, float remaining, PoseSettings settings)
        {
            if (kind != ReactionKind.Groin && kind != ReactionKind.Down)
            {
                return 1f;
            }
            var crouch = settings.GroinCrouchHeight;
            var down = Lerp(1f, crouch, elapsed / BendTime);
            var low = crouch;
            if (kind == ReactionKind.Down)
            {
                if (settings.FallStyle == FallStyle.Ragdoll)
                {
                    // The ragdoll takes over at the end of the fall; the game stands it up at normal height.
                    return IsDown(kind, elapsed, settings) ? 1f : down;
                }
                low = settings.LyingHeight;
                if (elapsed > BendTime)
                {
                    down = Lerp(crouch, low, settings.FallTime > 0f ? (elapsed - BendTime) / settings.FallTime : 1f);
                }
            }
            var up = settings.GetUpTime > 0f ? Lerp(1f, low, remaining / settings.GetUpTime) : (remaining > 0f ? low : 1f);
            return Math.Max(down, up);
        }

        /// <summary>The combo's NPC is on the ground from the end of the bend and the fall.</summary>
        public static bool IsDown(ReactionKind kind, float elapsed, PoseSettings settings)
        {
            return kind == ReactionKind.Down && elapsed >= BendTime + settings.FallTime - 1e-4f;
        }

        public static bool DropsWeapons(ReactionKind kind, DropWeapons drop)
        {
            if (kind == ReactionKind.None)
            {
                return false;
            }
            switch (drop)
            {
                case DropWeapons.Always:
                    return true;
                case DropWeapons.Never:
                    return false;
                default:
                    return kind == ReactionKind.Down;
            }
        }

        public static BodyTarget HandsTo(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Throat:
                    return BodyTarget.Throat;
                case ReactionKind.Groin:
                case ReactionKind.Down:
                    return BodyTarget.Groin;
                default:
                    return BodyTarget.None;
            }
        }

        /// <summary>
        /// Where a hand rests, from the hurt spot's center, in the NPC's own directions: X to its right, Y up,
        /// Z forward. Both hands side by side, just in front of the spot so they lie on it rather than in it.
        /// </summary>
        public static Vec3 HandOffset(BodyTarget target, bool leftHand)
        {
            var side = leftHand ? -1f : 1f;
            switch (target)
            {
                case BodyTarget.Throat:
                    return new Vec3(side * 0.03f, 0f, 0.04f);
                case BodyTarget.Groin:
                    return new Vec3(side * 0.05f, 0f, 0.05f);
                default:
                    return Vec3.Zero;
            }
        }

        /// <summary>The voice line at the start (the game's dialog ids, present in every human male voice).</summary>
        public static string StartSound(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Throat:
                    return "grappled";
                case ReactionKind.Groin:
                case ReactionKind.Down:
                    return "hit";
                default:
                    return null;
            }
        }

        /// <summary>The voice line repeated while it lasts.</summary>
        public static string RepeatSound(ReactionKind kind)
        {
            switch (kind)
            {
                case ReactionKind.Throat:
                    return "grappled";
                case ReactionKind.Groin:
                case ReactionKind.Down:
                    return "heavybreath";
                default:
                    return null;
            }
        }

        public static bool GroanDue(float lastGroanAt, float now, float interval)
        {
            return interval > 0f && now - lastGroanAt >= interval;
        }

        private static float Lerp(float from, float to, float t)
        {
            var clamped = t < 0f ? 0f : (t > 1f ? 1f : t);
            return from + (to - from) * clamped;
        }
    }
}
