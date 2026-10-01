using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    /// <summary>
    /// D-101 hero promotion (genre "advancement"): four tiers, each opened by a hero level and bought with gold.
    /// A tier multiplies HP, ATK and DEF by PROMOTE_STAT_MULT (StatAggregator) and changes the hero's look.
    /// </summary>
    public sealed class PromotionService
    {
        public const int MaxTier = 4;

        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public PromotionService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public event Action<int> Promoted;

        public int Tier => _data.promotionTier < 0 ? 0 : _data.promotionTier > MaxTier ? MaxTier : _data.promotionTier;

        public bool IsMax => Tier >= MaxTier;

        public int RequiredLevel(int tier)
        {
            switch (tier)
            {
                case 1: return _balance.PROMOTE_LV_1;
                case 2: return _balance.PROMOTE_LV_2;
                case 3: return _balance.PROMOTE_LV_3;
                case 4: return _balance.PROMOTE_LV_4;
                default: return int.MaxValue;
            }
        }

        public double Cost(int tier) => Formulas.PromotionCost(_balance, _data.highestStage < 1 ? 1 : _data.highestStage, tier);

        public Result CanPromote()
        {
            if (IsMax) return Result.Fail(FailReason.MaxLevel);
            int next = Tier + 1;
            if (_data.heroLevel < RequiredLevel(next)) return Result.Fail(FailReason.Locked);
            if (_data.gold < Cost(next)) return Result.Fail(FailReason.NotEnoughGold);
            return Result.Success;
        }

        public Result TryPromote()
        {
            Result can = CanPromote();
            if (!can.Ok) return can;
            int next = Tier + 1;
            _data.gold -= Cost(next);
            _data.promotionTier = next;
            Promoted?.Invoke(next);
            _save?.RequestSave();
            return Result.Success;
        }
    }
}
