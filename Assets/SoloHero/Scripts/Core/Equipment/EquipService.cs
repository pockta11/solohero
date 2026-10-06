using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Equipment
{
    public sealed class EquipService
    {
        private readonly ISaveRequester _save;

        public EquipService(ISaveRequester save = null)
        {
            _save = save;
        }

        public Result TryEquip(SaveDataV2 save, EquipmentSlot slot, string id)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            if (string.IsNullOrEmpty(id) || !ContainsId(save.ownedEquipment, id))
                return Result.Fail(FailReason.Locked);

            if (GetEquipped(save, slot) == id)
                return Result.Success;

            SetEquipped(save, slot, id);
            _save?.RequestSave();
            return Result.Success;
        }

        public Result TryUnequip(SaveDataV2 save, EquipmentSlot slot)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            SetEquipped(save, slot, "");
            _save?.RequestSave();
            return Result.Success;
        }

        private static bool ContainsId(List<string> owned, string id)
        {
            if (owned == null)
                return false;

            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] == id)
                    return true;
            }

            return false;
        }

        private static string GetEquipped(SaveDataV2 data, EquipmentSlot slot) => EquippedSlots.Get(data, slot);

        private static void SetEquipped(SaveDataV2 data, EquipmentSlot slot, string id) => EquippedSlots.Set(data, slot, id);
    }
}
