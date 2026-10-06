using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    public sealed class JobTests
    {
        private sealed class FixedRandom : IRandom
        {
            public double NextDouble() => 0.99d;

            public int Next(int maxExclusive) => 0;
        }

        [Test]
        public void TryAdvance_LevelGatedChoices_FirstThenSecondJob()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var jobs = new JobService(save, b);

            Assert.AreEqual(JobCatalog.Beginner, jobs.Current.Id);
            Assert.AreEqual(3, jobs.Choices().Length);
            save.heroLevel = b.JOB_LV_1 - 1;
            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance("warrior").Reason);
            save.heroLevel = b.JOB_LV_1;
            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance("knight").Reason, "a second job needs its first job");
            Assert.IsTrue(jobs.TryAdvance("mage").Ok);
            Assert.AreEqual(1, jobs.Tier);

            CollectionAssert.AreEquivalent(new[] { "pyro", "cryo" }, System.Array.ConvertAll(jobs.Choices(), d => d.Id));
            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance("pyro").Reason);
            save.heroLevel = b.JOB_LV_2;
            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance("knight").Reason, "another line's second job");
            Assert.IsTrue(jobs.TryAdvance("pyro").Ok);
            Assert.IsTrue(jobs.IsMax);
            Assert.AreEqual(FailReason.MaxLevel, jobs.TryAdvance("cryo").Reason);
        }

        [Test]
        public void Skills_BeginnerUsesStartersOnly_JobUsesItsLine()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = 50;
            var skills = new SkillService(save, b);
            SkillBook.EnsureStarters(save, b);
            SkillBook.AddOwned(save, "fireball");
            SkillBook.AddOwned(save, "blade_storm");

            Assert.AreEqual(FailReason.JobLocked, skills.TryEquip("fireball").Reason);
            Assert.IsTrue(skills.TryEquip(SkillCatalog.PowerStrike).Ok);

            new JobService(save, b).TryAdvance("warrior");
            Assert.AreEqual(FailReason.JobLocked, skills.TryEquip("fireball").Reason);
            Assert.IsTrue(skills.TryEquip("blade_storm").Ok);
            Assert.IsFalse(JobService.CanUse(save, SkillCatalog.Find(SkillCatalog.Whirlwind)), "D-107: starters are the beginner's alone");
            foreach (SkillDef def in SkillCatalog.OfLine(JobLine.Warrior, Grade.Common))
                Assert.IsTrue(SkillBook.IsOwned(save, def.Id), "the first job hands out its commons: " + def.Id);

            skills.AutoEquip();
            for (int s = 0; s < SkillService.SlotCount(b); s++)
            {
                SkillDef def = SkillCatalog.Find(SkillBook.EquippedAt(save, s));
                if (def != null) Assert.IsTrue(JobService.CanUse(save, def), def.Id);
            }
        }

        [Test]
        public void Catalog_EveryLineHasSixteenSkills_FourPerGrade()
        {
            int[,] count = new int[4, 4];
            foreach (SkillDef def in SkillCatalog.All) count[(int)def.Line, (int)def.Grade]++;
            Assert.AreEqual(3, count[0, 0], "the three starters are line-free commons");
            for (int line = 1; line <= 3; line++)
                for (int grade = 0; grade < 4; grade++)
                    Assert.AreEqual(4, count[line, grade], "line " + line + " grade " + grade);
        }

        [Test]
        public void EnsureLineSkills_OldJobSave_GetsCommonsAndUsableLoadout()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.SKILL_UNLOCK_LV_2;
            SkillBook.EnsureStarters(save, b);
            var skills = new SkillService(save, b);
            skills.AutoEquip();
            save.jobId = "archer";

            Assert.IsTrue(JobService.EnsureLineSkills(save, skills, b));
            for (int s = 0; s < SkillService.SlotCount(b); s++)
            {
                SkillDef def = SkillCatalog.Find(SkillBook.EquippedAt(save, s));
                if (def != null) Assert.AreEqual(JobLine.Archer, def.Line, def.Id);
            }

            Assert.IsNotNull(SkillCatalog.Find(SkillBook.EquippedAt(save, 0)));
            Assert.IsFalse(JobService.EnsureLineSkills(save, skills, b), "nothing to repair the second time");
        }

        [Test]
        public void SkillSummon_LockedForBeginner_OwnLineOnlyAfterFirstJob()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gold = 1e9;
            save.heroLevel = b.JOB_LV_1;
            var summon = new SkillSummonService(b, GachaTableValues.FromBalance(b), new SystemRandom(new System.Random(7)));

            Assert.AreEqual(FailReason.JobLocked, summon.TryPullTen(save).Status.Reason);
            Assert.AreEqual(1e9, save.gold, "a refused summon costs nothing");

            Assert.IsTrue(new JobService(save, b).TryAdvance("archer").Ok);
            for (int k = 0; k < 20; k++)
            {
                SkillSummonResult r = summon.TryPullTen(save);
                Assert.IsTrue(r.Status.Ok);
                foreach (SkillPullItem item in r.Items)
                    Assert.AreEqual(JobLine.Archer, SkillCatalog.Find(item.Id).Line, item.Id);
            }
        }

        [Test]
        public void Loadout_SecondJob_SetsMasteryStatsAndUltimate()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.JOB_LV_2;
            HeroStats beginner = CombatLoadout.ComputeStats(b, save);
            var jobs = new JobService(save, b);
            Assert.IsTrue(jobs.TryAdvance("warrior").Ok);
            Assert.IsTrue(jobs.TryAdvance("knight").Ok);
            HeroStats knight = CombatLoadout.ComputeStats(b, save);
            Assert.Greater(knight.Hp, beginner.Hp * b.JOB_STAT_MULT * b.JOB_STAT_MULT);

            var runner = new StageRunner(b, new FixedRandom(), knight, save);
            CombatLoadout.Apply(runner, b, save);
            Assert.AreEqual(JobCatalog.Find("knight").Ultimate, runner.Skills.DefAt(runner.Skills.UltimateSlot));
        }

        [Test]
        public void Ultimate_AutoCastsEvenInManualMode()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.JOB_LV_2;
            save.jobId = "sniper";
            var runner = new StageRunner(b, new FixedRandom(), new HeroStats(1e6, 1d, 1e4, b.ATKSPD_BASE, 0d), save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            CombatLoadout.Apply(runner, b, save);
            runner.Skills.AutoEnabled = false;
            int casts = 0;
            runner.Skills.SkillCast += (slot, def) => { if (slot == runner.Skills.UltimateSlot) casts++; };
            runner.Begin(1);
            for (int i = 0; i < 200; i++) runner.Tick(0.05f);
            Assert.Greater(casts, 0);
        }
    }
}
