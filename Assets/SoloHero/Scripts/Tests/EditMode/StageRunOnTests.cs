using System;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-125: between normal stages the hero runs on and the next stage starts where it is; a boss starts at 0.</summary>
    public sealed class StageRunOnTests
    {
        private static StageRunner Strong(BalanceValues b, SaveDataV2 save)
        {
            PetService.EnsureOwned(save);
            var runner = new StageRunner(b, new SystemRandom(new Random(3)), new HeroStats(1e9, 1e7, 1e7, b.ATKSPD_BASE, 0d), save);
            // No CombatLoadout: it would replace these stats with the new save's.
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            return runner;
        }

        private static void TickUntil(StageRunner runner, Func<bool> done)
        {
            for (int i = 0; i < 40000 && !done(); i++) runner.Tick(0.05f);
            Assert.IsTrue(done(), "timed out");
        }

        [Test]
        public void ClearingANormalStage_RunsOn_AndTheNextStageKeepsThePosition()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            StageRunner runner = Strong(b, save);
            runner.Begin(1);

            TickUntil(runner, () => runner.State == StageState.Clearing);
            double atClear = runner.Hero.X;
            runner.Tick(0.5f);
            Assert.Greater(runner.Hero.X, atClear, "the hero runs on through the pause");

            TickUntil(runner, () => runner.GlobalStage == 2);
            Assert.Greater(runner.Hero.X, atClear, "stage 2 starts where the run got to, not back at 0");
        }

        [Test]
        public void TheBossStage_StartsOverAtZero()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = b.STAGES_PER_CHAPTER - 1;
            StageRunner runner = Strong(b, save);
            runner.Begin(b.STAGES_PER_CHAPTER - 1);

            TickUntil(runner, () => runner.GlobalStage == b.STAGES_PER_CHAPTER);
            Assert.IsTrue(runner.IsBoss);
            Assert.AreEqual(0d, runner.Hero.X, 1e-9);
        }
    }
}
