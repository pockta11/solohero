using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Gacha
{
    public sealed class GachaService
    {
        private readonly BalanceValues _balance;
        private readonly GachaTableValues _table;
        private readonly IRandom _rng;
        private readonly GachaEquipmentDef[] _catalog;

        public GachaService(
            BalanceValues balance,
            GachaTableValues table,
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

        private GachaPullItem ExecuteOnePull(SaveDataV2 data)
        {
            data.pityCount++;
            data.totalPullCount++;

            // GDD: pick slot first (uniform 25%), then grade from the rate table / pity.
            var slot = (EquipmentSlot)_rng.Next(4);

            Grade grade;
            if (data.pityCount >= _table.PityCeiling)
            {
                grade = Grade.Legendary;
                data.pityCount = 0;
            }
            else
            {
                grade = _table.PickGrade(_rng.NextDouble());
                if (grade == Grade.Legendary && _table.ResetOnLegendary)
                    data.pityCount = 0;
            }

            GachaEquipmentDef def = Find(slot, grade);
            return Acquire(data, def);
        }

        private GachaPullItem Acquire(SaveDataV2 data, GachaEquipmentDef def)
        {
            if (ContainsId(data.ownedEquipment, def.Id))
            {
                data.gold += def.RefundGold;
                return new GachaPullItem(def.Id, def.Slot, def.Grade, true, def.RefundGold, false);
            }

            data.ownedEquipment.Add(def.Id);
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

        private GachaEquipmentDef Find(EquipmentSlot slot, Grade grade)
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

        private static string GetEquipped(SaveDataV2 data, EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Sword: return data.equippedSword;
                case EquipmentSlot.Helm: return data.equippedHelm;
                case EquipmentSlot.Armor: return data.equippedArmor;
                case EquipmentSlot.Boots: return data.equippedBoots;
                default: return "";
            }
        }

        private static void SetEquipped(SaveDataV2 data, EquipmentSlot slot, string id)
        {
            switch (slot)
            {
                case EquipmentSlot.Sword: data.equippedSword = id; break;
                case EquipmentSlot.Helm: data.equippedHelm = id; break;
                case EquipmentSlot.Armor: data.equippedArmor = id; break;
                case EquipmentSlot.Boots: data.equippedBoots = id; break;
            }
        }
    }
}
