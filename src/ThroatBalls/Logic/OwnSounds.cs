using System;
using System.IO;

namespace ThroatBalls.Logic
{
    /// <summary>Which reaction a sound file of the mod's Sounds folder belongs to.</summary>
    public enum SoundSlot
    {
        None,

        /// <summary>A choke or gag: files named throat*.wav.</summary>
        Throat,

        /// <summary>An "ow": files named groin*.wav.</summary>
        Groin,
    }

    /// <summary>
    /// The player's own sound files (Nadi, 2026-11-24: "choke bei throat und Owww bei eier schlag"). The game's male
    /// voices have no "ow" and no choke line (catalog 2026-11-24: hit, grappled, muffled, heavybreath, breathlessdeath,
    /// ...), so the mod plays WAV files from its Sounds folder. Without files the game's own voice plays, as before.
    /// </summary>
    public static class OwnSounds
    {
        public const string FolderName = "Sounds";

        /// <summary>throat*.wav and groin*.wav, any case; everything else is ignored.</summary>
        public static SoundSlot Classify(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || !string.Equals(Path.GetExtension(fileName), ".wav", StringComparison.OrdinalIgnoreCase))
            {
                return SoundSlot.None;
            }
            if (fileName.StartsWith("throat", StringComparison.OrdinalIgnoreCase))
            {
                return SoundSlot.Throat;
            }
            if (fileName.StartsWith("groin", StringComparison.OrdinalIgnoreCase))
            {
                return SoundSlot.Groin;
            }
            return SoundSlot.None;
        }

        /// <summary>
        /// Which files a sound of this reaction plays. The throat chokes at the start and on every repeat; groin and
        /// down say "ow" once at the start and keep the game's heavy breathing for the repeats.
        /// </summary>
        public static SoundSlot SlotFor(ReactionKind kind, bool repeat)
        {
            switch (kind)
            {
                case ReactionKind.Throat:
                    return SoundSlot.Throat;
                case ReactionKind.Groin:
                case ReactionKind.Down:
                    return repeat ? SoundSlot.None : SoundSlot.Groin;
                default:
                    return SoundSlot.None;
            }
        }

        /// <summary>
        /// Index of the file to play: any of them with <paramref name="roll"/> in [0, 1], never the one played last
        /// (<paramref name="last"/>, -1 for none) when there are two or more. -1 when there are no files.
        /// </summary>
        public static int Pick(int count, int last, double roll)
        {
            if (count <= 0)
            {
                return -1;
            }
            if (count == 1)
            {
                return 0;
            }
            var hasLast = last >= 0 && last < count;
            var choices = hasLast ? count - 1 : count;
            var index = Math.Min((int)(roll * choices), choices - 1);
            if (index < 0)
            {
                index = 0;
            }
            return hasLast && index >= last ? index + 1 : index;
        }
    }
}
