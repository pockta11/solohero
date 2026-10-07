using System;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>Shared lookups and formatting for the growth panels.</summary>
    public static class PanelServices
    {
        public static T TryGet<T>() where T : class
        {
            try
            {
                return Services.Get<T>();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// GDD grade colours on the D-113 seven-grade ladder: Common #9E9E9E, Uncommon #4CC35A, Rare #3D8BFF,
        /// Epic #A24BFF, Legendary #FFC531, Mythic #FF4D5E, Ancient #2FE0CC. Skills use the same colours for their four.
        /// </summary>
        public static Color GradeColor(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.Common: return new Color32(0x9E, 0x9E, 0x9E, 0xFF);
                case GearGrade.Uncommon: return new Color32(0x4C, 0xC3, 0x5A, 0xFF);
                case GearGrade.Rare: return new Color32(0x3D, 0x8B, 0xFF, 0xFF);
                case GearGrade.Epic: return new Color32(0xA2, 0x4B, 0xFF, 0xFF);
                case GearGrade.Mythic: return new Color32(0xFF, 0x4D, 0x5E, 0xFF);
                case GearGrade.Ancient: return new Color32(0x2F, 0xE0, 0xCC, 0xFF);
                default: return new Color32(0xFF, 0xC5, 0x31, 0xFF);
            }
        }

        public static Color GradeColor(Grade grade) => GradeColor(grade.ToGearGrade());

        public static string GradeName(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.Common: return Strings.Get("grade.common");
                case GearGrade.Uncommon: return Strings.Get("grade.uncommon");
                case GearGrade.Rare: return Strings.Get("grade.rare");
                case GearGrade.Epic: return Strings.Get("grade.epic");
                case GearGrade.Mythic: return Strings.Get("grade.mythic");
                case GearGrade.Ancient: return Strings.Get("grade.ancient");
                default: return Strings.Get("grade.legendary");
            }
        }

        public static string GradeName(Grade grade) => GradeName(grade.ToGearGrade());

        public static string SlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Sword: return Strings.Get("slot.sword");
                case EquipmentSlot.Helm: return Strings.Get("slot.helm");
                case EquipmentSlot.Armor: return Strings.Get("slot.armor");
                case EquipmentSlot.Gloves: return Strings.Get("slot.gloves");
                case EquipmentSlot.Necklace: return Strings.Get("slot.necklace");
                case EquipmentSlot.Ring: return Strings.Get("slot.ring");
                case EquipmentSlot.Earring: return Strings.Get("slot.earring");
                default: return Strings.Get("slot.boots");
            }
        }

        /// <summary>What happened to a pulled item: enhanced, refunded, equipped or stored.</summary>
        public static string PullNote(GachaPullItem item)
        {
            if (item.EnhancedLevel > 0) return Strings.Format("gacha.enhanced", item.EnhancedLevel);
            if (item.WasDuplicate) return Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold));
            return Strings.Get(item.AutoEquipped ? "gacha.equipped" : "gacha.stored");
        }
    }
}
