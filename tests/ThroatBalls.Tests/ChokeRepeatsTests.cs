using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    /// <summary>
    /// The throat sound plays once at the hit and then Choke Repeats more times, each after the one before has ended
    /// plus a short pause (Nadi, 2026-11-26: "den wiederholen wir dann halt paar mal, erstmal 2x, einstellbar"). The
    /// reaction lasts until the last one has played.
    /// </summary>
    public class ChokeRepeatsTests
    {
        private const float Length = 1.55f;

        [Fact]
        public void Choke_Repeats_starts_at_2_and_goes_from_0_to_5()
        {
            Assert.Equal(2f, OptionSpecs.ChokeRepeats.Default);
            Assert.Equal(0f, OptionSpecs.ChokeRepeats.Min);
            Assert.Equal(5f, OptionSpecs.ChokeRepeats.Max);
            Assert.Contains(OptionSpecs.ChokeRepeats, OptionSpecs.All);
        }

        [Fact]
        public void The_next_choke_waits_for_the_last_one_and_the_pause()
        {
            Assert.False(PoseRules.ChokeDue(1, 2, Length + PoseRules.ChokePause - 0.01f, Length));
            Assert.True(PoseRules.ChokeDue(1, 2, Length + PoseRules.ChokePause, Length));
        }

        [Theory]
        [InlineData(1, 2, true)]
        [InlineData(2, 2, true)]
        [InlineData(3, 2, false)]
        [InlineData(1, 0, false)]
        [InlineData(0, 2, false)]
        public void Only_as_many_repeats_as_set(int played, int repeats, bool due)
        {
            Assert.Equal(due, PoseRules.ChokeDue(played, repeats, 10f, Length));
        }

        [Fact]
        public void The_reaction_lasts_until_the_last_choke_has_played()
        {
            // First choke at 0: two more, each after 1.55 s + pause.
            Assert.Equal(3 * Length + 2 * PoseRules.ChokePause, PoseRules.ChokesEnd(0f, 1, 2, Length), 3);
            // All played: the last one's end.
            Assert.Equal(5f + Length, PoseRules.ChokesEnd(5f, 3, 2, Length), 3);
            // No repeats: the first one's end.
            Assert.Equal(Length, PoseRules.ChokesEnd(0f, 1, 0, Length), 3);
        }
    }
}
