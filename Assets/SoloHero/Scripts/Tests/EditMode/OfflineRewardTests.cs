using NUnit.Framework;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;

namespace SoloHero.Tests.EditMode
{
    public sealed class OfflineRewardTests
    {
        [Test]
        public void Compute_FirstRun_HasNoReward()
        {
            OfflineReward reward = OfflineReward.Compute(new BalanceValues(), 1, 0, 1000);

            Assert.AreEqual(0d, reward.Gold);
            Assert.IsFalse(reward.ShowPopup);
            Assert.IsFalse(reward.GrantNow);
            Assert.IsFalse(reward.ResetQuitTime);
        }

        [Test]
        public void Compute_TenMinutes_IsStageGoldOverDivisor()
        {
            var balance = new BalanceValues();
            OfflineReward reward = OfflineReward.Compute(balance, 1, 1000, 1600);

            Assert.AreEqual(new BalanceValues().STAGE_GOLD_BASE / new BalanceValues().OFFLINE_DIVISOR * 600d, reward.Gold, 1e-9);
            Assert.IsTrue(reward.ShowPopup);
            Assert.IsFalse(reward.GrantNow);
        }

        [Test]
        public void Compute_UnderOneMinute_GrantsWithoutPopup()
        {
            OfflineReward reward = OfflineReward.Compute(new BalanceValues(), 1, 1000, 1030);

            Assert.AreEqual(new BalanceValues().STAGE_GOLD_BASE / new BalanceValues().OFFLINE_DIVISOR * 30d, reward.Gold, 1e-9);
            Assert.IsFalse(reward.ShowPopup);
            Assert.IsTrue(reward.GrantNow);
        }

        [Test]
        public void Compute_WithinTwiceCap_UsesCapSeconds()
        {
            var balance = new BalanceValues();
            long now = 100 + balance.OFFLINE_CAP + 50;
            OfflineReward reward = OfflineReward.Compute(balance, 1, 100, now);

            Assert.AreEqual(new BalanceValues().STAGE_GOLD_BASE / new BalanceValues().OFFLINE_DIVISOR * balance.OFFLINE_CAP, reward.Gold, 1e-9);
            Assert.IsTrue(reward.ShowPopup);
            Assert.IsFalse(reward.ResetQuitTime);
        }

        [Test]
        public void Compute_BeyondTwiceCap_PaysCap()
        {
            // D-133: the clock is trusted, so a day away pays the full cap instead of nothing.
            var balance = new BalanceValues();
            OfflineReward reward = OfflineReward.Compute(balance, 1, 100, 100 + balance.OFFLINE_CAP * 2L + 1);

            Assert.AreEqual(balance.STAGE_GOLD_BASE / balance.OFFLINE_DIVISOR * balance.OFFLINE_CAP, reward.Gold, 1e-9);
            Assert.AreEqual((long)balance.OFFLINE_CAP, reward.CountedSeconds);
            Assert.IsTrue(reward.ShowPopup);
            Assert.IsFalse(reward.ResetQuitTime);
        }

        [Test]
        public void Compute_NegativeElapsedOnServerTime_Resets()
        {
            OfflineReward reward = OfflineReward.Compute(new BalanceValues(), 1, 500, 400);

            Assert.IsTrue(reward.ResetQuitTime);
            Assert.AreEqual(0d, reward.Gold);
            Assert.IsFalse(reward.ShowPopup);
        }

        [Test]
        public void Compute_NegativeElapsedOnDeviceClock_KeepsQuitTime()
        {
            OfflineReward reward = OfflineReward.Compute(new BalanceValues(), 1, 500, 400, serverTime: false);

            Assert.IsFalse(reward.ResetQuitTime);
            Assert.AreEqual(0d, reward.Gold);
            Assert.IsFalse(reward.GrantNow);
        }

        [Test]
        public void Compute_CountedSeconds_IsElapsedCappedAtOfflineCap()
        {
            var b = new BalanceValues();

            OfflineReward under = OfflineReward.Compute(b, 5, 1000, 1000 + 3600);
            OfflineReward over = OfflineReward.Compute(b, 5, 1000, 1000 + b.OFFLINE_CAP + 500);

            Assert.AreEqual(3600L, under.CountedSeconds);
            Assert.AreEqual((long)b.OFFLINE_CAP, over.CountedSeconds);
        }
    }
}
