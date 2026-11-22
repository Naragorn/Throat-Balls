namespace ThroatBalls.Logic
{
    /// <summary>The spot a hit landed on.</summary>
    public enum HitZone
    {
        None,
        Throat,
        Groin,
    }

    /// <summary>Damage type of a hit, reduced to what the check cares about.</summary>
    public enum HitDamageType
    {
        Other,
        Slash,
        Pierce,
        Blunt,
    }

    /// <summary>
    /// Which hits count. Keep <see cref="BluntOnly"/> first: the game's mod menu cannot match an
    /// enum default and always falls back to the first entry.
    /// </summary>
    public enum AllowedHits
    {
        /// <summary>Fists, kicks, maces, hammers, pommels: anything blunt.</summary>
        BluntOnly = 0,

        /// <summary>Only the player's own fists and feet.</summary>
        FistsAndKicksOnly = 1,

        /// <summary>Any hit, blades too.</summary>
        AnyHit = 2,
    }

    /// <summary>Why a hit did not count. Checked in this order; the first failing one is the reason.</summary>
    public enum LowBlowRejection
    {
        None,

        /// <summary>The creature hit itself. Never counts and is not logged.</summary>
        SelfHit,

        /// <summary>Not the player hitting an NPC: NPC vs NPC, any hit on the player, or no known attacker.</summary>
        NotPlayerVsNpc,
        TargetDead,
        NotMale,
        HitNotAllowed,
        TooSlow,
        OutsideZones,
        ZoneTurnedOff,
        MetalArmor,
        AttackAngleUndetermined,
        NotFromTheFront,
    }

    /// <summary>Tuning values, all of them Mod Menu settings. Lengths in meters, angles in degrees.</summary>
    public sealed class LowBlowSettings
    {
        public bool ThroatEnabled;
        public bool GroinEnabled;
        public bool MenOnly;
        public bool MetalArmorProtects;
        public AllowedHits AllowedHits;

        /// <summary>Lowest impact speed (m/s) that counts.</summary>
        public float MinHitSpeed;

        public float ThroatZoneSize;
        public float ThroatForwardOffset;
        public float ThroatHeightOffset;
        public float GroinZoneSize;
        public float GroinForwardOffset;
        public float GroinHeightOffset;

        /// <summary>Largest angle between where the NPC faces and where the attacker stands; 180 = anywhere.</summary>
        public float MaxHitAngle;
    }

    /// <summary>Plain values the game side reads from one hit.</summary>
    public struct LowBlowInput
    {
        public bool AttackerIsPlayer;
        public bool TargetIsPlayer;
        public bool AttackerIsTarget;
        public bool TargetIsKilled;
        public bool TargetIsMale;

        /// <summary>The player's own fist or foot dealt the hit (no item, no spell).</summary>
        public bool IsUnarmed;

        public HitDamageType DamageType;
        public float ImpactSpeed;
        public Vec3 ContactPoint;
        public Vec3 TargetPosition;
        public Vec3 TargetForward;
        public bool HasNeck;
        public Vec3 Neck;
        public bool HasHips;
        public Vec3 Hips;

        /// <summary>Metal armor is worn on the neck part.</summary>
        public bool ThroatHasMetal;

        /// <summary>Metal armor is worn on the hips part.</summary>
        public bool GroinHasMetal;

        public bool HasAttackerPosition;
        public Vec3 AttackerPosition;
    }

    /// <summary>Outcome of one hit: the zone it counts for, or why it does not.</summary>
    public readonly struct LowBlowResult
    {
        public LowBlowResult(HitZone zone, LowBlowRejection rejection, HitZone nearestZone, float zoneDistance, float angleFromFront)
        {
            HitZone = zone;
            Rejection = rejection;
            NearestZone = nearestZone;
            ZoneDistance = zoneDistance;
            AngleFromFront = angleFromFront;
        }

        /// <summary>The zone the hit counts for; None when refused.</summary>
        public HitZone HitZone { get; }

        public LowBlowRejection Rejection { get; }

        /// <summary>The zone whose center is nearest to the contact (also for refused hits, for the log).</summary>
        public HitZone NearestZone { get; }

        /// <summary>Distance from the contact to the nearest zone's center (m); NaN before the zone check.</summary>
        public float ZoneDistance { get; }

        /// <summary>Horizontal angle between the NPC's facing and the attacker (degrees); NaN when not judged.</summary>
        public float AngleFromFront { get; }

        public bool IsHit => Rejection == LowBlowRejection.None;
    }

    /// <summary>Where the two zones sit on a body: offsets along the NPC's horizontal facing and straight up.</summary>
    public static class LowBlowZones
    {
        public static Vec3 ThroatCenter(Vec3 neck, Vec3 forward, LowBlowSettings settings)
        {
            return Offset(neck, forward, settings.ThroatForwardOffset, settings.ThroatHeightOffset);
        }

        public static Vec3 GroinCenter(Vec3 hips, Vec3 forward, LowBlowSettings settings)
        {
            return Offset(hips, forward, settings.GroinForwardOffset, settings.GroinHeightOffset);
        }

        private static Vec3 Offset(Vec3 origin, Vec3 forward, float ahead, float up)
        {
            return origin + forward.Flattened.Normalized * ahead + Vec3.Up * up;
        }
    }
}
