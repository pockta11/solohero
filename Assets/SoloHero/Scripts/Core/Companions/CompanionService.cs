using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Companions
{
    /// <summary>
    /// D-102 companions: unlock by progress, equip one, level up with gold. Equip and level-up change the combat
    /// loadout (the caller refreshes it) and request a save.
    /// </summary>
    public sealed class CompanionService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public CompanionService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public event Action Changed;

        public bool IsUnlocked(int index) => index >= 0 && index < CompanionCatalog.Count && _data.highestStage >= CompanionCatalog.All[index].UnlockStage;

        public int Level(int index) => Level(_data, index);

        public static int Level(SaveDataV2 data, int index)
        {
            if (data == null || index < 0 || index >= data.companionLevels.Count) return 1;
            int lv = data.companionLevels[index];
            return lv < 1 ? 1 : lv;
        }

        public int EquippedIndex => CompanionCatalog.IndexOf(_data.companionEquipped);

        public bool IsMax(int index) => Level(index) >= _balance.COMPANION_MAX_LEVEL;

        public double LevelCost(int index) => Formulas.CompanionLevelCost(_balance, CompanionCatalog.All[index].Grade, Level(index));

        public Result TryEquip(int index)
        {
            if (!IsUnlocked(index)) return Result.Fail(FailReason.Locked);
            _data.companionEquipped = CompanionCatalog.All[index].Id;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        public Result TryLevelUp(int index)
        {
            if (!IsUnlocked(index)) return Result.Fail(FailReason.Locked);
            if (IsMax(index)) return Result.Fail(FailReason.MaxLevel);
            double cost = LevelCost(index);
            if (_data.gold < cost) return Result.Fail(FailReason.NotEnoughGold);
            _data.gold -= cost;
            while (_data.companionLevels.Count <= index) _data.companionLevels.Add(1);
            _data.companionLevels[index] = Level(index) + 1;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }
    }
}
