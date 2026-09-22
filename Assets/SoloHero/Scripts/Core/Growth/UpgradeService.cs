using System;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    public sealed class UpgradeService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public event Action<UpgradeLane, int> LaneUpgraded;

        public UpgradeService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public Result TryUpgrade(UpgradeLane lane)
        {
            int level = GetLevel(lane);
            if (lane == UpgradeLane.Spd && level >= _balance.UPG_MAX_LEVEL_SPD)
                return Result.Fail(FailReason.MaxLevel);

            double cost = Formulas.UpgradeCost(_balance, lane, level);
            if (_data.gold < cost)
                return Result.Fail(FailReason.NotEnoughGold);

            _data.gold -= cost;
            int next = level + 1;
            SetLevel(lane, next);
            LaneUpgraded?.Invoke(lane, next);
            _save?.RequestSave();
            return Result.Success;
        }

        public int GetLevel(UpgradeLane lane)
        {
            switch (lane)
            {
                case UpgradeLane.Hp: return _data.upgradeHp;
                case UpgradeLane.Atk: return _data.upgradeAtk;
                case UpgradeLane.Def: return _data.upgradeDef;
                case UpgradeLane.Spd: return _data.upgradeSpd;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }

        private void SetLevel(UpgradeLane lane, int level)
        {
            switch (lane)
            {
                case UpgradeLane.Hp: _data.upgradeHp = level; break;
                case UpgradeLane.Atk: _data.upgradeAtk = level; break;
                case UpgradeLane.Def: _data.upgradeDef = level; break;
                case UpgradeLane.Spd: _data.upgradeSpd = level; break;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }
    }
}
