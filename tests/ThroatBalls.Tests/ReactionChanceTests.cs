using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    /// <summary>
    /// The chance rolls (Nadi, 2026-11-22: "in die eier schlagen/treten alleine kann männer auch zu boden gehen
    /// lassen. 50% chance. throat balls 100%. chance einstellbar für alles"). A roll is a uniform value in [0, 1);
    /// it hits when it is below the chance, so 100 % always hits and 0 % never does.
    /// </summary>
    public class ReactionChanceTests
    {
        private static ReactionSettings Settings(float groinKnockdown = 0.5f, float combo = 1f, float throat = 1f, float groin = 1f)
        {
            var s = ReactionRulesTests.Settings();
            s.GroinKnockdownChance = groinKnockdown;
            s.ComboKnockdownChance = combo;
            s.ThroatReactionChance = throat;
            s.GroinReactionChance = groin;
            return s;
        }

        private static ReactionRolls Rolls(float combo = 0.99f, float reaction = 0f, float knockdown = 0.99f)
        {
            return new ReactionRolls(combo, reaction, knockdown);
        }

        [Theory]
        [InlineData(0f, 0f, false)]
        [InlineData(0.5f, 0.49f, true)]
        [InlineData(0.5f, 0.5f, false)]
        [InlineData(1f, 0.999f, true)]
        [InlineData(1f, 0f, true)]
        public void A_roll_hits_below_the_chance(float chance, float roll, bool hits)
        {
            Assert.Equal(hits, ReactionRules.Hits(roll, chance));
        }

        [Fact]
        public void Groin_hit_alone_takes_him_down_when_the_knockdown_roll_hits()
        {
            var step = ReactionRules.OnHit(ReactionState.Idle, HitZone.Groin, 10f, Settings(), Rolls(knockdown: 0.49f));

            Assert.Equal(ReactionAction.StartGroinDown, step.Action);
            Assert.Equal(ReactionKind.Down, step.Next.Kind);
            Assert.Equal(10f, step.Next.StartedAt);
            Assert.Equal(18f, step.Next.Until);
            Assert.Equal(HitZone.None, step.Next.LastZone);
        }

        [Fact]
        public void Groin_hit_alone_only_doubles_him_over_when_the_knockdown_roll_misses()
        {
            var step = ReactionRules.OnHit(ReactionState.Idle, HitZone.Groin, 10f, Settings(), Rolls(knockdown: 0.5f));

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(ReactionKind.Groin, step.Next.Kind);
        }

        [Fact]
        public void A_throat_hit_alone_never_takes_him_down()
        {
            var step = ReactionRules.OnHit(ReactionState.Idle, HitZone.Throat, 10f, Settings(groinKnockdown: 1f), Rolls(knockdown: 0f));

            Assert.Equal(ReactionKind.Throat, step.Next.Kind);
        }

        [Fact]
        public void Another_groin_hit_while_doubled_over_rolls_the_knockdown_again()
        {
            var groin = ReactionRules.OnHit(ReactionState.Idle, HitZone.Groin, 10f, Settings(), Rolls(knockdown: 0.9f)).Next;

            var step = ReactionRules.OnHit(groin, HitZone.Groin, 11f, Settings(), Rolls(knockdown: 0.1f));

            Assert.Equal(ReactionAction.StartGroinDown, step.Action);
            Assert.Equal(19f, step.Next.Until);
        }

        [Fact]
        public void Groin_knockdown_needs_the_groin_reaction_first()
        {
            // Groin Reaction Chance gates everything a groin hit does: no reaction, no fall.
            var step = ReactionRules.OnHit(ReactionState.Idle, HitZone.Groin, 10f, Settings(groinKnockdown: 1f, groin: 0f), Rolls(knockdown: 0f));

            Assert.Equal(ReactionAction.Shrugged, step.Action);
            Assert.Equal(ReactionKind.None, step.Next.Kind);
        }

        [Fact]
        public void A_shrugged_off_hit_still_counts_as_the_first_of_both_hits()
        {
            var shrugged = ReactionRules.OnHit(ReactionState.Idle, HitZone.Throat, 10f, Settings(throat: 0f), Rolls()).Next;

            var step = ReactionRules.OnHit(shrugged, HitZone.Groin, 11f, Settings(throat: 0f), Rolls(combo: 0f));

            Assert.Equal(HitZone.Throat, shrugged.LastZone);
            Assert.Equal(10f, shrugged.LastZoneAt);
            Assert.Equal(ReactionAction.StartCombo, step.Action);
        }

        [Fact]
        public void A_shrugged_off_hit_leaves_a_running_reaction_as_it_is()
        {
            var throat = ReactionRules.OnHit(ReactionState.Idle, HitZone.Throat, 10f, Settings(), Rolls()).Next;

            var step = ReactionRules.OnHit(throat, HitZone.Throat, 11f, Settings(throat: 0f), Rolls());

            Assert.Equal(ReactionAction.Shrugged, step.Action);
            Assert.Equal(ReactionKind.Throat, step.Next.Kind);
            Assert.Equal(13f, step.Next.Until);
            Assert.Equal(11f, step.Next.LastZoneAt);
        }

        [Fact]
        public void Both_hits_take_him_down_at_100_percent()
        {
            var throat = ReactionRules.OnHit(ReactionState.Idle, HitZone.Throat, 10f, Settings(), Rolls()).Next;

            Assert.Equal(ReactionAction.StartCombo, ReactionRules.OnHit(throat, HitZone.Groin, 11f, Settings(), Rolls(combo: 0.999f)).Action);
        }

        [Fact]
        public void A_missed_both_hits_roll_leaves_the_second_hit_its_own_reaction()
        {
            var throat = ReactionRules.OnHit(ReactionState.Idle, HitZone.Throat, 10f, Settings(), Rolls()).Next;

            var step = ReactionRules.OnHit(throat, HitZone.Groin, 11f, Settings(combo: 0.5f), Rolls(combo: 0.7f, knockdown: 0.9f));

            Assert.Equal(ReactionAction.Start, step.Action);
            Assert.Equal(ReactionKind.Groin, step.Next.Kind);
            Assert.Equal(HitZone.Groin, step.Next.LastZone);
        }

        [Fact]
        public void A_missed_both_hits_roll_can_still_drop_him_by_the_groin_roll()
        {
            var throat = ReactionRules.OnHit(ReactionState.Idle, HitZone.Throat, 10f, Settings(), Rolls()).Next;

            var step = ReactionRules.OnHit(throat, HitZone.Groin, 11f, Settings(combo: 0f), Rolls(combo: 0f, knockdown: 0.2f));

            Assert.Equal(ReactionAction.StartGroinDown, step.Action);
        }

        [Fact]
        public void Lowest_rolls_hit_every_chance_above_zero()
        {
            Assert.True(ReactionRules.Hits(ReactionRolls.Lowest.Combo, 0.05f));
            Assert.True(ReactionRules.Hits(ReactionRolls.Lowest.Reaction, 0.05f));
            Assert.True(ReactionRules.Hits(ReactionRolls.Lowest.Knockdown, 0.05f));
            Assert.False(ReactionRules.Hits(ReactionRolls.Lowest.Knockdown, 0f));
        }
    }
}
