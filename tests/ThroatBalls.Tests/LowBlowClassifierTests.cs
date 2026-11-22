using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    /// <summary>
    /// Every decision of <see cref="LowBlowClassifier"/>: each refusal in its order, both zones,
    /// the per-zone armor, the allowed hits and the front check.
    /// The NPC stands at the origin facing +Z; its neck at 1.5 m, its hips at 1.0 m.
    /// </summary>
    public class LowBlowClassifierTests
    {
        private static readonly Vec3 Neck = new Vec3(0f, 1.5f, 0f);
        private static readonly Vec3 Hips = new Vec3(0f, 1.0f, 0f);

        internal static LowBlowSettings Settings()
        {
            return new LowBlowSettings
            {
                ThroatEnabled = true,
                GroinEnabled = true,
                MenOnly = true,
                MetalArmorProtects = true,
                AllowedHits = AllowedHits.BluntOnly,
                MinHitSpeed = 1.5f,
                ThroatZoneSize = 0.07f,
                ThroatForwardOffset = 0.05f,
                ThroatHeightOffset = 0.05f,
                GroinZoneSize = 0.09f,
                GroinForwardOffset = 0.08f,
                GroinHeightOffset = -0.08f,
                MaxHitAngle = 100f,
            };
        }

        private static Vec3 ThroatCenter() => LowBlowZones.ThroatCenter(Neck, new Vec3(0f, 0f, 1f), Settings());

        private static Vec3 GroinCenter() => LowBlowZones.GroinCenter(Hips, new Vec3(0f, 0f, 1f), Settings());

        internal static LowBlowInput Punch(Vec3 contact)
        {
            return new LowBlowInput
            {
                AttackerIsPlayer = true,
                TargetIsMale = true,
                IsUnarmed = true,
                DamageType = HitDamageType.Blunt,
                ImpactSpeed = 4f,
                ContactPoint = contact,
                TargetPosition = Vec3.Zero,
                TargetForward = new Vec3(0f, 0f, 1f),
                HasNeck = true,
                Neck = Neck,
                HasHips = true,
                Hips = Hips,
                HasAttackerPosition = true,
                AttackerPosition = new Vec3(0f, 0f, 1f),
            };
        }

        private static LowBlowResult Classify(LowBlowInput input) => LowBlowClassifier.Classify(input, Settings());

        [Fact]
        public void Punch_on_the_adams_apple_is_a_throat_hit()
        {
            var result = Classify(Punch(ThroatCenter()));

            Assert.True(result.IsHit);
            Assert.Equal(HitZone.Throat, result.HitZone);
            Assert.Equal(LowBlowRejection.None, result.Rejection);
            Assert.Equal(0f, result.ZoneDistance, 4);
        }

        [Fact]
        public void Punch_on_the_groin_is_a_groin_hit()
        {
            var result = Classify(Punch(GroinCenter()));

            Assert.True(result.IsHit);
            Assert.Equal(HitZone.Groin, result.HitZone);
        }

        [Fact]
        public void Hit_just_inside_the_zone_edge_counts_and_just_outside_does_not()
        {
            var inside = Classify(Punch(ThroatCenter() + new Vec3(0f, 0f, 0.069f)));
            var outside = Classify(Punch(ThroatCenter() + new Vec3(0f, 0f, 0.071f)));

            Assert.Equal(HitZone.Throat, inside.HitZone);
            Assert.Equal(LowBlowRejection.OutsideZones, outside.Rejection);
            Assert.Equal(HitZone.Throat, outside.NearestZone);
            Assert.Equal(0.071f, outside.ZoneDistance, 3);
        }

        [Fact]
        public void Hit_on_the_chest_is_outside_both_zones_and_names_the_nearest()
        {
            var result = Classify(Punch(new Vec3(0f, 1.25f, 0.12f)));

            Assert.False(result.IsHit);
            Assert.Equal(LowBlowRejection.OutsideZones, result.Rejection);
            Assert.Equal(HitZone.None, result.HitZone);
            Assert.NotEqual(HitZone.None, result.NearestZone);
        }

        [Fact]
        public void Self_hit_is_refused_first()
        {
            var input = Punch(ThroatCenter());
            input.AttackerIsTarget = true;
            input.TargetIsMale = false;

            Assert.Equal(LowBlowRejection.SelfHit, Classify(input).Rejection);
        }

        [Fact]
        public void Npc_hitting_an_npc_does_not_count()
        {
            var input = Punch(ThroatCenter());
            input.AttackerIsPlayer = false;

            Assert.Equal(LowBlowRejection.NotPlayerVsNpc, Classify(input).Rejection);
        }

        [Fact]
        public void Hit_on_the_player_does_not_count()
        {
            var input = Punch(ThroatCenter());
            input.TargetIsPlayer = true;

            Assert.Equal(LowBlowRejection.NotPlayerVsNpc, Classify(input).Rejection);
        }

        [Fact]
        public void Dead_npc_does_not_react()
        {
            var input = Punch(ThroatCenter());
            input.TargetIsKilled = true;

            Assert.Equal(LowBlowRejection.TargetDead, Classify(input).Rejection);
        }

        [Fact]
        public void Woman_does_not_react_with_men_only_on()
        {
            var input = Punch(GroinCenter());
            input.TargetIsMale = false;

            Assert.Equal(LowBlowRejection.NotMale, Classify(input).Rejection);
        }

        [Fact]
        public void Woman_reacts_with_men_only_off()
        {
            var input = Punch(GroinCenter());
            input.TargetIsMale = false;
            var settings = Settings();
            settings.MenOnly = false;

            Assert.Equal(HitZone.Groin, LowBlowClassifier.Classify(input, settings).HitZone);
        }

        [Theory]
        [InlineData(AllowedHits.BluntOnly, HitDamageType.Blunt, false, true)]
        [InlineData(AllowedHits.BluntOnly, HitDamageType.Blunt, true, true)]
        [InlineData(AllowedHits.BluntOnly, HitDamageType.Slash, false, false)]
        [InlineData(AllowedHits.BluntOnly, HitDamageType.Pierce, false, false)]
        [InlineData(AllowedHits.BluntOnly, HitDamageType.Other, false, false)]
        [InlineData(AllowedHits.FistsAndKicksOnly, HitDamageType.Blunt, true, true)]
        [InlineData(AllowedHits.FistsAndKicksOnly, HitDamageType.Blunt, false, false)]
        [InlineData(AllowedHits.FistsAndKicksOnly, HitDamageType.Slash, false, false)]
        [InlineData(AllowedHits.AnyHit, HitDamageType.Slash, false, true)]
        [InlineData(AllowedHits.AnyHit, HitDamageType.Pierce, false, true)]
        [InlineData(AllowedHits.AnyHit, HitDamageType.Other, false, true)]
        [InlineData(AllowedHits.AnyHit, HitDamageType.Blunt, true, true)]
        public void Allowed_hits_decide_which_hits_count(AllowedHits allowed, HitDamageType type, bool unarmed, bool counts)
        {
            var input = Punch(ThroatCenter());
            input.DamageType = type;
            input.IsUnarmed = unarmed;
            var settings = Settings();
            settings.AllowedHits = allowed;

            var result = LowBlowClassifier.Classify(input, settings);

            Assert.Equal(counts ? LowBlowRejection.None : LowBlowRejection.HitNotAllowed, result.Rejection);
        }

        [Fact]
        public void Too_slow_hit_is_refused_and_the_minimum_counts()
        {
            var slow = Punch(ThroatCenter());
            slow.ImpactSpeed = 1.49f;
            var atMinimum = Punch(ThroatCenter());
            atMinimum.ImpactSpeed = 1.5f;

            Assert.Equal(LowBlowRejection.TooSlow, Classify(slow).Rejection);
            Assert.True(Classify(atMinimum).IsHit);
        }

        [Fact]
        public void Min_hit_speed_zero_lets_a_touch_count()
        {
            var input = Punch(ThroatCenter());
            input.ImpactSpeed = 0f;
            var settings = Settings();
            settings.MinHitSpeed = 0f;

            Assert.True(LowBlowClassifier.Classify(input, settings).IsHit);
        }

        [Fact]
        public void Throat_turned_off_refuses_throat_hits_but_not_groin_hits()
        {
            var settings = Settings();
            settings.ThroatEnabled = false;

            Assert.Equal(LowBlowRejection.ZoneTurnedOff, LowBlowClassifier.Classify(Punch(ThroatCenter()), settings).Rejection);
            Assert.Equal(HitZone.Groin, LowBlowClassifier.Classify(Punch(GroinCenter()), settings).HitZone);
        }

        [Fact]
        public void Groin_turned_off_refuses_groin_hits_but_not_throat_hits()
        {
            var settings = Settings();
            settings.GroinEnabled = false;

            var groin = LowBlowClassifier.Classify(Punch(GroinCenter()), settings);
            Assert.Equal(LowBlowRejection.ZoneTurnedOff, groin.Rejection);
            Assert.Equal(HitZone.Groin, groin.NearestZone);
            Assert.Equal(HitZone.Throat, LowBlowClassifier.Classify(Punch(ThroatCenter()), settings).HitZone);
        }

        [Fact]
        public void Metal_on_the_neck_protects_the_throat_only()
        {
            var throat = Punch(ThroatCenter());
            throat.ThroatHasMetal = true;
            var groin = Punch(GroinCenter());
            groin.ThroatHasMetal = true;

            Assert.Equal(LowBlowRejection.MetalArmor, Classify(throat).Rejection);
            Assert.Equal(HitZone.Groin, Classify(groin).HitZone);
        }

        [Fact]
        public void Metal_on_the_hips_protects_the_groin_only()
        {
            var groin = Punch(GroinCenter());
            groin.GroinHasMetal = true;
            var throat = Punch(ThroatCenter());
            throat.GroinHasMetal = true;

            Assert.Equal(LowBlowRejection.MetalArmor, Classify(groin).Rejection);
            Assert.Equal(HitZone.Throat, Classify(throat).HitZone);
        }

        [Fact]
        public void Metal_does_not_protect_with_metal_armor_protects_off()
        {
            var input = Punch(GroinCenter());
            input.GroinHasMetal = true;
            var settings = Settings();
            settings.MetalArmorProtects = false;

            Assert.Equal(HitZone.Groin, LowBlowClassifier.Classify(input, settings).HitZone);
        }

        [Fact]
        public void Hit_from_behind_is_refused()
        {
            var input = Punch(ThroatCenter());
            input.AttackerPosition = new Vec3(0f, 0f, -1f);

            var result = Classify(input);

            Assert.Equal(LowBlowRejection.NotFromTheFront, result.Rejection);
            Assert.Equal(180f, result.AngleFromFront, 1);
        }

        [Fact]
        public void Hit_from_the_side_counts_within_the_angle()
        {
            var input = Punch(ThroatCenter());
            input.AttackerPosition = new Vec3(1f, 0f, 0f);

            var result = Classify(input);

            Assert.True(result.IsHit);
            Assert.Equal(90f, result.AngleFromFront, 1);
        }

        [Fact]
        public void Max_hit_angle_180_lets_hits_from_anywhere_count()
        {
            var input = Punch(GroinCenter());
            input.AttackerPosition = new Vec3(0f, 0f, -1f);
            var settings = Settings();
            settings.MaxHitAngle = 180f;

            Assert.True(LowBlowClassifier.Classify(input, settings).IsHit);
        }

        [Fact]
        public void Unknown_attacker_position_cannot_be_judged()
        {
            var input = Punch(ThroatCenter());
            input.HasAttackerPosition = false;

            Assert.Equal(LowBlowRejection.AttackAngleUndetermined, Classify(input).Rejection);
        }

        [Fact]
        public void Attacker_standing_on_the_npc_cannot_be_judged()
        {
            var input = Punch(ThroatCenter());
            input.AttackerPosition = new Vec3(0f, 2f, 0f);

            Assert.Equal(LowBlowRejection.AttackAngleUndetermined, Classify(input).Rejection);
        }

        [Fact]
        public void Missing_neck_leaves_only_the_groin()
        {
            var input = Punch(ThroatCenter());
            input.HasNeck = false;

            Assert.Equal(LowBlowRejection.OutsideZones, Classify(input).Rejection);
            var groin = Punch(GroinCenter());
            groin.HasNeck = false;
            Assert.Equal(HitZone.Groin, Classify(groin).HitZone);
        }

        [Fact]
        public void Missing_neck_and_hips_is_outside_every_zone()
        {
            var input = Punch(ThroatCenter());
            input.HasNeck = false;
            input.HasHips = false;

            var result = Classify(input);

            Assert.Equal(LowBlowRejection.OutsideZones, result.Rejection);
            Assert.Equal(HitZone.None, result.NearestZone);
        }
    }
}
