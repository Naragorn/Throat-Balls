using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class KnockOutRulesTests
    {
        [Fact]
        public void No_busy_state_entered_means_not_busy()
        {
            Assert.False(KnockOutRules.BusyAfterKnockOut(hasCount: false, busyStates: 0));
        }

        [Fact]
        public void All_busy_states_left_means_not_busy()
        {
            // The case of the bug: the cut stagger had set IsBusy by hand, no busy state is running.
            Assert.False(KnockOutRules.BusyAfterKnockOut(hasCount: true, busyStates: 0));
        }

        [Fact]
        public void A_running_busy_state_keeps_him_busy()
        {
            Assert.True(KnockOutRules.BusyAfterKnockOut(hasCount: true, busyStates: 1));
            Assert.True(KnockOutRules.BusyAfterKnockOut(hasCount: true, busyStates: 3));
        }

        [Fact]
        public void A_negative_count_is_not_busy()
        {
            // BusyState counts down on an exit without an enter; its own rule is count > 0.
            Assert.False(KnockOutRules.BusyAfterKnockOut(hasCount: true, busyStates: -1));
        }

        [Fact]
        public void The_hit_reaction_goes_off_with_the_brain()
        {
            Assert.True(KnockOutRules.ToggleHitReaction);
        }
    }
}
