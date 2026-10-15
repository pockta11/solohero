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
            Assert.IsFalse(jobs.IsMax);

            // D-157: the third job is the second job's only successor, at JOB_LV_3.
            CollectionAssert.AreEquivalent(new[] { "archpyro" }, System.Array.ConvertAll(jobs.Choices(), d => d.Id));
            Assert.AreEqual(b.JOB_LV_3, jobs.NextLevel);
            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance("archpyro").Reason);
            save.heroLevel = b.JOB_LV_3;
            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance("archcryo").Reason, "another second job's successor");
            Assert.IsTrue(jobs.TryAdvance("archpyro").Ok);
            Assert.AreEqual(3, jobs.Tier);
            Assert.IsTrue(jobs.IsMax);
            Assert.AreEqual(int.MaxValue, jobs.NextLevel);
            Assert.AreEqual(FailReason.MaxLevel, jobs.TryAdvance("archcryo").Reason);
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
            foreach (SkillDef def in SkillCatalog.OfLine(JobLine.Warrior, Grade.Common, 1))
                Assert.IsTrue(SkillBook.IsOwned(save, def.Id), "the first job hands out its commons: " + def.Id);
            Assert.IsFalse(SkillBook.IsOwned(save, "sword_aura"), "D-157: the third-job common waits for the third job");

            skills.AutoEquip();
            for (int s = 0; s < SkillService.SlotCount(b); s++)
            {
                SkillDef def = SkillCatalog.Find(SkillBook.EquippedAt(save, s));
                if (def != null) Assert.IsTrue(JobService.CanUse(save, def), def.Id);
            }
        }

        [Test]
        public void Catalog_EveryLineHasTwentySkills_FivePerGrade_OneForTheThirdJob()
        {
            int[,] count = new int[4, 4];
            int[,] third = new int[4, 4];
            foreach (SkillDef def in SkillCatalog.All)
            {
                count[(int)def.Line, (int)def.Grade]++;
                if (def.JobTier == JobCatalog.MaxTier) third[(int)def.Line, (int)def.Grade]++;
                else Assert.AreEqual(0, def.JobTier, def.Id);
            }

            Assert.AreEqual(3, count[0, 0], "the three starters are line-free commons");
            for (int line = 1; line <= 3; line++)
            {
                for (int grade = 0; grade < 4; grade++)
                {
                    Assert.AreEqual(5, count[line, grade], "line " + line + " grade " + grade);
                    Assert.AreEqual(1, third[line, grade], "third-job skill, line " + line + " grade " + grade);
                    Assert.AreEqual(4, SkillCatalog.OfLine((JobLine)line, (Grade)grade, 2).Length);
                    Assert.AreEqual(5, SkillCatalog.OfLine((JobLine)line, (Grade)grade, 3).Length);
                }
            }
        }

        [Test]
        public void Catalog_EverySecondJobHasOneThirdJob_WithItsOwnMainUltimateAndMastery()
        {
            foreach (JobDef second in JobCatalog.All)
            {
                if (second.Tier != 2) continue;
                JobDef[] next = JobService.ChoicesFrom(second);
                Assert.AreEqual(1, next.Length, second.Id);
                JobDef third = next[0];
                Assert.AreEqual(JobCatalog.MaxTier, third.Tier);
                Assert.AreEqual(second.Line, third.Line);
                Assert.IsNotNull(third.Main, third.Id);
                Assert.IsNotNull(third.Ultimate, third.Id);
                Assert.AreNotEqual(second.Ultimate.Id, third.Ultimate.Id);
                Assert.Greater(third.Main.Mult * third.Main.Targets * third.Main.Hits,
                    second.Main.Mult * second.Main.Targets * second.Main.Hits, third.Id);
                Assert.Greater(third.Mastery.Length, 0, third.Id);
            }
        }

        [Test]
        public void ThirdJob_StacksMastery_GrantsItsCommon_AndOpensItsSkills()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.JOB_LV_3;
            var jobs = new JobService(save, b);
            Assert.IsTrue(jobs.TryAdvance("warrior").Ok);
            Assert.IsTrue(jobs.TryAdvance("knight").Ok);
            HeroStats knight = CombatLoadout.ComputeStats(b, save);
            double knightHpPct = JobService.Effects(save).HpPct;
            Assert.IsFalse(JobService.CanUse(save, SkillCatalog.Find("ragnarok")), "third-job skills need the third job");

            Assert.IsTrue(jobs.TryAdvance("paladin").Ok);
            Assert.IsTrue(SkillBook.IsOwned(save, "sword_aura"), "the third job hands out its common");
            Assert.IsTrue(JobService.CanUse(save, SkillCatalog.Find("ragnarok")));
            Assert.IsFalse(JobService.CanUse(save, SkillCatalog.Find("starfall")), "another line's third-job skill");

            // The mastery of the warrior, the knight and the paladin all count.
            double paladinHp = 0d;
            foreach (Mastery m in JobCatalog.Find("paladin").Mastery)
                if (m.Stat == SoloHero.Core.Talents.TalentStat.HpPct) paladinHp += m.Amount;
            Assert.AreEqual(knightHpPct + paladinHp, JobService.Effects(save).HpPct, 1e-9);
            Assert.AreEqual(0.05, JobService.Effects(save).DamageTakenPct, 1e-9);
            HeroStats paladin = CombatLoadout.ComputeStats(b, save);
            Assert.Greater(paladin.Hp, knight.Hp * b.JOB_STAT_MULT);

            var runner = new StageRunner(b, new FixedRandom(), paladin, save);
            CombatLoadout.Apply(runner, b, save);
            Assert.AreEqual(JobCatalog.Find("paladin").Ultimate, runner.Skills.DefAt(runner.Skills.UltimateSlot));
        }

        [Test]
        public void SkillSummon_ThirdJobSkills_OnlyAfterTheThirdJob()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gold = 1e12;
            save.heroLevel = b.JOB_LV_3;
            save.jobId = "ranger";
            var summon = new SkillSummonService(b, GachaTableValues.FromBalance(b), new SystemRandom(new System.Random(11)));
            for (int k = 0; k < 40; k++)
                foreach (SkillPullItem item in summon.TryPullTen(save).Items)
                    Assert.AreEqual(0, SkillCatalog.Find(item.Id).JobTier, item.Id);

            Assert.IsTrue(new JobService(save, b).TryAdvance("bowmaster").Ok);
            bool third = false;
            for (int k = 0; k < 40 && !third; k++)
                foreach (SkillPullItem item in summon.TryPullTen(save).Items)
                    third |= SkillCatalog.Find(item.Id).JobTier == 3;
            Assert.IsTrue(third, "the third job's skills join the pool");
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
