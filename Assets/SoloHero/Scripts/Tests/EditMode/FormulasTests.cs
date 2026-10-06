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
            Assert.AreEqual(c.ENEMY_HP_BASE, Formulas.EnemyHp(c, 1), 1e-9);
        }

        [Test]
        public void EnemyHp_StageTwo_AppliesGrowth()
        {
            var c = new BalanceValues();
            Assert.AreEqual(c.ENEMY_HP_BASE * c.ENEMY_HP_GROWTH, Formulas.EnemyHp(c, 2), 1e-9);
        }

        [Test]
        public void EnemyAtk_StageTwo_AppliesGrowth()
        {
            var c = new BalanceValues();
            Assert.AreEqual(5d, Formulas.EnemyAtk(c, 1), 1e-9);
            Assert.AreEqual(c.ENEMY_ATK_BASE * c.ENEMY_ATK_GROWTH, Formulas.EnemyAtk(c, 2), 1e-9);
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
            Assert.AreEqual(c.STAGE_GOLD_BASE * c.STAGE_GOLD_GROWTH, Formulas.StageGold(c, 2), 1e-9);
        }

        [Test]
        public void UpgradeCost_HpLevelZero_EqualsBase()
        {
            var c = new BalanceValues();
            Assert.AreEqual(c.UPG_BASE_HP, Formulas.UpgradeCost(c, UpgradeLane.Hp, 0), 1e-9);
        }

        [Test]
        public void UpgradeCost_EachLaneLevelOne_AppliesGrowth()
        {
            var c = new BalanceValues();
            double growth = c.UPG_COST_GROWTH;
            Assert.AreEqual(c.UPG_BASE_HP * growth, Formulas.UpgradeCost(c, UpgradeLane.Hp, 1), 1e-9);
            Assert.AreEqual(c.UPG_BASE_ATK * growth, Formulas.UpgradeCost(c, UpgradeLane.Atk, 1), 1e-9);
            Assert.AreEqual(c.UPG_BASE_DEF * growth, Formulas.UpgradeCost(c, UpgradeLane.Def, 1), 1e-9);
            Assert.AreEqual(c.UPG_BASE_SPD * growth, Formulas.UpgradeCost(c, UpgradeLane.Spd, 1), 1e-9);
        }

        [Test]
        public void HitDamage_DefEqualsRef_IsHalf()
        {
            var c = new BalanceValues();
            Assert.AreEqual(4d, Formulas.HitDamage(c, 8d, c.DEF_REF_MULT * 8d), 1e-9);
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
