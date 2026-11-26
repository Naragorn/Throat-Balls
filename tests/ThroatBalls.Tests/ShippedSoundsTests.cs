using System;
using System.IO;
using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    /// <summary>
    /// The choke the release ships in Sounds/ (Nadi, 2026-11-25: "soll ins release"): ZapSplat's "Male choking, being
    /// strangled or dying vocal, gag. Version 9", its first two bursts and its last three, the 3 s between cut out
    /// (Nadi, 2026-11-26: "nicht nur die ersten 2 spitzen, sondern auch die letzten 3 ... das dazwischen rausgeschnitten").
    /// </summary>
    public class ShippedSoundsTests
    {
        private const string Choke = "src/ThroatBalls/Sounds/throat-choke.wav";

        [Fact]
        public void The_shipped_choke_is_a_throat_file()
        {
            Assert.Equal(SoundSlot.Throat, OwnSounds.Classify(Path.GetFileName(Choke)));
        }

        [Fact]
        public void The_shipped_choke_is_a_mono_wav_the_mod_reads()
        {
            Assert.True(WavReader.TryRead(File.ReadAllBytes(RepoFile(Choke)), out var wav, out var error), error);
            Assert.Equal(1, wav.Channels);
            Assert.Equal(48000, wav.SampleRate);
            // 0.005 to 0.600 s and 3.520 to 4.475 s of the original, each with a short fade: 1.55 s.
            Assert.InRange(wav.Samples.Length / (double)wav.SampleRate, 1.53, 1.57);
        }

        [Fact]
        public void The_shipped_choke_peaks_where_the_games_own_chokes_do()
        {
            // The four breathless-death clips peak at -2.1 to -2.5 dBFS (measured 2026-11-25); the choke is set to -2 dBFS.
            Assert.True(WavReader.TryRead(File.ReadAllBytes(RepoFile(Choke)), out var wav, out var error), error);
            var peak = 0f;
            foreach (var sample in wav.Samples)
            {
                peak = Math.Max(peak, Math.Abs(sample));
            }
            Assert.InRange(20 * Math.Log10(peak), -2.3, -1.7);
        }

        private static string RepoFile(string relative)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("not found above the test folder", relative);
        }
    }
}
