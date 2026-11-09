using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-181 store: catalog, grants, first-purchase bonus, token replay, VIP perks and the store-not-ready path.</summary>
    public sealed class PurchaseTests
    {
        private const string TablePath = "Assets/SoloHero/Data/Strings/strings_ko.txt";

        private sealed class FixedClock : IClock
        {
            public long UtcNowSeconds { get; set; } = 1_800_000_000L;

            public DateTime LocalNow => DateTimeOffset.FromUnixTimeSeconds(UtcNowSeconds).UtcDateTime;
        }

        private sealed class CountingRequester : ISaveRequester
        {
            public int Count;

            public void RequestSave() => Count++;
        }

        /// <summary>
        /// A store that answers at once, or holds the purchase until <see cref="Finish"/>. A successful purchase goes
        /// through the delivery the service attached (D-183), as the real store's verified receipts do.
        /// </summary>
        private sealed class ScriptedGateway : IPurchaseGateway
        {
            public bool IsReady { get; set; } = true;
            public PurchaseOutcome Outcome = PurchaseOutcome.Success;
            public string Token = "tx-1";
            public bool Hold;
            public DeliveryResult? LastDelivery;
            public PurchaseDelivery Deliver;
            private Action<PurchaseOutcome> _held;
            private IapProduct _heldProduct;

            public string PriceLabel(IapProduct product) => product == IapProduct.Vip ? "KRW 5,900" : null;

            public void Attach(PurchaseDelivery deliver) => Deliver = deliver;

            public void Buy(IapProduct product, Action<PurchaseOutcome> onDone)
            {
                if (Hold)
                {
                    _held = onDone;
                    _heldProduct = product;
                }
                else Run(product, onDone);
            }

            public void Finish()
            {
                Action<PurchaseOutcome> held = _held;
                _held = null;
                if (held != null) Run(_heldProduct, held);
            }

            private void Run(IapProduct product, Action<PurchaseOutcome> onDone)
            {
                if (Outcome != PurchaseOutcome.Success)
                {
                    onDone(Outcome);
                    return;
                }

                // The local ledger answers at once, so the delivery has finished when Result is read.
                DeliveryResult result = Deliver(product, Token).Result;
                LastDelivery = result;
                onDone(result == DeliveryResult.Granted || result == DeliveryResult.AlreadyGranted ? PurchaseOutcome.Success
                    : result == DeliveryResult.RetryLater ? PurchaseOutcome.Pending : PurchaseOutcome.Failed);
            }
        }

        private static PurchaseService Make(BalanceValues b, SaveDataV2 save, FixedClock clock, ScriptedGateway gateway, CountingRequester requester = null,
            IPurchaseLedger ledger = null) =>
            new PurchaseService(b, save, clock, gateway, requester, ledger);

        /// <summary>A ledger whose claims wait until the test answers them (a slow network).</summary>
        private sealed class HeldLedger : IPurchaseLedger
        {
            public readonly TaskCompletionSource<LedgerClaim> Claim = new TaskCompletionSource<LedgerClaim>();
            public int Claims;

            public bool Online => true;

            public Task<LedgerClaim> ClaimAsync(string key, string productId)
            {
                Claims++;
                return Claim.Task;
            }

            public Task MarkGrantedAsync(string key) => Task.CompletedTask;
        }

        [Test]
        public void Catalog_FiveProducts_UniqueIdsAndStoreIds_WithNames()
        {
            Dictionary<string, string> table = Strings.ParseTsv(File.ReadAllText(TablePath));
            var ids = new HashSet<string>();
            var storeIds = new HashSet<string>();
            Assert.AreEqual(5, IapCatalog.Count);
            foreach (IapProductDef def in IapCatalog.All)
            {
                Assert.IsTrue(ids.Add(def.Id), def.Id);
                Assert.IsTrue(storeIds.Add(def.StoreId), def.StoreId);
                Assert.IsTrue(table.ContainsKey(def.NameKey), def.NameKey);
                Assert.IsNotEmpty(def.FallbackPrice, def.Id);
                Assert.AreSame(def, IapCatalog.Find(def.Product));
                Assert.AreSame(def, IapCatalog.FindByStoreId(def.StoreId));
            }

            Assert.IsNull(IapCatalog.FindByStoreId("nope"));
        }

        [Test]
        public void GemBundle_FirstPurchaseDoubles_ThenPlain_AndSaves()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FixedClock();
            var requester = new CountingRequester();
            var gateway = new ScriptedGateway();
            PurchaseService store = Make(b, save, clock, gateway, requester);
            PurchaseGrant last = default;
            store.Purchased += g => last = g;

            Assert.IsTrue(store.FirstBonusAvailable(IapProduct.GemMedium));
            Assert.AreEqual(b.IAP_GEM_MEDIUM * b.IAP_FIRST_BONUS_MULT, store.GemAmount(IapProduct.GemMedium), 1e-9);
            double gems = save.gem;
            Assert.IsTrue(store.TryBuy(IapProduct.GemMedium).Ok);
            Assert.AreEqual(gems + b.IAP_GEM_MEDIUM * b.IAP_FIRST_BONUS_MULT, save.gem, 1e-9);
            Assert.IsTrue(last.FirstBonus);
            Assert.AreEqual(1, requester.Count);
            Assert.AreEqual(1, save.iapCount);

            gateway.Token = "tx-2";
            Assert.IsFalse(store.FirstBonusAvailable(IapProduct.GemMedium));
            Assert.IsTrue(store.FirstBonusAvailable(IapProduct.GemSmall), "each bundle has its own first bonus");
            Assert.IsTrue(store.TryBuy(IapProduct.GemMedium).Ok);
            Assert.AreEqual(gems + b.IAP_GEM_MEDIUM * (b.IAP_FIRST_BONUS_MULT + 1), save.gem, 1e-9);
            Assert.IsFalse(last.FirstBonus);
            Assert.AreEqual("KRW 5,900", store.PriceLabel(IapProduct.Vip), "the store's price wins");
            Assert.AreEqual(IapCatalog.Find(IapProduct.GemSmall).FallbackPrice, store.PriceLabel(IapProduct.GemSmall), "fallback without a store price");
        }

        [Test]
        public void Grant_SameTokenTwice_PaysOnce_HistoryBounded()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway());
            double gems = save.gem;
            Assert.IsTrue(store.Grant(IapProduct.GemSmall, "same"));
            Assert.IsFalse(store.Grant(IapProduct.GemSmall, "same"), "a replayed receipt pays nothing");
            Assert.AreEqual(gems + b.IAP_GEM_SMALL * b.IAP_FIRST_BONUS_MULT, save.gem, 1e-9);
            for (int i = 0; i < b.IAP_GRANT_HISTORY + 5; i++) Assert.IsTrue(store.Grant(IapProduct.GemSmall, "t" + i));
            Assert.AreEqual(b.IAP_GRANT_HISTORY, save.iapGrantedTokens.Count);
            Assert.IsTrue(store.Grant(IapProduct.GemSmall, null), "a store without ids still pays");
        }

        [Test]
        public void GoldPack_PaysStageClearsOfTheFarmingStage()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.farmingStage = 23;
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway());
            double expected = Math.Floor(Formulas.StageGold(b, 23) * b.IAP_GOLD_PACK_STAGES);
            Assert.AreEqual(expected, store.GoldPackAmount(), 1e-9);
            Assert.IsTrue(store.TryBuy(IapProduct.GoldPack).Ok);
            Assert.AreEqual(expected, save.gold, 1e-9);
        }

        [Test]
        public void Vip_ExtendsFromItsEnd_AndTurnsOnEveryPerk()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FixedClock();
            PurchaseService store = Make(b, save, clock, new ScriptedGateway());
            var ads = new AdSlotPolicy(b, save, clock);
            var daily = new DailyService(b, save, clock);
            Assert.IsFalse(store.VipActive);
            Assert.AreEqual(1f, ads.BattleSpeedMultiplier);
            Assert.AreEqual(1d, daily.VipMultiplier);
            Assert.AreEqual(b.OFFLINE_CAP, VipPerks.OfflineCap(b, save, clock.UtcNowSeconds));
            double attendanceGems = daily.AttendanceGems(1);
            double missionGems = daily.MissionGems(0);

            Assert.IsTrue(store.TryBuy(IapProduct.Vip).Ok);
            Assert.IsTrue(store.VipActive);
            Assert.AreEqual(b.VIP_DAYS, store.VipDaysLeft);
            Assert.AreEqual(clock.UtcNowSeconds + b.VIP_DAYS * VipPerks.DaySeconds, save.vipUntilUtc);
            Assert.AreEqual(b.AD_SPEED_MULT, ads.BattleSpeedMultiplier, "double speed without an ad");
            Assert.IsTrue(ads.VipSpeed);
            Assert.AreEqual(b.VIP_DAILY_MULT, daily.VipMultiplier);
            Assert.AreEqual(attendanceGems * b.VIP_DAILY_MULT, daily.AttendanceGems(1), 1e-9);
            Assert.AreEqual(missionGems * b.VIP_DAILY_MULT, daily.MissionGems(0), 1e-9);
            Assert.AreEqual(b.VIP_OFFLINE_CAP, VipPerks.OfflineCap(b, save, clock.UtcNowSeconds));

            // Bought again ten days in: the pack stacks onto its end, not onto today.
            clock.UtcNowSeconds += 10 * VipPerks.DaySeconds;
            Assert.IsTrue(store.Grant(IapProduct.Vip, "vip-2"));
            Assert.AreEqual(2 * b.VIP_DAYS - 10, store.VipDaysLeft);

            // Expired: perks off, and a new pack starts from now.
            clock.UtcNowSeconds = save.vipUntilUtc + 1;
            Assert.IsFalse(store.VipActive);
            Assert.AreEqual(1f, ads.BattleSpeedMultiplier);
            Assert.IsTrue(store.Grant(IapProduct.Vip, "vip-3"));
            Assert.AreEqual(clock.UtcNowSeconds + b.VIP_DAYS * VipPerks.DaySeconds, save.vipUntilUtc);
        }

        [Test]
        public void Vip_OfflineCapDoubles_ForALongAbsence()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.farmingStage = 10;
            long now = 1_800_000_000L;
            save.lastQuitTimeUtc = now - 2 * VipPerks.DaySeconds;
            OfflineReward plain = OfflineReturn.Apply(b, save, now, true);
            Assert.AreEqual(b.OFFLINE_CAP, plain.CountedSeconds);

            save.lastQuitTimeUtc = now - 2 * VipPerks.DaySeconds;
            save.vipUntilUtc = now + VipPerks.DaySeconds;
            OfflineReward vip = OfflineReturn.Apply(b, save, now, true);
            Assert.AreEqual(b.VIP_OFFLINE_CAP, vip.CountedSeconds);
            Assert.AreEqual(plain.Gold * b.VIP_OFFLINE_CAP / b.OFFLINE_CAP, vip.Gold, 1e-6);
        }

        [Test]
        public void TryBuy_StoreNotReady_Busy_CancelledAndFailed_GrantNothing()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var gateway = new ScriptedGateway { IsReady = false };
            PurchaseService store = Make(b, save, new FixedClock(), gateway);
            int grants = 0, ended = 0;
            PurchaseOutcome lastOutcome = PurchaseOutcome.Success;
            store.Purchased += g => grants++;
            store.PurchaseEnded += (p, o) => { ended++; lastOutcome = o; };

            Assert.AreEqual(FailReason.StoreUnavailable, store.TryBuy(IapProduct.GemSmall).Reason);
            Assert.IsFalse(store.StoreReady);

            gateway.IsReady = true;
            gateway.Hold = true;
            Assert.IsTrue(store.TryBuy(IapProduct.GemSmall).Ok);
            Assert.IsTrue(store.Busy);
            Assert.AreEqual(FailReason.Busy, store.TryBuy(IapProduct.GemLarge).Reason, "one purchase at a time");
            gateway.Outcome = PurchaseOutcome.Cancelled;
            gateway.Finish();
            Assert.IsFalse(store.Busy);
            Assert.AreEqual(0, grants);
            Assert.AreEqual(1, ended);
            Assert.AreEqual(PurchaseOutcome.Cancelled, lastOutcome);

            gateway.Hold = false;
            gateway.Outcome = PurchaseOutcome.Failed;
            Assert.IsTrue(store.TryBuy(IapProduct.GemSmall).Ok);
            Assert.AreEqual(0, grants);
            Assert.AreEqual(2, ended);
            Assert.AreEqual(0d, save.gem, 1e-9);
            Assert.AreEqual(0, save.iapCount);
        }

        // ---- D-183: verified purchase delivery (ledger, replay, restore, retry) ----

        [Test]
        public void Deliver_PaysOnce_RestoreOfTheSameToken_PaysNothing_AndTheLedgerIsMarked()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new LocalPurchaseLedger();
            var requester = new CountingRequester();
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), requester, ledger);
            int grants = 0;
            store.Purchased += g => grants++;

            Assert.AreEqual(DeliveryResult.Granted, store.DeliverAsync(IapProduct.GemSmall, "gp-token-1").Result);
            double after = save.gem;
            Assert.AreEqual(b.IAP_GEM_SMALL * b.IAP_FIRST_BONUS_MULT, after, 1e-9);
            Assert.IsTrue(ledger.IsGranted(PurchaseTokens.Key("gp-token-1")), "the ledger knows it was paid");
            Assert.AreEqual(1, requester.Count, "the grant saved");

            // The store reports the same purchase again (a crash before it was closed): nothing more is paid.
            Assert.AreEqual(DeliveryResult.AlreadyGranted, store.DeliverAsync(IapProduct.GemSmall, "gp-token-1").Result);
            Assert.AreEqual(after, save.gem, 1e-9);
            Assert.AreEqual(1, grants);
            Assert.AreEqual(1, save.iapCount);
        }

        [Test]
        public void Deliver_ReceiptRecordedForAnotherAccount_IsRejected_AndPaysNothing()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new LocalPurchaseLedger { Override = LedgerClaim.Taken };
            var requester = new CountingRequester();
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), requester, ledger);
            int grants = 0;
            store.Purchased += g => grants++;

            Assert.AreEqual(DeliveryResult.Rejected, store.DeliverAsync(IapProduct.GemLarge, "replayed").Result);
            Assert.AreEqual(0d, save.gem, 1e-9);
            Assert.AreEqual(0, grants);
            Assert.AreEqual(0, save.iapCount);
            Assert.AreEqual(0, requester.Count);
            CollectionAssert.IsEmpty(save.iapGrantedTokens);
        }

        [Test]
        public void Deliver_LedgerUnreachable_RetriesLater_ThenPaysWhenItAnswers()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new LocalPurchaseLedger { Override = LedgerClaim.Unavailable };
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), null, ledger);

            Assert.AreEqual(DeliveryResult.RetryLater, store.DeliverAsync(IapProduct.GoldPack, "slow").Result);
            Assert.AreEqual(0d, save.gold, 1e-9, "nothing is paid before the purchase is recorded");

            ledger.Override = null;
            Assert.AreEqual(DeliveryResult.Granted, store.DeliverAsync(IapProduct.GoldPack, "slow").Result);
            Assert.Greater(save.gold, 0d);
        }

        [Test]
        public void Deliver_PaidBeforeOnThisAccount_LocalHistoryLost_PaysNothing_AndRemembersIt()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new LocalPurchaseLedger { Override = LedgerClaim.MineGranted };
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), null, ledger);

            Assert.AreEqual(DeliveryResult.AlreadyGranted, store.DeliverAsync(IapProduct.Vip, "old").Result);
            Assert.AreEqual(0L, (long)save.vipUntilUtc);
            CollectionAssert.Contains(save.iapGrantedTokens, "old");
        }

        [Test]
        public void Deliver_ClaimedButNotPaid_ACrashBetween_PaysNow()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new LocalPurchaseLedger { Override = LedgerClaim.Mine };
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), null, ledger);

            Assert.AreEqual(DeliveryResult.Granted, store.DeliverAsync(IapProduct.GemMedium, "half-done").Result);
            Assert.Greater(save.gem, 0d);
        }

        [Test]
        public void Deliver_NoTokenOrUnknownProduct_IsRejected()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway());

            Assert.AreEqual(DeliveryResult.Rejected, store.DeliverAsync(IapProduct.GemSmall, null).Result);
            Assert.AreEqual(DeliveryResult.Rejected, store.DeliverAsync(IapProduct.GemSmall, "").Result);
            Assert.AreEqual(DeliveryResult.Rejected, store.DeliverAsync((IapProduct)99, "x").Result);
            Assert.AreEqual(0d, save.gem, 1e-9);
        }

        [Test]
        public void Deliver_SameTokenTwiceAtOnce_SecondIsInProgress_PaidOnce()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new HeldLedger();
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), null, ledger);

            Task<DeliveryResult> first = store.DeliverAsync(IapProduct.GemSmall, "dup");
            Assert.IsFalse(first.IsCompleted, "waiting for the ledger");
            Assert.AreEqual(DeliveryResult.InProgress, store.DeliverAsync(IapProduct.GemSmall, "dup").Result);
            ledger.Claim.SetResult(LedgerClaim.Fresh);
            Assert.AreEqual(DeliveryResult.Granted, first.Result);
            Assert.AreEqual(1, ledger.Claims);
            Assert.AreEqual(b.IAP_GEM_SMALL * b.IAP_FIRST_BONUS_MULT, save.gem, 1e-9);
            Assert.AreEqual(DeliveryResult.AlreadyGranted, store.DeliverAsync(IapProduct.GemSmall, "dup").Result);
            Assert.AreEqual(1, ledger.Claims, "a token paid here never asks the ledger again");
        }

        [Test]
        public void StoreReady_NeedsTheLedger_ShopClosedWithoutTheServer()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var ledger = new LocalPurchaseLedger { Online = false };
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), null, ledger);

            Assert.IsFalse(store.StoreReady);
            Assert.AreEqual(FailReason.StoreUnavailable, store.TryBuy(IapProduct.GemSmall).Reason);
            Assert.AreEqual(0d, save.gem, 1e-9);
        }

        [Test]
        public void TryBuy_LedgerUnreachable_EndsPending_AndPaysLater()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var gateway = new ScriptedGateway { Token = "late" };
            var ledger = new LocalPurchaseLedger { Override = LedgerClaim.Unavailable };
            PurchaseService store = Make(b, save, new FixedClock(), gateway, null, ledger);
            PurchaseOutcome ended = PurchaseOutcome.Success;
            store.PurchaseEnded += (p, o) => ended = o;

            Assert.IsTrue(store.TryBuy(IapProduct.GemSmall).Ok);
            Assert.AreEqual(PurchaseOutcome.Pending, ended, "the shop says the payment is being checked");
            Assert.IsFalse(store.Busy);
            Assert.AreEqual(0d, save.gem, 1e-9);

            // The store retries the open purchase once the server answers.
            ledger.Override = null;
            Assert.AreEqual(DeliveryResult.Granted, gateway.Deliver(IapProduct.GemSmall, "late").Result);
            Assert.Greater(save.gem, 0d);
        }

        [Test]
        public void SavesNotKept_ShopClosed_AndADeliveryWaits_ThenPaysInATrustedSession()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            bool kept = false;
            var ledger = new LocalPurchaseLedger();
            PurchaseService store = new PurchaseService(b, save, new FixedClock(), new ScriptedGateway(), null, ledger, () => kept);

            Assert.IsFalse(store.StoreReady, "a session whose saves the server copy replaces sells nothing");
            Assert.AreEqual(FailReason.StoreUnavailable, store.TryBuy(IapProduct.GemSmall).Reason);
            Assert.AreEqual(DeliveryResult.RetryLater, store.DeliverAsync(IapProduct.GemSmall, "restored").Result,
                "a restored purchase stays open instead of being paid into a copy that will be thrown away");
            Assert.AreEqual(0d, save.gem, 1e-9);
            Assert.IsFalse(ledger.IsGranted(PurchaseTokens.Key("restored")));

            kept = true;
            Assert.IsTrue(store.StoreReady);
            Assert.AreEqual(DeliveryResult.Granted, store.DeliverAsync(IapProduct.GemSmall, "restored").Result);
            Assert.Greater(save.gem, 0d);
        }

        [Test]
        public void Grant_SavesBeforeListeners_AListenerThatThrowsLosesNothing()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var requester = new CountingRequester();
            PurchaseService store = Make(b, save, new FixedClock(), new ScriptedGateway(), requester);
            store.Purchased += g => throw new InvalidOperationException("a broken listener");

            Assert.AreEqual(DeliveryResult.Granted, store.DeliverAsync(IapProduct.GemSmall, "t-throw").Result);
            Assert.AreEqual(1, requester.Count, "saved although the listener threw");
            Assert.Greater(save.gem, 0d);
        }

        [Test]
        public void PurchaseTokens_Key_IsStableHex64()
        {
            string key = PurchaseTokens.Key("abc");
            Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", key, "SHA-256 of the token");
            Assert.AreEqual(64, PurchaseTokens.Key(new string('x', 500)).Length);
            Assert.Throws<ArgumentException>(() => PurchaseTokens.Key(""));
        }
    }
}
