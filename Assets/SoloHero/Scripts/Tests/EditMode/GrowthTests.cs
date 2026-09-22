using System;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class GrowthTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;

            public void RequestSave() => RequestCount++;
        }

        [Test]
        public void TryUpgrade_NotEnoughGold_DoesNotMutate()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 50d;
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var service = new UpgradeService(data, balance, save);

            Result result = service.TryUpgrade(UpgradeLane.Hp);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.NotEnoughGold, result.Reason);
            Assert.AreEqual(50d, data.gold);
            Assert.AreEqual(0, data.upgradeHp);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryUpgrade_SpdAtMax_ReturnsMaxLevel()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 1e9d;
            data.upgradeSpd = 100;
            var balance = new BalanceValues();
            var save = new FakeSaveRequester();
            var service = new UpgradeService(data, balance, save);

            Result result = service.TryUpgrade(UpgradeLane.Spd);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.MaxLevel, result.Reason);
            Assert.AreEqual(100, data.upgradeSpd);
            Assert.AreEqual(1e9d, data.gold);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryUpgrade_EnoughGold_UsesFormulasCostAndMutates()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 1000d;
            data.upgradeAtk = 2;
            var balance = new BalanceValues();
            double expectedCost = Formulas.UpgradeCost(balance, UpgradeLane.Atk, 2);
            var save = new FakeSaveRequester();
            var service = new UpgradeService(data, balance, save);
            UpgradeLane raisedLane = (UpgradeLane)(-1);
            int raisedLevel = -1;
            service.LaneUpgraded += (lane, level) =>
            {
                raisedLane = lane;
                raisedLevel = level;
            };

            Result result = service.TryUpgrade(UpgradeLane.Atk);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1000d - expectedCost, data.gold, 1e-9);
            Assert.AreEqual(3, data.upgradeAtk);
            Assert.AreEqual(UpgradeLane.Atk, raisedLane);
            Assert.AreEqual(3, raisedLevel);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void TryUpgrade_HpUnbounded_AllowsAboveOneHundred()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 1e12d;
            data.upgradeHp = 100;
            var service = new UpgradeService(data, new BalanceValues(), new FakeSaveRequester());

            Result result = service.TryUpgrade(UpgradeLane.Hp);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(101, data.upgradeHp);
        }

        [Test]
        public void Compute_HeroLevelOne_HasZeroLevelBonus()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(balance, 1, 0, 0, 0, 0);

            Assert.AreEqual(balance.HP_BASE, stats.Hp, 1e-9);
            Assert.AreEqual(balance.ATK_BASE, stats.Atk, 1e-9);
            Assert.AreEqual(balance.DEF_BASE, stats.Def, 1e-9);
            Assert.AreEqual(balance.ATKSPD_BASE, stats.AtkSpd, 1e-9);
            Assert.AreEqual(balance.CRIT_RATE_BASE, stats.CritRate, 1e-9);
        }

        [Test]
        public void Compute_UpgradeLevels_AppliesStatMult()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(balance, 1, 2, 1, 3, 0);

            Assert.AreEqual(balance.HP_BASE * Math.Pow(balance.UPG_STAT_MULT, 2), stats.Hp, 1e-9);
            Assert.AreEqual(balance.ATK_BASE * Math.Pow(balance.UPG_STAT_MULT, 1), stats.Atk, 1e-9);
            Assert.AreEqual(balance.DEF_BASE * Math.Pow(balance.UPG_STAT_MULT, 3), stats.Def, 1e-9);
        }

        [Test]
        public void Compute_HeroLevelTwo_AddsLevelGain()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(balance, 2, 0, 0, 0, 0);

            Assert.AreEqual(balance.HP_BASE + balance.LEVEL_HP_GAIN, stats.Hp, 1e-9);
            Assert.AreEqual(balance.ATK_BASE + balance.LEVEL_ATK_GAIN, stats.Atk, 1e-9);
        }

        [Test]
        public void Compute_SpdAtMaxLevel_CapsAtBasePlusGainTimesMax()
        {
            var balance = new BalanceValues();
            double expectedCap = balance.ATKSPD_BASE + balance.UPG_GAIN_SPD * balance.UPG_MAX_LEVEL_SPD;
            HeroStats stats = StatAggregator.Compute(balance, 1, 0, 0, 0, balance.UPG_MAX_LEVEL_SPD);

            Assert.AreEqual(expectedCap, stats.AtkSpd, 1e-9);
            Assert.AreEqual(3.0d, stats.AtkSpd, 1e-9);
        }

        [Test]
        public void Compute_Buffs_ApplyOnceAfterEquipment()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(
                balance, 1, 0, 0, 0, 0,
                swordMult: 2d,
                buffs: new BuffSet(0.3d));

            Assert.AreEqual(balance.ATK_BASE * 2d * 1.3d, stats.Atk, 1e-9);
        }

        [Test]
        public void Compute_Crit_IsAdditive()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(
                balance, 1, 0, 0, 0, 0,
                bootsCritBonus: 10d);

            Assert.AreEqual(balance.CRIT_RATE_BASE + 10d, stats.CritRate, 1e-9);
        }

        [Test]
        public void AddExp_EnoughForOneLevel_RaisesHeroLevel()
        {
            var data = SaveDataV2.CreateNew();
            var balance = new BalanceValues();
            var service = new HeroLevelService(data, balance);
            int raisedTo = 0;
            service.HeroLeveledUp += level => raisedTo = level;

            double need = HeroLevelService.ExpRequired(balance, 1);
            int gained = service.AddExp(need);

            Assert.AreEqual(1, gained);
            Assert.AreEqual(2, data.heroLevel);
            Assert.AreEqual(0d, data.heroExp, 1e-9);
            Assert.AreEqual(2, raisedTo);
        }

        [Test]
        public void ExpRequired_LevelOne_EqualsBase()
        {
            var balance = new BalanceValues();
            Assert.AreEqual(balance.EXP_REQ_BASE, HeroLevelService.ExpRequired(balance, 1), 1e-9);
        }

        [Test]
        public void TryLevelUp_NotEnoughGold_DoesNotMutate()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 0d;
            data.skillLevel1 = 1;
            var save = new FakeSaveRequester();
            var service = new SkillLevelService(data, new BalanceValues(), save);

            Result result = service.TryLevelUp(SkillSlot.Slot1);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.NotEnoughGold, result.Reason);
            Assert.AreEqual(1, data.skillLevel1);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryLevelUp_LockedSkill_ReturnsLocked()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 1e9d;
            data.heroLevel = 1;
            var service = new SkillLevelService(data, new BalanceValues());

            Result result = service.TryLevelUp(SkillSlot.Slot2);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
        }

        [Test]
        public void TryLevelUp_AtMax_ReturnsMaxLevel()
        {
            var data = SaveDataV2.CreateNew();
            data.gold = 1e9d;
            data.skillLevel1 = 10;
            var service = new SkillLevelService(data, new BalanceValues());

            Result result = service.TryLevelUp(SkillSlot.Slot1);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.MaxLevel, result.Reason);
        }

        [Test]
        public void DamageMultiplier_LevelOne_EqualsBase()
        {
            var balance = new BalanceValues();
            Assert.AreEqual(balance.SKILL_MULT_1, SkillLevelService.DamageMultiplier(balance, SkillSlot.Slot1, 1), 1e-9);
        }

        [Test]
        public void DamageMultiplier_LevelTen_AddsNineGains()
        {
            var balance = new BalanceValues();
            double expected = balance.SKILL_MULT_1 * (1d + balance.SKILL_LEVEL_GAIN / 100d * 9d);
            Assert.AreEqual(expected, SkillLevelService.DamageMultiplier(balance, SkillSlot.Slot1, 10), 1e-9);
            Assert.AreEqual(5.7d, expected, 1e-9);
        }

        [Test]
        public void TryBegin_WhileCooling_ReturnsOnCooldown()
        {
            var balance = new BalanceValues();
            var cd = new SkillCooldown();
            Assert.IsTrue(cd.TryBegin(SkillSlot.Slot1, balance).Ok);

            Result second = cd.TryBegin(SkillSlot.Slot1, balance);

            Assert.IsFalse(second.Ok);
            Assert.AreEqual(FailReason.OnCooldown, second.Reason);
        }

        [Test]
        public void Tick_AfterFullDuration_IsReady()
        {
            var balance = new BalanceValues();
            var cd = new SkillCooldown();
            cd.TryBegin(SkillSlot.Slot1, balance);

            cd.Tick(balance.SKILL_CD_1);

            Assert.IsTrue(cd.IsReady(SkillSlot.Slot1));
            Assert.AreEqual(0f, cd.Remaining(SkillSlot.Slot1));
        }
    }
}
