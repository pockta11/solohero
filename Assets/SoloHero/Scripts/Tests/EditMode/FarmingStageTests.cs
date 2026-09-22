using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class FarmingStageTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;

            public void RequestSave() => RequestCount++;
        }

        [Test]
        public void TrySet_StageBelowOne_FailsLockedWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 5;
            data.farmingStage = 3;
            var save = new FakeSaveRequester();
            var service = new FarmingStageService(save);

            Result result = service.TrySet(data, 0);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual(3, data.farmingStage);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TrySet_StageAboveHighest_FailsLockedWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 4;
            data.farmingStage = 2;
            var save = new FakeSaveRequester();
            var service = new FarmingStageService(save);

            Result result = service.TrySet(data, 5);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual(2, data.farmingStage);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TrySet_CreateNew_OnlyStageOneAllowed()
        {
            var data = SaveDataV2.CreateNew();
            var save = new FakeSaveRequester();
            var service = new FarmingStageService(save);

            Assert.AreEqual(1, data.farmingStage);
            Assert.AreEqual(1, data.highestStage);

            Result above = service.TrySet(data, 2);
            Assert.IsFalse(above.Ok);
            Assert.AreEqual(FailReason.Locked, above.Reason);
            Assert.AreEqual(1, data.farmingStage);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TrySet_ValidStage_SetsAndRequestsSave()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 10;
            data.farmingStage = 1;
            var save = new FakeSaveRequester();
            var service = new FarmingStageService(save);

            Result result = service.TrySet(data, 7);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(7, data.farmingStage);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void TrySet_AtHighestStage_Succeeds()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 8;
            data.farmingStage = 3;
            var save = new FakeSaveRequester();
            var service = new FarmingStageService(save);

            Result result = service.TrySet(data, 8);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(8, data.farmingStage);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void TrySet_SameStage_SuccessWithoutRequestSave()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 6;
            data.farmingStage = 4;
            var save = new FakeSaveRequester();
            var service = new FarmingStageService(save);

            Result result = service.TrySet(data, 4);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(4, data.farmingStage);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TrySet_NullSaveRequester_StillSucceeds()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 3;
            data.farmingStage = 1;
            var service = new FarmingStageService();

            Result result = service.TrySet(data, 2);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(2, data.farmingStage);
        }
    }
}
