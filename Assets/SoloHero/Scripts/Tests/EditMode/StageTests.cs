using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    public sealed class StageTests
    {
        private static HeroStats TankStats(BalanceValues c) =>
            new HeroStats(1_000_000d, 50d, 10_000d, c.ATKSPD_BASE, 0d);

        private static StageRunner CreateRunner(BalanceValues c) =>
            new StageRunner(c, new FixedRandom(0.99d), TankStats(c));

        private static StageRunner CreateRunner(BalanceValues c, HeroStats stats) =>
            new StageRunner(c, new FixedRandom(0.99d), stats);

        [Test]
        public void IsBoss_EveryTenthGlobalStage_IsTrue()
        {
            var c = new BalanceValues();
            Assert.IsFalse(StageIndex.IsBoss(1, c.STAGES_PER_CHAPTER));
            Assert.IsFalse(StageIndex.IsBoss(9, c.STAGES_PER_CHAPTER));
            Assert.IsTrue(StageIndex.IsBoss(10, c.STAGES_PER_CHAPTER));
            Assert.IsTrue(StageIndex.IsBoss(20, c.STAGES_PER_CHAPTER));
        }

        [Test]
        public void ToGlobal_ChapterOneStageTen_IsTen()
        {
            var c = new BalanceValues();
            Assert.AreEqual(10, StageIndex.ToGlobal(1, 10, c.STAGES_PER_CHAPTER));
            Assert.AreEqual(11, StageIndex.ToGlobal(2, 1, c.STAGES_PER_CHAPTER));
        }

        [Test]
        public void ThemeIndex_ChapterSix_WrapsToZero()
        {
            Assert.AreEqual(0, StageIndex.ThemeIndex(1, 5));
            Assert.AreEqual(0, StageIndex.ThemeIndex(6, 5));
            Assert.AreEqual(4, StageIndex.ThemeIndex(5, 5));
        }

        [Test]
        public void Begin_NormalStage_UsesKillTargetEight()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(1);

            Assert.AreEqual(StageState.Running, runner.State);
            Assert.AreEqual(c.KILL_TARGET_NORMAL, runner.KillTarget);
            Assert.IsFalse(runner.IsBoss);
        }

        [Test]
        public void Begin_BossStage_StartsBossIntroWithSingleKill()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(10);

            Assert.AreEqual(StageState.BossIntro, runner.State);
            Assert.AreEqual(1, runner.KillTarget);
            Assert.IsTrue(runner.IsBoss);
        }

        [Test]
        public void Tick_BossIntro_SpawnsBossWithFifteenTimesHp()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(10);
            runner.Tick(c.BOSS_INTRO_TIME);

            Assert.AreEqual(StageState.BossTimer, runner.State);
            Assert.AreEqual(1, runner.World.AliveCount);
            EnemyBrain boss = runner.World.NearestEnemyToTheRight();
            Assert.IsNotNull(boss);
            Assert.IsTrue(boss.IsBoss);
            double expected = Formulas.EnemyHp(c, 10) * c.BOSS_HP_MULT;
            Assert.AreEqual(expected, boss.MaxHp, 1e-6);
            Assert.AreEqual(c.BOSS_TIME_LIMIT, runner.BossTimerRemaining, 1e-4);
        }

        [Test]
        public void Tick_BossTimerExpiry_Fails()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(10);
            runner.Tick(c.BOSS_INTRO_TIME);
            runner.Tick(c.BOSS_TIME_LIMIT + 0.01f);

            Assert.AreEqual(StageState.Failed, runner.State);
        }

        [Test]
        public void Tick_BossKillOnTimerExpiryFrame_Clears()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(10);
            runner.Tick(c.BOSS_INTRO_TIME);
            EnemyBrain boss = runner.World.NearestEnemyToTheRight();
            boss.TakeDamage(boss.Hp);
            runner.Tick(c.BOSS_TIME_LIMIT + 1f);

            Assert.AreEqual(StageState.Clearing, runner.State);
        }

        [Test]
        public void Tick_LastEnemyDeadAndHeroDeadSameFrame_Clears()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(1);

            int guard = 0;
            while (runner.Kills < c.KILL_TARGET_NORMAL - 1 && guard++ < 1000)
            {
                if (runner.World.AliveCount == 0)
                {
                    runner.Tick(guard == 1 ? 0f : c.SPAWN_INTERVAL);
                    continue;
                }

                EnemyBrain one = runner.World.NearestEnemyToTheRight();
                one.TakeDamage(one.Hp);
                runner.Tick(0f);
            }

            Assert.AreEqual(StageState.Running, runner.State);
            Assert.AreEqual(c.KILL_TARGET_NORMAL - 1, runner.Kills);

            if (runner.World.AliveCount == 0)
                runner.Tick(c.SPAWN_INTERVAL);

            EnemyBrain last = runner.World.NearestEnemyToTheRight();
            Assert.IsNotNull(last);
            last.TakeDamage(last.Hp);
            runner.Hero.Kill();
            runner.Tick(0f);

            Assert.AreEqual(StageState.Clearing, runner.State);
        }

        [Test]
        public void Tick_HeroDeathAlone_FailsAfterDeathAnim()
        {
            var c = new BalanceValues();
            var frail = new HeroStats(10d, 1d, 0d, 1d, 0d);
            StageRunner runner = CreateRunner(c, frail);
            runner.Begin(1);
            runner.Hero.Kill();
            runner.Tick(c.DEATH_ANIM_TIME);

            Assert.AreEqual(StageState.Failed, runner.State);
        }

        [Test]
        public void ChooseRetreat_AfterBossFail_FarmsPreviousStage()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(10);
            runner.Tick(c.BOSS_INTRO_TIME);
            runner.Tick(c.BOSS_TIME_LIMIT + 0.01f);
            Assert.AreEqual(StageState.Failed, runner.State);

            runner.ChooseRetreat();

            Assert.IsTrue(runner.RetreatMode);
            Assert.AreEqual(9, runner.GlobalStage);
            Assert.AreEqual(StageState.Retreat, runner.State);
        }

        [Test]
        public void Tick_NormalStage_SpawnsUpToMaxAlive()
        {
            var c = new BalanceValues();
            var stats = new HeroStats(1_000_000d, 0d, 10_000d, 0d, 0d);
            StageRunner runner = CreateRunner(c, stats);
            runner.Begin(1);
            runner.Skills.SetCooldown(SkillSlot.Slot1, 999f);
            runner.Skills.SetCooldown(SkillSlot.Slot2, 999f);
            runner.Skills.SetCooldown(SkillSlot.Slot3, 999f);

            float step = c.SPAWN_INTERVAL + 0.05f;
            for (int i = 0; i < c.SPAWN_MAX_ALIVE; i++)
                runner.Tick(i == 0 ? 0f : step);

            Assert.AreEqual(c.SPAWN_MAX_ALIVE, runner.World.AliveCount);
            runner.Tick(step);
            Assert.AreEqual(c.SPAWN_MAX_ALIVE, runner.World.AliveCount);
        }

        [Test]
        public void Tick_ClearDelay_AdvancesToNextStage()
        {
            var c = new BalanceValues();
            StageRunner runner = CreateRunner(c);
            runner.Begin(1);

            int guard = 0;
            while (runner.State != StageState.Clearing && guard++ < 500)
            {
                for (int i = 0; i < runner.World.SlotCount; i++)
                {
                    EnemyBrain e = runner.World.GetSlot(i);
                    if (e.IsAlive) e.TakeDamage(e.Hp);
                }

                runner.Tick(0f);
                if (runner.State == StageState.Running)
                    runner.Tick(c.SPAWN_INTERVAL);
            }

            Assert.AreEqual(StageState.Clearing, runner.State);
            runner.Tick(c.STAGE_CLEAR_DELAY);
            Assert.AreEqual(2, runner.GlobalStage);
            Assert.AreEqual(StageState.Running, runner.State);
        }

        private sealed class FixedRandom : IRandom
        {
            private readonly double _value;

            public FixedRandom(double value) => _value = value;

            public double NextDouble() => _value;

            public int Next(int maxExclusive) => 0;
        }
    }
}
