using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>What a granted purchase paid, for the toast and the reward animation.</summary>
    public readonly struct PurchaseGrant
    {
        public readonly IapProduct Product;
        public readonly double Gems;
        public readonly double Gold;
        public readonly int VipDays;
        public readonly bool FirstBonus;

        public PurchaseGrant(IapProduct product, double gems, double gold, int vipDays, bool firstBonus)
        {
            Product = product;
            Gems = gems;
            Gold = gold;
            VipDays = vipDays;
            FirstBonus = firstBonus;
        }
    }

    /// <summary>
    /// D-181 store: starts a purchase on the gateway and grants it once the store confirms. Grants are idempotent on the
    /// store's transaction id (a replayed receipt or a restore after a crash pays nothing twice), the first purchase of
    /// each gem bundle pays IAP_FIRST_BONUS_MULT times, the gold bundle pays IAP_GOLD_PACK_STAGES clears of the farming
    /// stage and the VIP pack extends vipUntilUtc by VIP_DAYS. A grant is a save trigger (a purchase is a player decision
    /// that changes currency).
    /// </summary>
    public sealed class PurchaseService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly IPurchaseGateway _gateway;
        private readonly ISaveRequester _save;
        private bool _busy;

        public PurchaseService(BalanceValues balance, SaveDataV2 data, IClock clock, IPurchaseGateway gateway, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _gateway = gateway ?? new NoPurchaseGateway();
            _save = save;
        }

        /// <summary>Raised after a grant, with what it paid.</summary>
        public event Action<PurchaseGrant> Purchased;

        /// <summary>Raised when a started purchase ends without a grant (cancelled, failed, pending).</summary>
        public event Action<IapProduct, PurchaseOutcome> PurchaseEnded;

        public bool Busy => _busy;

        public bool StoreReady => _gateway.IsReady;

        public bool VipActive => VipPerks.IsActive(_data, _clock.UtcNowSeconds);

        public int VipDaysLeft => VipPerks.DaysLeft(_data, _clock.UtcNowSeconds);

        /// <summary>The store's price for the label, or the catalog's won fallback.</summary>
        public string PriceLabel(IapProduct product)
        {
            string price = _gateway.PriceLabel(product);
            if (!string.IsNullOrEmpty(price)) return price;
            IapProductDef def = IapCatalog.Find(product);
            return def != null ? def.FallbackPrice : "";
        }

        public bool FirstBonusAvailable(IapProduct product)
        {
            IapProductDef def = IapCatalog.Find(product);
            return def != null && def.IsGems && _balance.IAP_FIRST_BONUS_MULT > 1d && !_data.iapFirstBonusUsed.Contains(def.Id);
        }

        /// <summary>Gems the bundle pays right now, the first-purchase bonus included.</summary>
        public double GemAmount(IapProduct product)
        {
            double gems = IapCatalog.Gems(_balance, product);
            return FirstBonusAvailable(product) ? gems * _balance.IAP_FIRST_BONUS_MULT : gems;
        }

        public double GoldPackAmount()
        {
            int g = _data.farmingStage < 1 ? 1 : _data.farmingStage;
            return Math.Floor(Formulas.StageGold(_balance, g) * _balance.IAP_GOLD_PACK_STAGES);
        }

        /// <summary>Starts the store flow; the grant (or the end without one) is raised later on the main thread.</summary>
        public Result TryBuy(IapProduct product)
        {
            if (IapCatalog.Find(product) == null) return Result.Fail(FailReason.Locked);
            if (_busy) return Result.Fail(FailReason.Busy);
            if (!_gateway.IsReady) return Result.Fail(FailReason.StoreUnavailable);
            _busy = true;
            try
            {
                _gateway.Buy(product, (outcome, token) => OnStoreDone(product, outcome, token));
            }
            catch (Exception)
            {
                _busy = false;
                return Result.Fail(FailReason.StoreUnavailable);
            }

            return Result.Success;
        }

        private void OnStoreDone(IapProduct product, PurchaseOutcome outcome, string token)
        {
            _busy = false;
            if (outcome == PurchaseOutcome.Success)
            {
                Grant(product, token);
                return;
            }

            PurchaseEnded?.Invoke(product, outcome);
        }

        /// <summary>
        /// Pays a confirmed purchase. <paramref name="token"/> is the store's transaction id; a token already granted
        /// pays nothing (false). A null token (a store without ids) always pays.
        /// </summary>
        public bool Grant(IapProduct product, string token)
        {
            IapProductDef def = IapCatalog.Find(product);
            if (def == null) return false;
            if (!string.IsNullOrEmpty(token))
            {
                if (_data.iapGrantedTokens.Contains(token)) return false;
                _data.iapGrantedTokens.Add(token);
                int keep = _balance.IAP_GRANT_HISTORY < 1 ? 1 : _balance.IAP_GRANT_HISTORY;
                while (_data.iapGrantedTokens.Count > keep) _data.iapGrantedTokens.RemoveAt(0);
            }

            long now = _clock.UtcNowSeconds;
            double gems = 0d, gold = 0d;
            int vipDays = 0;
            bool firstBonus = false;
            if (def.IsGems)
            {
                firstBonus = FirstBonusAvailable(product);
                gems = GemAmount(product);
                if (firstBonus) _data.iapFirstBonusUsed.Add(def.Id);
                _data.gem += gems;
            }
            else if (product == IapProduct.GoldPack)
            {
                gold = GoldPackAmount();
                _data.gold += gold;
            }
            else if (product == IapProduct.Vip)
            {
                VipPerks.Extend(_balance, _data, now);
                vipDays = _balance.VIP_DAYS;
            }

            _data.iapCount++;
            var grant = new PurchaseGrant(product, gems, gold, vipDays, firstBonus);
            Purchased?.Invoke(grant);
            _save?.RequestSave();
            return true;
        }
    }
}
