using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class OwnSoundsTests
    {
        [Theory]
        [InlineData("throat.wav", SoundSlot.Throat)]
        [InlineData("Throat-choke-2.WAV", SoundSlot.Throat)]
        [InlineData("groin.wav", SoundSlot.Groin)]
        [InlineData("GROIN_oww.wav", SoundSlot.Groin)]
        public void Files_named_after_the_spot_go_to_that_spot(string file, SoundSlot slot)
        {
            Assert.Equal(slot, OwnSounds.Classify(file));
        }

        [Theory]
        [InlineData("throat.ogg")] // only WAV is read
        [InlineData("throat.mp3")]
        [InlineData("readme.txt")]
        [InlineData("choke.wav")] // no spot in the name
        [InlineData("wav")]
        [InlineData("")]
        [InlineData(null)]
        public void Other_files_are_left_alone(string file)
        {
            Assert.Equal(SoundSlot.None, OwnSounds.Classify(file));
        }

        [Fact]
        public void Throat_plays_its_files_at_the_start_and_on_every_repeat()
        {
            Assert.Equal(SoundSlot.Throat, OwnSounds.SlotFor(ReactionKind.Throat, repeat: false));
            Assert.Equal(SoundSlot.Throat, OwnSounds.SlotFor(ReactionKind.Throat, repeat: true));
        }

        [Theory]
        [InlineData(ReactionKind.Groin)]
        [InlineData(ReactionKind.Down)]
        public void Groin_and_down_play_their_files_once_at_the_start(ReactionKind kind)
        {
            Assert.Equal(SoundSlot.Groin, OwnSounds.SlotFor(kind, repeat: false));
            // The repeats stay the game's heavy breathing: an "ow" every few seconds would sound silly.
            Assert.Equal(SoundSlot.None, OwnSounds.SlotFor(kind, repeat: true));
        }

        [Fact]
        public void No_reaction_has_no_sound()
        {
            Assert.Equal(SoundSlot.None, OwnSounds.SlotFor(ReactionKind.None, repeat: false));
            Assert.Equal(SoundSlot.None, OwnSounds.SlotFor(ReactionKind.None, repeat: true));
        }

        [Fact]
        public void No_files_pick_nothing()
        {
            Assert.Equal(-1, OwnSounds.Pick(0, -1, 0.5));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void One_file_is_always_picked(int last)
        {
            Assert.Equal(0, OwnSounds.Pick(1, last, 0.99));
        }

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(0.34, 1)]
        [InlineData(0.99, 2)]
        [InlineData(1.0, 2)] // a roll of exactly 1 stays in range
        public void First_pick_spreads_over_all_files(double roll, int expected)
        {
            Assert.Equal(expected, OwnSounds.Pick(3, -1, roll));
        }

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(0.49, 0)]
        [InlineData(0.5, 2)] // skips the last one played (1)
        [InlineData(1.0, 2)]
        public void The_same_file_never_plays_twice_in_a_row(double roll, int expected)
        {
            Assert.Equal(expected, OwnSounds.Pick(3, 1, roll));
        }

        [Fact]
        public void A_last_index_out_of_range_counts_as_none()
        {
            Assert.Equal(2, OwnSounds.Pick(3, 7, 0.99));
        }

        [Fact]
        public void Every_roll_gives_a_valid_index_that_differs_from_the_last()
        {
            for (var count = 2; count <= 5; count++)
            {
                for (var last = 0; last < count; last++)
                {
                    for (var roll = 0.0; roll <= 1.0; roll += 0.01)
                    {
                        var index = OwnSounds.Pick(count, last, roll);
                        Assert.InRange(index, 0, count - 1);
                        Assert.NotEqual(last, index);
                    }
                }
            }
        }
    }
}
