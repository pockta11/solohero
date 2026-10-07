using System;
using SoloHero.Core;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Equipment
{
    /// <summary>
    /// What the equipped gear and the gear collection give. Sword ATK x, helm DEF x, armor HP x, boots attack speed
    /// and crit; D-109 accessories: gloves ATK x, necklace HP x, ring crit damage, earring skill damage; and the owned
    /// bonus (every owned item ATK +x%, equipped or not). Duplicates enhance an item (D-062): the grade's effect
    /// grows by EQUIP_ENHANCE_GAIN per level. Grades are the seven-tier <see cref="GearGrade"/> (D-113).
    /// </summary>
    public readonly struct EquipmentBonus
    {
        private const string IdPrefix = "Equipment";
        private const double EmptyMult = 1d;

        public readonly double SwordMult;
        public readonly double ArmorMult;
        public readonly double HelmMult;
        public readonly double BootsSpeedBonus;
        public readonly double BootsCritBonus;
        public readonly double GlovesMult;
        public readonly double NecklaceMult;

        /// <summary>Added to the crit multiplier (0.25 = +25 percentage points of a hit).</summary>
        public readonly double RingCritDamage;

        /// <summary>Skill damage fraction (0.12 = skills deal 12% more).</summary>
        public readonly double EarringSkillDamage;

        /// <summary>Owned bonus as an ATK fraction (0.05 = +5%).</summary>
        public readonly double OwnedAtk;

        public EquipmentBonus(
            double swordMult,
            double armorMult,
            double helmMult,
            double bootsSpeedBonus,
            double bootsCritBonus,
            double glovesMult = EmptyMult,
            double necklaceMult = EmptyMult,
            double ringCritDamage = 0d,
            double earringSkillDamage = 0d,
            double ownedAtk = 0d)
        {
            SwordMult = swordMult;
            ArmorMult = armorMult;
            HelmMult = helmMult;
            BootsSpeedBonus = bootsSpeedBonus;
            BootsCritBonus = bootsCritBonus;
            GlovesMult = glovesMult;
            NecklaceMult = necklaceMult;
            RingCritDamage = ringCritDamage;
            EarringSkillDamage = earringSkillDamage;
            OwnedAtk = ownedAtk;
        }

        /// <summary>Every ATK multiplier of the gear (sword x gloves).</summary>
        public double AtkMult => SwordMult * GlovesMult;

        /// <summary>Every HP multiplier of the gear (armor x necklace).</summary>
        public double HpMult => ArmorMult * NecklaceMult;

        public static EquipmentBonus Resolve(BalanceValues balance, SaveDataV2 data)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var grades = new int[GachaCatalog.SlotCount];
            var levels = new int[GachaCatalog.SlotCount];
            for (int s = 0; s < grades.Length; s++)
            {
                string id = EquippedSlots.Get(data, (EquipmentSlot)s);
                grades[s] = GradeOrNone(id, (EquipmentSlot)s);
                levels[s] = grades[s] < 0 ? 0 : EquipmentLevels.Get(data, id);
            }

            return FromGrades(balance, grades, levels, OwnedAtkBonus(balance, data));
        }

        /// <summary>The v1.0 four slots only (accessories empty). <see cref="GearGrade"/> as int; -1 means the slot is empty.</summary>
        public static EquipmentBonus FromGrades(BalanceValues balance, int sword, int helm, int armor, int boots,
            int swordLevel = 0, int helmLevel = 0, int armorLevel = 0, int bootsLevel = 0)
        {
            var grades = new int[GachaCatalog.SlotCount];
            var levels = new int[GachaCatalog.SlotCount];
            for (int s = 0; s < grades.Length; s++) grades[s] = -1;
            grades[(int)EquipmentSlot.Sword] = sword;
            grades[(int)EquipmentSlot.Helm] = helm;
            grades[(int)EquipmentSlot.Armor] = armor;
            grades[(int)EquipmentSlot.Boots] = boots;
            levels[(int)EquipmentSlot.Sword] = swordLevel;
            levels[(int)EquipmentSlot.Helm] = helmLevel;
            levels[(int)EquipmentSlot.Armor] = armorLevel;
            levels[(int)EquipmentSlot.Boots] = bootsLevel;
            return FromGrades(balance, grades, levels);
        }

        /// <summary>
        /// Grade per slot (index = <see cref="EquipmentSlot"/>, -1 empty) and enhance level per slot; missing entries
        /// are empty. <paramref name="ownedAtk"/> is passed through (see <see cref="OwnedAtkBonus"/>).
        /// </summary>
        public static EquipmentBonus FromGrades(BalanceValues balance, int[] grades, int[] levels, double ownedAtk = 0d)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));

            int Grade(EquipmentSlot slot) => grades != null && (int)slot < grades.Length ? grades[(int)slot] : -1;
            int Level(EquipmentSlot slot) => levels != null && (int)slot < levels.Length ? levels[(int)slot] : 0;

            return new EquipmentBonus(
                Multiplier(balance, EquipmentSlot.Sword, Grade(EquipmentSlot.Sword), Level(EquipmentSlot.Sword)),
                Multiplier(balance, EquipmentSlot.Armor, Grade(EquipmentSlot.Armor), Level(EquipmentSlot.Armor)),
                Multiplier(balance, EquipmentSlot.Helm, Grade(EquipmentSlot.Helm), Level(EquipmentSlot.Helm)),
                Additive(balance, EquipmentSlot.Boots, Grade(EquipmentSlot.Boots), Level(EquipmentSlot.Boots)),
                BootsCrit(balance, Grade(EquipmentSlot.Boots), Level(EquipmentSlot.Boots)),
                Multiplier(balance, EquipmentSlot.Gloves, Grade(EquipmentSlot.Gloves), Level(EquipmentSlot.Gloves)),
                Multiplier(balance, EquipmentSlot.Necklace, Grade(EquipmentSlot.Necklace), Level(EquipmentSlot.Necklace)),
                Additive(balance, EquipmentSlot.Ring, Grade(EquipmentSlot.Ring), Level(EquipmentSlot.Ring)),
                Additive(balance, EquipmentSlot.Earring, Grade(EquipmentSlot.Earring), Level(EquipmentSlot.Earring)),
                ownedAtk);
        }

        /// <summary>D-109 owned bonus: the sum over owned items of EQUIP_OWNED_ATK_{grade}% x enhance, as a fraction.</summary>
        public static double OwnedAtkBonus(BalanceValues balance, SaveDataV2 data)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (data == null || data.ownedEquipment == null) return 0d;
            double sum = 0d;
            for (int i = 0; i < data.ownedEquipment.Count; i++)
            {
                if (!TryParseAny(data.ownedEquipment[i], out _, out GearGrade grade)) continue;
                if (data.ownedEquipment.IndexOf(data.ownedEquipment[i]) != i) continue; // a stray duplicate counts once
                int level = data.ownedEquipmentLevels != null && i < data.ownedEquipmentLevels.Count ? data.ownedEquipmentLevels[i] : 0;
                sum += OwnedAtkPercent(balance, grade, level);
            }

            return sum / 100d;
        }

        /// <summary>One item's owned bonus in percent.</summary>
        public static double OwnedAtkPercent(BalanceValues balance, GearGrade grade, int level) =>
            Formulas.EnhancedEffect(balance,
                ByGrade(grade, balance.EQUIP_OWNED_ATK_C, balance.EQUIP_OWNED_ATK_U, balance.EQUIP_OWNED_ATK_R,
                    balance.EQUIP_OWNED_ATK_E, balance.EQUIP_OWNED_ATK_L, balance.EQUIP_OWNED_ATK_M, balance.EQUIP_OWNED_ATK_A, 0d),
                level);

        /// <summary>D-109: the ring and earring reach combat through the talent channel (crit damage, skill damage).</summary>
        public void AddTo(TalentEffects effects)
        {
            if (effects == null) return;
            effects.CritDamage += RingCritDamage;
            effects.SkillDamagePct += EarringSkillDamage;
        }

        /// <summary>Returns the grade of an equipped id as int, or -1 when empty or not a valid id for the slot.</summary>
        public static int GradeOrNone(string id, EquipmentSlot slot) =>
            TryParse(id, slot, out GearGrade grade) ? (int)grade : -1;

        /// <summary>The slot's own effect at a grade (and enhance level): x for multiplier slots, + for the rest.</summary>
        public static double SlotEffect(BalanceValues balance, EquipmentSlot slot, GearGrade grade, int level)
        {
            switch (slot)
            {
                case EquipmentSlot.Boots:
                case EquipmentSlot.Ring:
                case EquipmentSlot.Earring:
                    return Additive(balance, slot, (int)grade, level);
                default:
                    return Multiplier(balance, slot, (int)grade, level);
            }
        }

        private static double Multiplier(BalanceValues balance, EquipmentSlot slot, int gradeIndex, int level)
        {
            if (gradeIndex < 0)
                return EmptyMult;

            return EmptyMult + Formulas.EnhancedEffect(balance, GradeMultiplier(balance, gradeIndex, slot) - EmptyMult, level);
        }

        /// <summary>The additive slots: boots attack speed, ring crit damage, earring skill damage.</summary>
        private static double Additive(BalanceValues balance, EquipmentSlot slot, int gradeIndex, int level)
        {
            if (gradeIndex < 0)
                return 0d;

            var grade = (GearGrade)gradeIndex;
            double effect;
            switch (slot)
            {
                case EquipmentSlot.Boots:
                    effect = ByGrade(grade, balance.BOOTS_ATKSPD_C, balance.BOOTS_ATKSPD_U, balance.BOOTS_ATKSPD_R,
                        balance.BOOTS_ATKSPD_E, balance.BOOTS_ATKSPD_L, balance.BOOTS_ATKSPD_M, balance.BOOTS_ATKSPD_A, 0d);
                    break;
                case EquipmentSlot.Ring:
                    effect = ByGrade(grade, balance.RING_CRITDMG_C, balance.RING_CRITDMG_U, balance.RING_CRITDMG_R,
                        balance.RING_CRITDMG_E, balance.RING_CRITDMG_L, balance.RING_CRITDMG_M, balance.RING_CRITDMG_A, 0d);
                    break;
                case EquipmentSlot.Earring:
                    effect = ByGrade(grade, balance.EARRING_SKILL_C, balance.EARRING_SKILL_U, balance.EARRING_SKILL_R,
                        balance.EARRING_SKILL_E, balance.EARRING_SKILL_L, balance.EARRING_SKILL_M, balance.EARRING_SKILL_A, 0d);
                    break;
                default:
                    return 0d;
            }

            return Formulas.EnhancedEffect(balance, effect, level);
        }

        /// <summary>The boots' second effect, crit rate in percentage points.</summary>
        private static double BootsCrit(BalanceValues balance, int gradeIndex, int level)
        {
            if (gradeIndex < 0)
                return 0d;

            double effect = ByGrade((GearGrade)gradeIndex, balance.BOOTS_CRIT_C, balance.BOOTS_CRIT_U, balance.BOOTS_CRIT_R,
                balance.BOOTS_CRIT_E, balance.BOOTS_CRIT_L, balance.BOOTS_CRIT_M, balance.BOOTS_CRIT_A, 0d);
            return Formulas.EnhancedEffect(balance, effect, level);
        }

        private static double GradeMultiplier(BalanceValues balance, int gradeIndex, EquipmentSlot slot)
        {
            var grade = (GearGrade)gradeIndex;

            switch (slot)
            {
                case EquipmentSlot.Sword:
                    return ByGrade(grade, balance.SWORD_ATK_C, balance.SWORD_ATK_U, balance.SWORD_ATK_R, balance.SWORD_ATK_E,
                        balance.SWORD_ATK_L, balance.SWORD_ATK_M, balance.SWORD_ATK_A, EmptyMult);
                case EquipmentSlot.Helm:
                    return ByGrade(grade, balance.HELM_DEF_C, balance.HELM_DEF_U, balance.HELM_DEF_R, balance.HELM_DEF_E,
                        balance.HELM_DEF_L, balance.HELM_DEF_M, balance.HELM_DEF_A, EmptyMult);
                case EquipmentSlot.Armor:
                    return ByGrade(grade, balance.ARMOR_HP_C, balance.ARMOR_HP_U, balance.ARMOR_HP_R, balance.ARMOR_HP_E,
                        balance.ARMOR_HP_L, balance.ARMOR_HP_M, balance.ARMOR_HP_A, EmptyMult);
                case EquipmentSlot.Gloves:
                    return ByGrade(grade, balance.GLOVES_ATK_C, balance.GLOVES_ATK_U, balance.GLOVES_ATK_R, balance.GLOVES_ATK_E,
                        balance.GLOVES_ATK_L, balance.GLOVES_ATK_M, balance.GLOVES_ATK_A, EmptyMult);
                case EquipmentSlot.Necklace:
                    return ByGrade(grade, balance.NECKLACE_HP_C, balance.NECKLACE_HP_U, balance.NECKLACE_HP_R, balance.NECKLACE_HP_E,
                        balance.NECKLACE_HP_L, balance.NECKLACE_HP_M, balance.NECKLACE_HP_A, EmptyMult);
                default:
                    return EmptyMult;
            }
        }

        private static double ByGrade(
            GearGrade grade,
            double common,
            double uncommon,
            double rare,
            double epic,
            double legendary,
            double mythic,
            double ancient,
            double empty)
        {
            switch (grade)
            {
                case GearGrade.Common: return common;
                case GearGrade.Uncommon: return uncommon;
                case GearGrade.Rare: return rare;
                case GearGrade.Epic: return epic;
                case GearGrade.Legendary: return legendary;
                case GearGrade.Mythic: return mythic;
                case GearGrade.Ancient: return ancient;
                default: return empty;
            }
        }

        private static bool TryParse(string id, EquipmentSlot expectedSlot, out GearGrade grade) =>
            TryParseAny(id, out EquipmentSlot slot, out grade) && slot == expectedSlot;

        /// <summary>"Equipment_{Slot}_{Grade}" into its slot and grade; false for anything else.</summary>
        public static bool TryParseAny(string id, out EquipmentSlot slot, out GearGrade grade)
        {
            slot = default;
            grade = default;
            if (string.IsNullOrEmpty(id))
                return false;

            string[] parts = id.Split('_');
            if (parts.Length != 3 || parts[0] != IdPrefix)
                return false;

            return TryNamedEnum(parts[1], out slot) && TryNamedEnum(parts[2], out grade);
        }

        private static bool TryNamedEnum<TEnum>(string token, out TEnum value) where TEnum : struct, Enum
        {
            if (!Enum.TryParse(token, false, out value))
                return false;

            return value.ToString() == token;
        }
    }
}
