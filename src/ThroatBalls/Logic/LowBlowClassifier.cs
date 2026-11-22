namespace ThroatBalls.Logic
{
    /// <summary>
    /// Decides whether a hit is a throat or groin hit. Pure, so every flow is unit-tested.
    /// Order: self hit, player vs NPC, alive, men only, allowed hits, speed, zone, zone on,
    /// metal armor, from the front.
    /// </summary>
    public static class LowBlowClassifier
    {
        public static LowBlowResult Classify(LowBlowInput input, LowBlowSettings settings)
        {
            var early = EarlyRejection(input, settings);
            if (early != LowBlowRejection.None)
            {
                return Refused(early, Zone.None, float.NaN);
            }

            var nearest = NearestZone(input, settings, out var distance, out var inside);
            if (!inside)
            {
                return Refused(LowBlowRejection.OutsideZones, nearest, distance);
            }
            if (!ZoneEnabled(nearest, settings))
            {
                return Refused(LowBlowRejection.ZoneTurnedOff, nearest, distance);
            }
            if (settings.MetalArmorProtects && HasMetal(nearest, input))
            {
                return Refused(LowBlowRejection.MetalArmor, nearest, distance);
            }

            var angle = AngleFromFront(input);
            if (settings.MaxHitAngle < 180f)
            {
                if (float.IsNaN(angle))
                {
                    return new LowBlowResult(Zone.None, LowBlowRejection.AttackAngleUndetermined, nearest, distance, angle);
                }
                if (angle > settings.MaxHitAngle)
                {
                    return new LowBlowResult(Zone.None, LowBlowRejection.NotFromTheFront, nearest, distance, angle);
                }
            }
            return new LowBlowResult(nearest, LowBlowRejection.None, nearest, distance, angle);
        }

        /// <summary>The checks that need no geometry.</summary>
        private static LowBlowRejection EarlyRejection(LowBlowInput input, LowBlowSettings settings)
        {
            if (input.AttackerIsTarget)
            {
                return LowBlowRejection.SelfHit;
            }
            if (!input.AttackerIsPlayer || input.TargetIsPlayer)
            {
                return LowBlowRejection.NotPlayerVsNpc;
            }
            if (input.TargetIsKilled)
            {
                return LowBlowRejection.TargetDead;
            }
            if (settings.MenOnly && !input.TargetIsMale)
            {
                return LowBlowRejection.NotMale;
            }
            if (!HitAllowed(input, settings.AllowedHits))
            {
                return LowBlowRejection.HitNotAllowed;
            }
            if (input.ImpactSpeed < settings.MinHitSpeed)
            {
                return LowBlowRejection.TooSlow;
            }
            return LowBlowRejection.None;
        }

        private static bool HitAllowed(LowBlowInput input, AllowedHits allowed)
        {
            switch (allowed)
            {
                case AllowedHits.AnyHit:
                    return true;
                case AllowedHits.FistsAndKicksOnly:
                    return input.IsUnarmed;
                default:
                    return input.DamageType == HitDamageType.Blunt;
            }
        }

        /// <summary>The zone whose center is nearest to the contact, and whether the contact lies inside it.</summary>
        private static Zone NearestZone(LowBlowInput input, LowBlowSettings settings, out float distance, out bool inside)
        {
            var nearest = Zone.None;
            distance = float.NaN;
            inside = false;
            if (input.HasNeck)
            {
                var center = LowBlowZones.ThroatCenter(input.Neck, input.TargetForward, settings);
                distance = Vec3.Distance(input.ContactPoint, center);
                nearest = Zone.Throat;
                inside = distance <= settings.ThroatZoneSize;
            }
            if (input.HasHips)
            {
                var center = LowBlowZones.GroinCenter(input.Hips, input.TargetForward, settings);
                var groin = Vec3.Distance(input.ContactPoint, center);
                if (nearest == Zone.None || groin < distance)
                {
                    distance = groin;
                    nearest = Zone.Groin;
                    inside = groin <= settings.GroinZoneSize;
                }
            }
            return nearest;
        }

        private static bool ZoneEnabled(Zone zone, LowBlowSettings settings)
        {
            return zone == Zone.Throat ? settings.ThroatEnabled : settings.GroinEnabled;
        }

        private static bool HasMetal(Zone zone, LowBlowInput input)
        {
            return zone == Zone.Throat ? input.ThroatHasMetal : input.GroinHasMetal;
        }

        /// <summary>Horizontal angle between where the NPC faces and where the attacker stands; NaN if unknown.</summary>
        private static float AngleFromFront(LowBlowInput input)
        {
            if (!input.HasAttackerPosition)
            {
                return float.NaN;
            }
            var toAttacker = (input.AttackerPosition - input.TargetPosition).Flattened;
            return Vec3.AngleDegrees(input.TargetForward.Flattened, toAttacker);
        }

        private static LowBlowResult Refused(LowBlowRejection rejection, Zone nearest, float distance)
        {
            return new LowBlowResult(Zone.None, rejection, nearest, distance, float.NaN);
        }
    }
}
