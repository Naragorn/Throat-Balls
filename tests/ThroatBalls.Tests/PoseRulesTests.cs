using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    /// <summary>The body height over a reaction, the moment of the fall, weapons and sounds.</summary>
    public class PoseRulesTests
    {
        private static PoseSettings Settings(FallStyle style = FallStyle.CurlUp)
        {
            return new PoseSettings
            {
                GroinCrouchHeight = 0.6f,
                LyingHeight = 0.2f,
                FallTime = 1f,
                GetUpTime = 1.5f,
                FallStyle = style,
                DropWeapons = DropWeapons.WhenTheyGoDown,
            };
        }

        [Theory]
        [InlineData(0f, 3f)]
        [InlineData(1f, 2f)]
        [InlineData(2.9f, 0.1f)]
        public void Throat_reaction_keeps_standing_height(float elapsed, float remaining)
        {
            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.Throat, elapsed, remaining, Settings()));
        }

        [Fact]
        public void No_reaction_is_standing_height()
        {
            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.None, 1f, 1f, Settings()));
        }

        [Fact]
        public void Groin_reaction_bends_down_then_holds_the_crouch()
        {
            var s = Settings();

            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.Groin, 0f, 5f, s), 4);
            Assert.Equal(0.8f, PoseRules.BodyHeight(ReactionKind.Groin, PoseRules.BendTime / 2f, 5f, s), 4);
            Assert.Equal(0.6f, PoseRules.BodyHeight(ReactionKind.Groin, PoseRules.BendTime, 5f, s), 4);
            Assert.Equal(0.6f, PoseRules.BodyHeight(ReactionKind.Groin, 2f, 3f, s), 4);
        }

        [Fact]
        public void Groin_reaction_straightens_up_over_the_get_up_time_at_the_end()
        {
            var s = Settings();

            Assert.Equal(0.6f, PoseRules.BodyHeight(ReactionKind.Groin, 3.5f, 1.5f, s), 4);
            Assert.Equal(0.8f, PoseRules.BodyHeight(ReactionKind.Groin, 4.25f, 0.75f, s), 4);
            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.Groin, 5f, 0f, s), 4);
        }

        [Fact]
        public void Short_reaction_never_drops_below_what_the_rise_allows()
        {
            var s = Settings();

            // 0.5 s in total: half way into the bend the rise already caps it.
            var height = PoseRules.BodyHeight(ReactionKind.Groin, 0.3f, 0.2f, s);

            Assert.True(height > 0.6f);
            Assert.True(height < 1f);
        }

        [Fact]
        public void Combo_curls_up_from_the_crouch_down_to_the_ground_and_gets_up_at_the_end()
        {
            var s = Settings();

            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.Combo, 0f, 8f, s), 4);
            Assert.Equal(0.6f, PoseRules.BodyHeight(ReactionKind.Combo, PoseRules.BendTime, 8f, s), 4);
            Assert.Equal(0.4f, PoseRules.BodyHeight(ReactionKind.Combo, PoseRules.BendTime + 0.5f, 7f, s), 4);
            Assert.Equal(0.2f, PoseRules.BodyHeight(ReactionKind.Combo, PoseRules.BendTime + 1f, 6f, s), 4);
            Assert.Equal(0.2f, PoseRules.BodyHeight(ReactionKind.Combo, 5f, 1.5f, s), 4);
            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.Combo, 8f, 0f, s), 4);
        }

        [Fact]
        public void Ragdoll_fall_holds_the_crouch_until_the_fall_then_leaves_height_to_the_game()
        {
            var s = Settings(FallStyle.Ragdoll);

            Assert.Equal(0.6f, PoseRules.BodyHeight(ReactionKind.Combo, PoseRules.BendTime + 0.5f, 7f, s), 4);
            Assert.Equal(1f, PoseRules.BodyHeight(ReactionKind.Combo, PoseRules.BendTime + 1f, 6f, s), 4);
        }

        [Theory]
        [InlineData(ReactionKind.Combo, 1.29f, false)]
        [InlineData(ReactionKind.Combo, 1.3f, true)]
        [InlineData(ReactionKind.Combo, 5f, true)]
        [InlineData(ReactionKind.Groin, 5f, false)]
        [InlineData(ReactionKind.Throat, 5f, false)]
        [InlineData(ReactionKind.None, 5f, false)]
        public void Down_from_the_end_of_the_fall_in_the_combo_only(ReactionKind kind, float elapsed, bool down)
        {
            Assert.Equal(down, PoseRules.IsDown(kind, elapsed, Settings()));
        }

        [Theory]
        [InlineData(DropWeapons.WhenTheyGoDown, ReactionKind.Combo, true)]
        [InlineData(DropWeapons.WhenTheyGoDown, ReactionKind.Groin, false)]
        [InlineData(DropWeapons.WhenTheyGoDown, ReactionKind.Throat, false)]
        [InlineData(DropWeapons.Always, ReactionKind.Throat, true)]
        [InlineData(DropWeapons.Always, ReactionKind.Groin, true)]
        [InlineData(DropWeapons.Always, ReactionKind.Combo, true)]
        [InlineData(DropWeapons.Never, ReactionKind.Combo, false)]
        [InlineData(DropWeapons.Never, ReactionKind.Groin, false)]
        [InlineData(DropWeapons.Always, ReactionKind.None, false)]
        public void Drop_weapons_setting_decides_per_reaction(DropWeapons drop, ReactionKind kind, bool drops)
        {
            Assert.Equal(drops, PoseRules.DropsWeapons(kind, drop));
        }

        [Theory]
        [InlineData(ReactionKind.Throat, BodyTarget.Throat)]
        [InlineData(ReactionKind.Groin, BodyTarget.Groin)]
        [InlineData(ReactionKind.Combo, BodyTarget.Groin)]
        [InlineData(ReactionKind.None, BodyTarget.None)]
        public void Hands_go_to_the_hurt_spot(ReactionKind kind, BodyTarget target)
        {
            Assert.Equal(target, PoseRules.HandsTo(kind));
        }

        [Theory]
        [InlineData(BodyTarget.Throat)]
        [InlineData(BodyTarget.Groin)]
        public void Hands_rest_side_by_side_in_front_of_the_spot(BodyTarget target)
        {
            var left = PoseRules.HandOffset(target, true);
            var right = PoseRules.HandOffset(target, false);

            Assert.True(left.X < 0f);
            Assert.True(right.X > 0f);
            Assert.Equal(-left.X, right.X, 4);
            Assert.True(left.Z > 0f);
            Assert.Equal(left.Z, right.Z, 4);
        }

        [Fact]
        public void No_target_has_no_hand_offset()
        {
            Assert.Equal(Vec3.Zero, PoseRules.HandOffset(BodyTarget.None, true));
        }

        [Theory]
        [InlineData(ReactionKind.Throat, "grappled", "grappled")]
        [InlineData(ReactionKind.Groin, "hit", "heavybreath")]
        [InlineData(ReactionKind.Combo, "hit", "heavybreath")]
        [InlineData(ReactionKind.None, null, null)]
        public void Each_reaction_has_its_voice(ReactionKind kind, string start, string repeat)
        {
            Assert.Equal(start, PoseRules.StartSound(kind));
            Assert.Equal(repeat, PoseRules.RepeatSound(kind));
        }

        [Theory]
        [InlineData(10f, 12.9f, 3f, false)]
        [InlineData(10f, 13f, 3f, true)]
        [InlineData(10f, 30f, 0f, false)]
        public void Groans_repeat_at_the_interval(float last, float now, float interval, bool due)
        {
            Assert.Equal(due, PoseRules.GroanDue(last, now, interval));
        }
    }
}
