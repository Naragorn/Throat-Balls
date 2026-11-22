using System.Collections.Generic;
using System.Text;
using ThroatBalls.Logic;
using ThunderRoad;
using UnityEngine;

namespace ThroatBalls
{
    /// <summary>
    /// Turns ThunderRoad / Unity objects into the plain values the logic works on.
    /// No decisions are made here; those live in <see cref="LowBlowClassifier"/>.
    /// </summary>
    internal static class GameAdapters
    {
        public static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);

        public static Vector3 ToVector3(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        public static HitDamageType ToHitDamageType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Slash:
                    return HitDamageType.Slash;
                case DamageType.Pierce:
                    return HitDamageType.Pierce;
                case DamageType.Blunt:
                    return HitDamageType.Blunt;
                default:
                    return HitDamageType.Other;
            }
        }

        /// <summary>The neck part (the throat zone sits in front of it).</summary>
        public static RagdollPart NeckOf(Creature creature)
        {
            return creature != null && creature.ragdoll != null ? creature.ragdoll.GetPart(RagdollPart.Type.Neck) : null;
        }

        /// <summary>
        /// The hips: the ragdoll's root part (the game casts to the ground from it, Ragdoll 1.3.2), else the lowest torso part.
        /// That the root is the pelvis on human NPCs is assumed; Show Zones shows it.
        /// </summary>
        public static RagdollPart HipsOf(Creature creature)
        {
            if (creature == null || creature.ragdoll == null)
            {
                return null;
            }
            return creature.ragdoll.rootPart != null
                ? creature.ragdoll.rootPart
                : creature.ragdoll.GetPart(RagdollPart.Type.Torso, RagdollPart.Section.Lower);
        }

        /// <summary>
        /// Who dealt the hit, as Heel of Achilles resolves it: a held or thrown item's last handler,
        /// else the creature of a hitting body part (punch, kick), else the spell's caster.
        /// </summary>
        public static Creature Attacker(CollisionInstance hit)
        {
            var handler = hit.sourceColliderGroup != null ? hit.sourceColliderGroup.collisionHandler : null;
            if (handler != null)
            {
                var hand = handler.item != null ? handler.item.lastHandler : null;
                if (hand != null && hand.creature != null)
                {
                    return hand.creature;
                }
                var part = handler.ragdollPart;
                if (part != null && part.ragdoll != null && part.ragdoll.creature != null)
                {
                    return part.ragdoll.creature;
                }
            }
            var caster = hit.casterHand;
            return caster != null && caster.mana != null ? caster.mana.creature : null;
        }

        /// <summary>The attacker's own body part dealt the hit: a fist or a foot, no item, no spell.</summary>
        public static bool IsUnarmed(CollisionInstance hit)
        {
            var handler = hit.sourceColliderGroup != null ? hit.sourceColliderGroup.collisionHandler : null;
            return hit.casterHand == null && handler != null && handler.item == null && handler.ragdollPart != null;
        }

        /// <summary>
        /// Metal armor worn on a part: the game's own test (RagdollPart.HasMetalArmor, 1.3.2) is the wardrobe
        /// module's isMetal flag of the items on the part's wardrobe channel. Leather and cloth are not metal.
        /// </summary>
        public static bool HasMetal(Creature creature, RagdollPart part)
        {
            var items = WornOn(creature, part);
            if (items == null)
            {
                return false;
            }
            foreach (var item in items)
            {
                var wardrobe = item != null && item.data != null ? item.data.GetModule<ItemModuleWardrobe>() : null;
                if (wardrobe != null && wardrobe.isMetal)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The groin is covered by what is worn on the legs: the game's legs channel holds the Pants, Skirt, Armor
        /// and Underwear layers (ArmorStats 1.3.2). Both thighs are asked. The hips part is not: its channel is not
        /// known offline, and if it were the torso's, a chest plate would wrongly shield the groin (Debug Logging
        /// prints the hips' channel to settle it).
        /// </summary>
        public static bool GroinHasMetal(Creature creature)
        {
            var ragdoll = creature.ragdoll;
            return HasMetal(creature, ThighOf(ragdoll, RagdollPart.Type.LeftLeg))
                || HasMetal(creature, ThighOf(ragdoll, RagdollPart.Type.RightLeg));
        }

        public static RagdollPart ThighOf(Ragdoll ragdoll, RagdollPart.Type leg)
        {
            return ragdoll != null ? ragdoll.GetPart(leg, RagdollPart.Section.Upper) : null;
        }

        /// <summary>For Debug Logging: the channel of a part and what is worn there, e.g. "Legs [PantsLeather, -]".</summary>
        public static string DescribeArmor(Creature creature, RagdollPart part)
        {
            if (part == null || part.wearable == null)
            {
                return "no wardrobe";
            }
            var builder = new StringBuilder(part.wearable.wardrobeChannel).Append(" [");
            var items = WornOn(creature, part);
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var data = items[i] != null ? items[i].data : null;
                    var wardrobe = data != null ? data.GetModule<ItemModuleWardrobe>() : null;
                    builder.Append(i > 0 ? ", " : string.Empty).Append(data != null ? data.id : "?");
                    builder.Append(wardrobe != null && wardrobe.isMetal ? " (metal)" : string.Empty);
                }
            }
            return builder.Append(']').ToString();
        }

        /// <summary>Items on the part's wardrobe channel; null without a wardrobe (the game would log a warning).</summary>
        private static List<ItemContent> WornOn(Creature creature, RagdollPart part)
        {
            if (creature == null || creature.equipment == null || part == null || part.wearable == null)
            {
                return null;
            }
            return creature.equipment.GetEquipmentOnPart(part);
        }

        public static LowBlowInput BuildInput(Creature target, CollisionInstance hit, Creature attacker)
        {
            var neck = NeckOf(target);
            var hips = HipsOf(target);
            var input = new LowBlowInput
            {
                AttackerIsPlayer = attacker != null && attacker.isPlayer,
                TargetIsPlayer = target.isPlayer,
                AttackerIsTarget = attacker != null && attacker == target,
                TargetIsKilled = target.isKilled,
                TargetIsMale = target.data != null && target.data.gender == CreatureData.Gender.Male,
                IsUnarmed = IsUnarmed(hit),
                DamageType = ToHitDamageType(hit.damageStruct.damageType),
                ImpactSpeed = hit.impactVelocity.magnitude,
                ContactPoint = ToVec3(hit.contactPoint),
                TargetPosition = ToVec3(target.transform.position),
                TargetForward = ToVec3(target.transform.forward),
                HasNeck = neck != null,
                HasHips = hips != null,
                HasAttackerPosition = attacker != null,
            };
            if (neck != null)
            {
                input.Neck = ToVec3(neck.transform.position);
                input.ThroatHasMetal = HasMetal(target, neck);
            }
            if (hips != null)
            {
                input.Hips = ToVec3(hips.transform.position);
                input.GroinHasMetal = GroinHasMetal(target);
            }
            if (attacker != null)
            {
                input.AttackerPosition = ToVec3(attacker.transform.position);
            }
            return input;
        }

        public static string Label(Creature creature)
        {
            var id = string.IsNullOrEmpty(creature.creatureId) ? creature.name : creature.creatureId;
            return id + " #" + creature.GetInstanceID();
        }
    }
}
