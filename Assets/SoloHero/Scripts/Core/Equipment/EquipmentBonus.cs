using System;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Equipment
{
    public readonly struct EquipmentBonus
    {
        private const string IdPrefix = "Equipment";
        private const double EmptyMult = 1d;
        private const double EmptyBoots = 0d;

        public readonly double SwordMult;
        public readonly double ArmorMult;
        public readonly double HelmMult;
        public readonly double BootsSpeedBonus;
        public readonly double BootsCritBonus;

        public EquipmentBonus(
            double swordMult,
            double armorMult,
            double helmMult,
            double bootsSpeedBonus,
            double bootsCritBonus)
        {
            SwordMult = swordMult;
            ArmorMult = armorMult;
            HelmMult = helmMult;
            BootsSpeedBonus = bootsSpeedBonus;
            BootsCritBonus = bootsCritBonus;
        }

        public static EquipmentBonus Resolve(BalanceValues balance, SaveDataV2 data)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (data == null) throw new ArgumentNullException(nameof(data));

            double bootsSpeed = EmptyBoots;
            double bootsCrit = EmptyBoots;
            if (TryParse(data.equippedBoots, EquipmentSlot.Boots, out Grade bootsGrade))
            {
                bootsSpeed = ByGrade(
                    bootsGrade,
                    balance.BOOTS_ATKSPD_C,
                    balance.BOOTS_ATKSPD_R,
                    balance.BOOTS_ATKSPD_E,
                    balance.BOOTS_ATKSPD_L,
                    EmptyBoots);
                bootsCrit = ByGrade(
                    bootsGrade,
                    balance.BOOTS_CRIT_C,
                    balance.BOOTS_CRIT_R,
                    balance.BOOTS_CRIT_E,
                    balance.BOOTS_CRIT_L,
                    EmptyBoots);
            }

            return new EquipmentBonus(
                Multiplier(balance, data.equippedSword, EquipmentSlot.Sword),
                Multiplier(balance, data.equippedArmor, EquipmentSlot.Armor),
                Multiplier(balance, data.equippedHelm, EquipmentSlot.Helm),
                bootsSpeed,
                bootsCrit);
        }

        private static double Multiplier(BalanceValues balance, string id, EquipmentSlot slot)
        {
            if (!TryParse(id, slot, out Grade grade))
                return EmptyMult;

            switch (slot)
            {
                case EquipmentSlot.Sword:
                    return ByGrade(grade, balance.SWORD_ATK_C, balance.SWORD_ATK_R, balance.SWORD_ATK_E, balance.SWORD_ATK_L, EmptyMult);
                case EquipmentSlot.Helm:
                    return ByGrade(grade, balance.HELM_DEF_C, balance.HELM_DEF_R, balance.HELM_DEF_E, balance.HELM_DEF_L, EmptyMult);
                case EquipmentSlot.Armor:
                    return ByGrade(grade, balance.ARMOR_HP_C, balance.ARMOR_HP_R, balance.ARMOR_HP_E, balance.ARMOR_HP_L, EmptyMult);
                default:
                    return EmptyMult;
            }
        }

        private static double ByGrade(
            Grade grade,
            double common,
            double rare,
            double epic,
            double legendary,
            double empty)
        {
            switch (grade)
            {
                case Grade.Common: return common;
                case Grade.Rare: return rare;
                case Grade.Epic: return epic;
                case Grade.Legendary: return legendary;
                default: return empty;
            }
        }

        private static bool TryParse(string id, EquipmentSlot expectedSlot, out Grade grade)
        {
            grade = default;
            if (string.IsNullOrEmpty(id))
                return false;

            string[] parts = id.Split('_');
            if (parts.Length != 3 || parts[0] != IdPrefix)
                return false;

            if (!TryNamedEnum(parts[1], out EquipmentSlot slot) || slot != expectedSlot)
                return false;

            return TryNamedEnum(parts[2], out grade);
        }

        private static bool TryNamedEnum<TEnum>(string token, out TEnum value) where TEnum : struct, Enum
        {
            if (!Enum.TryParse(token, false, out value))
                return false;

            return value.ToString() == token;
        }
    }
}
