using System;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

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

        private static LaneLevels Lanes(int hp = 0, int atk = 0, int def = 0, int spd = 0) =>
            new LaneLevels { Hp = hp, Atk = atk, Def = def, Spd = spd };

        [Test]
        public void Compute_NoApNoLanes_IsTheBaseStats()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(balance, default, Lanes());

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
            HeroStats stats = StatAggregator.Compute(balance, default, Lanes(hp: 2, atk: 1, def: 3));

            Assert.AreEqual(balance.HP_BASE * Math.Pow(balance.UPG_STAT_MULT, 2), stats.Hp, 1e-9);
            Assert.AreEqual(balance.ATK_BASE * Math.Pow(balance.UPG_STAT_MULT, 1), stats.Atk, 1e-9);
            Assert.AreEqual(balance.DEF_BASE * Math.Pow(balance.UPG_STAT_MULT, 3), stats.Def, 1e-9);
        }

        [Test]
        public void Compute_Ap_AddsMainToAtkAndVitalityToHp()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(balance, new ApPoints(4, 2), Lanes());

            Assert.AreEqual(balance.HP_BASE + 2 * balance.AP_VIT_HP, stats.Hp, 1e-9);
            Assert.AreEqual(balance.ATK_BASE + 4 * balance.AP_MAIN_ATK, stats.Atk, 1e-9);
        }

        [Test]
        public void Compute_AutoAp_GivesTheOldPerLevelGains()
        {
            // D-141: the auto split reproduces the flat +0.5 ATK / +5 HP a level of D-053.
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(balance, ApPoints.Auto(balance, 31), Lanes());

            Assert.AreEqual(balance.HP_BASE + 5d * 30, stats.Hp, 1e-9);
            Assert.AreEqual(balance.ATK_BASE + 0.5d * 30, stats.Atk, 1e-9);
        }

        [Test]
        public void Compute_SpdAtMaxLevel_CapsAtBasePlusGainTimesMax()
        {
            var balance = new BalanceValues();
            double expectedCap = balance.ATKSPD_BASE + balance.UPG_GAIN_SPD * balance.UPG_MAX_LEVEL_SPD;
            HeroStats stats = StatAggregator.Compute(balance, default, Lanes(spd: balance.UPG_MAX_LEVEL_SPD));

            Assert.AreEqual(expectedCap, stats.AtkSpd, 1e-9);
            Assert.AreEqual(3.0d, stats.AtkSpd, 1e-9);
        }

        [Test]
        public void Compute_Buffs_ApplyOnceAfterEquipment()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(
                balance, default, Lanes(),
                swordMult: 2d,
                buffs: new BuffSet(0.3d));

            Assert.AreEqual(balance.ATK_BASE * 2d * 1.3d, stats.Atk, 1e-9);
        }

        [Test]
        public void Compute_Crit_IsAdditive()
        {
            var balance = new BalanceValues();
            HeroStats stats = StatAggregator.Compute(
                balance, default, Lanes(),
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
        public void ComputeStatsAfterUpgrade_Hp_MatchesStatsAfterRealUpgrade()
        {
            var balance = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.upgradeHp = 4;
            save.upgradeAtk = 2;

            HeroStats preview = CombatLoadout.ComputeStatsAfterUpgrade(balance, save, UpgradeLane.Hp);
            HeroStats now = CombatLoadout.ComputeStats(balance, save);
            save.upgradeHp = 5;
            HeroStats after = CombatLoadout.ComputeStats(balance, save);

            Assert.AreEqual(after.Hp, preview.Hp, 1e-9);
            Assert.AreEqual(now.Hp * balance.UPG_STAT_MULT, preview.Hp, 1e-9);
            Assert.AreEqual(now.Atk, preview.Atk, 1e-9);
            Assert.AreEqual(now.Def, preview.Def, 1e-9);
        }

        [Test]
        public void ComputeStatsAfterUpgrade_Spd_AddsOneGainAndLeavesSaveUntouched()
        {
            var balance = new BalanceValues();
            var save = SaveDataV2.CreateNew();

            HeroStats preview = CombatLoadout.ComputeStatsAfterUpgrade(balance, save, UpgradeLane.Spd);

            Assert.AreEqual(balance.ATKSPD_BASE + balance.UPG_GAIN_SPD, preview.AtkSpd, 1e-9);
            Assert.AreEqual(0, save.upgradeSpd);
            Assert.AreEqual(balance.ATKSPD_BASE, CombatLoadout.ComputeStats(balance, save).AtkSpd, 1e-9);
        }
    }
}
