using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class StageRewardTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;

            public void RequestSave() => RequestCount++;
        }

        [Test]
        public void ApplyClear_GLessThanOne_FailsWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 10d;
            data.gem = 5d;
            data.heroExp = 3d;
            data.highestStage = 4;
            data.farmingStage = 2;
            var save = new FakeSaveRequester();
            var reward = new StageReward(data, new BalanceValues(), save);

            Result result = reward.ApplyClear(0);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual(10d, data.gold, 1e-9);
            Assert.AreEqual(5d, data.gem, 1e-9);
            Assert.AreEqual(3d, data.heroExp, 1e-9);
            Assert.AreEqual(4, data.highestStage);
            Assert.AreEqual(2, data.farmingStage);
            Assert.AreEqual(0, data.chapterFirstClearFlags.Count);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void ApplyClear_NormalStage_GrantsStageGoldAndRaisesHighest()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 100d;
            data.highestStage = 1;
            data.farmingStage = 1;
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var reward = new StageReward(data, balance, save);
            double expectedGold = Formulas.StageGold(balance, 3);

            Result result = reward.ApplyClear(3);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(100d + expectedGold, data.gold, 1e-9);
            Assert.AreEqual(0d, data.heroExp, 1e-9);
            Assert.AreEqual(0d, data.gem, 1e-9);
            Assert.AreEqual(3, data.highestStage);
            Assert.AreEqual(1, data.farmingStage);
            Assert.AreEqual(0, data.chapterFirstClearFlags.Count);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void ApplyClear_DoesNotChangeFarmingStage()
        {
            var data = SaveDataV2.CreateNew();
            data.farmingStage = 7;
            var reward = new StageReward(data, new BalanceValues(), new FakeSaveRequester());

            reward.ApplyClear(10);

            Assert.AreEqual(7, data.farmingStage);
        }

        [Test]
        public void ApplyClear_GrantsZeroHeroExp_WhenNoExpPerStageConstant()
        {
            var data = SaveDataV2.CreateNew();
            data.heroExp = 12d;
            var reward = new StageReward(data, new BalanceValues(), new FakeSaveRequester());

            Result result = reward.ApplyClear(1);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(12d, data.heroExp, 1e-9);
        }

        [Test]
        public void ApplyClear_BossFirstClear_GrantsChapterGemAndSetsFlag()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 0d;
            data.gem = 0d;
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var reward = new StageReward(data, balance, save);
            double expectedGold = Formulas.StageGold(balance, 10);

            Result result = reward.ApplyClear(10);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expectedGold, data.gold, 1e-9);
            Assert.AreEqual(balance.CHAPTER_CLEAR_GEM, data.gem, 1e-9);
            Assert.AreEqual(10, data.highestStage);
            Assert.IsTrue(data.chapterFirstClearFlags.Count >= 1);
            Assert.IsTrue(data.chapterFirstClearFlags[0]);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void ApplyClear_BossRepeat_GrantsGoldButNotGem()
        {
            var data = SaveDataV2.CreateNew();
            data.chapterFirstClearFlags.Add(true);
            data.gem = 60d;
            data.highestStage = 10;
            var balance = new BalanceValues();
            var reward = new StageReward(data, balance, new FakeSaveRequester());
            double expectedGold = Formulas.StageGold(balance, 10);

            Result result = reward.ApplyClear(10);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(expectedGold, data.gold, 1e-9);
            Assert.AreEqual(60d, data.gem, 1e-9);
            Assert.IsTrue(data.chapterFirstClearFlags[0]);
            Assert.AreEqual(10, data.highestStage);
        }

        [Test]
        public void ApplyClear_LowerThanHighest_DoesNotLowerHighest()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 15;
            var reward = new StageReward(data, new BalanceValues(), new FakeSaveRequester());

            reward.ApplyClear(5);

            Assert.AreEqual(15, data.highestStage);
        }

        [Test]
        public void ApplyClear_ChapterTwoBossFirstClear_UsesSecondFlagSlot()
        {
            var data = SaveDataV2.CreateNew();
            var balance = new BalanceValues();
            var reward = new StageReward(data, balance, new FakeSaveRequester());

            Result result = reward.ApplyClear(20);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(balance.CHAPTER_CLEAR_GEM, data.gem, 1e-9);
            Assert.IsTrue(data.chapterFirstClearFlags.Count >= 2);
            Assert.IsFalse(data.chapterFirstClearFlags[0]);
            Assert.IsTrue(data.chapterFirstClearFlags[1]);
            Assert.AreEqual(20, data.highestStage);
        }
    }
}
