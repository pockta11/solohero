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

            Assert.AreEqual(50d / 400d * 600d, reward.Gold, 1e-9);
            Assert.IsTrue(reward.ShowPopup);
            Assert.IsFalse(reward.GrantNow);
        }

        [Test]
        public void Compute_UnderOneMinute_GrantsWithoutPopup()
        {
            OfflineReward reward = OfflineReward.Compute(new BalanceValues(), 1, 1000, 1030);

            Assert.AreEqual(50d / 400d * 30d, reward.Gold, 1e-9);
            Assert.IsFalse(reward.ShowPopup);
            Assert.IsTrue(reward.GrantNow);
        }

        [Test]
        public void Compute_WithinTwiceCap_UsesCapSeconds()
        {
            var balance = new BalanceValues();
            long now = 100 + balance.OFFLINE_CAP + 50;
            OfflineReward reward = OfflineReward.Compute(balance, 1, 100, now);

            Assert.AreEqual(50d / 400d * balance.OFFLINE_CAP, reward.Gold, 1e-9);
            Assert.IsTrue(reward.ShowPopup);
            Assert.IsFalse(reward.ResetQuitTime);
        }

        [Test]
        public void Compute_BeyondTwiceCap_Resets()
        {
            var balance = new BalanceValues();
            OfflineReward reward = OfflineReward.Compute(balance, 1, 100, 100 + balance.OFFLINE_CAP * 2L + 1);

            Assert.AreEqual(0d, reward.Gold);
            Assert.IsTrue(reward.ResetQuitTime);
            Assert.IsFalse(reward.ShowPopup);
        }

        [Test]
        public void Compute_NegativeElapsed_Resets()
        {
            OfflineReward reward = OfflineReward.Compute(new BalanceValues(), 1, 500, 400);

            Assert.IsTrue(reward.ResetQuitTime);
            Assert.AreEqual(0d, reward.Gold);
        }
    }
}
