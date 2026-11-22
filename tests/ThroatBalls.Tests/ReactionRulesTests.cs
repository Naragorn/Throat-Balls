using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    /// <summary>Every flow of the reaction state: single reactions, the combo in each order and window, the end.</summary>
    public class ReactionRulesTests
    {
        private static ReactionSettings Settings()
        {
            return new ReactionSettings
            {
                ComboEnabled = true,
                ComboOrder = ComboOrder.EitherOrder,
                ComboWindow = 4f,
                ThroatDuration = 3f,
                GroinDuration = 5f,
                DownDuration = 8f,
            };
        }

        private static ReactionStep Hit(ReactionState state, Zone zone, float now, ReactionSettings settings = null)
        {
            return ReactionRules.OnHit(state, zone, now, settings ?? Settings());
        }

        [Fact]
        public void Idle_state_has_no_reaction_and_no_previous_zone()
        {
            Assert.Equal(ReactionKind.None, ReactionState.Idle.Kind);
            Assert.Equal(Zone.None, ReactionState.Idle.LastZone);
            Assert.False(ReactionState.Idle.IsActive);
        }

        [Fact]
        public void Throat_hit_starts_the_throat_reaction_for_its_duration()
        {
            var step = Hit(ReactionState.Idle, Zone.Throat, 10f);

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(ReactionKind.Throat, step.Next.Kind);
            Assert.Equal(10f, step.Next.StartedAt);
            Assert.Equal(13f, step.Next.Until);
            Assert.Equal(Zone.Throat, step.Next.LastZone);
            Assert.Equal(10f, step.Next.LastZoneAt);
        }

        [Fact]
        public void Groin_hit_starts_the_groin_reaction_for_its_duration()
        {
            var step = Hit(ReactionState.Idle, Zone.Groin, 10f);

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(ReactionKind.Groin, step.Next.Kind);
            Assert.Equal(15f, step.Next.Until);
        }

        [Fact]
        public void No_zone_changes_nothing()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;

            var step = Hit(throat, Zone.None, 11f);

            Assert.Equal(ReactionAction.None, step.Action);
            Assert.Equal(throat, step.Next);
        }

        [Fact]
        public void Same_zone_again_extends_the_running_reaction()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;

            var step = Hit(throat, Zone.Throat, 12f);

            Assert.Equal(ReactionAction.Extend, step.Action);
            Assert.Equal(ReactionKind.Throat, step.Next.Kind);
            Assert.Equal(10f, step.Next.StartedAt);
            Assert.Equal(15f, step.Next.Until);
            Assert.Equal(12f, step.Next.LastZoneAt);
        }

        [Fact]
        public void Same_zone_after_the_reaction_ended_starts_it_again()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;
            var ended = ReactionRules.OnTick(throat, 13f, true).Next;

            var step = Hit(ended, Zone.Throat, 14f);

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(14f, step.Next.StartedAt);
        }

        [Fact]
        public void Throat_then_groin_within_the_window_is_the_combo()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;

            var step = Hit(throat, Zone.Groin, 13.5f);

            Assert.Equal(ReactionAction.StartCombo, step.Action);
            Assert.Equal(ReactionKind.Combo, step.Next.Kind);
            Assert.Equal(13.5f, step.Next.StartedAt);
            Assert.Equal(21.5f, step.Next.Until);
            Assert.Equal(Zone.None, step.Next.LastZone);
        }

        [Fact]
        public void Groin_then_throat_within_the_window_is_the_combo()
        {
            var groin = Hit(ReactionState.Idle, Zone.Groin, 10f).Next;

            Assert.Equal(ReactionAction.StartCombo, Hit(groin, Zone.Throat, 11f).Action);
        }

        [Fact]
        public void Second_hit_exactly_at_the_window_end_still_counts()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;

            Assert.Equal(ReactionAction.StartCombo, Hit(throat, Zone.Groin, 14f).Action);
        }

        [Fact]
        public void Second_hit_after_the_window_starts_its_own_reaction()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;
            var ended = ReactionRules.OnTick(throat, 13f, true).Next;

            var step = Hit(ended, Zone.Groin, 14.01f);

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(ReactionKind.Groin, step.Next.Kind);
            Assert.Equal(Zone.Groin, step.Next.LastZone);
        }

        [Fact]
        public void Combo_counts_after_the_first_reaction_ended_while_still_in_the_window()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;
            var ended = ReactionRules.OnTick(throat, 13f, true).Next;

            Assert.Equal(ReactionAction.StartCombo, Hit(ended, Zone.Groin, 13.5f).Action);
        }

        [Fact]
        public void Combo_off_switches_to_the_other_reaction()
        {
            var settings = Settings();
            settings.ComboEnabled = false;
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f, settings).Next;

            var step = Hit(throat, Zone.Groin, 11f, settings);

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(ReactionKind.Groin, step.Next.Kind);
            Assert.Equal(16f, step.Next.Until);
        }

        [Theory]
        [InlineData(ComboOrder.EitherOrder, Zone.Throat, Zone.Groin, true)]
        [InlineData(ComboOrder.EitherOrder, Zone.Groin, Zone.Throat, true)]
        [InlineData(ComboOrder.ThroatThenGroin, Zone.Throat, Zone.Groin, true)]
        [InlineData(ComboOrder.ThroatThenGroin, Zone.Groin, Zone.Throat, false)]
        [InlineData(ComboOrder.GroinThenThroat, Zone.Groin, Zone.Throat, true)]
        [InlineData(ComboOrder.GroinThenThroat, Zone.Throat, Zone.Groin, false)]
        [InlineData(ComboOrder.EitherOrder, Zone.Throat, Zone.Throat, false)]
        [InlineData(ComboOrder.EitherOrder, Zone.Groin, Zone.Groin, false)]
        [InlineData(ComboOrder.EitherOrder, Zone.None, Zone.Groin, false)]
        public void Combo_order_decides_which_sequences_complete_it(ComboOrder order, Zone first, Zone second, bool completes)
        {
            Assert.Equal(completes, ReactionRules.CompletesCombo(first, second, order));
        }

        [Fact]
        public void Wrong_order_starts_the_second_zones_reaction_which_can_then_complete_the_combo()
        {
            var settings = Settings();
            settings.ComboOrder = ComboOrder.ThroatThenGroin;
            var groin = Hit(ReactionState.Idle, Zone.Groin, 10f, settings).Next;

            var throat = Hit(groin, Zone.Throat, 11f, settings);
            var combo = Hit(throat.Next, Zone.Groin, 12f, settings);

            Assert.Equal(ReactionAction.Start, throat.Action);
            Assert.Equal(ReactionKind.Throat, throat.Next.Kind);
            Assert.Equal(ReactionAction.StartCombo, combo.Action);
        }

        [Fact]
        public void Hits_while_down_are_ignored()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;
            var combo = Hit(throat, Zone.Groin, 11f).Next;

            var step = Hit(combo, Zone.Throat, 12f);

            Assert.Equal(ReactionAction.IgnoredWhileDown, step.Action);
            Assert.Equal(combo, step.Next);
        }

        [Fact]
        public void Tick_before_the_end_changes_nothing()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;

            var step = ReactionRules.OnTick(throat, 12.9f, true);

            Assert.Equal(ReactionAction.None, step.Action);
            Assert.Equal(throat, step.Next);
        }

        [Fact]
        public void Tick_at_the_end_ends_a_single_reaction_and_keeps_its_zone_for_the_combo()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;

            var step = ReactionRules.OnTick(throat, 13f, true);

            Assert.Equal(ReactionAction.End, step.Action);
            Assert.Equal(ReactionKind.None, step.Next.Kind);
            Assert.Equal(Zone.Throat, step.Next.LastZone);
            Assert.Equal(10f, step.Next.LastZoneAt);
        }

        [Fact]
        public void Tick_at_the_end_of_the_combo_forgets_the_zones()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;
            var combo = Hit(throat, Zone.Groin, 11f).Next;

            var step = ReactionRules.OnTick(combo, 19f, true);

            Assert.Equal(ReactionAction.End, step.Action);
            Assert.Equal(ReactionState.Idle, step.Next);
        }

        [Fact]
        public void Turning_the_mod_off_ends_a_running_reaction_at_once()
        {
            var groin = Hit(ReactionState.Idle, Zone.Groin, 10f).Next;

            var step = ReactionRules.OnTick(groin, 10.5f, false);

            Assert.Equal(ReactionAction.End, step.Action);
            Assert.Equal(ReactionKind.None, step.Next.Kind);
        }

        [Fact]
        public void Tick_without_a_reaction_does_nothing_even_when_off()
        {
            Assert.Equal(ReactionAction.None, ReactionRules.OnTick(ReactionState.Idle, 1f, false).Action);
        }

        [Fact]
        public void A_zone_older_than_the_window_is_forgotten()
        {
            var throat = Hit(ReactionState.Idle, Zone.Throat, 10f).Next;
            var ended = ReactionRules.OnTick(throat, 13f, true).Next;

            Assert.True(ReactionRules.Forgettable(ended, 14.01f, Settings()));
            Assert.False(ReactionRules.Forgettable(ended, 14f, Settings()));
            Assert.False(ReactionRules.Forgettable(throat, 20f, Settings()));
        }
    }
}
