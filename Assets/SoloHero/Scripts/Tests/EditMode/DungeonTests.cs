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
    }
}
