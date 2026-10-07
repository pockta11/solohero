using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Save;

namespace SoloHero.Core.Gacha
{
    public sealed class GachaService
    {
        private readonly BalanceValues _balance;
        private readonly GearTableValues _table;
        private readonly IRandom _rng;
        private readonly GachaEquipmentDef[] _catalog;

        public GachaService(
            BalanceValues balance,
            GearTableValues table,
            IRandom rng,
            GachaEquipmentDef[] catalog)
        {
            _balance = balance;
            _table = table;
            _rng = rng;
            _catalog = catalog;
        }

        public GachaBatchResult TryPull(SaveDataV2 data)
        {
            if (data.gold < _balance.GACHA_COST_SINGLE)
                return GachaBatchResult.Fail(FailReason.NotEnoughGold);

            data.gold -= _balance.GACHA_COST_SINGLE;
            var items = new GachaPullItem[1];
            items[0] = ExecuteOnePull(data);
            return GachaBatchResult.Ok(items);
        }

        public GachaBatchResult TryPullTen(SaveDataV2 data)
        {
            if (data.gold < _balance.GACHA_COST_TEN)
                return GachaBatchResult.Fail(FailReason.NotEnoughGold);

            data.gold -= _balance.GACHA_COST_TEN;
            var items = new GachaPullItem[10];
            for (int i = 0; i < 10; i++)
                items[i] = ExecuteOnePull(data);
            return GachaBatchResult.Ok(items);
        }

        /// <summary>D-120: free pulls (an ad summon); same rates, pity and counters as paid ones.</summary>
        public GachaBatchResult PullFree(SaveDataV2 data, int count)
        {
            if (count < 1) count = 1;
            var items = new GachaPullItem[count];
            for (int i = 0; i < count; i++)
                items[i] = ExecuteOnePull(data);
            return GachaBatchResult.Ok(items);
        }

        public GachaBatchResult TryPullTenWithGem(SaveDataV2 data)
        {
            if (data.gem < _balance.GACHA_COST_TEN_GEM)
                return GachaBatchResult.Fail(FailReason.NotEnoughGem);

            data.gem -= _balance.GACHA_COST_TEN_GEM;
            var items = new GachaPullItem[10];
            for (int i = 0; i < 10; i++)
                items[i] = ExecuteOnePull(data);
            return GachaBatchResult.Ok(items);
        }

        private GachaPullItem ExecuteOnePull(SaveDataV2 data)
        {
            // D-115: the summon level before this pull picks the rate table.
            GearTableValues table = _table.AtLevel(SummonLevel.Of(_balance, data, SummonKind.Gear));
            // D-123: the pity starts counting once the summon level has opened its grade.
            bool pity = table.PityOpen;
            if (pity) data.pityCount++;
            data.totalPullCount++;

            // GDD: pick slot first (uniform over the slots; D-109: 8), then grade from the rate table / pity.
            var slot = (EquipmentSlot)_rng.Next(GachaCatalog.SlotCount);

            GearGrade grade;
            if (pity && data.pityCount >= table.PityCeiling)
            {
                grade = GearTableValues.PityGrade;
                data.pityCount = 0;
            }
            else
            {
                grade = table.PickGrade(_rng.NextDouble());
                if (grade >= GearTableValues.PityGrade && table.ResetOnPityGrade)
                    data.pityCount = 0;
            }

            GachaEquipmentDef def = Find(slot, grade);
            return Acquire(data, def);
        }

        private GachaPullItem Acquire(SaveDataV2 data, GachaEquipmentDef def)
        {
            if (ContainsId(data.ownedEquipment, def.Id))
            {
                // D-062: a duplicate enhances the owned copy; only a max-level copy refunds gold.
                int level = EquipmentLevels.Get(data, def.Id);
                if (level < _balance.EQUIP_MAX_LEVEL)
                {
                    EquipmentLevels.Set(data, def.Id, level + 1);
                    return new GachaPullItem(def.Id, def.Slot, def.Grade, true, 0d, false, level + 1);
                }

                data.gold += def.RefundGold;
                return new GachaPullItem(def.Id, def.Slot, def.Grade, true, def.RefundGold, false);
            }

            EquipmentLevels.AddOwned(data, def.Id);
            bool autoEquipped = TryAutoEquip(data, def);
            return new GachaPullItem(def.Id, def.Slot, def.Grade, false, 0d, autoEquipped);
        }

        private bool TryAutoEquip(SaveDataV2 data, GachaEquipmentDef def)
        {
            string currentId = GetEquipped(data, def.Slot);
            if (string.IsNullOrEmpty(currentId))
            {
                SetEquipped(data, def.Slot, def.Id);
                return true;
            }

            GachaEquipmentDef current = FindById(currentId);
            if (current == null || def.Grade > current.Grade)
            {
                SetEquipped(data, def.Slot, def.Id);
                return true;
            }

            return false;
        }

        private GachaEquipmentDef Find(EquipmentSlot slot, GearGrade grade)
        {
            for (int i = 0; i < _catalog.Length; i++)
            {
                GachaEquipmentDef def = _catalog[i];
                if (def.Slot == slot && def.Grade == grade)
                    return def;
            }

            throw new System.InvalidOperationException(
                "Gacha catalog missing equipment for slot " + slot + " grade " + grade);
        }

        private GachaEquipmentDef FindById(string id)
        {
            for (int i = 0; i < _catalog.Length; i++)
            {
                if (_catalog[i].Id == id)
                    return _catalog[i];
            }

            return null;
        }

        private static bool ContainsId(List<string> owned, string id)
        {
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
