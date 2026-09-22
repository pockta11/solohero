using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class KillExpTests
    {
        [Test]
        public void Grant_StageOne_AddsEnemyExpBase()
        {
            var data = SaveDataV2.CreateNew();
            var balance = new BalanceValues();

            Result result = KillExp.Grant(data, balance, 1);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(balance.ENEMY_EXP_BASE, data.heroExp, 1e-9);
            Assert.AreEqual(1, data.heroLevel);
        }

        [Test]
        public void Grant_EnoughExp_RaisesHeroLevel()
        {
            var data = SaveDataV2.CreateNew();
            var balance = new BalanceValues();
            balance.ENEMY_EXP_BASE = HeroLevelService.ExpRequired(balance, 1);

            Result result = KillExp.Grant(data, balance, 1);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(2, data.heroLevel);
            Assert.AreEqual(0d, data.heroExp, 1e-9);
        }

        [Test]
        public void Grant_GLessThanOne_FailsWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.heroExp = 7d;
            data.heroLevel = 3;
            var balance = new BalanceValues();

            Result result = KillExp.Grant(data, balance, 0);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual(7d, data.heroExp, 1e-9);
            Assert.AreEqual(3, data.heroLevel);
        }
    }
}
