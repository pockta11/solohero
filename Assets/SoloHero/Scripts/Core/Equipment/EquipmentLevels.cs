using System;
using SoloHero.Core.Save;

namespace SoloHero.Core.Equipment
{
    /// <summary>
    /// Enhancement level per owned equipment id (D-062). Stored in <see cref="SaveDataV2.ownedEquipmentLevels"/>,
    /// index-aligned with <see cref="SaveDataV2.ownedEquipment"/>; older saves without levels read as 0.
    /// </summary>
    public static class EquipmentLevels
    {
        public static int Get(SaveDataV2 save, string id)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (string.IsNullOrEmpty(id)) return 0;
            int index = save.ownedEquipment.IndexOf(id);
            if (index < 0) return 0;
            Align(save);
            return save.ownedEquipmentLevels[index];
        }

        public static void Set(SaveDataV2 save, string id, int level)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            int index = save.ownedEquipment.IndexOf(id);
            if (index < 0) return;
            Align(save);
            save.ownedEquipmentLevels[index] = level < 0 ? 0 : level;
        }

        /// <summary>Adds a newly owned id at level 0, keeping both lists aligned.</summary>
        public static void AddOwned(SaveDataV2 save, string id)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            Align(save);
            save.ownedEquipment.Add(id);
            save.ownedEquipmentLevels.Add(0);
        }

        public static void Align(SaveDataV2 save)
        {
            if (save.ownedEquipmentLevels == null) save.ownedEquipmentLevels = new System.Collections.Generic.List<int>();
            while (save.ownedEquipmentLevels.Count < save.ownedEquipment.Count)
                save.ownedEquipmentLevels.Add(0);
            if (save.ownedEquipmentLevels.Count > save.ownedEquipment.Count)
                save.ownedEquipmentLevels.RemoveRange(save.ownedEquipment.Count, save.ownedEquipmentLevels.Count - save.ownedEquipment.Count);
        }
    }
}
