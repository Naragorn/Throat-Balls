using ThroatBalls.Logic;
using ThunderRoad;

namespace ThroatBalls
{
    /// <summary>
    /// Every setting of the mod, shown in the game's Mod Menu.
    ///
    /// How the game handles these (read in ThunderRoad.dll 1.3.2, same as Heel of Achilles):
    /// - ModManager scans all static members with [ModOption] in this assembly.
    /// - The initial field value is the default; float value lists come from <see cref="OptionSpecs"/>
    ///   so the default matches one entry exactly (object.Equals on the boxed float).
    /// - Enum defaults are never matched, the menu falls back to the first entry: every enum lists its default first.
    /// - A change writes the static field at once (live); the chosen index is saved per mod folder.
    /// - Option names must be unique per mod (saved by name), so no plain "Enabled".
    /// - The mod page has its own reset button, so there is no reset option here.
    /// </summary>
    public static class ModOptions
    {
        private const string MainCategory = "Throat Balls";
        private const string ThroatCategory = "Throat";
        private const string GroinCategory = "Groin";
        private const string ComboCategory = "Going Down";
        private const string DebugCategory = "Debug";

        // ---- Throat Balls ------------------------------------------------------------

        [ModOption("Throat Balls Enabled", "Turns the mod on or off. Off: anyone still reacting recovers at once.")]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(0)]
        public static bool ModEnabled = true;

        [ModOption("Men Only", "Only men react. Off: women react too.")]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(1)]
        public static bool MenOnly = true;

        [ModOption("Metal Armor Protects", "Metal armor on the throat or on the hips and legs stops the hit there. Leather and cloth do not.")]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(2)]
        public static bool MetalArmorProtects = true;

        [ModOption("Allowed Hits", "What can land the blow. Fists, Kicks, Blunt Weapons: your hands and feet, maces, pommels. Fists And Kicks Only: no weapons. Any Hit: blades too.", nameof(AllowedHitsValues))]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(3)]
        public static AllowedHits AllowedHits = AllowedHits.BluntOnly;

        [ModOption("Min Hit Speed", "How hard you must hit. 0 = a touch is enough.", nameof(MinHitSpeedValues))]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float MinHitSpeed = OptionSpecs.MinHitSpeed.Default;

        [ModOption("Max Hit Angle", "How far to the side of him you may stand. 90 = from his side, 180 = from anywhere, even behind.", nameof(MaxHitAngleValues))]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        public static float MaxHitAngle = OptionSpecs.MaxHitAngle.Default;

        [ModOption("Drop Weapons", "When he lets his weapons fall. When They Go Down: only when he goes to the ground.")]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(6)]
        public static DropWeapons DropWeapons = DropWeapons.WhenTheyGoDown;

        [ModOption("Sounds", "He gags, groans and gasps.")]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(7)]
        public static bool Sounds = true;

        [ModOption("Own Sounds", "Plays the sound files in the mod's Sounds folder: throat*.wav when he chokes, groin*.wav for the ow. Off, or no files: the game's own voice.")]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(8)]
        public static bool PlayOwnSounds = true;

        [ModOption("Groan Interval", "How often he gags or groans while it lasts. 0 = only once, when hit.", nameof(GroanIntervalValues))]
        [ModOptionCategory(MainCategory, 0)]
        [ModOptionOrder(9)]
        [ModOptionSlider]
        public static float GroanInterval = OptionSpecs.GroanInterval.Default;

        // ---- Throat ---------------------------------------------------------------------

        [ModOption("Throat Hits", "A hit on the Adam's apple: he grabs his throat and gags.")]
        [ModOptionCategory(ThroatCategory, 1)]
        [ModOptionOrder(0)]
        public static bool ThroatEnabled = true;

        [ModOption("Throat Reaction Chance", "Chance that a throat hit gets to him. A missed hit still counts as the first of both hits.", nameof(ThroatReactionChanceValues))]
        [ModOptionCategory(ThroatCategory, 1)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float ThroatReactionChance = OptionSpecs.ThroatReactionChance.Default;

        [ModOption("Throat Duration", "How long he holds his throat.", nameof(ThroatDurationValues))]
        [ModOptionCategory(ThroatCategory, 1)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        public static float ThroatDuration = OptionSpecs.ThroatDuration.Default;

        [ModOption("Throat Zone Size", "How big the spot is that counts as the throat.", nameof(ThroatZoneSizeValues))]
        [ModOptionCategory(ThroatCategory, 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float ThroatZoneSize = OptionSpecs.ThroatZoneSize.Default;

        [ModOption("Throat Forward Offset", "Moves the throat spot forward or back. Turn on Show Zones to see it.", nameof(ThroatForwardOffsetValues))]
        [ModOptionCategory(ThroatCategory, 1)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float ThroatForwardOffset = OptionSpecs.ThroatForwardOffset.Default;

        [ModOption("Throat Height Offset", "Moves the throat spot up or down. Turn on Show Zones to see it.", nameof(ThroatHeightOffsetValues))]
        [ModOptionCategory(ThroatCategory, 1)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        public static float ThroatHeightOffset = OptionSpecs.ThroatHeightOffset.Default;

        // ---- Groin ----------------------------------------------------------------------

        [ModOption("Groin Hits", "A hit between the legs: he doubles over and holds himself.")]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(0)]
        public static bool GroinEnabled = true;

        [ModOption("Groin Reaction Chance", "Chance that a groin hit gets to him at all. A missed hit still counts as the first of both hits.", nameof(GroinReactionChanceValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float GroinReactionChance = OptionSpecs.GroinReactionChance.Default;

        [ModOption("Groin Knockdown Chance", "Chance that a groin hit alone takes him down, as both hits do. Otherwise he only doubles over.", nameof(GroinKnockdownChanceValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        public static float GroinKnockdownChance = OptionSpecs.GroinKnockdownChance.Default;

        [ModOption("Groin Duration", "How long he stays doubled over.", nameof(GroinDurationValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float GroinDuration = OptionSpecs.GroinDuration.Default;

        [ModOption("Groin Crouch Height", "How far he bends down. 1 = he stays upright.", nameof(GroinCrouchHeightValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float GroinCrouchHeight = OptionSpecs.GroinCrouchHeight.Default;

        [ModOption("Groin Zone Size", "How big the spot is that counts as the groin.", nameof(GroinZoneSizeValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        public static float GroinZoneSize = OptionSpecs.GroinZoneSize.Default;

        [ModOption("Groin Forward Offset", "Moves the groin spot forward or back. Turn on Show Zones to see it.", nameof(GroinForwardOffsetValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        public static float GroinForwardOffset = OptionSpecs.GroinForwardOffset.Default;

        [ModOption("Groin Height Offset", "Moves the groin spot up or down. Turn on Show Zones to see it.", nameof(GroinHeightOffsetValues))]
        [ModOptionCategory(GroinCategory, 2)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        public static float GroinHeightOffset = OptionSpecs.GroinHeightOffset.Default;

        // ---- Going Down: both hits in a row, or a groin hit alone by chance --------------

        [ModOption("Both Hits Take Him Down", "Throat and groin one after the other: he grabs his groin and goes down.")]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(0)]
        public static bool ComboEnabled = true;

        [ModOption("Both Hits Knockdown Chance", "Chance that throat and groin in a row take him down. Otherwise the second hit gets its own reaction.", nameof(ComboKnockdownChanceValues))]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float ComboKnockdownChance = OptionSpecs.ComboKnockdownChance.Default;

        [ModOption("Hit Order", "Which hit must come first. Either Order: both ways work.")]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(2)]
        public static ComboOrder ComboOrder = ComboOrder.EitherOrder;

        [ModOption("Time Between Hits", "How soon the second hit must follow the first.", nameof(ComboWindowValues))]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float ComboWindow = OptionSpecs.ComboWindow.Default;

        [ModOption("Down Duration", "How long he stays on the ground.", nameof(DownDurationValues))]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float DownDuration = OptionSpecs.DownDuration.Default;

        [ModOption("Fall Style", "Curl Up: he sinks to the ground holding his groin. Ragdoll: he drops limp and gets up on his own.")]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(5)]
        public static FallStyle FallStyle = FallStyle.CurlUp;

        [ModOption("Lying Height", "Curl Up: how low he lies. 0 = flat on the ground.", nameof(LyingHeightValues))]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        public static float LyingHeight = OptionSpecs.LyingHeight.Default;

        [ModOption("Fall Time", "How long it takes him to go down.", nameof(FallTimeValues))]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        public static float FallTime = OptionSpecs.FallTime.Default;

        [ModOption("Get Up Time", "How long it takes him to get back up at the end.", nameof(GetUpTimeValues))]
        [ModOptionCategory(ComboCategory, 3)]
        [ModOptionOrder(8)]
        [ModOptionSlider]
        public static float GetUpTime = OptionSpecs.GetUpTime.Default;

        // ---- Debug ----------------------------------------------------------------------

        [ModOption("Debug Logging", "Writes every hit and reaction to the game's log file, for bug reports.")]
        [ModOptionCategory(DebugCategory, 9)]
        [ModOptionOrder(0)]
        public static bool DebugLogging;

        [ModOption("Show Zones", "Shows the throat and groin spots on every enemy as colored balls, to adjust them.")]
        [ModOptionCategory(DebugCategory, 9)]
        [ModOptionOrder(1)]
        public static bool ShowZones;

        // The Fill methods copy the menu values into a kept settings object: read every frame, no allocation.

        public static LowBlowSettings Fill(LowBlowSettings s)
        {
            s.ThroatEnabled = ThroatEnabled;
            s.GroinEnabled = GroinEnabled;
            s.MenOnly = MenOnly;
            s.MetalArmorProtects = MetalArmorProtects;
            s.AllowedHits = AllowedHits;
            s.MinHitSpeed = MinHitSpeed;
            s.ThroatZoneSize = ThroatZoneSize;
            s.ThroatForwardOffset = ThroatForwardOffset;
            s.ThroatHeightOffset = ThroatHeightOffset;
            s.GroinZoneSize = GroinZoneSize;
            s.GroinForwardOffset = GroinForwardOffset;
            s.GroinHeightOffset = GroinHeightOffset;
            s.MaxHitAngle = MaxHitAngle;
            return s;
        }

        public static ReactionSettings Fill(ReactionSettings s)
        {
            s.ComboEnabled = ComboEnabled;
            s.ComboOrder = ComboOrder;
            s.ComboWindow = ComboWindow;
            s.ThroatDuration = ThroatDuration;
            s.GroinDuration = GroinDuration;
            s.DownDuration = DownDuration;
            s.ThroatReactionChance = ThroatReactionChance;
            s.GroinReactionChance = GroinReactionChance;
            s.GroinKnockdownChance = GroinKnockdownChance;
            s.ComboKnockdownChance = ComboKnockdownChance;
            return s;
        }

        public static PoseSettings Fill(PoseSettings s)
        {
            s.GroinCrouchHeight = GroinCrouchHeight;
            s.LyingHeight = LyingHeight;
            s.FallTime = FallTime;
            s.GetUpTime = GetUpTime;
            s.FallStyle = FallStyle;
            s.DropWeapons = DropWeapons;
            return s;
        }

        public static ModOptionParameter[] AllowedHitsValues() => new ModOptionParameter[]
        {
            new ModOptionInt(OptionTitles.Of(AllowedHits.BluntOnly), (int)AllowedHits.BluntOnly),
            new ModOptionInt(OptionTitles.Of(AllowedHits.FistsAndKicksOnly), (int)AllowedHits.FistsAndKicksOnly),
            new ModOptionInt(OptionTitles.Of(AllowedHits.AnyHit), (int)AllowedHits.AnyHit),
        };

        public static ModOptionParameter[] MinHitSpeedValues() => ToParameters(OptionSpecs.MinHitSpeed);

        public static ModOptionParameter[] MaxHitAngleValues() => ToParameters(OptionSpecs.MaxHitAngle);

        public static ModOptionParameter[] GroanIntervalValues() => ToParameters(OptionSpecs.GroanInterval);

        public static ModOptionParameter[] ThroatDurationValues() => ToParameters(OptionSpecs.ThroatDuration);

        public static ModOptionParameter[] ThroatZoneSizeValues() => ToParameters(OptionSpecs.ThroatZoneSize);

        public static ModOptionParameter[] ThroatForwardOffsetValues() => ToParameters(OptionSpecs.ThroatForwardOffset);

        public static ModOptionParameter[] ThroatHeightOffsetValues() => ToParameters(OptionSpecs.ThroatHeightOffset);

        public static ModOptionParameter[] GroinDurationValues() => ToParameters(OptionSpecs.GroinDuration);

        public static ModOptionParameter[] GroinCrouchHeightValues() => ToParameters(OptionSpecs.GroinCrouchHeight);

        public static ModOptionParameter[] GroinZoneSizeValues() => ToParameters(OptionSpecs.GroinZoneSize);

        public static ModOptionParameter[] GroinForwardOffsetValues() => ToParameters(OptionSpecs.GroinForwardOffset);

        public static ModOptionParameter[] GroinHeightOffsetValues() => ToParameters(OptionSpecs.GroinHeightOffset);

        public static ModOptionParameter[] ComboWindowValues() => ToParameters(OptionSpecs.ComboWindow);

        public static ModOptionParameter[] ThroatReactionChanceValues() => ToParameters(OptionSpecs.ThroatReactionChance);

        public static ModOptionParameter[] GroinReactionChanceValues() => ToParameters(OptionSpecs.GroinReactionChance);

        public static ModOptionParameter[] GroinKnockdownChanceValues() => ToParameters(OptionSpecs.GroinKnockdownChance);

        public static ModOptionParameter[] ComboKnockdownChanceValues() => ToParameters(OptionSpecs.ComboKnockdownChance);

        public static ModOptionParameter[] DownDurationValues() => ToParameters(OptionSpecs.DownDuration);

        public static ModOptionParameter[] LyingHeightValues() => ToParameters(OptionSpecs.LyingHeight);

        public static ModOptionParameter[] FallTimeValues() => ToParameters(OptionSpecs.FallTime);

        public static ModOptionParameter[] GetUpTimeValues() => ToParameters(OptionSpecs.GetUpTime);

        private static ModOptionParameter[] ToParameters(FloatOptionSpec spec)
        {
            var values = spec.Values();
            var parameters = new ModOptionParameter[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                parameters[i] = new ModOptionFloat(spec.Label(values[i]), values[i]);
            }
            return parameters;
        }
    }
}
