using System;
using System.Collections.Generic;

namespace ThroatBalls.Logic
{
    /// <summary>
    /// Throat Sound option: one of the four throat clips, or a different one each time. Keep <see cref="Random"/>
    /// first: the game's mod menu cannot match an enum default and always falls back to the first entry.
    /// </summary>
    public enum ThroatSound
    {
        Random = 0,
        Sound1 = 1,
        Sound2 = 2,
        Sound3 = 3,
        Sound4 = 4,
    }

    /// <summary>Where a reaction's sound comes from.</summary>
    public enum SoundSource
    {
        /// <summary>The NPC's own voice line (<see cref="PoseRules.StartSound"/>, <see cref="PoseRules.RepeatSound"/>).</summary>
        Dialog,

        /// <summary>A file from the mod's Sounds folder (<see cref="OwnSounds"/>).</summary>
        Own,

        /// <summary>One of the game's clips in <see cref="GameSounds"/>.</summary>
        Game,
    }

    /// <summary>One clip of the game: the audio group's address and the clip's name in it.</summary>
    public sealed class GameClip
    {
        public GameClip(string address, string clip)
        {
            Address = address;
            Clip = clip;
        }

        public string Address { get; }
        public string Clip { get; }
    }

    /// <summary>
    /// The game's own clips the mod plays (Nadi, 2026-11-24, picked by ear from a list of the game's male voice
    /// clips: "rand(BreathlessDeath1-4)) oder geben dem player einen setting auf eins der 4 von BreathlessDeath.
    /// balls: rand von CottageMale2 7, Edwyn 10"). The numbers are the clips' places in their group by name.
    /// The throat clips are the same for every man; the groin clips come from two voices, so every man says them.
    /// </summary>
    public static class GameSounds
    {
        private const string BreathlessDeath = "Bas.AudioGroup.Voice.Male.BreathlessDeath";

        /// <summary>The game's breathless death (a man choking to death), all four of the group.</summary>
        public static readonly IReadOnlyList<GameClip> Throat = new[]
        {
            new GameClip(BreathlessDeath, "Male_Shout-of-Pain_047"),
            new GameClip(BreathlessDeath, "Male_Shout-of-Pain_054"),
            new GameClip(BreathlessDeath, "Male_Shout-of-Pain_072"),
            new GameClip(BreathlessDeath, "Male_Shout-of-Pain_087"),
        };

        /// <summary>Two hit grunts: the 7th of the cottage man 2's voice, the 10th of Edwyn's.</summary>
        public static readonly IReadOnlyList<GameClip> Groin = new[]
        {
            new GameClip("Bas.AudioGroup.Voice.MaleCottage2.Hit", "male2_get_hit_07"),
            new GameClip("Bas.AudioGroup.Voice.MaleEdwyn.Hit", "Male_Hurt_10_Edwyn"),
        };

        private const string PunchLight = "Bas.AudioGroup.Collision.Punch";
        private const string PunchHeavy = "Bas.AudioGroup.Collision.PunchHeavy";

        /// <summary>
        /// The game's own hits of fists and kicks (Nadi, 2026-11-25: "die zusammen mit rand(der h2h punch sounds aus dem
        /// game) zusammenlegen"): the two audio groups the game's HitRagdoll effect plays for the Punch, Kick and
        /// KickTracked damagers (catalog 2026-11-25), light then heavy. One of them comes with the first choke.
        /// </summary>
        public static readonly IReadOnlyList<GameClip> Punch = new[]
        {
            new GameClip(PunchLight, "SFX_punch_intens1_01"),
            new GameClip(PunchLight, "SFX_punch_intens1_02"),
            new GameClip(PunchLight, "SFX_punch_intens1_03"),
            new GameClip(PunchLight, "SFX_punch_intens1_04"),
            new GameClip(PunchHeavy, "SFX_punch_intens2_01"),
            new GameClip(PunchHeavy, "SFX_punch_intens2_02"),
            new GameClip(PunchHeavy, "SFX_punch_intens2_03"),
            new GameClip(PunchHeavy, "SFX_punch_intens2_04"),
        };

        /// <summary>The game's own punch is never louder than this: HitRagdoll's volume curves for both groups top out at 0.6.</summary>
        public const float PunchVolume = 0.6f;

        private static readonly IReadOnlyList<GameClip> NoClips = new GameClip[0];

        /// <summary>
        /// Light or heavy (Nadi, 2026-11-25: "leicht abspielen wenn der schlag auch leichter ist und schwer wenn er
        /// schwerer ist"): a hit from <paramref name="heavyFrom"/> m/s on gets a heavy punch, a slower one a light one.
        /// </summary>
        public static bool HeavyPunch(float impactSpeed, float heavyFrom)
        {
            return impactSpeed >= heavyFrom;
        }

        /// <summary>
        /// Which of the <see cref="Punch"/> clips may play: the loaded ones of the hit's weight. When none of that weight
        /// loaded, the loaded ones of the other weight, so a hit never goes without a punch because of one group.
        /// </summary>
        public static bool[] PunchChoices(bool[] loaded, bool heavy)
        {
            var choices = new bool[Punch.Count];
            if (loaded == null)
            {
                return choices;
            }
            var any = false;
            for (var i = 0; i < choices.Length && i < loaded.Length; i++)
            {
                choices[i] = loaded[i] && IsHeavy(Punch[i]) == heavy;
                any |= choices[i];
            }
            if (!any)
            {
                for (var i = 0; i < choices.Length && i < loaded.Length; i++)
                {
                    choices[i] = loaded[i];
                }
            }
            return choices;
        }

        private static bool IsHeavy(GameClip clip)
        {
            return clip.Address == PunchHeavy;
        }

        /// <summary>
        /// A punch comes with the first choke of a throat reaction while Punch Sound is on. The repeats have none: he
        /// chokes again, nobody hit him. Groin and down have none either.
        /// </summary>
        public static bool PunchDue(ReactionKind kind, bool repeat, bool punchOn)
        {
            return punchOn && !repeat && kind == ReactionKind.Throat;
        }

        public static IReadOnlyList<GameClip> For(SoundSlot slot)
        {
            switch (slot)
            {
                case SoundSlot.Throat:
                    return Throat;
                case SoundSlot.Groin:
                    return Groin;
                default:
                    return NoClips;
            }
        }

        /// <summary>Every audio group the clips are in, each once, in the order of <see cref="Throat"/>, <see cref="Groin"/>, <see cref="Punch"/>.</summary>
        public static IReadOnlyList<string> Addresses()
        {
            var addresses = new List<string>();
            foreach (var list in new[] { Throat, Groin, Punch })
            {
                foreach (var clip in list)
                {
                    if (!addresses.Contains(clip.Address))
                    {
                        addresses.Add(clip.Address);
                    }
                }
            }
            return addresses;
        }

        /// <summary>The throat clip the option names, -1 for Random.</summary>
        public static int FixedIndex(ThroatSound choice)
        {
            return choice == ThroatSound.Random ? -1 : (int)choice - 1;
        }

        /// <summary>
        /// Index of the clip to play. A fixed choice plays that clip whenever it is there. Otherwise any of the clips
        /// that are there with <paramref name="roll"/> in [0, 1], never the one played last (<paramref name="last"/>,
        /// -1 for none) when two or more are there. -1 when none is there.
        /// </summary>
        public static int Pick(bool[] present, int fixedIndex, int last, double roll)
        {
            if (present == null)
            {
                return -1;
            }
            if (fixedIndex >= 0 && fixedIndex < present.Length && present[fixedIndex])
            {
                return fixedIndex;
            }
            var count = 0;
            for (var i = 0; i < present.Length; i++)
            {
                if (present[i])
                {
                    count++;
                }
            }
            if (count == 0)
            {
                return -1;
            }
            var skip = count > 1 && last >= 0 && last < present.Length && present[last] ? last : -1;
            var choices = skip >= 0 ? count - 1 : count;
            var n = Math.Min((int)(roll * choices), choices - 1);
            if (n < 0)
            {
                n = 0;
            }
            for (var i = 0; i < present.Length; i++)
            {
                if (!present[i] || i == skip)
                {
                    continue;
                }
                if (n == 0)
                {
                    return i;
                }
                n--;
            }
            return -1;
        }

        /// <summary>
        /// Own files first (Own Sounds on and files there), then the game clips, then the NPC's own voice line. A sound
        /// without a slot (the groin repeats: heavy breathing) is always the voice.
        /// </summary>
        public static SoundSource SourceFor(SoundSlot slot, bool ownOn, int ownCount, int gameCount)
        {
            if (slot == SoundSlot.None)
            {
                return SoundSource.Dialog;
            }
            if (ownOn && ownCount > 0)
            {
                return SoundSource.Own;
            }
            return gameCount > 0 ? SoundSource.Game : SoundSource.Dialog;
        }
    }
}
