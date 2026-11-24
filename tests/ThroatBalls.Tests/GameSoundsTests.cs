using System;
using System.Collections.Generic;
using System.Linq;
using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class GameSoundsTests
    {
        private static readonly bool[] AllFour = { true, true, true, true };

        // ---- Which clips (Nadi, 2026-11-24, picked by ear from the game's audio) ----------------

        [Fact]
        public void Throat_is_the_four_breathless_death_clips_in_the_order_they_were_picked()
        {
            Assert.Equal(
                new[] { "Male_Shout-of-Pain_047", "Male_Shout-of-Pain_054", "Male_Shout-of-Pain_072", "Male_Shout-of-Pain_087" },
                GameSounds.Throat.Select(c => c.Clip));
            Assert.All(GameSounds.Throat, c => Assert.Equal("Bas.AudioGroup.Voice.Male.BreathlessDeath", c.Address));
        }

        [Fact]
        public void Groin_is_hit_7_of_cottage_man_2_and_hit_10_of_edwyn()
        {
            Assert.Equal(
                new[] { "Bas.AudioGroup.Voice.MaleCottage2.Hit|male2_get_hit_07", "Bas.AudioGroup.Voice.MaleEdwyn.Hit|Male_Hurt_10_Edwyn" },
                GameSounds.Groin.Select(c => c.Address + "|" + c.Clip));
        }

        [Fact]
        public void Each_slot_has_its_own_clips_and_none_has_none()
        {
            Assert.Same(GameSounds.Throat, GameSounds.For(SoundSlot.Throat));
            Assert.Same(GameSounds.Groin, GameSounds.For(SoundSlot.Groin));
            Assert.Empty(GameSounds.For(SoundSlot.None));
        }

        [Fact]
        public void Every_audio_group_is_loaded_once()
        {
            Assert.Equal(
                new[] { "Bas.AudioGroup.Voice.Male.BreathlessDeath", "Bas.AudioGroup.Voice.MaleCottage2.Hit", "Bas.AudioGroup.Voice.MaleEdwyn.Hit" },
                GameSounds.Addresses());
        }

        // ---- Throat Sound option ------------------------------------------------------------------

        [Theory]
        [InlineData(ThroatSound.Random, -1)]
        [InlineData(ThroatSound.Sound1, 0)]
        [InlineData(ThroatSound.Sound2, 1)]
        [InlineData(ThroatSound.Sound3, 2)]
        [InlineData(ThroatSound.Sound4, 3)]
        public void Throat_sound_n_is_the_nth_clip_and_random_is_none(ThroatSound choice, int index)
        {
            Assert.Equal(index, GameSounds.FixedIndex(choice));
        }

        [Fact]
        public void Every_fixed_throat_sound_has_a_clip_and_random_comes_first()
        {
            var values = (ThroatSound[])Enum.GetValues(typeof(ThroatSound));
            Assert.Equal(ThroatSound.Random, values[0]); // the menu falls back to the first entry
            Assert.Equal(GameSounds.Throat.Count, values.Length - 1);
            Assert.All(values.Skip(1), v => Assert.InRange(GameSounds.FixedIndex(v), 0, GameSounds.Throat.Count - 1));
        }

        // ---- Which clip plays ---------------------------------------------------------------------

        [Theory]
        [InlineData(0, -1)]
        [InlineData(1, 1)] // a fixed choice may repeat: the player chose that one
        [InlineData(3, 0)]
        public void A_fixed_choice_plays_that_clip(int fixedIndex, int last)
        {
            Assert.Equal(fixedIndex, GameSounds.Pick(AllFour, fixedIndex, last, 0.5));
        }

        [Fact]
        public void A_fixed_choice_that_is_missing_falls_back_to_the_others()
        {
            var present = new[] { true, false, true, true };
            foreach (var roll in Rolls())
            {
                var index = GameSounds.Pick(present, 1, -1, roll);
                Assert.Contains(index, new[] { 0, 2, 3 });
            }
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(4)]
        public void A_fixed_choice_out_of_range_picks_at_random(int fixedIndex)
        {
            Assert.Equal(0, GameSounds.Pick(AllFour, fixedIndex, -1, 0.0));
            Assert.Equal(3, GameSounds.Pick(AllFour, fixedIndex, -1, 0.999));
        }

        [Fact]
        public void Random_never_plays_the_last_clip_twice_in_a_row()
        {
            for (var last = 0; last < 4; last++)
            {
                foreach (var roll in Rolls())
                {
                    Assert.NotEqual(last, GameSounds.Pick(AllFour, -1, last, roll));
                }
            }
        }

        [Fact]
        public void Random_reaches_every_clip_but_the_last()
        {
            var seen = new HashSet<int>(Rolls().Select(roll => GameSounds.Pick(AllFour, -1, 2, roll)));
            Assert.Equal(new[] { 0, 1, 3 }, seen.OrderBy(i => i));
        }

        [Fact]
        public void Random_without_a_last_clip_reaches_all_of_them()
        {
            var seen = new HashSet<int>(Rolls().Select(roll => GameSounds.Pick(AllFour, -1, -1, roll)));
            Assert.Equal(new[] { 0, 1, 2, 3 }, seen.OrderBy(i => i));
        }

        [Fact]
        public void Random_skips_clips_that_are_missing()
        {
            var present = new[] { false, true, false, true };
            var seen = new HashSet<int>(Rolls().Select(roll => GameSounds.Pick(present, -1, -1, roll)));
            Assert.Equal(new[] { 1, 3 }, seen.OrderBy(i => i));
        }

        [Fact]
        public void Two_clips_take_turns()
        {
            var two = new[] { true, true };
            foreach (var roll in Rolls())
            {
                Assert.Equal(1, GameSounds.Pick(two, -1, 0, roll));
                Assert.Equal(0, GameSounds.Pick(two, -1, 1, roll));
            }
        }

        [Fact]
        public void A_single_clip_plays_even_when_it_was_the_last()
        {
            Assert.Equal(1, GameSounds.Pick(new[] { false, true, false }, -1, 1, 0.7));
        }

        [Theory]
        [InlineData(5)] // out of range
        [InlineData(1)] // missing
        public void A_last_clip_that_is_not_there_counts_as_none(int last)
        {
            var present = new[] { true, false, true };
            Assert.Equal(0, GameSounds.Pick(present, -1, last, 0.0));
            Assert.Equal(2, GameSounds.Pick(present, -1, last, 0.999));
        }

        [Fact]
        public void A_roll_of_one_stays_in_range()
        {
            Assert.Equal(3, GameSounds.Pick(AllFour, -1, -1, 1.0));
            Assert.Equal(2, GameSounds.Pick(AllFour, -1, 3, 1.0));
        }

        [Fact]
        public void No_clips_play_nothing()
        {
            Assert.Equal(-1, GameSounds.Pick(new[] { false, false }, -1, -1, 0.5));
            Assert.Equal(-1, GameSounds.Pick(new[] { false, false }, 0, -1, 0.5));
            Assert.Equal(-1, GameSounds.Pick(new bool[0], -1, -1, 0.5));
            Assert.Equal(-1, GameSounds.Pick(null, 0, -1, 0.5));
        }

        // ---- Own file, game clip or the voice's line ------------------------------------------------

        [Theory]
        [InlineData(true, 1, 4, SoundSource.Own)]
        [InlineData(true, 1, 0, SoundSource.Own)]
        [InlineData(true, 0, 4, SoundSource.Game)] // Own Sounds on, no files: the game clips
        [InlineData(false, 2, 4, SoundSource.Game)] // Own Sounds off: the files stay unused
        [InlineData(false, 0, 2, SoundSource.Game)]
        [InlineData(true, 0, 0, SoundSource.Dialog)] // the game clips did not load: the voice's own line
        [InlineData(false, 3, 0, SoundSource.Dialog)]
        public void Own_files_come_first_then_the_game_clips_then_the_voice(bool ownOn, int ownCount, int gameCount, SoundSource expected)
        {
            Assert.Equal(expected, GameSounds.SourceFor(SoundSlot.Throat, ownOn, ownCount, gameCount));
            Assert.Equal(expected, GameSounds.SourceFor(SoundSlot.Groin, ownOn, ownCount, gameCount));
        }

        [Fact]
        public void A_sound_without_a_slot_is_always_the_voice()
        {
            // The groin repeats: the game's heavy breathing.
            Assert.Equal(SoundSource.Dialog, GameSounds.SourceFor(SoundSlot.None, true, 2, 2));
        }

        private static IEnumerable<double> Rolls()
        {
            for (var i = 0; i <= 20; i++)
            {
                yield return i / 20.0;
            }
        }
    }
}
