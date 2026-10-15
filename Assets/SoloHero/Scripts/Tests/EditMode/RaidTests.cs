using System;
using NUnit.Framework;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-175 (L-14): the weekly raid - entries, levels, patterns, medals, the shop and the sweep.</summary>
    public sealed class RaidTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime Local = new DateTime(2026, 10, 14, 9, 0, 0); // a Wednesday

            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => Local;
        }

        private sealed class FixedRandom : IRandom
        {
            public double NextDouble() => 0.99d;

            public int Next(int maxExclusive) => 0;
        }

        private static StageRunner Runner(BalanceValues b, SaveDataV2 save, double atk, double hp = 1e12)
        {
            var runner = new StageRunner(b, new FixedRandom(), new HeroStats(hp, atk, 10d, b.ATKSPD_BASE, 0d), save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            return runner;
        }

        private static SaveDataV2 Open(BalanceValues b)
        {
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.highestStage = b.RAID_UNLOCK_STAGE;
            save.farmingStage = b.RAID_UNLOCK_STAGE - 1;
            return save;
        }

        private static void Run(StageRunner runner, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.05f) runner.Tick(0.05f);
        }

        [Test]
        public void Entries_WeeklyAndLevelGated()
        {
            var b = new BalanceValues();
            var clock = new FakeClock();
            SaveDataV2 save = SaveDataV2.CreateNew();
            var raid = new RaidService(b, save, clock);
            StageRunner runner = Runner(b, save, 1d);
            runner.Resume(1, false);
            Assert.AreEqual(FailReason.Locked, raid.TryEnter(1, runner).Reason, "closed before the unlock stage");

            save.highestStage = b.RAID_UNLOCK_STAGE;
            Assert.AreEqual(1, raid.MaxLevel);
            Assert.AreEqual(FailReason.Locked, raid.TryEnter(2, runner).Reason, "level 2 waits for a level 1 kill");
            for (int i = 0; i < b.RAID_WEEKLY_TICKETS; i++)
            {
                runner.Resume(1, false);
                Assert.IsTrue(raid.TryEnter(1, runner).Ok);
                runner.LeaveRun();
                Run(runner, b.DUNGEON_RESULT_TIME + 0.1f);
            }

            runner.Resume(1, false);
            Assert.AreEqual(FailReason.DailyLimit, raid.TryEnter(1, runner).Reason);
            clock.Local = clock.Local.AddDays(4); // Sunday: same week
            Assert.AreEqual(0, raid.Remaining);
            clock.Local = clock.Local.AddDays(1); // Monday
            Assert.AreEqual(b.RAID_WEEKLY_TICKETS, raid.Remaining);
        }

        [Test]
        public void StrongHero_KillsTheBoss_OpensTheNextLevel_PaysMedalsAndGems()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Open(b);
            var raid = new RaidService(b, save, new FakeClock());
            StageRunner runner = Runner(b, save, 1e15);
            runner.Resume(save.farmingStage, false);
            double gems = save.gem;

            Assert.IsTrue(raid.TryEnter(1, runner).Ok);
            Assert.IsTrue(runner.InRaid);
            for (float t = 0f; t < b.RAID_TIME && runner.State != StageState.DungeonResult; t += 0.05f) runner.Tick(0.05f);

            Assert.AreEqual(StageState.DungeonResult, runner.State);
            Assert.IsTrue(runner.LastRaid.Killed);
            Assert.AreEqual(1000, runner.LastRaid.Permille);
            Assert.AreEqual(RaidService.MedalsFor(b, 1, 1000, true), save.raidMedals);
            Assert.AreEqual(1, save.raidCleared);
            Assert.AreEqual(2, raid.MaxLevel);
            Assert.AreEqual(b.RAID_FIRST_GEM, save.gem - gems, 1e-9);

            Run(runner, b.DUNGEON_RESULT_TIME + 0.1f);
            Assert.IsFalse(runner.InDungeon, "back to the stage");
        }

        [Test]
        public void WeakHero_SurvivesTheTimer_IsPaidByShare_PatternsWarnThenStrike()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Open(b);
            var raid = new RaidService(b, save, new FakeClock());
            StageRunner runner = Runner(b, save, 50d);
            runner.Resume(save.farmingStage, false);
            int warned = 0, struck = 0, summons = 0;
            runner.RaidWarned += p => warned++;
            runner.RaidStruck += (p, dmg) =>
            {
                struck++;
                if (p == RaidPattern.Summon) summons++;
            };

            Assert.IsTrue(raid.TryEnter(1, runner).Ok);
            Run(runner, b.BOSS_INTRO_TIME + b.RAID_TIME + 0.2f);

            Assert.AreEqual(StageState.DungeonResult, runner.State);
            Assert.IsFalse(runner.LastRaid.Killed);
            Assert.Greater(runner.RaidDamage, 0d);
            Assert.Less(runner.LastRaid.Permille, 1000);
            Assert.AreEqual(RaidService.MedalsFor(b, 1, runner.LastRaid.Permille, false), runner.LastRaid.Medals);
            Assert.AreEqual(0, save.raidCleared);
            Assert.GreaterOrEqual(warned, 6, "a move every few seconds");
            Assert.GreaterOrEqual(struck, warned - 1);
            Assert.GreaterOrEqual(summons, 2);
            Assert.AreEqual(runner.LastRaid.Permille, raid.BestPermille(1));
        }

        [Test]
        public void Slam_MissesAHeroOutOfRange_ChargeAlwaysHits()
        {
            var b = new BalanceValues();
            var patterns = new RaidPatterns();
            patterns.Reset(b);
            var seen = new System.Collections.Generic.List<RaidPattern>();
            for (float t = 0f; t < 3 * (b.RAID_PATTERN_INTERVAL + b.RAID_TELEGRAPH) + 1f; t += 0.05f)
            {
                RaidPattern landed = patterns.Tick(0.05f, true, out RaidPattern warned);
                if (warned != RaidPattern.None) Assert.AreEqual(RaidPattern.None, landed);
                if (landed != RaidPattern.None) seen.Add(landed);
            }

            CollectionAssert.AreEqual(new[] { RaidPattern.Slam, RaidPattern.Charge, RaidPattern.Summon }, seen.GetRange(0, 3));

            patterns.Reset(b);
            for (float t = 0f; t < 30f; t += 0.05f) Assert.AreEqual(RaidPattern.None, patterns.Tick(0.05f, false, out _), "a stunned boss waits");
        }

        [Test]
        public void Shop_WeeklyLimits_MedalsCarryOver_SweepPaysAKill()
        {
            var b = new BalanceValues();
            var clock = new FakeClock();
            SaveDataV2 save = Open(b);
            var raid = new RaidService(b, save, clock);
            save.raidMedals = 1000;
            RaidShopItem first = RaidService.Shop[0];
            int awaken = save.awakenStones;
            for (int i = 0; i < first.Limit; i++) Assert.IsTrue(raid.TryBuy(0).Ok);
            Assert.AreEqual(FailReason.MaxLevel, raid.TryBuy(0).Reason);
            Assert.AreEqual(awaken + first.Limit * first.Reward.AwakenStones, save.awakenStones);
            int medals = save.raidMedals;
            clock.Local = clock.Local.AddDays(7);
            Assert.AreEqual(0, raid.Bought(0));
            Assert.AreEqual(medals, save.raidMedals);

            Assert.IsFalse(raid.CanSweep(1));
            save.raidCleared = 3;
            Assert.IsTrue(raid.TrySweep(3, out int paid).Ok);
            Assert.AreEqual(RaidService.MedalsFor(b, 3, 1000, true), paid);
            Assert.AreEqual(b.RAID_WEEKLY_TICKETS - 1, raid.Remaining);
            save.raidMedals = 0;
            Assert.AreEqual(FailReason.NotEnoughMedals, raid.TryBuy(1).Reason);
        }
    }
}
