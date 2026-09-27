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

        /// <summary>GDD grade colors: Common #9E9E9E, Rare #3D8BFF, Epic #A24BFF, Legendary #FFC531.</summary>
        public static Color GradeColor(Grade grade)
        {
            switch (grade)
            {
                case Grade.Common: return new Color32(0x9E, 0x9E, 0x9E, 0xFF);
                case Grade.Rare: return new Color32(0x3D, 0x8B, 0xFF, 0xFF);
                case Grade.Epic: return new Color32(0xA2, 0x4B, 0xFF, 0xFF);
                default: return new Color32(0xFF, 0xC5, 0x31, 0xFF);
            }
        }

        public static string GradeName(Grade grade)
        {
            switch (grade)
            {
                case Grade.Common: return Strings.Get("grade.common");
                case Grade.Rare: return Strings.Get("grade.rare");
                case Grade.Epic: return Strings.Get("grade.epic");
                default: return Strings.Get("grade.legendary");
            }
        }

        public static string SlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Sword: return Strings.Get("slot.sword");
                case EquipmentSlot.Helm: return Strings.Get("slot.helm");
                case EquipmentSlot.Armor: return Strings.Get("slot.armor");
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
