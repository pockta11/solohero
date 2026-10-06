using System;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Equipment
{
    /// <summary>The save's equipped id per slot ("" empty). The single place that maps a slot to its save field.</summary>
    public static class EquippedSlots
    {
        public static string Get(SaveDataV2 data, EquipmentSlot slot)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            string id;
            switch (slot)
            {
                case EquipmentSlot.Sword: id = data.equippedSword; break;
                case EquipmentSlot.Helm: id = data.equippedHelm; break;
                case EquipmentSlot.Armor: id = data.equippedArmor; break;
                case EquipmentSlot.Boots: id = data.equippedBoots; break;
                case EquipmentSlot.Gloves: id = data.equippedGloves; break;
                case EquipmentSlot.Necklace: id = data.equippedNecklace; break;
                case EquipmentSlot.Ring: id = data.equippedRing; break;
                case EquipmentSlot.Earring: id = data.equippedEarring; break;
                default: id = ""; break;
            }

            return id ?? "";
        }

        public static void Set(SaveDataV2 data, EquipmentSlot slot, string id)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            id ??= "";
            switch (slot)
            {
                case EquipmentSlot.Sword: data.equippedSword = id; break;
                case EquipmentSlot.Helm: data.equippedHelm = id; break;
                case EquipmentSlot.Armor: data.equippedArmor = id; break;
                case EquipmentSlot.Boots: data.equippedBoots = id; break;
                case EquipmentSlot.Gloves: data.equippedGloves = id; break;
                case EquipmentSlot.Necklace: data.equippedNecklace = id; break;
                case EquipmentSlot.Ring: data.equippedRing = id; break;
                case EquipmentSlot.Earring: data.equippedEarring = id; break;
            }
        }

        /// <summary>All equipped ids joined; views compare it to notice a change without allocating per frame.</summary>
        public static string Key(SaveDataV2 data) =>
            data.equippedSword + "|" + data.equippedHelm + "|" + data.equippedArmor + "|" + data.equippedBoots + "|"
            + data.equippedGloves + "|" + data.equippedNecklace + "|" + data.equippedRing + "|" + data.equippedEarring;
    }
}
