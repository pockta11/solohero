using System;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Tests.EditMode
{
    public sealed class FormulasTests
    {
        [Test]
        public void EnemyHp_StageOne_EqualsBase()
        {
            var c = new BalanceValues();
            Assert.AreEqual(30d, Formulas.EnemyHp(c, 1), 1e-9);
        }

        [Test]
        public void EnemyHp_StageTwo_AppliesGrowth()
        {
            var c = new BalanceValues();
            Assert.AreEqual(30d * Math.Pow(1.10d, 1), Formulas.EnemyHp(c, 2), 1e-9);
        }

        [Test]
        public void EnemyAtk_StageTwo_AppliesGrowth()
        {
            var c = new BalanceValues();
            Assert.AreEqual(5d, Formulas.EnemyAtk(c, 1), 1e-9);
            Assert.AreEqual(5d * Math.Pow(1.10d, 1), Formulas.EnemyAtk(c, 2), 1e-9);
        }

        [Test]
        public void StageGold_StageOne_EqualsBase()
        {
            var c = new BalanceValues();
            Assert.AreEqual(50d, Formulas.StageGold(c, 1), 1e-9);
        }

        [Test]
        public void StageGold_StageTwo_AppliesGrowth()
        {
            var c = new BalanceValues();
            Assert.AreEqual(50d * Math.Pow(1.08d, 1), Formulas.StageGold(c, 2), 1e-9);
        }

        [Test]
        public void UpgradeCost_HpLevelZero_EqualsBase()
        {
            var c = new BalanceValues();
            Assert.AreEqual(100d, Formulas.UpgradeCost(c, UpgradeLane.Hp, 0), 1e-9);
        }

        [Test]
        public void UpgradeCost_EachLaneLevelOne_AppliesGrowth()
        {
            var c = new BalanceValues();
            double growth = Math.Pow(1.12d, 1);
            Assert.AreEqual(100d * growth, Formulas.UpgradeCost(c, UpgradeLane.Hp, 1), 1e-9);
            Assert.AreEqual(150d * growth, Formulas.UpgradeCost(c, UpgradeLane.Atk, 1), 1e-9);
            Assert.AreEqual(150d * growth, Formulas.UpgradeCost(c, UpgradeLane.Def, 1), 1e-9);
            Assert.AreEqual(200d * growth, Formulas.UpgradeCost(c, UpgradeLane.Spd, 1), 1e-9);
        }

        [Test]
        public void HitDamage_DefEqualsRef_IsHalf()
        {
            var c = new BalanceValues();
            Assert.AreEqual(4d, Formulas.HitDamage(c, 8d, 64d), 1e-9);
        }

        [Test]
        public void HitDamage_ZeroDef_EqualsAttack()
        {
            var c = new BalanceValues();
            Assert.AreEqual(5d, Formulas.HitDamage(c, 5d, 0d), 1e-9);
        }

        [Test]
        public void HitDamage_HugeDef_IsOne()
        {
            var c = new BalanceValues();
            Assert.AreEqual(1d, Formulas.HitDamage(c, 1d, 1e9d), 1e-9);
        }
    }
}
