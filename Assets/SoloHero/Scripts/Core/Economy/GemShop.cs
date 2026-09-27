using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>
    /// Gem sink besides the 10-pull (E6-02, GDD gem table): the instant gold package. GEM_GOLD_PACK_COST gems buy
    /// the current farming stage's clear gold x GEM_GOLD_PACK_STAGES, so the package keeps its value as the player
    /// advances. Gems are a shortcut only - nothing is gated behind them.
    /// </summary>
    public sealed class GemShop
    {
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public GemShop(BalanceValues balance, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public double GoldPackAmount(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int g = data.farmingStage < 1 ? 1 : data.farmingStage;
            return Math.Floor(Formulas.StageGold(_balance, g) * _balance.GEM_GOLD_PACK_STAGES);
        }

        public Result TryBuyGoldPack(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.gem < _balance.GEM_GOLD_PACK_COST) return Result.Fail(FailReason.NotEnoughGem);
            double gold = GoldPackAmount(data);
            data.gem -= _balance.GEM_GOLD_PACK_COST;
            data.gold += gold;
            _save?.RequestSave();
            return Result.Success;
        }
    }
}
