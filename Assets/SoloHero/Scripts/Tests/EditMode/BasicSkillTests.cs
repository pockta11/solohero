using NUnit.Framework;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-093: the basic skill replaces the plain attack - nearest targets in range, up to the target count.</summary>
    public sealed class BasicSkillTests
    {
        private sealed class FixedRandom : IRandom
        {
            private readonly double _value;
            public FixedRandom(double value) => _value = value;
            public double NextDouble() => _value;
            public int Next(int maxExclusive) => 0;
        }

        private static HeroStats BaseStats(BalanceValues c) =>
            new HeroStats(c.HP_BASE, c.ATK_BASE, c.DEF_BASE, c.ATKSPD_BASE, c.CRIT_RATE_BASE);

        private static HeroBrain Swing(BalanceValues c, CombatWorld world)
        {
            var hero = new HeroBrain(c, new FixedRandom(0.99d), BaseStats(c));
            world.BindHero(hero);
            hero.Tick(0f, world);
            hero.Tick(0f, world);
            hero.OnHitFrame(world);
            return hero;
        }

        private static EnemyBrain Spawn(BalanceValues c, CombatWorld world, double x)
        {
            Assert.IsTrue(world.TryActivateSlot(out EnemyBrain enemy));
            enemy.Reset(c, 1000d, 5d, c.ENEMY_ATK_INTERVAL, x, false);
            return enemy;
        }

        [Test]
        public void OnHitFrame_FourInRange_HitsTheNearestThree()
        {
            var c = new BalanceValues();
            var world = new CombatWorld(c);
            EnemyBrain a = Spawn(c, world, 0.5d);
            EnemyBrain b = Spawn(c, world, 1.0d);
            EnemyBrain d = Spawn(c, world, 2.2d);
            EnemyBrain far = Spawn(c, world, 1.5d);

            Swing(c, world);

            double hit = DamageCalc.HeroHit(BaseStats(c), false, c, c.BASIC_SKILL_MULT);
            Assert.AreEqual(1000d - hit, a.Hp, 1e-9);
            Assert.AreEqual(1000d - hit, b.Hp, 1e-9);
            Assert.AreEqual(1000d - hit, far.Hp, 1e-9);
            Assert.AreEqual(1000d, d.Hp, 1e-9, "the fourth, farthest enemy is past the target count");
        }

        [Test]
        public void OnHitFrame_BeyondBasicRange_IsNotHit()
        {
            var c = new BalanceValues();
            var world = new CombatWorld(c);
            EnemyBrain near = Spawn(c, world, 0.5d);
            EnemyBrain outside = Spawn(c, world, c.BASIC_SKILL_RANGE + 0.5d);

            Swing(c, world);

            Assert.Less(near.Hp, 1000d);
            Assert.AreEqual(1000d, outside.Hp, 1e-9);
        }
    }
}
