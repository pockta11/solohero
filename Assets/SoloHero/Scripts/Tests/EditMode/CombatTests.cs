using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Tests.EditMode
{
    public sealed class CombatTests
    {
        private static HeroStats BaseStats(BalanceValues c) =>
            new HeroStats(c.HP_BASE, c.ATK_BASE, c.DEF_BASE, c.ATKSPD_BASE, c.CRIT_RATE_BASE);

        private static HeroBrain CreateHero(BalanceValues c, IRandom rng = null) =>
            new HeroBrain(c, rng ?? new FixedRandom(0.99d), BaseStats(c));

        [Test]
        public void Tick_NoEnemy_AdvancesAtMoveSpeed()
        {
            var c = new BalanceValues();
            HeroBrain hero = CreateHero(c);
            var world = new CombatWorld(c);
            world.BindHero(hero);

            hero.Tick(1f, world);

            Assert.AreEqual(HeroState.Advance, hero.State);
            Assert.AreEqual(c.MOVE_SPEED, hero.X, 1e-6);
        }

        [Test]
        public void Tick_EnemyInRange_EntersEngage()
        {
            var c = new BalanceValues();
            HeroBrain hero = CreateHero(c);
            var world = new CombatWorld(c);
            world.BindHero(hero);
            EnemyBrain enemy;
            Assert.IsTrue(world.TryActivateSlot(out enemy));
            enemy.Reset(c, 30d, 5d, c.ENEMY_ATK_INTERVAL, c.ATTACK_RANGE, false);

            hero.Tick(0f, world);

            Assert.AreEqual(HeroState.Engage, hero.State);
            Assert.AreEqual(0d, hero.X, 1e-9);
        }

        [Test]
        public void OnHitFrame_InRange_DealsHeroHitDamage()
        {
            var c = new BalanceValues();
            var rng = new FixedRandom(0.99d);
            HeroBrain hero = CreateHero(c, rng);
            var world = new CombatWorld(c);
            world.BindHero(hero);
            EnemyBrain enemy;
            world.TryActivateSlot(out enemy);
            enemy.Reset(c, 100d, 5d, c.ENEMY_ATK_INTERVAL, c.ATTACK_RANGE * 0.5d, false);

            bool attacked = false;
            hero.AttackRequested += () => attacked = true;
            hero.Tick(0f, world);
            hero.Tick(0f, world);
            Assert.IsTrue(attacked);

            hero.OnHitFrame(world);

            double expected = DamageCalc.HeroHit(BaseStats(c), false, c);
            Assert.AreEqual(100d - expected, enemy.Hp, 1e-9);
        }

        [Test]
        public void OnHitFrame_OutOfRange_DoesNotDamage()
        {
            var c = new BalanceValues();
            HeroBrain hero = CreateHero(c);
            var world = new CombatWorld(c);
            world.BindHero(hero);
            EnemyBrain enemy;
            world.TryActivateSlot(out enemy);
            enemy.Reset(c, 100d, 5d, c.ENEMY_ATK_INTERVAL, c.ATTACK_RANGE * 0.5d, false);

            hero.Tick(0f, world);
            hero.Tick(0f, world);
            enemy.Reset(c, 100d, 5d, c.ENEMY_ATK_INTERVAL, c.ATTACK_RANGE + 5d, false);

            hero.OnHitFrame(world);

            Assert.AreEqual(100d, enemy.Hp, 1e-9);
        }

        [Test]
        public void EnemyHit_UsesFormulasHitDamage()
        {
            var c = new BalanceValues();
            double expected = Formulas.HitDamage(c, 8d, 64d);
            Assert.AreEqual(expected, DamageCalc.EnemyHit(c, 8d, 64d), 1e-9);
        }

        [Test]
        public void ApplyDamage_UsesRatioDefenseViaEnemyHit()
        {
            var c = new BalanceValues();
            HeroBrain hero = CreateHero(c);
            double dmg = DamageCalc.EnemyHit(c, c.ENEMY_ATK_BASE, hero.Def);
            double hpBefore = hero.Hp;

            hero.ApplyDamage(dmg);

            Assert.AreEqual(hpBefore - dmg, hero.Hp, 1e-9);
            Assert.AreEqual(HeroState.Hit, hero.State);
        }

        [Test]
        public void Targeting_PrefersNearestToTheRight()
        {
            var c = new BalanceValues();
            HeroBrain hero = CreateHero(c);
            var world = new CombatWorld(c);
            world.BindHero(hero);
            EnemyBrain far;
            EnemyBrain near;
            world.TryActivateSlot(out far);
            world.TryActivateSlot(out near);
            far.Reset(c, 10d, 1d, 1f, 5d, false);
            near.Reset(c, 10d, 1d, 1f, 2d, false);

            EnemyBrain picked = Targeting.FindNearestToTheRight(world, 0d);

            Assert.AreSame(near, picked);
        }

        [Test]
        public void SpawnScheduler_AtMaxAlive_WaitsWithoutConsuming()
        {
            var c = new BalanceValues();
            var spawner = new SpawnScheduler(c);
            spawner.Reset(c.KILL_TARGET_NORMAL, false);

            Assert.IsTrue(spawner.TryConsumeSpawn(0));
            Assert.IsFalse(spawner.TryConsumeSpawn(c.SPAWN_MAX_ALIVE));
            Assert.AreEqual(1, spawner.Spawned);
        }

        [Test]
        public void SpawnScheduler_RespectsKillTargetTotal()
        {
            var c = new BalanceValues();
            var spawner = new SpawnScheduler(c);
            spawner.Reset(c.KILL_TARGET_NORMAL, false);

            for (int i = 0; i < c.KILL_TARGET_NORMAL; i++)
            {
                spawner.Tick(c.SPAWN_INTERVAL);
                Assert.IsTrue(spawner.TryConsumeSpawn(0));
            }

            spawner.Tick(c.SPAWN_INTERVAL);
            Assert.IsFalse(spawner.TryConsumeSpawn(0));
            Assert.AreEqual(c.KILL_TARGET_NORMAL, spawner.Spawned);
        }

        [Test]
        public void CombatWorld_NeverExceedsMaxAliveSlots()
        {
            var c = new BalanceValues();
            var world = new CombatWorld(c);
            int activated = 0;
            for (int i = 0; i < c.SPAWN_MAX_ALIVE + 2; i++)
            {
                EnemyBrain brain;
                if (world.TryActivateSlot(out brain))
                {
                    brain.Reset(c, 1d, 1d, 1f, i + 1d, false);
                    activated++;
                }
            }

            Assert.AreEqual(c.SPAWN_MAX_ALIVE, activated);
            Assert.AreEqual(c.SPAWN_MAX_ALIVE, world.AliveCount);
        }

        [Test]
        public void RollCrit_BelowRate_IsCritical()
        {
            var stats = new HeroStats(100d, 10d, 10d, 1d, 5d);
            Assert.IsTrue(DamageCalc.RollCrit(stats, new FixedRandom(0.01d)));
            Assert.IsFalse(DamageCalc.RollCrit(stats, new FixedRandom(0.99d)));
        }

        [Test]
        public void TryBeginSkill_WhenDead_ReturnsBusy()
        {
            var c = new BalanceValues();
            HeroBrain hero = CreateHero(c);
            hero.Kill();
            Result result = hero.TryBeginSkill();
            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Busy, result.Reason);
        }

        [Test]
        public void TryCast_OnCooldown_ReturnsOnCooldown()
        {
            var c = new BalanceValues();
            var skills = new SkillAutoCaster(c);
            HeroBrain hero = CreateHero(c);
            var world = new CombatWorld(c);
            world.BindHero(hero);
            EnemyBrain enemy;
            world.TryActivateSlot(out enemy);
            enemy.Reset(c, 1000d, 1d, 1f, c.ATTACK_RANGE * 0.5d, false);

            Assert.IsTrue(skills.TryCast(SkillSlot.Slot1, hero, world).Ok);
            Result second = skills.TryCast(SkillSlot.Slot1, hero, world);
            Assert.IsFalse(second.Ok);
            Assert.AreEqual(FailReason.OnCooldown, second.Reason);
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
