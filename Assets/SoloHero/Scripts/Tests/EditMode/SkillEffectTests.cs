using NUnit.Framework;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Tests.EditMode
{
    /// <summary>E4-07: the three skills behave as the GDD constant table says.</summary>
    public sealed class SkillEffectTests
    {
        private const double Atk = 100d;
        private const double EnemyHp = 1e6;

        private static HeroBrain Hero(BalanceValues b, double hp = 1000d) =>
            new HeroBrain(b, new SystemRandom(new System.Random(1)), new HeroStats(hp, Atk, b.DEF_BASE, b.ATKSPD_BASE, 0d));

        private static CombatWorld World(BalanceValues b, HeroBrain hero)
        {
            var world = new CombatWorld(b);
            world.BindHero(hero);
            return world;
        }

        private static EnemyBrain Spawn(BalanceValues b, CombatWorld world, double x)
        {
            Assert.IsTrue(world.TryActivateSlot(out EnemyBrain e));
            e.Reset(b, EnemyHp, 1d, 99f, x, false);
            return e;
        }

        [Test]
        public void PowerStrike_HitsNearestForAtkTimesMultAndStartsCooldown()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain near = Spawn(b, world, 1d);
            EnemyBrain far = Spawn(b, world, 2.5d);
            var skills = new SkillAutoCaster(b);

            Assert.IsTrue(skills.TryCast(SkillSlot.Slot1, hero, world).Ok);

            Assert.AreEqual(EnemyHp - Atk * b.SKILL_MULT_1, near.Hp, 1e-6);
            Assert.AreEqual(EnemyHp, far.Hp, 1e-6);
            Assert.AreEqual(b.SKILL_CD_1, skills.CooldownRemaining(SkillSlot.Slot1), 1e-6);
        }

        [Test]
        public void PowerStrike_LevelTen_UsesLevelMultiplier()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain e = Spawn(b, world, 1d);
            var skills = new SkillAutoCaster(b);
            skills.SetLevel(SkillSlot.Slot1, b.SKILL_MAX_LEVEL);

            skills.TryCast(SkillSlot.Slot1, hero, world);

            double mult = SkillLevelService.DamageMultiplier(b, SkillSlot.Slot1, b.SKILL_MAX_LEVEL);
            Assert.AreEqual(EnemyHp - Atk * mult, e.Hp, 1e-6);
        }

        [Test]
        public void Whirlwind_HitsEveryEnemyInFrontWithinRangeOnly()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain a = Spawn(b, world, 0.5d);
            EnemyBrain c = Spawn(b, world, b.WHIRLWIND_RANGE - 0.1d);
            EnemyBrain outside = Spawn(b, world, b.WHIRLWIND_RANGE + 0.5d);
            var skills = new SkillAutoCaster(b);

            skills.TryCast(SkillSlot.Slot2, hero, world);

            double hit = Atk * b.SKILL_MULT_2;
            Assert.AreEqual(EnemyHp - hit, a.Hp, 1e-6);
            Assert.AreEqual(EnemyHp - hit, c.Hp, 1e-6);
            Assert.AreEqual(EnemyHp, outside.Hp, 1e-6);
            Assert.AreEqual(b.SKILL_CD_2, skills.CooldownRemaining(SkillSlot.Slot2), 1e-6);
        }

        [Test]
        public void Whirlwind_AutoCast_NeedsMinimumTargets()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            Spawn(b, world, 1d);
            var skills = new SkillAutoCaster(b);
            skills.SetUnlocked(SkillSlot.Slot1, false);
            skills.SetUnlocked(SkillSlot.Slot3, false);

            skills.Tick(0.01f, hero, world, false);
            Assert.AreEqual(0f, skills.CooldownRemaining(SkillSlot.Slot2), 1e-6);

            Spawn(b, world, 2d);
            skills.Tick(0.01f, hero, world, false);
            Assert.AreEqual(b.SKILL_CD_2, skills.CooldownRemaining(SkillSlot.Slot2), 1e-6);
        }

        [Test]
        public void BattleCry_HealsAndBuffsForDuration()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1000d);
            CombatWorld world = World(b, hero);
            hero.ApplyDamage(500d);
            var skills = new SkillAutoCaster(b);

            skills.TryCast(SkillSlot.Slot3, hero, world);

            Assert.AreEqual(500d + 1000d * b.BATTLECRY_HEAL / 100d, hero.Hp, 1e-6);
            Assert.AreEqual(b.BATTLECRY_ATK_BUFF / 100d, skills.AtkBuffSum, 1e-9);
            skills.Tick(b.BATTLECRY_DURATION + 0.01f, hero, world, false);
            Assert.AreEqual(0d, skills.AtkBuffSum, 1e-9);
        }

        [Test]
        public void BattleCry_AutoCast_OnlyWhenHurtOrBossFight()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1000d);
            CombatWorld world = World(b, hero);
            var skills = new SkillAutoCaster(b);
            skills.SetUnlocked(SkillSlot.Slot1, false);
            skills.SetUnlocked(SkillSlot.Slot2, false);

            skills.Tick(0.01f, hero, world, false);
            Assert.AreEqual(0f, skills.CooldownRemaining(SkillSlot.Slot3), 1e-6);

            skills.Tick(0.01f, hero, world, true);
            Assert.AreEqual(b.SKILL_CD_3, skills.CooldownRemaining(SkillSlot.Slot3), 1e-6);
        }

        [Test]
        public void AutoCast_TwoReady_SecondWaitsSequenceGap()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            Spawn(b, world, 1d);
            Spawn(b, world, 2d);
            var skills = new SkillAutoCaster(b);
            skills.SetUnlocked(SkillSlot.Slot3, false);

            skills.Tick(0.01f, hero, world, false);
            Assert.Greater(skills.CooldownRemaining(SkillSlot.Slot1), 0f);
            Assert.AreEqual(0f, skills.CooldownRemaining(SkillSlot.Slot2), 1e-6);

            skills.Tick(b.SKILL_SEQUENCE_GAP / 2f, hero, world, false);
            Assert.AreEqual(0f, skills.CooldownRemaining(SkillSlot.Slot2), 1e-6);

            skills.Tick(b.SKILL_SEQUENCE_GAP, hero, world, false);
            Assert.Greater(skills.CooldownRemaining(SkillSlot.Slot2), 0f);
        }
    }
}
