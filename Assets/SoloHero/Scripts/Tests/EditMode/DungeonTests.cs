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
    public sealed class DungeonTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime Local = new DateTime(2026, 10, 2, 9, 0, 0);

            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => Local;
        }

        private sealed class FixedRandom : IRandom
        {
            private readonly double _value;

            public FixedRandom(double value) => _value = value;

            public double NextDouble() => _value;

            public int Next(int maxExclusive) => 0;
        }

        private static StageRunner Runner(BalanceValues b, SaveDataV2 save)
        {
            var runner = new StageRunner(b, new FixedRandom(0.99d), new HeroStats(1_000_000d, 10_000d, 10_000d, b.ATKSPD_BASE, 0d), save);
            // The game lands the hit on the swing (CombatSession); tests wire it the same way as the simulator.
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            return runner;
        }

        private static void Run(StageRunner runner, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.05f) runner.Tick(0.05f);
        }

        [Test]
        public void GoldDungeon_RunsForTheTimeLimit_PaysPerKillAndReturns()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 12;
            save.farmingStage = 12;
            StageRunner runner = Runner(b, save);
            runner.Resume(12, false);
            double gold = save.gold;

            Assert.IsTrue(runner.StartDungeon(DungeonKind.Gold));
            Assert.AreEqual(StageState.Dungeon, runner.State);
            Assert.IsFalse(runner.FarmAt(5), "stage moves are refused inside a dungeon");
            DungeonKind ended = DungeonKind.None;
            runner.DungeonEnded += (kind, earned) => ended = kind;

            Run(runner, b.DUNGEON_TIME + 0.1f);
            Assert.AreEqual(StageState.DungeonResult, runner.State);
            Assert.AreEqual(DungeonKind.Gold, ended);
            Assert.Greater(runner.Kills, 4);
            Assert.AreEqual(runner.Kills * Formulas.DungeonGoldPerKill(b, 12), save.gold - gold, 1e-6);
            Assert.AreEqual(save.gold - gold, runner.DungeonEarned, 1e-6);

            Run(runner, b.DUNGEON_RESULT_TIME + 0.1f);
            Assert.IsFalse(runner.InDungeon);
            Assert.AreEqual(12, runner.GlobalStage);
        }

        [Test]
        public void ExpDungeon_GrantsHeroExp()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 3;
            save.farmingStage = 3;
            StageRunner runner = Runner(b, save);
            runner.Resume(3, false);
            int level = save.heroLevel;
            double exp = save.heroExp;

            Assert.IsTrue(runner.StartDungeon(DungeonKind.Exp));
            Run(runner, 5f);
            Assert.IsTrue(save.heroLevel > level || save.heroExp > exp);
        }

        [Test]
        public void TryEnter_SpendsTicketsPerDayAndRefusesDuringARun()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var dungeons = new DungeonService(b, save, clock);
            StageRunner runner = Runner(b, save);
            runner.Resume(1, false);
            // D-143: closed until the first boss falls.
            Assert.AreEqual(FailReason.Locked, dungeons.TryEnter(DungeonKind.Gold, runner).Reason);
            save.highestStage = b.DUNGEON_UNLOCK_STAGE;

            Assert.AreEqual(b.DUNGEON_DAILY_TICKETS, dungeons.Remaining(DungeonKind.Gold));
            Assert.IsTrue(dungeons.TryEnter(DungeonKind.Gold, runner).Ok);
            Assert.AreEqual(FailReason.Busy, dungeons.TryEnter(DungeonKind.Exp, runner).Reason);
            Assert.AreEqual(b.DUNGEON_DAILY_TICKETS - 1, dungeons.Remaining(DungeonKind.Gold));
            Assert.AreEqual(b.DUNGEON_DAILY_TICKETS, dungeons.Remaining(DungeonKind.Exp), "a refused entry costs nothing");

            for (int i = 1; i < b.DUNGEON_DAILY_TICKETS; i++)
            {
                runner.Resume(1, false);
                Assert.IsTrue(dungeons.TryEnter(DungeonKind.Gold, runner).Ok);
            }

            runner.Resume(1, false);
            Assert.AreEqual(FailReason.DailyLimit, dungeons.TryEnter(DungeonKind.Gold, runner).Reason);
            clock.Local = clock.Local.AddDays(1);
            Assert.AreEqual(b.DUNGEON_DAILY_TICKETS, dungeons.Remaining(DungeonKind.Gold));
        }

        [Test]
        public void Sweep_NeedsARecord_ThenPaysTheBestRunAtTodaysRate()
        {
            // D-161 (L-12): a finished run sets the record; a sweep spends an entry for record x reward per kill.
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 12;
            save.farmingStage = 12;
            var dungeons = new DungeonService(b, save, new FakeClock());
            Assert.AreEqual(FailReason.Locked, dungeons.TrySweep(DungeonKind.Gold, out _).Reason, "no record yet");

            StageRunner runner = Runner(b, save);
            runner.Resume(12, false);
            Assert.IsTrue(dungeons.TryEnter(DungeonKind.Gold, runner).Ok);
            Run(runner, b.DUNGEON_TIME + 0.1f);
            int kills = runner.Kills;
            Assert.Greater(kills, 0);
            Assert.AreEqual(kills, dungeons.Best(DungeonKind.Gold));
            Assert.AreEqual(0, dungeons.Best(DungeonKind.Exp), "records are per dungeon");

            save.farmingStage = 20;
            double gold = save.gold;
            int stones = save.breakStones;
            int left = dungeons.Remaining(DungeonKind.Gold);
            Assert.IsTrue(dungeons.TrySweep(DungeonKind.Gold, out double earned).Ok);
            Assert.AreEqual(kills * Formulas.DungeonGoldPerKill(b, 20), earned, 1e-6, "the record at today's farming stage");
            Assert.AreEqual(gold + earned, save.gold, 1e-6);
            Assert.AreEqual(stones + b.DUNGEON_STONES, save.breakStones);
            Assert.AreEqual(left - 1, dungeons.Remaining(DungeonKind.Gold));
            Assert.AreEqual(FailReason.DailyLimit, dungeons.TrySweep(DungeonKind.Gold, out _).Reason);

            DungeonService.RecordRun(save, DungeonKind.Gold, kills - 1);
            Assert.AreEqual(kills, save.dungeonGoldBest, "a worse run keeps the record");
        }

        [Test]
        public void ScoreTier_DoublesFromTheBase_AndPaysMoreStonesEachTier()
        {
            // D-165 (L-13): tier 1 from SCORE_TIER_BASE chapter-boss HP of damage, one more each doubling.
            var b = new BalanceValues();
            Assert.AreEqual(0, DungeonService.ScoreTier(b, 0d));
            Assert.AreEqual(0, DungeonService.ScoreTier(b, b.SCORE_TIER_BASE * 0.99));
            Assert.AreEqual(1, DungeonService.ScoreTier(b, b.SCORE_TIER_BASE));
            Assert.AreEqual(2, DungeonService.ScoreTier(b, b.SCORE_TIER_BASE * 2));
            Assert.AreEqual(4, DungeonService.ScoreTier(b, 1d), "a whole chapter boss in 30 s");
            Assert.AreEqual(b.SCORE_TIER_MAX, DungeonService.ScoreTier(b, 1e30));
            Assert.AreEqual(b.DUNGEON_STONES, DungeonService.ScoreStones(b, 0));
            Assert.AreEqual(b.SCORE_STONES_BASE, DungeonService.ScoreStones(b, 1));
            Assert.AreEqual(b.SCORE_STONES_BASE + 3 * b.SCORE_STONES_PER_TIER, DungeonService.ScoreStones(b, 4));
            Assert.AreEqual(1d, DungeonService.ScoreTierStart(b, 4), 1e-12);
        }

        [Test]
        public void ScoreDungeon_TrainingBossOutlastsTheTimer_PaysByTierAndSweeps()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 14;
            save.farmingStage = 14;
            var dungeons = new DungeonService(b, save, new FakeClock());
            StageRunner runner = Runner(b, save);
            runner.Resume(14, false);
            Assert.AreEqual(b.SCORE_DAILY_TICKETS, dungeons.Remaining(DungeonKind.Score));
            int stones = save.breakStones;
            DungeonKind ended = DungeonKind.None;
            runner.DungeonEnded += (kind, earned) => ended = kind;

            Assert.IsTrue(dungeons.TryEnter(DungeonKind.Score, runner).Ok);
            Assert.AreEqual(20, runner.GlobalStage, "the farming stage's chapter boss");
            Run(runner, b.BOSS_INTRO_TIME + b.SCORE_DUNGEON_TIME + 0.2f);
            Assert.AreEqual(DungeonKind.Score, ended);
            Assert.Greater(runner.ScoreDamage, 0d);
            Assert.Less(runner.ScoreDamage, runner.ScoreUnit * b.SCORE_HP_MULT, "the training boss outlasts the run");
            int tier = DungeonService.ScoreTier(b, runner.ScoreDamage / runner.ScoreUnit);
            Assert.AreEqual(tier, runner.ScoreTier);
            Assert.AreEqual(stones + DungeonService.ScoreStones(b, tier), save.breakStones);
            Assert.AreEqual(tier, dungeons.Best(DungeonKind.Score));
            Assert.AreEqual(b.SCORE_DAILY_TICKETS - 1, dungeons.Remaining(DungeonKind.Score));

            if (tier > 0)
            {
                int before = save.breakStones;
                Assert.IsTrue(dungeons.TrySweep(DungeonKind.Score, out double paid).Ok);
                Assert.AreEqual(DungeonService.ScoreStones(b, tier), (int)paid);
                Assert.AreEqual(before + (int)paid, save.breakStones);
            }
        }
    }
}
