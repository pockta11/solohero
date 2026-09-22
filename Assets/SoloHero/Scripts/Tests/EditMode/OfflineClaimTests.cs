using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class OfflineClaimTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;

            public void RequestSave() => RequestCount++;
        }

        [Test]
        public void Apply_ShowPopup_AddsGoldAndSetsQuitTime()
        {
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var claim = new OfflineClaim(balance, save);
            var data = SaveDataV2.CreateNew();
            data.gold = 100d;
            data.lastQuitTimeUtc = 1000;
            var reward = new OfflineReward(75d, true, false, false);

            Result result = claim.Apply(data, reward, 2000, adDoubled: false);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(175d, data.gold, 1e-9);
            Assert.AreEqual(2000, data.lastQuitTimeUtc);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void Apply_AdDoubled_UsesOfflineAdMult()
        {
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var claim = new OfflineClaim(balance, save);
            var data = SaveDataV2.CreateNew();
            data.gold = 10d;
            var reward = new OfflineReward(50d, true, false, false);

            Result result = claim.Apply(data, reward, 5000, adDoubled: true);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(10d + 50d * balance.OFFLINE_AD_MULT, data.gold, 1e-9);
            Assert.AreEqual(5000, data.lastQuitTimeUtc);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void Apply_ShowPopupFalse_DoesNotMutate()
        {
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var claim = new OfflineClaim(balance, save);
            var data = SaveDataV2.CreateNew();
            data.gold = 40d;
            data.lastQuitTimeUtc = 111;
            var reward = new OfflineReward(99d, false, false, false);

            Result result = claim.Apply(data, reward, 9999, adDoubled: true);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Busy, result.Reason);
            Assert.AreEqual(40d, data.gold);
            Assert.AreEqual(111, data.lastQuitTimeUtc);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void Apply_GrantNowReward_DoesNotDoublePay()
        {
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var claim = new OfflineClaim(balance, save);
            var data = SaveDataV2.CreateNew();
            data.gold = 12.5d;
            data.lastQuitTimeUtc = 200;
            // Boot already applied GrantNow; claim must refuse.
            var reward = new OfflineReward(12.5d, false, true, false);

            Result result = claim.Apply(data, reward, 300, adDoubled: false);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Busy, result.Reason);
            Assert.AreEqual(12.5d, data.gold);
            Assert.AreEqual(200, data.lastQuitTimeUtc);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void Apply_WithoutSaveRequester_StillMutates()
        {
            var claim = new OfflineClaim(new BalanceValues());
            var data = SaveDataV2.CreateNew();
            data.gold = 0d;
            var reward = new OfflineReward(20d, true, false, false);

            Result result = claim.Apply(data, reward, 42, adDoubled: false);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(20d, data.gold, 1e-9);
            Assert.AreEqual(42, data.lastQuitTimeUtc);
        }

        [Test]
        public void Apply_ComputedShowPopup_UsesComputeGold()
        {
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var claim = new OfflineClaim(balance, save);
            var data = SaveDataV2.CreateNew();
            data.gold = 0d;
            data.farmingStage = 1;
            OfflineReward reward = OfflineReward.Compute(balance, 1, 1000, 1600);

            Result result = claim.Apply(data, reward, 1600, adDoubled: false);

            Assert.IsTrue(reward.ShowPopup);
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(50d / 400d * 600d, data.gold, 1e-9);
            Assert.AreEqual(1600, data.lastQuitTimeUtc);
            Assert.AreEqual(1, save.RequestCount);
        }
    }
}
