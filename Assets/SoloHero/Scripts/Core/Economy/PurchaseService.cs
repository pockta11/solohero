using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// D-181 store: starts a purchase on the gateway; D-183: every purchase the store verified (the one started here or
    /// one it restores) comes back through <see cref="DeliverAsync"/>, which records it once in the purchase ledger
    /// (one account per purchase token - a receipt replayed into another account pays nothing), pays it, saves, and
    /// only then lets the store close it. A grant is idempotent on the store's purchase token (a restore after a crash
    /// pays nothing twice), the first purchase of each gem bundle pays IAP_FIRST_BONUS_MULT times, the gold bundle pays
    /// IAP_GOLD_PACK_STAGES clears of the farming stage and the VIP pack extends vipUntilUtc by VIP_DAYS. A grant is a
    /// save trigger (a purchase is a player decision that changes currency); the local backup is written at once.
    /// </summary>
    public sealed class PurchaseService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly IPurchaseGateway _gateway;
        private readonly ISaveRequester _save;
        private readonly IPurchaseLedger _ledger;
        private readonly Func<bool> _savesKept;
        private readonly HashSet<string> _delivering = new HashSet<string>();
        private bool _busy;

        /// <param name="savesKept">
        /// D-183: true while a grant would be kept - the save reaches the server copy (false in a session started from an
        /// untrusted local backup, whose progress the server copy replaces later, or while the account tools hold saves).
        /// Null = always.
        /// </param>
        public PurchaseService(BalanceValues balance, SaveDataV2 data, IClock clock, IPurchaseGateway gateway, ISaveRequester save = null,
            IPurchaseLedger ledger = null, Func<bool> savesKept = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _gateway = gateway ?? new NoPurchaseGateway();
            _save = save;
            _ledger = ledger ?? new LocalPurchaseLedger();
            _savesKept = savesKept;
            _gateway.Attach(DeliverAsync);
        }

        private bool SavesKept
        {
            get
            {
                try
                {
                    return _savesKept == null || _savesKept();
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>Raised after a grant, with what it paid.</summary>
        public event Action<PurchaseGrant> Purchased;

        /// <summary>Raised when a started purchase ends without a grant (cancelled, failed, pending).</summary>
        public event Action<IapProduct, PurchaseOutcome> PurchaseEnded;

        public bool Busy => _busy;

        /// <summary>The store answered, purchases can be recorded (signed in to the server) and a grant would be kept.</summary>
        public bool StoreReady => _gateway.IsReady && _ledger.Online && SavesKept;

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
            if (!StoreReady) return Result.Fail(FailReason.StoreUnavailable);
            _busy = true;
            try
            {
                _gateway.Buy(product, outcome => OnStoreDone(product, outcome));
            }
            catch (Exception)
            {
                _busy = false;
                return Result.Fail(FailReason.StoreUnavailable);
            }

            return Result.Success;
        }

        /// <summary>The purchase flow ended; a delivered purchase already raised <see cref="Purchased"/>.</summary>
        private void OnStoreDone(IapProduct product, PurchaseOutcome outcome)
        {
            _busy = false;
            if (outcome != PurchaseOutcome.Success) PurchaseEnded?.Invoke(product, outcome);
        }

        /// <summary>
        /// D-183: delivers one purchase the store verified. Pays it at most once per purchase token: the local history
        /// first (a restore on this device), then the ledger (the same receipt reported by another account is
        /// <see cref="DeliveryResult.Rejected"/>). The store closes the purchase only on Granted / AlreadyGranted, after
        /// the grant was saved; without the ledger it stays open and comes back later (RetryLater).
        /// </summary>
        public async Task<DeliveryResult> DeliverAsync(IapProduct product, string token)
        {
            IapProductDef def = IapCatalog.Find(product);
            if (def == null || string.IsNullOrEmpty(token)) return DeliveryResult.Rejected;
            string key = PurchaseTokens.Key(token);
            if (_data.iapGrantedTokens.Contains(token))
            {
                await MarkGrantedQuietly(key);
                return DeliveryResult.AlreadyGranted;
            }

            // A grant this session could not keep (the server copy replaces it later): leave the purchase open.
            if (!SavesKept) return DeliveryResult.RetryLater;
            if (!_delivering.Add(token)) return DeliveryResult.InProgress;
            try
            {
                LedgerClaim claim;
                try
                {
                    claim = await _ledger.ClaimAsync(key, def.Id);
                }
                catch (Exception e)
                {
                    Log.Warn(LogTag.Boot, "purchase ledger unreachable: " + e.Message);
                    claim = LedgerClaim.Unavailable;
                }

                switch (claim)
                {
                    case LedgerClaim.Unavailable:
                        return DeliveryResult.RetryLater;
                    case LedgerClaim.Taken:
                        Log.Warn(LogTag.Boot, "purchase recorded for another account, not paid: " + def.Id);
                        return DeliveryResult.Rejected;
                    case LedgerClaim.MineGranted:
                        // Paid before on this account (the local history lost it): remember it, pay nothing.
                        RememberToken(token);
                        _save?.RequestSave();
                        return DeliveryResult.AlreadyGranted;
                }

                if (!Grant(product, token)) return DeliveryResult.AlreadyGranted;
                await MarkGrantedQuietly(key);
                return DeliveryResult.Granted;
            }
            finally
            {
                _delivering.Remove(token);
            }
        }

        private async Task MarkGrantedQuietly(string key)
        {
            try
            {
                await _ledger.MarkGrantedAsync(key);
            }
            catch (Exception e)
            {
                // The record stays "claimed by this account"; the local history still stops a second payment here.
                Log.Warn(LogTag.Boot, "purchase not marked granted: " + e.Message);
            }
        }

        private void RememberToken(string token)
        {
            if (string.IsNullOrEmpty(token) || _data.iapGrantedTokens.Contains(token)) return;
            _data.iapGrantedTokens.Add(token);
            int keep = _balance.IAP_GRANT_HISTORY < 1 ? 1 : _balance.IAP_GRANT_HISTORY;
            while (_data.iapGrantedTokens.Count > keep) _data.iapGrantedTokens.RemoveAt(0);
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
                RememberToken(token);
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
            // Saved before anyone hears of it: a listener that throws must not leave the grant unsaved.
            _save?.RequestSave();
            var grant = new PurchaseGrant(product, gems, gold, vipDays, firstBonus);
            try
            {
                Purchased?.Invoke(grant);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "purchase listener failed: " + e.Message);
            }

            return true;
        }
    }
}
