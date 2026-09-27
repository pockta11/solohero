using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    public sealed class StageRunnerFlowTests
    {
        private const float Dt = 1f / 30f;

        private sealed class CountingRequester : ISaveRequester
        {
            public int Count;

            public void RequestSave() => Count++;
        }

        private static StageRunner Create(BalanceValues b, SaveDataV2 save, HeroStats stats, ISaveRequester requester = null)
        {
            var runner = new StageRunner(b, new SystemRandom(new System.Random(1)), stats, save, requester);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            return runner;
        }

        private static HeroStats Strong(BalanceValues b) => new HeroStats(1e9, 1e9, b.DEF_BASE, b.ATKSPD_BASE, 0d);

        private static HeroStats Fragile(BalanceValues b) => new HeroStats(1d, 1d, b.DEF_BASE, b.ATKSPD_BASE, 0d);

        [Test]
        public void Resume_RetreatSaved_RestoresRetreatMode()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            StageRunner runner = Create(b, save, Strong(b));

            runner.Resume(9, retreatMode: true);

            Assert.IsTrue(runner.RetreatMode);
            Assert.AreEqual(StageState.Retreat, runner.State);
            Assert.AreEqual(9, runner.GlobalStage);
        }

        [Test]
        public void Resume_RetreatOnBossStage_IgnoresRetreat()
        {
            var b = new BalanceValues();
            StageRunner runner = Create(b, SaveDataV2.CreateNew(), Strong(b));

            runner.Resume(10, retreatMode: true);

            Assert.IsFalse(runner.RetreatMode);
            Assert.AreEqual(StageState.BossIntro, runner.State);
        }

        [Test]
        public void StageClear_RequestsSave()
        {
            var b = new BalanceValues();
            var requester = new CountingRequester();
            StageRunner runner = Create(b, SaveDataV2.CreateNew(), Strong(b), requester);
            runner.Begin(1);

            for (int i = 0; i < 60 * 30 && runner.State != StageState.Clearing; i++)
                runner.Tick(Dt);

            Assert.AreEqual(StageState.Clearing, runner.State);
            Assert.AreEqual(1, requester.Count);
        }

        [Test]
        public void StepDown_AfterFailStreak_BeginsOneStageLower()
        {
            var b = new BalanceValues();
            StageRunner runner = Create(b, SaveDataV2.CreateNew(), Fragile(b));
            runner.Begin(5);

            for (int i = 0; i < 600 * 30 && !runner.PromptRetreat; i++)
                runner.Tick(Dt);
            Assert.IsTrue(runner.PromptRetreat);

            bool moved = runner.StepDown();

            Assert.IsTrue(moved);
            Assert.AreEqual(4, runner.GlobalStage);
            Assert.AreEqual(0, runner.FailStreak);
        }

        [Test]
        public void NormalDeath_AfterRetryDelay_FarmsOneStageLowerThenChallengesFrontier()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 4;
            StageRunner runner = Create(b, save, Fragile(b));
            runner.Begin(5);

            for (int i = 0; i < 600 * 30 && runner.State != StageState.Failed; i++)
                runner.Tick(Dt);
            runner.Tick(b.STAGE_RETRY_DELAY + Dt);

            Assert.IsTrue(runner.RetreatMode);
            Assert.AreEqual(StageState.Retreat, runner.State);
            Assert.AreEqual(4, runner.GlobalStage);

            runner.ChallengeBoss();

            Assert.IsFalse(runner.RetreatMode);
            Assert.AreEqual(5, runner.GlobalStage);
        }

        [Test]
        public void Tick_HeroHits_ReportEveryHitToWorld()
        {
            var b = new BalanceValues();
            StageRunner runner = Create(b, SaveDataV2.CreateNew(), Strong(b));
            int hits = 0;
            runner.World.HitLanded += (target, amount, crit) => hits++;
            runner.Begin(1);

            for (int i = 0; i < 60 * 30 && runner.State != StageState.Clearing; i++)
                runner.Tick(Dt);

            Assert.AreEqual(StageState.Clearing, runner.State);
            Assert.GreaterOrEqual(hits, b.KILL_TARGET_NORMAL);
        }

        [Test]
        public void StepDown_WithoutFailStreak_DoesNothing()
        {
            var b = new BalanceValues();
            StageRunner runner = Create(b, SaveDataV2.CreateNew(), Strong(b));
            runner.Begin(5);

            Assert.IsFalse(runner.StepDown());
            Assert.AreEqual(5, runner.GlobalStage);
        }

        [Test]
        public void FarmAt_ClearedNormalStage_FarmsThereInRetreatMode()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 14;
            StageRunner runner = Create(b, save, Strong(b));
            runner.Resume(15, false);

            Assert.IsTrue(runner.FarmAt(7));

            Assert.AreEqual(7, runner.GlobalStage);
            Assert.IsTrue(runner.RetreatMode);
        }

        [Test]
        public void FarmAt_LockedOrBossStage_ChangesNothing()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 14;
            StageRunner runner = Create(b, save, Strong(b));
            runner.Resume(15, false);

            Assert.IsFalse(runner.FarmAt(15), "not cleared yet");
            Assert.IsFalse(runner.FarmAt(10), "boss stage");
            Assert.IsFalse(runner.FarmAt(0));
            Assert.AreEqual(15, runner.GlobalStage);
            Assert.IsFalse(runner.RetreatMode);
        }
    }
}
