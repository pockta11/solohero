using System;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    /// <summary>
    /// Gold stat lanes (character panel). D-142: six lanes, the crit ones open with hero levels. D-143: a lane stops
    /// every LIMIT_STEP levels until <see cref="TryBreak"/> spends breakthrough stones (LaneRules).
    /// </summary>
    public sealed class UpgradeService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public event Action<UpgradeLane, int> LaneUpgraded;

        /// <summary>D-143: a lane was broken; the int is its break count afterwards.</summary>
        public event Action<UpgradeLane, int> LaneBroken;

        public UpgradeService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
            LaneRules.EnsureBreaks(_balance, _data);
        }

        public LaneState State(UpgradeLane lane) => LaneRules.State(_balance, _data, lane);

        public Result TryUpgrade(UpgradeLane lane)
        {
            switch (State(lane))
            {
                case LaneState.Locked: return Result.Fail(FailReason.Locked);
                case LaneState.Max: return Result.Fail(FailReason.MaxLevel);
                case LaneState.NeedsBreak: return Result.Fail(FailReason.LimitReached);
            }

            int level = GetLevel(lane);
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

        public int Breaks(UpgradeLane lane) => LaneRules.Breaks(_data, lane);

        public int BreakCost(UpgradeLane lane) => LaneRules.BreakCost(_balance, Breaks(lane));

        /// <summary>D-143: spends stones to lift the lane's limit by LIMIT_STEP levels.</summary>
        public Result TryBreak(UpgradeLane lane)
        {
            if (State(lane) != LaneState.NeedsBreak) return Result.Fail(FailReason.Locked);
            int cost = BreakCost(lane);
            if (_data.breakStones < cost) return Result.Fail(FailReason.NotEnoughStones);
            LaneRules.EnsureBreaks(_balance, _data);
            _data.breakStones -= cost;
            int breaks = Breaks(lane) + 1;
            _data.laneBreaks[(int)lane] = breaks;
            LaneBroken?.Invoke(lane, breaks);
            _save?.RequestSave();
            return Result.Success;
        }

        public int GetLevel(UpgradeLane lane) => LaneLevels.From(_data).Get(lane);

        private void SetLevel(UpgradeLane lane, int level)
        {
            LaneLevels levels = LaneLevels.From(_data);
            levels.Set(lane, level);
            levels.WriteTo(_data);
        }
    }
}
