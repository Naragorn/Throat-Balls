using System.Collections.Generic;

namespace ThroatBalls.Logic
{
    /// <summary>
    /// Every numeric Mod Menu option of the mod. No magic numbers elsewhere.
    /// The zone offsets are starting values, not measured on the model: Show Zones draws them for tuning.
    /// </summary>
    public static class OptionSpecs
    {
        // ---- Hits ----------------------------------------------------------------

        public static readonly FloatOptionSpec MinHitSpeed =
            new FloatOptionSpec("Min Hit Speed", 0f, 6f, 0.5f, 1.5f, OptionUnit.MetersPerSecond);

        public static readonly FloatOptionSpec MaxHitAngle =
            new FloatOptionSpec("Max Hit Angle", 30f, 180f, 5f, 100f, OptionUnit.Degrees);

        public static readonly FloatOptionSpec ThroatZoneSize =
            new FloatOptionSpec("Throat Zone Size", 0.03f, 0.15f, 0.01f, 0.07f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec ThroatForwardOffset =
            new FloatOptionSpec("Throat Forward Offset", -0.05f, 0.15f, 0.01f, 0.05f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec ThroatHeightOffset =
            new FloatOptionSpec("Throat Height Offset", -0.1f, 0.15f, 0.01f, 0.05f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec GroinZoneSize =
            new FloatOptionSpec("Groin Zone Size", 0.03f, 0.2f, 0.01f, 0.09f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec GroinForwardOffset =
            new FloatOptionSpec("Groin Forward Offset", -0.05f, 0.2f, 0.01f, 0.08f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec GroinHeightOffset =
            new FloatOptionSpec("Groin Height Offset", -0.25f, 0.1f, 0.01f, -0.08f, OptionUnit.Centimeters);

        // ---- Reactions -------------------------------------------------------------

        public static readonly FloatOptionSpec ThroatDuration =
            new FloatOptionSpec("Throat Duration", 1f, 15f, 0.5f, 3f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec GroinDuration =
            new FloatOptionSpec("Groin Duration", 1f, 15f, 0.5f, 4f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec GroinCrouchHeight =
            new FloatOptionSpec("Groin Crouch Height", 0.3f, 1f, 0.05f, 0.6f, OptionUnit.Multiplier);

        public static readonly FloatOptionSpec GroanInterval =
            new FloatOptionSpec("Groan Interval", 0f, 10f, 0.5f, 3f, OptionUnit.Seconds);

        // ---- Chances (Nadi, 2026-11-22: groin alone 50 %, both hits 100 %, every chance adjustable) ----

        public static readonly FloatOptionSpec ThroatReactionChance =
            new FloatOptionSpec("Throat Reaction Chance", 0f, 1f, 0.05f, 1f, OptionUnit.Percent);

        public static readonly FloatOptionSpec GroinReactionChance =
            new FloatOptionSpec("Groin Reaction Chance", 0f, 1f, 0.05f, 1f, OptionUnit.Percent);

        public static readonly FloatOptionSpec GroinKnockdownChance =
            new FloatOptionSpec("Groin Knockdown Chance", 0f, 1f, 0.05f, 0.5f, OptionUnit.Percent);

        public static readonly FloatOptionSpec ComboKnockdownChance =
            new FloatOptionSpec("Both Hits Knockdown Chance", 0f, 1f, 0.05f, 1f, OptionUnit.Percent);

        // ---- Going down ------------------------------------------------------------

        public static readonly FloatOptionSpec ComboWindow =
            new FloatOptionSpec("Time Between Hits", 1f, 10f, 0.5f, 4f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec DownDuration =
            new FloatOptionSpec("Down Duration", 2f, 30f, 1f, 8f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec LyingHeight =
            new FloatOptionSpec("Lying Height", 0f, 0.6f, 0.05f, 0.2f, OptionUnit.Multiplier);

        public static readonly FloatOptionSpec FallTime =
            new FloatOptionSpec("Fall Time", 0.2f, 3f, 0.1f, 1f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec GetUpTime =
            new FloatOptionSpec("Get Up Time", 0.5f, 4f, 0.25f, 1.5f, OptionUnit.Seconds);

        public static readonly IReadOnlyList<FloatOptionSpec> All = new[]
        {
            MinHitSpeed,
            MaxHitAngle,
            ThroatZoneSize,
            ThroatForwardOffset,
            ThroatHeightOffset,
            GroinZoneSize,
            GroinForwardOffset,
            GroinHeightOffset,
            ThroatDuration,
            GroinDuration,
            GroinCrouchHeight,
            GroanInterval,
            ThroatReactionChance,
            GroinReactionChance,
            GroinKnockdownChance,
            ComboKnockdownChance,
            ComboWindow,
            DownDuration,
            LyingHeight,
            FallTime,
            GetUpTime,
        };
    }
}
