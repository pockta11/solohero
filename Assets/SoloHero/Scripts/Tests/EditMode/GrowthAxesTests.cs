using System;
using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// D-140 job terms, D-141 level-up AP, D-142 crit lanes and skill crits, D-143 limit breaks and breakthrough
    /// stones, D-144 tower boss HP.
    /// </summary>
    public sealed class GrowthAxesTests
    {
        private sealed class FixedRandom : IRandom
        {
            private readonly double _value;

            public FixedRandom(double value) => _value = value;

            public double NextDouble() => _value;

            public int Next(int maxExclusive) => 0;
        }

        private sealed class FakeClock : IClock
        {
            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => new DateTime(2026, 10, 8, 9, 0, 0);
        }

        private static SaveDataV2 Save(int heroLevel = 1)
        {
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.heroLevel = heroLevel;
            return save;
        }

        [TearDown]
        public void ResetStrings()
        {
            Strings.Load(null);
            Strings.UseTermSet("");
        }

        // D-141 AP ------------------------------------------------------------------------------------------------------

        [Test]
        public void Ap_AutoSplitsEveryLevelTwoToOne()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            Assert.IsFalse(HeroAp.Settle(b, save), "level 1 has no AP");

            save.heroLevel = 4;
            Assert.IsTrue(HeroAp.Settle(b, save));
            Assert.AreEqual(9, HeroAp.Total(b, 4));
            Assert.AreEqual(6, save.apMain);
            Assert.AreEqual(3, save.apVit);
            Assert.AreEqual(0, HeroAp.Unspent(b, save));
        }

        [Test]
        public void Ap_LevelUpSpendsInAutoMode_ManualKeepsThePoints()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            var levels = new HeroLevelService(save, b);

            levels.AddExp(HeroLevelService.ExpRequired(b, 1));
            Assert.AreEqual(2, save.apMain);
            Assert.AreEqual(1, save.apVit);

            save.apManual = true;
            levels.AddExp(HeroLevelService.ExpRequired(b, 2));
            Assert.AreEqual(2, save.apMain, "manual mode waits for the player");
            Assert.AreEqual(b.AP_PER_LEVEL, HeroAp.Unspent(b, save));
        }

        [Test]
        public void Ap_ManualAddSwitchAndReset()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(11);
            save.apManual = true;
            var ap = new ApService(save, b);

            Assert.IsTrue(ap.TryAdd(ApStat.Main, 25).Ok);
            Assert.AreEqual(25, save.apMain);
            Assert.AreEqual(5, ap.Unspent);
            Assert.IsTrue(ap.TryAdd(ApStat.Vit, 99).Ok, "asking for more spends what is left");
            Assert.AreEqual(5, save.apVit);
            Assert.AreEqual(FailReason.NoApPoints, ap.TryAdd(ApStat.Main).Reason);

            Assert.AreEqual(FailReason.NotEnoughGem, ap.TryReset().Reason);
            save.gem = b.AP_RESET_GEM;
            Assert.IsTrue(ap.TryReset().Ok);
            Assert.AreEqual(0d, save.gem);
            Assert.AreEqual(30, ap.Unspent);

            ap.SetManual(false);
            Assert.AreEqual(20, save.apMain);
            Assert.AreEqual(10, save.apVit);
        }

        [Test]
        public void Ap_OldSaveGetsEveryPastLevel_StatsUnchanged()
        {
            // A save from before AP: level 31, nothing allocated. Boot settles it into the old per-level gains.
            var b = new BalanceValues();
            SaveDataV2 save = Save(31);
            Assert.IsTrue(HeroAp.Settle(b, save));
            HeroStats stats = StatAggregator.Compute(b, save);
            Assert.AreEqual(b.ATK_BASE + 0.5d * 30, stats.Atk, 1e-9);
            Assert.AreEqual(b.HP_BASE + 5d * 30, stats.Hp, 1e-9);
        }

        [Test]
        public void Ap_ImpossibleAllocationIsRepaired()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(2);
            save.apMain = 999;
            Assert.IsTrue(HeroAp.Settle(b, save));
            Assert.AreEqual(2, save.apMain);
            Assert.AreEqual(1, save.apVit);
        }

        // D-142 crit lanes and crits --------------------------------------------------------------------------------------

        [Test]
        public void CritLanes_OpenWithHeroLevels_AndAddCritRateAndDamage()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(b.UPG_UNLOCK_LV_CRIT - 1);
            save.gold = 1e9;
            var lanes = new UpgradeService(save, b);

            Assert.AreEqual(FailReason.Locked, lanes.TryUpgrade(UpgradeLane.Crit).Reason);
            save.heroLevel = b.UPG_UNLOCK_LV_CRIT;
            Assert.IsTrue(lanes.TryUpgrade(UpgradeLane.Crit).Ok);
            Assert.AreEqual(FailReason.Locked, lanes.TryUpgrade(UpgradeLane.CritDmg).Reason);
            save.heroLevel = b.UPG_UNLOCK_LV_CRITDMG;
            Assert.IsTrue(lanes.TryUpgrade(UpgradeLane.CritDmg).Ok);

            HeroStats stats = StatAggregator.Compute(b, save);
            Assert.AreEqual(b.CRIT_RATE_BASE + b.UPG_GAIN_CRIT, stats.CritRate, 1e-9);
            Assert.AreEqual(b.UPG_GAIN_CRITDMG, stats.CritDamageBonus, 1e-9);
            Assert.AreEqual(b.UPG_BASE_CRIT, Formulas.UpgradeCost(b, UpgradeLane.Crit, 0), 1e-9);
        }

        [Test]
        public void CritRateLane_StopsAtItsMax()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(99);
            save.gold = 1e12;
            save.upgradeCrit = b.UPG_MAX_LEVEL_CRIT;
            var lanes = new UpgradeService(save, b);
            Assert.AreEqual(LaneState.Max, lanes.State(UpgradeLane.Crit));
            Assert.AreEqual(FailReason.MaxLevel, lanes.TryUpgrade(UpgradeLane.Crit).Reason);
        }

        [Test]
        public void SkillHit_RollsTheHeroCrit()
        {
            var b = new BalanceValues();
            var hero = new HeroBrain(b, new FixedRandom(0d), new HeroStats(1000d, 100d, b.DEF_BASE, b.ATKSPD_BASE, 50d, 0.5d));
            var world = new CombatWorld(b);
            world.BindHero(hero);
            Assert.IsTrue(world.TryActivateSlot(out EnemyBrain enemy));
            enemy.Reset(b, 1e9, 10d, 1f, 1d, false);
            SkillDef thunder = SkillCatalog.Find("thunder");
            HitKind kind = HitKind.Normal;
            double dealt = 0d;
            world.HitLanded += (target, amount, k) => { kind = k; dealt = amount; };

            var crits = new SkillAutoCaster(b, new FixedRandom(0d));
            crits.SetSlot(0, thunder, 1);
            crits.TryCast(0, hero, world);
            Assert.AreEqual(HitKind.SkillCrit, kind);
            Assert.AreEqual(100d * thunder.DamageMult * (b.CRIT_MULT + 0.5d), dealt, 1e-6);

            var never = new SkillAutoCaster(b, new FixedRandom(0.99d));
            never.SetSlot(0, thunder, 1);
            never.TryCast(0, hero, world);
            Assert.AreEqual(HitKind.Skill, kind);
            Assert.AreEqual(100d * thunder.DamageMult, dealt, 1e-6);
        }

        // D-143 limit breaks and stones -----------------------------------------------------------------------------------

        [Test]
        public void Limit_LaneStopsAtTheStep_BreakSpendsStones_ThenItGoesOn()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            save.gold = 1e12;
            var lanes = new UpgradeService(save, b);
            for (int i = 0; i < b.LIMIT_STEP; i++) Assert.IsTrue(lanes.TryUpgrade(UpgradeLane.Atk).Ok);

            Assert.AreEqual(LaneState.NeedsBreak, lanes.State(UpgradeLane.Atk));
            Assert.AreEqual(FailReason.LimitReached, lanes.TryUpgrade(UpgradeLane.Atk).Reason);
            Assert.AreEqual(FailReason.NotEnoughStones, lanes.TryBreak(UpgradeLane.Atk).Reason);

            save.breakStones = b.LIMIT_STONE_BASE * 3;
            Assert.IsTrue(lanes.TryBreak(UpgradeLane.Atk).Ok);
            Assert.AreEqual(b.LIMIT_STONE_BASE * 2, save.breakStones);
            Assert.AreEqual(1, lanes.Breaks(UpgradeLane.Atk));
            Assert.AreEqual(b.LIMIT_STONE_BASE * 2, lanes.BreakCost(UpgradeLane.Atk), "the next break costs more");
            Assert.IsTrue(lanes.TryUpgrade(UpgradeLane.Atk).Ok);
            Assert.AreEqual(FailReason.Locked, lanes.TryBreak(UpgradeLane.Atk).Reason, "nothing to break below the limit");
        }

        [Test]
        public void Limit_SaveFromBeforeBreaksKeepsItsLevels()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            save.gold = 1e12;
            save.upgradeHp = 66;
            save.upgradeAtk = 20;
            Assert.IsTrue(LaneRules.EnsureBreaks(b, save));
            Assert.AreEqual(UpgradeLanes.Count, save.laneBreaks.Count);
            Assert.IsFalse(LaneRules.EnsureBreaks(b, save), "idempotent");
            var lanes = new UpgradeService(save, b);
            Assert.AreEqual(LaneState.Open, lanes.State(UpgradeLane.Hp));
            Assert.AreEqual(LaneState.Open, lanes.State(UpgradeLane.Atk));
            Assert.IsTrue(lanes.TryUpgrade(UpgradeLane.Hp).Ok);
        }

        [Test]
        public void Stones_DungeonRunPaysAtItsEnd()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            save.highestStage = b.DUNGEON_UNLOCK_STAGE;
            save.farmingStage = 5;
            var runner = new StageRunner(b, new FixedRandom(0.99d), new HeroStats(1e6, 1e4, 1e4, b.ATKSPD_BASE, 0d), save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            runner.Resume(5, false);
            var dungeons = new DungeonService(b, save, new FakeClock());

            Assert.IsTrue(dungeons.TryEnter(DungeonKind.Gold, runner).Ok);
            for (float t = 0f; t < b.DUNGEON_TIME - 1f; t += 0.05f) runner.Tick(0.05f);
            Assert.AreEqual(0, save.breakStones, "paid when the run ends");
            for (float t = 0f; t < 2f; t += 0.05f) runner.Tick(0.05f);
            Assert.AreEqual(b.DUNGEON_STONES, save.breakStones);
            Assert.AreEqual(b.DUNGEON_STONES, runner.DungeonStones);
        }

        [Test]
        public void Stones_EveryTowerFloorPays_RareFloorsMore()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            Assert.AreEqual(b.TOWER_STONES, TowerService.Preview(b, 1).Stones);
            Assert.AreEqual(b.TOWER_STONES + b.TOWER_STONES_RARE, TowerService.Preview(b, b.TOWER_RARE_EVERY).Stones);
            TowerService.Grant(b, save, b.TOWER_RARE_EVERY);
            Assert.AreEqual(b.TOWER_STONES + b.TOWER_STONES_RARE, save.breakStones);
        }

        // D-144 tower ----------------------------------------------------------------------------------------------------

        [Test]
        public void Tower_BossHasTheTowerHpMultiplier()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save();
            save.highestStage = b.TOWER_UNLOCK_STAGE;
            var runner = new StageRunner(b, new FixedRandom(0.99d), new HeroStats(1e9, 1d, 1e7, b.ATKSPD_BASE, 0d), save);
            runner.Resume(3, false);
            Assert.IsTrue(runner.StartTower(5));
            for (int i = 0; i < 40; i++) runner.Tick(0.05f);

            int g = TowerService.StageOf(b, 5);
            double expected = Formulas.EnemyHp(b, g) * Formulas.BossHpMult(b, g) * b.TOWER_BOSS_HP_MULT;
            EnemyBrain boss = null;
            for (int s = 0; s < runner.World.SlotCount; s++)
                if (runner.World.GetSlot(s) != null && runner.World.GetSlot(s).IsAlive) boss = runner.World.GetSlot(s);
            Assert.IsNotNull(boss);
            Assert.AreEqual(expected, boss.MaxHp, expected * 1e-9);
        }

        // D-140 terms ----------------------------------------------------------------------------------------------------

        [Test]
        public void Terms_ExpandInEveryValue_AndFollowTheJobLine()
        {
            Strings.Load(new Dictionary<string, string>
            {
                { "term.atk", "ATK" },
                { "term.atk.mage", "SPELL" },
                { "term.main", "STR" },
                { "term.main.mage", "INT" },
                { "term.main.archer", "DEX" },
                { "stat.atk", "{atk}" },
                { "bonus", "{atk} +{0}% ({main})" },
            });
            int changed = 0;
            Action count = () => changed++;
            Strings.TermsChanged += count;
            try
            {
                Assert.AreEqual("ATK", Strings.Get("stat.atk"));
                Assert.AreEqual("ATK +5% (STR)", Strings.Format("bonus", 5));

                JobTerms.Apply(JobLine.Mage);
                Assert.AreEqual("SPELL +5% (INT)", Strings.Format("bonus", 5));
                Assert.AreEqual(1, changed);

                JobTerms.Apply(JobLine.Archer);
                Assert.AreEqual("ATK +5% (DEX)", Strings.Format("bonus", 5), "a set without its own row keeps the plain word");
                Assert.AreEqual("DEX", Strings.Term("main"));

                JobTerms.Apply(JobLine.Archer);
                Assert.AreEqual(2, changed, "the same set again changes nothing");
            }
            finally
            {
                Strings.TermsChanged -= count;
            }
        }

        [Test]
        public void Weapon_FollowsTheJobLine()
        {
            Assert.AreEqual(WeaponKind.Sword, JobTerms.WeaponOf(JobLine.None));
            Assert.AreEqual(WeaponKind.Sword, JobTerms.WeaponOf(JobLine.Warrior));
            Assert.AreEqual(WeaponKind.Staff, JobTerms.WeaponOf(JobLine.Mage));
            Assert.AreEqual(WeaponKind.Bow, JobTerms.WeaponOf(JobLine.Archer));
        }
    }
}
