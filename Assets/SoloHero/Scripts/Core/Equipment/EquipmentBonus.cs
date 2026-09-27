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

            return FromGrades(
                balance,
                GradeOrNone(data.equippedSword, EquipmentSlot.Sword),
                GradeOrNone(data.equippedHelm, EquipmentSlot.Helm),
                GradeOrNone(data.equippedArmor, EquipmentSlot.Armor),
                GradeOrNone(data.equippedBoots, EquipmentSlot.Boots));
        }

        /// <summary>Grade per slot as <see cref="Grade"/> cast to int; -1 means the slot is empty.</summary>
        public static EquipmentBonus FromGrades(BalanceValues balance, int sword, int helm, int armor, int boots)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));

            double bootsSpeed = EmptyBoots;
            double bootsCrit = EmptyBoots;
            if (boots >= 0)
            {
                var bootsGrade = (Grade)boots;
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
                Multiplier(balance, sword, EquipmentSlot.Sword),
                Multiplier(balance, armor, EquipmentSlot.Armor),
                Multiplier(balance, helm, EquipmentSlot.Helm),
                bootsSpeed,
                bootsCrit);
        }

        /// <summary>Returns the grade of an equipped id as int, or -1 when empty or not a valid id for the slot.</summary>
        public static int GradeOrNone(string id, EquipmentSlot slot) =>
            TryParse(id, slot, out Grade grade) ? (int)grade : -1;

        private static double Multiplier(BalanceValues balance, int gradeIndex, EquipmentSlot slot)
        {
            if (gradeIndex < 0)
                return EmptyMult;

            var grade = (Grade)gradeIndex;

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
