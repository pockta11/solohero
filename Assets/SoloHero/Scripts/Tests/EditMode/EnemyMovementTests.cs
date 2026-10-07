using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-110: enemies walk in, the nearest take the front spots, the rest queue, shooters fire from range.</summary>
    public sealed class EnemyMovementTests
    {
        private static HeroBrain Hero(BalanceValues b) =>
            new HeroBrain(b, new SystemRandom(new System.Random(1)), new HeroStats(1e6, 1d, b.DEF_BASE, b.ATKSPD_BASE, 0d));

        private static CombatWorld World(BalanceValues b, HeroBrain hero)
        {
            var world = new CombatWorld(b);
            world.BindHero(hero);
            return world;
        }

        private static EnemyBrain Walker(BalanceValues b, CombatWorld world, double x, EnemyRole role = EnemyRole.Melee)
        {
            Assert.IsTrue(world.TryActivateSlot(out EnemyBrain e));
            e.Reset(b, 1e6, 10d, 1f, x, false, 0, role, b.ENEMY_MOVE_SPEED);
            return e;
        }

        [Test]
        public void Walkers_StopAtFrontSpots_ThenQueueBehind()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            var enemies = new EnemyBrain[5];
            for (int i = 0; i < enemies.Length; i++) enemies[i] = Walker(b, world, 6d + i * 0.4d);

            for (int t = 0; t < 400; t++) world.TickEnemies(0.05f);

            for (int i = 0; i < enemies.Length; i++)
            {
                double expected = i < b.ENEMY_FRONT_SLOTS
                    ? b.ENEMY_STAND_MIN + i * b.ENEMY_STAND_STEP
                    : b.ENEMY_STAND_MIN + (b.ENEMY_FRONT_SLOTS - 1) * b.ENEMY_STAND_STEP + (i - b.ENEMY_FRONT_SLOTS + 1) * b.ENEMY_QUEUE_SPACING;
                Assert.AreEqual(expected, enemies[i].X - hero.X, 1e-6, "enemy " + i);
                Assert.IsFalse(enemies[i].IsMoving);
            }

            // The front three are in reach and fight; the queue waits.
            Assert.AreEqual(EnemyState.Attacking, enemies[0].State);
            Assert.AreEqual(EnemyState.Attacking, enemies[2].State);
            Assert.AreNotEqual(EnemyState.Attacking, enemies[4].State);
        }

        [Test]
        public void QueuedEnemy_StepsUp_WhenAFrontEnemyFalls()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            var enemies = new EnemyBrain[4];
            for (int i = 0; i < enemies.Length; i++) enemies[i] = Walker(b, world, 6d + i * 0.4d);
            for (int t = 0; t < 400; t++) world.TickEnemies(0.05f);
            double queued = enemies[3].X - hero.X;

            enemies[0].TakeDamage(1e9);
            world.ResolveDeaths();
            world.TickEnemies(0.05f);
            Assert.IsTrue(enemies[3].IsMoving);
            for (int t = 0; t < 100; t++) world.TickEnemies(0.05f);

            Assert.Less(enemies[3].X - hero.X, queued);
            Assert.AreEqual(b.ENEMY_STAND_MIN + 2 * b.ENEMY_STAND_STEP, enemies[3].X - hero.X, 1e-6);
        }

        [Test]
        public void Shooter_StopsAtRange_AndItsShotLandsAfterFlying()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain shooter = Walker(b, world, 8d, EnemyRole.Ranged);
            double full = hero.Hp;

            bool sawShot = false;
            for (int t = 0; t < 600 && !sawShot; t++)
            {
                world.TickEnemies(0.02f);
                for (int i = 0; i < CombatWorld.ShotCapacity; i++) sawShot |= world.TryGetShot(i, out _, out _);
            }

            Assert.AreEqual(b.ENEMY_RANGED_STAND, shooter.X - hero.X, 1e-6);
            Assert.IsTrue(sawShot, "a shot is in the air");
            Assert.AreEqual(full, hero.Hp, 1e-9, "nothing lands before the flight time");
            for (int t = 0; t < 60; t++) world.TickEnemies(0.02f);
            Assert.Less(hero.Hp, full);
        }

        [Test]
        public void Waves_RoleHpAveragesOne_ForEveryChapterTable()
        {
            var b = new BalanceValues();
            foreach (int g in new[] { 1, 5, 15, 35 })
            {
                double sum = 0d;
                for (int slot = 0; slot < 8; slot++)
                    sum += EnemyWaves.StageHpMult(b, g, EnemyWaves.RoleFor(g, slot, b.STAGES_PER_CHAPTER));
                Assert.AreEqual(8d, sum, 1e-9, "stage " + g);
            }

            Assert.AreEqual(EnemyRole.Melee, EnemyWaves.RoleFor(1, 3, b.STAGES_PER_CHAPTER), "the first stages are soldiers only");
            Assert.AreEqual(EnemyRole.Ranged, EnemyWaves.RoleFor(35, 7, b.STAGES_PER_CHAPTER));
        }

        [Test]
        public void Runner_Enemies_WalkInFromOffscreen()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var runner = new StageRunner(b, new SystemRandom(new System.Random(3)), CombatLoadout.ComputeStats(b, save), save);
            runner.Begin(15);
            runner.Tick(0.02f);
            EnemyBrain first = null;
            for (int i = 0; i < runner.World.SlotCount; i++)
            {
                if (runner.World.GetSlot(i).IsAlive) { first = runner.World.GetSlot(i); break; }
            }

            Assert.IsNotNull(first);
            double start = first.X;
            runner.Tick(0.2f);
            Assert.Less(first.X, start);
            Assert.IsTrue(first.IsMoving);
        }

        [Test]
        public void ChapterOneBoss_IsSofterThanLaterBosses()
        {
            var b = new BalanceValues();
            Assert.AreEqual(b.BOSS_HP_MULT_CH1, Formulas.BossHpMult(b, 10), 1e-12);
            Assert.AreEqual(b.BOSS_HP_MULT, Formulas.BossHpMult(b, 20), 1e-12);
            Assert.Less(b.BOSS_HP_MULT_CH1, b.BOSS_HP_MULT);
        }
    }
}
