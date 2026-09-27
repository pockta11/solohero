using System;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class AdSlotPolicyTests
    {
        private sealed class FakeClock : IClock
        {
            public long Utc = 1_800_000_000L;
            public DateTime Local = new DateTime(2026, 9, 27, 10, 0, 0);

            public long UtcNowSeconds => Utc;

            public DateTime LocalNow => Local;
        }

        [Test]
        public void Complete_GemRewarded_AddsGemsAndCountsSlot()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var policy = new AdSlotPolicy(b, save, new FakeClock());

            Result r = policy.Complete(AdSlot.Gem, AdOutcome.Rewarded);

            Assert.IsTrue(r.Ok);
            Assert.AreEqual(b.AD_GEM_REWARD, save.gem, 1e-9);
            Assert.AreEqual(b.AD_GEM_DAILY - 1, policy.Remaining(AdSlot.Gem));
        }

        [Test]
        public void Complete_ClosedOrFailed_GrantsNothingAndKeepsCount()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var policy = new AdSlotPolicy(b, save, new FakeClock());

            Result closed = policy.Complete(AdSlot.Gem, AdOutcome.Closed);
            Result failed = policy.Complete(AdSlot.Gem, AdOutcome.Failed);

            Assert.AreEqual(FailReason.AdUnavailable, closed.Reason);
            Assert.AreEqual(FailReason.AdUnavailable, failed.Reason);
            Assert.AreEqual(0d, save.gem, 1e-9);
            Assert.AreEqual(b.AD_GEM_DAILY, policy.Remaining(AdSlot.Gem));
        }

        [Test]
        public void CanUse_AfterDailyLimit_ReturnsDailyLimit()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var policy = new AdSlotPolicy(b, save, new FakeClock());

            for (int i = 0; i < b.AD_OFFLINE_DAILY; i++)
                Assert.IsTrue(policy.Complete(AdSlot.OfflineDouble, AdOutcome.Rewarded).Ok);

            Assert.AreEqual(FailReason.DailyLimit, policy.CanUse(AdSlot.OfflineDouble).Reason);
            Assert.AreEqual(FailReason.DailyLimit, policy.Complete(AdSlot.OfflineDouble, AdOutcome.Rewarded).Reason);
        }

        [Test]
        public void Remaining_AfterLocalMidnight_ResetsEverySlot()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var policy = new AdSlotPolicy(b, save, clock);
            policy.Complete(AdSlot.Gem, AdOutcome.Rewarded);
            policy.Complete(AdSlot.OfflineDouble, AdOutcome.Rewarded);

            clock.Local = new DateTime(2026, 9, 27, 23, 59, 0);
            Assert.AreEqual(b.AD_GEM_DAILY - 1, policy.Remaining(AdSlot.Gem));

            clock.Local = new DateTime(2026, 9, 28, 0, 0, 1);
            Assert.AreEqual(b.AD_GEM_DAILY, policy.Remaining(AdSlot.Gem));
            Assert.AreEqual(b.AD_OFFLINE_DAILY, policy.Remaining(AdSlot.OfflineDouble));
        }

        [Test]
        public void GoldBooster_Rewarded_DoublesStageGoldUntilItEnds()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var policy = new AdSlotPolicy(b, save, clock);

            Assert.AreEqual(1d, policy.StageGoldMultiplier, 1e-9);
            Assert.IsTrue(policy.Complete(AdSlot.GoldBooster, AdOutcome.Rewarded).Ok);
            Assert.AreEqual(b.AD_BOOSTER_GOLD_MULT, policy.StageGoldMultiplier, 1e-9);
            Assert.AreEqual((long)b.AD_BOOSTER_SECONDS, policy.BoosterSecondsLeft);

            clock.Utc += (long)b.AD_BOOSTER_SECONDS;
            Assert.AreEqual(1d, policy.StageGoldMultiplier, 1e-9);
        }

        [Test]
        public void GoldBooster_WhileActive_CannotStack()
        {
            var b = new BalanceValues { AD_BOOSTER_DAILY = 3 };
            var save = SaveDataV2.CreateNew();
            var policy = new AdSlotPolicy(b, save, new FakeClock());

            policy.Complete(AdSlot.GoldBooster, AdOutcome.Rewarded);

            Assert.AreEqual(FailReason.Busy, policy.CanUse(AdSlot.GoldBooster).Reason);
        }
    }
}
