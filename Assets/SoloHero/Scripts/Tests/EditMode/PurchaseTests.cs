using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>A store that answers at once, or holds the callback until <see cref="Finish"/>.</summary>
        private sealed class ScriptedGateway : IPurchaseGateway
        {
            public bool IsReady { get; set; } = true;
            public PurchaseOutcome Outcome = PurchaseOutcome.Success;
            public string Token = "tx-1";
            public bool Hold;
            private Action<PurchaseOutcome, string> _held;

            public string PriceLabel(IapProduct product) => product == IapProduct.Vip ? "KRW 5,900" : null;

            public void Buy(IapProduct product, Action<PurchaseOutcome, string> onDone)
            {
                if (Hold) _held = onDone;
                else onDone(Outcome, Token);
            }

            public void Finish()
            {
                Action<PurchaseOutcome, string> held = _held;
                _held = null;
                held?.Invoke(Outcome, Token);
            }
        }

        private static PurchaseService Make(BalanceValues b, SaveDataV2 save, FixedClock clock, ScriptedGateway gateway, CountingRequester requester = null) =>
            new PurchaseService(b, save, clock, gateway, requester);

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
    }
}
