using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Skills;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-078: catalog skills behave as their data says (targets, waves, burn, stun, buffs, heal, shield).</summary>
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

        private static EnemyBrain Spawn(BalanceValues b, CombatWorld world, double x, bool boss = false)
        {
            Assert.IsTrue(world.TryActivateSlot(out EnemyBrain e));
            e.Reset(b, EnemyHp, 10d, 1f, x, boss);
            return e;
        }

        private static SkillAutoCaster Caster(BalanceValues b, params string[] ids)
        {
            var skills = new SkillAutoCaster(b);
            for (int i = 0; i < ids.Length; i++) skills.SetSlot(i, SkillCatalog.Find(ids[i]), 1);
            return skills;
        }

        private static SkillDef Def(string id) => SkillCatalog.Find(id);

        [Test]
        public void Catalog_HasSixSkillsPerGradeWithUniqueIds()
        {
            Assert.AreEqual(24, SkillCatalog.Count);
            for (int g = 0; g < 4; g++) Assert.AreEqual(6, SkillCatalog.OfGrade((SoloHero.Core.Gacha.Grade)g).Length);
            for (int i = 0; i < SkillCatalog.All.Length; i++)
            {
                Assert.AreEqual(i, SkillCatalog.IndexOf(SkillCatalog.All[i].Id), SkillCatalog.All[i].Id);
                Assert.Greater(SkillCatalog.All[i].Cooldown, 0f);
            }
        }

        [Test]
        public void PowerStrike_HitsNearestForAtkTimesMultAndStartsCooldown()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain near = Spawn(b, world, 1d);
            EnemyBrain far = Spawn(b, world, 1.8d);
            SkillAutoCaster skills = Caster(b, SkillCatalog.PowerStrike);

            Assert.IsTrue(skills.TryCast(0, hero, world).Ok);

            Assert.AreEqual(EnemyHp - Atk * Def(SkillCatalog.PowerStrike).DamageMult, near.Hp, 1e-6);
            Assert.AreEqual(EnemyHp, far.Hp, 1e-6);
            Assert.AreEqual(Def(SkillCatalog.PowerStrike).Cooldown, skills.CooldownRemaining(0), 1e-6);
        }

        [Test]
        public void SkillHit_LevelTen_UsesLevelScale()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain e = Spawn(b, world, 1d);
            var skills = new SkillAutoCaster(b);
            skills.SetSlot(0, Def(SkillCatalog.PowerStrike), b.SKILL_MAX_LEVEL);

            skills.TryCast(0, hero, world);

            double scale = Formulas.SkillLevelScale(b, b.SKILL_MAX_LEVEL);
            Assert.AreEqual(1.9d, scale, 1e-9);
            Assert.AreEqual(EnemyHp - Atk * Def(SkillCatalog.PowerStrike).DamageMult * scale, e.Hp, 1e-6);
        }

        [Test]
        public void AreaSkill_HitsEveryEnemyInFrontWithinRangeOnly()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            SkillDef meteor = Def("meteor");
            EnemyBrain a = Spawn(b, world, 0.5d);
            EnemyBrain c = Spawn(b, world, meteor.Range - 0.1d);
            EnemyBrain outside = Spawn(b, world, meteor.Range + 0.5d);
            SkillAutoCaster skills = Caster(b, "meteor");

            skills.TryCast(0, hero, world);

            double hit = Atk * meteor.DamageMult;
            Assert.AreEqual(EnemyHp - hit, a.Hp, 1e-6);
            Assert.AreEqual(EnemyHp - hit, c.Hp, 1e-6);
            Assert.AreEqual(EnemyHp, outside.Hp, 1e-6);
        }

        [Test]
        public void MultiWave_FirstWaveAtOnceRestFollowEveryInterval()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            EnemyBrain e = Spawn(b, world, 1d);
            SkillDef quick = Def("quick_slash");
            SkillAutoCaster skills = Caster(b, "quick_slash");
            int hits = 0;
            world.HitLanded += (t, amount, kind) => { if (kind == HitKind.Skill) hits++; };

            skills.TryCast(0, hero, world);
            Assert.AreEqual(1, hits);

            for (int i = 0; i < 20; i++) skills.Tick(quick.WaveInterval, hero, world, false);

            Assert.AreEqual(quick.Waves, hits);
            Assert.AreEqual(EnemyHp - Atk * quick.DamageMult * quick.Waves, e.Hp, 1e-6);
        }

        [Test]
        public void Fireball_BurnTicksReportDotHits()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            SkillDef fireball = Def("fireball");
            EnemyBrain e = Spawn(b, world, 1d);
            SkillAutoCaster skills = Caster(b, "fireball");
            double dot = 0d;
            world.HitLanded += (t, amount, kind) => { if (kind == HitKind.Dot) dot += amount; };

            skills.TryCast(0, hero, world);
            Assert.IsTrue(e.HasDot);
            for (int i = 0; i < 200; i++) world.TickEnemies(0.05f);

            double expected = Atk * fireball.DotPercent / 100d * fireball.DotSeconds;
            Assert.AreEqual(expected, dot, expected * 0.15d);
            Assert.IsFalse(e.HasDot);
        }

        [Test]
        public void FrostNova_StunStopsEnemyAttacksAndBossesGetHalf()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1e6);
            CombatWorld world = World(b, hero);
            EnemyBrain e = Spawn(b, world, 1d);
            SkillAutoCaster skills = Caster(b, "frost_nova");

            skills.TryCast(0, hero, world);
            Assert.IsTrue(e.IsStunned);
            double hp = hero.Hp;
            for (int i = 0; i < 10; i++) world.TickEnemies(0.1f);
            Assert.AreEqual(hp, hero.Hp, 1e-9, "stunned enemy must not attack");
            for (int i = 0; i < 40; i++) world.TickEnemies(0.1f);
            Assert.IsFalse(e.IsStunned);
            Assert.Less(hero.Hp, hp);

            var world2 = World(b, hero);
            EnemyBrain boss = Spawn(b, world2, 1d, boss: true);
            boss.Stun(2f);
            world2.TickEnemies(2f * (float)b.SKILL_BOSS_STUN_MULT + 0.01f);
            Assert.IsFalse(boss.IsStunned);
        }

        [Test]
        public void BattleCry_HealsAndBuffsForDuration()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1000d);
            CombatWorld world = World(b, hero);
            hero.ApplyDamage(500d);
            SkillDef cry = Def(SkillCatalog.BattleCry);
            SkillAutoCaster skills = Caster(b, SkillCatalog.BattleCry);

            skills.TryCast(0, hero, world);

            Assert.AreEqual(500d + 1000d * cry.HealPercent / 100d, hero.Hp, 1e-6);
            Assert.AreEqual(cry.BuffAmount / 100d, skills.BuffAtk, 1e-9);
            skills.Tick(cry.BuffSeconds + 0.01f, hero, world, false);
            Assert.AreEqual(0d, skills.BuffAtk, 1e-9);
        }

        [Test]
        public void IronSkin_GuardCutsDamageTaken()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1000d);
            CombatWorld world = World(b, hero);
            SkillAutoCaster skills = Caster(b, "iron_skin");
            skills.TryCast(0, hero, world);
            hero.SetSkillBuffs(skills.BuffAtk, skills.BuffAtkSpd, skills.BuffCrit, skills.BuffGuard);

            hero.ApplyDamage(100d);

            Assert.AreEqual(1000d - 100d * (1d - Def("iron_skin").BuffAmount / 100d), hero.Hp, 1e-6);
        }

        [Test]
        public void HolyLight_ShieldAbsorbsBeforeHpThenExpires()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1000d);
            CombatWorld world = World(b, hero);
            hero.ApplyDamage(600d);
            SkillDef holy = Def("holy_light");
            SkillAutoCaster skills = Caster(b, "holy_light");

            skills.TryCast(0, hero, world);
            double shield = 1000d * holy.ShieldPercent / 100d;
            Assert.AreEqual(shield, hero.Shield, 1e-6);
            double hp = hero.Hp;
            hero.ApplyDamage(shield - 1d);
            Assert.AreEqual(hp, hero.Hp, 1e-6);

            hero.Tick(holy.ShieldSeconds + 0.1f, world);
            Assert.AreEqual(0d, hero.Shield, 1e-9);
        }

        [Test]
        public void HealSkill_AutoCastsOnlyUnderThreshold()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b, 1000d);
            CombatWorld world = World(b, hero);
            SkillAutoCaster skills = Caster(b, "first_aid");

            skills.Tick(0.01f, hero, world, false);
            Assert.AreEqual(0f, skills.CooldownRemaining(0), 1e-6);

            hero.ApplyDamage(1000d * (1d - Def("first_aid").HpThreshold / 100d) + 1d);
            skills.Tick(0.01f, hero, world, false);
            Assert.Greater(skills.CooldownRemaining(0), 0f);
        }

        [Test]
        public void EmptySlot_TryCast_ReturnsLocked()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            var skills = new SkillAutoCaster(b);

            Result r = skills.TryCast(2, hero, world);

            Assert.IsFalse(r.Ok);
            Assert.AreEqual(FailReason.Locked, r.Reason);
        }

        [Test]
        public void AutoCast_TwoReady_SecondWaitsSequenceGap()
        {
            var b = new BalanceValues();
            HeroBrain hero = Hero(b);
            CombatWorld world = World(b, hero);
            Spawn(b, world, 1d);
            Spawn(b, world, 2d);
            SkillAutoCaster skills = Caster(b, SkillCatalog.PowerStrike, SkillCatalog.Whirlwind);

            skills.Tick(0.01f, hero, world, false);
            Assert.Greater(skills.CooldownRemaining(0), 0f);
            Assert.AreEqual(0f, skills.CooldownRemaining(1), 1e-6);

            skills.Tick(b.SKILL_SEQUENCE_GAP / 2f, hero, world, false);
            Assert.AreEqual(0f, skills.CooldownRemaining(1), 1e-6);

            skills.Tick(b.SKILL_SEQUENCE_GAP, hero, world, false);
            Assert.Greater(skills.CooldownRemaining(1), 0f);
        }

        [Test]
        public void EverySkill_CastsWithoutThrowingAndDamageSkillsHit()
        {
            var b = new BalanceValues();
            for (int i = 0; i < SkillCatalog.All.Length; i++)
            {
                SkillDef def = SkillCatalog.All[i];
                HeroBrain hero = Hero(b, 1000d);
                CombatWorld world = World(b, hero);
                hero.ApplyDamage(800d);
                EnemyBrain e = Spawn(b, world, 1d);
                var skills = new SkillAutoCaster(b);
                skills.SetSlot(0, def, 1);

                Assert.IsTrue(skills.TryCast(0, hero, world).Ok, def.Id);
                for (int t = 0; t < 40; t++) skills.Tick(0.1f, hero, world, false);
                if (def.DealsDamage) Assert.Less(e.Hp, EnemyHp, def.Id);
            }
        }
    }
}
