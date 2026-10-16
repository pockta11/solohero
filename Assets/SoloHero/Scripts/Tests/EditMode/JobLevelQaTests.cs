using System;
using System.Collections.Generic;
using System.IO;
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
using SoloHero.Core.Talents;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// Release QA of the job and level content (2026-10-16): every job path from level 1 to 100 with every level
    /// gate (advancement, skill slots, crit lanes, AP, talent points, job talent page), every job in a headless stage
    /// (main attack, burn / stun, ultimate, clear), the skill summon pool for every line, tier and summon level, the
    /// job words, the level rewards and the save fallbacks.
    /// </summary>
    public sealed class JobLevelQaTests
    {
        private const string TablePath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const float Dt = 1f / 30f;
        private const int TopLevel = 100;

        private static readonly string[][] Paths =
        {
            new[] { "warrior", "knight", "paladin" }, new[] { "warrior", "berserker", "warlord" },
            new[] { "mage", "pyro", "archpyro" }, new[] { "mage", "cryo", "archcryo" },
            new[] { "archer", "ranger", "bowmaster" }, new[] { "archer", "sniper", "marksman" },
        };

        private sealed class CountingRequester : ISaveRequester
        {
            public int Count;

            public void RequestSave() => Count++;
        }

        [TearDown]
        public void ResetStrings() => Strings.Load(null);

        private static StageRunner Runner(BalanceValues b, SaveDataV2 save, HeroStats stats, int seed)
        {
            var runner = new StageRunner(b, new SystemRandom(new Random(seed)), stats, save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            CombatLoadout.Apply(runner, b, save);
            runner.SetHeroStats(stats);
            return runner;
        }

        private static int UnlockedSlots(BalanceValues b, int level)
        {
            int n = 0;
            for (int s = 0; s < SkillService.SlotCount(b); s++)
                if (SkillService.IsSlotUnlocked(b, s, level)) n++;
            return n;
        }

        private static void AssertFinite(double value, string what)
        {
            Assert.IsFalse(double.IsNaN(value) || double.IsInfinity(value), what + " is not finite");
            Assert.Greater(value, 0d, what);
        }

        [Test]
        public void Catalog_JobTree_SixteenJobsWithConsistentTiersParentsLinesAndLooks()
        {
            int[] perTier = new int[JobCatalog.MaxTier + 1];
            var ids = new HashSet<string>();
            var looks = new HashSet<string>();
            var ultimates = new HashSet<string>();
            foreach (JobDef job in JobCatalog.All)
            {
                Assert.IsTrue(ids.Add(job.Id), "duplicate job id " + job.Id);
                Assert.IsTrue(looks.Add(job.Look), "two jobs share the look " + job.Look);
                Assert.IsTrue(job.Tier >= 0 && job.Tier <= JobCatalog.MaxTier, job.Id);
                perTier[job.Tier]++;
                if (job.Tier == 0)
                {
                    Assert.AreEqual(JobLine.None, job.Line);
                    Assert.IsNull(job.Main, "the beginner keeps the flash slash");
                    Assert.IsNull(job.Ultimate);
                    continue;
                }

                Assert.AreNotEqual(JobLine.None, job.Line, job.Id);
                Assert.IsNotNull(job.Main, job.Id + " has no main attack");
                Assert.Greater(job.Main.Targets, 0, job.Id);
                Assert.Greater(job.Main.Hits, 0, job.Id);
                Assert.Greater(job.Main.Mult, 0d, job.Id);
                Assert.Greater(job.Main.Range, 0d, job.Id);
                Assert.Greater(job.Mastery.Length, 0, job.Id + " has no mastery");
                if (job.Tier == 1)
                {
                    Assert.AreEqual("", job.Parent, job.Id);
                    Assert.IsNull(job.Ultimate, "first jobs have no ultimate: " + job.Id);
                    Assert.AreEqual(2, JobService.ChoicesFrom(job).Length, job.Id + " should lead to two second jobs");
                }
                else
                {
                    JobDef parent = JobCatalog.Find(job.Parent);
                    Assert.AreEqual(job.Parent, parent.Id, job.Id + " has an unknown parent " + job.Parent);
                    Assert.AreEqual(job.Tier - 1, parent.Tier, job.Id);
                    Assert.AreEqual(parent.Line, job.Line, job.Id + " changes line");
                    Assert.IsNotNull(job.Ultimate, job.Id + " has no ultimate");
                    Assert.IsTrue(ultimates.Add(job.Ultimate.Id), "two jobs share the ultimate " + job.Ultimate.Id);
                    Assert.AreEqual(job.Line, job.Ultimate.Line, job.Ultimate.Id);
                    Assert.AreEqual(Grade.Legendary, job.Ultimate.Grade, job.Ultimate.Id);
                    Assert.IsTrue(job.Ultimate.DealsDamage, job.Ultimate.Id + " should deal damage");
                    Assert.IsNull(SkillCatalog.Find(job.Ultimate.Id), "an ultimate must not be a catalog skill: " + job.Ultimate.Id);
                }
            }

            Assert.AreEqual(1, perTier[0]);
            Assert.AreEqual(3, perTier[1]);
            Assert.AreEqual(6, perTier[2]);
            Assert.AreEqual(6, perTier[3]);
            Assert.AreEqual(16, JobCatalog.Count);
            Assert.AreEqual(JobCatalog.Beginner, JobCatalog.Find("").Id);
            Assert.AreEqual(JobCatalog.Beginner, JobCatalog.Find("no_such_job").Id);
            Assert.AreEqual(0, JobCatalog.IndexOf("no_such_job"));
        }

        [Test]
        public void EveryPath_LevelOneToHundred_GatesLoadoutStatsAndPointsHold()
        {
            foreach (string[] path in Paths)
            {
                string tag = string.Join(">", path);
                var b = new BalanceValues();
                var save = SaveDataV2.CreateNew();
                var requester = new CountingRequester();
                SkillBook.EnsureStarters(save, b);
                var skills = new SkillService(save, b, requester);
                var jobs = new JobService(save, b, requester);
                int advanced = 0;
                for (int level = 1; level <= TopLevel; level++)
                {
                    string at = tag + " L" + level;
                    save.heroLevel = level;
                    HeroAp.Settle(b, save);

                    // Advancement opens exactly at its level and only to the path's next job.
                    int nextLevel = JobService.NextLevelFor(b, jobs.Tier);
                    Assert.AreEqual(!jobs.IsMax && level >= nextLevel, jobs.CanAdvance, at + " CanAdvance");
                    if (jobs.CanAdvance)
                    {
                        Assert.AreEqual(nextLevel, level, at + " advancement should have opened exactly at its level");
                        string next = path[jobs.Tier];
                        JobDef[] choices = jobs.Choices();
                        Assert.IsTrue(Array.Exists(choices, c => c.Id == next), at + " offers " + next);
                        foreach (JobDef other in JobCatalog.All)
                        {
                            if (Array.IndexOf(choices, other) >= 0 || other.Tier == 0) continue;
                            Assert.AreEqual(FailReason.Locked, jobs.TryAdvance(other.Id).Reason, at + " must refuse " + other.Id);
                        }

                        HeroStats before = CombatLoadout.ComputeStats(b, save);
                        int saves = requester.Count;
                        Assert.IsTrue(jobs.TryAdvance(next).Ok, at + " advance to " + next);
                        Assert.AreEqual(saves + 1, requester.Count, at + " advancing saves once");
                        Assert.AreEqual(next, jobs.Current.Id);
                        skills.AutoEquip();
                        HeroStats after = CombatLoadout.ComputeStats(b, save);
                        Assert.GreaterOrEqual(after.Hp, before.Hp * b.JOB_STAT_MULT * (1d - 1e-9), at + " HP after " + next);
                        Assert.GreaterOrEqual(after.Atk, before.Atk * b.JOB_STAT_MULT * (1d - 1e-9), at + " ATK after " + next);
                        Assert.GreaterOrEqual(after.Def, before.Def * b.JOB_STAT_MULT * (1d - 1e-9), at + " DEF after " + next);
                        advanced++;
                    }

                    // A newly unlocked slot is filled the way the player would (auto-equip), then every equipped
                    // skill is usable and no unlocked slot stays empty while a usable skill is left over.
                    skills.AutoEquip();
                    int unlocked = UnlockedSlots(b, level);
                    int expectedUnlocked = 0;
                    foreach (int lv in new[] { b.SKILL_UNLOCK_LV_1, b.SKILL_UNLOCK_LV_2, b.SKILL_UNLOCK_LV_3, b.SKILL_UNLOCK_LV_4, b.SKILL_UNLOCK_LV_5, b.SKILL_UNLOCK_LV_6 })
                        if (level >= lv) expectedUnlocked++;
                    Assert.AreEqual(expectedUnlocked, unlocked, at + " unlocked slots");
                    int usableOwned = 0;
                    foreach (string id in save.ownedSkills)
                        if (JobService.CanUse(save, SkillCatalog.Find(id))) usableOwned++;
                    int equipped = 0;
                    var seen = new HashSet<string>();
                    for (int s = 0; s < SkillService.SlotCount(b); s++)
                    {
                        string id = SkillBook.EquippedAt(save, s);
                        if (!SkillService.IsSlotUnlocked(b, s, level))
                        {
                            Assert.AreEqual("", id, at + " locked slot " + s + " holds " + id);
                            continue;
                        }

                        if (id == "") continue;
                        Assert.IsTrue(seen.Add(id), at + " equips " + id + " twice");
                        Assert.IsTrue(JobService.CanUse(save, SkillCatalog.Find(id)), at + " equips unusable " + id);
                        equipped++;
                    }

                    Assert.AreEqual(Math.Min(unlocked, usableOwned), equipped, at + " equipped count");
                    Assert.Greater(usableOwned, 0, at + " owns no usable skill");

                    // Crit lanes, AP and talent points follow the level.
                    Assert.AreEqual(level >= b.UPG_UNLOCK_LV_CRIT, LaneRules.IsUnlocked(b, UpgradeLane.Crit, level), at + " crit lane");
                    Assert.AreEqual(level >= b.UPG_UNLOCK_LV_CRITDMG, LaneRules.IsUnlocked(b, UpgradeLane.CritDmg, level), at + " crit damage lane");
                    Assert.AreEqual(b.AP_PER_LEVEL * (level - 1), HeroAp.Total(b, level), at + " AP total");
                    Assert.AreEqual(0, HeroAp.Unspent(b, save), at + " auto AP leaves nothing unspent");
                    Assert.AreEqual(b.TALENT_POINTS_PER_LEVEL * (level - 1), TalentService.Earned(b, save), at + " talent points");

                    // The job talent page opens with the third job of the hero's own line only.
                    for (int line = 1; line <= 3; line++)
                    {
                        bool open = (JobLine)line == jobs.Current.Line && jobs.Tier >= TalentCatalog.JobPageTier;
                        foreach (TalentDef def in TalentCatalog.JobNodes((JobLine)line))
                            Assert.AreEqual(open, TalentService.IsOpen(b, save, def), at + " job talent " + def.Id);
                    }

                    // Stats stay finite and the loadout carries the job into combat.
                    HeroStats stats = CombatLoadout.ComputeStats(b, save);
                    AssertFinite(stats.Hp, at + " HP");
                    AssertFinite(stats.Atk, at + " ATK");
                    AssertFinite(stats.Def, at + " DEF");
                    AssertFinite(CombatPower.OfSave(b, save), at + " CP");
                    StageRunner runner = Runner(b, save, stats, level);
                    Assert.AreSame(jobs.Current.Ultimate, runner.Skills.DefAt(runner.Skills.UltimateSlot), at + " ultimate slot");
                    Assert.AreEqual(jobs.Current.Main != null ? jobs.Current.Main.Range : b.BASIC_SKILL_RANGE, runner.Hero.MainRange, 1e-9, at + " main range");
                }

                Assert.AreEqual(JobCatalog.MaxTier, advanced, tag + " advancements");
                Assert.IsTrue(jobs.IsMax, tag);
                Assert.AreEqual(path[2], save.jobId, tag);
                foreach (string starter in SkillCatalog.StarterIds)
                    Assert.IsFalse(JobService.CanUse(save, SkillCatalog.Find(starter)), tag + " still uses the starter " + starter);
                foreach (SkillDef def in SkillCatalog.OfLine(jobs.Current.Line, Grade.Common, JobCatalog.MaxTier))
                    Assert.IsTrue(SkillBook.IsOwned(save, def.Id), tag + " should own the common " + def.Id);
            }
        }

        [Test]
        public void EveryJob_HeadlessStage_MainAttackHitsEffectsLandUltimateFiresAndStagesClear()
        {
            foreach (JobDef job in JobCatalog.All)
            {
                var b = new BalanceValues();
                var save = SaveDataV2.CreateNew();
                save.heroLevel = b.JOB_LV_3;
                save.jobId = job.Id;
                SkillBook.EnsureStarters(save, b);
                if (job.Tier > 0) JobService.GrantLineCommons(save, job.Line, job.Tier);
                new SkillService(save, b).AutoEquip();
                int slotsFilled = 0;
                for (int s = 0; s < SkillService.SlotCount(b); s++)
                    if (SkillBook.EquippedAt(save, s) != "") slotsFilled++;
                Assert.Greater(slotsFilled, 0, job.Id + " has no usable skill to equip");

                // Phase A: a hero that cannot kill and cannot die shows the main attack, its burn / stun and the ultimate.
                StageRunner probe = Runner(b, save, new HeroStats(1e12, 1d, b.DEF_BASE, b.ATKSPD_BASE, 0d), 7);
                int mainHits = 0, ultimateCasts = 0, skillCasts = 0;
                probe.Hero.DealtDamage += (amount, crit) => mainHits++;
                probe.Skills.SkillCast += (slot, def) =>
                {
                    if (slot == probe.Skills.UltimateSlot) ultimateCasts++;
                    else skillCasts++;
                };
                bool sawDot = false, sawStun = false;
                probe.Begin(1);
                for (int tick = 0; tick < 60 * 30; tick++)
                {
                    probe.Tick(Dt);
                    for (int i = 0; i < probe.World.SlotCount; i++)
                    {
                        EnemyBrain enemy = probe.World.GetSlot(i);
                        if (enemy == null || !enemy.IsAlive) continue;
                        sawDot |= enemy.HasDot;
                        sawStun |= enemy.IsStunned;
                    }
                }

                Assert.AreNotEqual(HeroState.Dead, probe.Hero.State, job.Id + " died in the probe");
                Assert.Greater(mainHits, 10, job.Id + " main attack hits");
                Assert.Greater(skillCasts, 0, job.Id + " cast no skill");
                if (job.Ultimate != null) Assert.Greater(ultimateCasts, 0, job.Id + " never cast its ultimate");
                else Assert.AreEqual(0, ultimateCasts, job.Id + " has no ultimate but one fired");
                if (job.Main != null && job.Main.DotPercent > 0d) Assert.IsTrue(sawDot, job.Id + " main attack never burned");
                if (job.Main != null && job.Main.StunSeconds > 0f) Assert.IsTrue(sawStun, job.Id + " main attack never stunned");

                // Phase B: a strong hero clears a normal stage and the first chapter boss with this job's loadout.
                foreach (int g in new[] { 1, 10 })
                {
                    StageRunner runner = Runner(b, save, new HeroStats(1e9, 1e9, b.DEF_BASE, b.ATKSPD_BASE, 0d), g);
                    int cleared = 0;
                    runner.StageCleared += stage => cleared++;
                    runner.Begin(g);
                    for (int tick = 0; tick < 60 * 30 && cleared == 0; tick++) runner.Tick(Dt);
                    Assert.AreEqual(1, cleared, job.Id + " did not clear stage " + g);
                }
            }
        }

        [Test]
        public void SkillSummon_EveryLineTierAndSummonLevel_PullsOnlyUsableSkills()
        {
            var b = new BalanceValues();
            GachaTableValues table = GachaTableValues.FromBalance(b);
            foreach (JobDef job in JobCatalog.All)
            {
                if (job.Tier == 0) continue;
                var summon = new SkillSummonService(b, table, new SystemRandom(new Random(JobCatalog.IndexOf(job.Id))));
                for (int level = 1; level <= b.SUMMON_LV_MAX; level++)
                {
                    var save = SaveDataV2.CreateNew();
                    save.heroLevel = b.JOB_LV_3;
                    save.jobId = job.Id;
                    save.gold = 1e15;
                    save.skillPullCount = SummonLevel.PullsFor(b, SummonKind.Skill, level);
                    Assert.AreEqual(level, SummonLevel.Of(b, save, SummonKind.Skill), job.Id + " summon level " + level);
                    SkillSummonResult result = summon.TryPullTen(save);
                    Assert.IsTrue(result.Status.Ok, job.Id + " Lv" + level + " " + result.Status.Reason);
                    Assert.AreEqual(10, result.Items.Length, job.Id + " Lv" + level);
                    foreach (SkillPullItem item in result.Items)
                    {
                        SkillDef def = SkillCatalog.Find(item.Id);
                        Assert.IsNotNull(def, job.Id + " pulled unknown " + item.Id);
                        Assert.IsTrue(JobService.CanUse(save, def), job.Id + " Lv" + level + " pulled unusable " + item.Id);
                        Assert.IsTrue(SkillBook.IsOwned(save, item.Id), item.Id);
                    }
                }
            }
        }

        [Test]
        public void Terms_EveryJobLine_LeavesNoPlaceholderInJobSkillOrStatText()
        {
            Strings.Load(Strings.ParseTsv(File.ReadAllText(TablePath)));
            string[] placeholders = { "{atk}", "{main}", "{weapon}" };
            var atkWords = new Dictionary<JobLine, string>();
            var mainWords = new Dictionary<JobLine, string>();
            var weaponWords = new Dictionary<JobLine, string>();
            foreach (JobLine line in new[] { JobLine.None, JobLine.Warrior, JobLine.Mage, JobLine.Archer })
            {
                JobTerms.Apply(line);
                atkWords[line] = Strings.Term("atk");
                mainWords[line] = Strings.Term("main");
                weaponWords[line] = Strings.Term("weapon");
                var keys = new List<string>
                {
                    "job.main_info", "job.main_hits", "job.stat.atk", "job.stat.hp", "job.stat.def", "job.stat.crit",
                    "job.stat.crit_damage", "job.stat.atkspd", "job.stat.boss", "job.stat.damage_taken",
                    "job.stat.skill_damage", "job.stat.cooldown", "job.stat.dot", "job.current", "job.need_lv",
                    "hud.ultimate_locked", "skill.tier_badge", "skill.tier_need", "talent.job_locked", "talent.job_none",
                };
                foreach (JobDef job in JobCatalog.All)
                {
                    keys.Add(job.NameKey);
                    keys.Add(job.MainKey);
                    keys.Add(job.DescKey);
                    if (job.Ultimate == null) continue;
                    keys.Add(job.Ultimate.NameKey);
                    keys.Add(job.Ultimate.DescKey);
                }

                foreach (SkillDef def in SkillCatalog.All)
                {
                    keys.Add(def.NameKey);
                    keys.Add(def.DescKey);
                }

                foreach (TalentDef def in TalentCatalog.Job)
                {
                    keys.Add(def.NameKey);
                    keys.Add(def.DescKey);
                }

                foreach (string key in keys)
                {
                    string text = Strings.Get(key);
                    Assert.AreNotEqual(key, text, "missing string " + key);
                    foreach (string placeholder in placeholders)
                        Assert.IsFalse(text.Contains(placeholder), line + " " + key + " keeps " + placeholder + ": " + text);
                }

            }

            // D-140: one ATK stat inside, named per line - the beginner reads like a warrior, mages differ in every
            // word, archers in the main stat and the weapon.
            foreach (string term in new[] { "atk", "main", "weapon" })
                Assert.AreNotEqual(term, Strings.Term(term), "term " + term + " is not defined");
            Assert.AreEqual(atkWords[JobLine.Warrior], atkWords[JobLine.None]);
            Assert.AreEqual(atkWords[JobLine.Warrior], atkWords[JobLine.Archer]);
            Assert.AreNotEqual(atkWords[JobLine.Warrior], atkWords[JobLine.Mage], "mages use spell power");
            Assert.AreNotEqual(mainWords[JobLine.Warrior], mainWords[JobLine.Mage]);
            Assert.AreNotEqual(mainWords[JobLine.Warrior], mainWords[JobLine.Archer]);
            Assert.AreNotEqual(mainWords[JobLine.Mage], mainWords[JobLine.Archer]);
            Assert.AreNotEqual(weaponWords[JobLine.Warrior], weaponWords[JobLine.Mage]);
            Assert.AreNotEqual(weaponWords[JobLine.Warrior], weaponWords[JobLine.Archer]);
            Assert.AreNotEqual(weaponWords[JobLine.Mage], weaponWords[JobLine.Archer]);
        }

        [Test]
        public void LevelUp_OneToHundredThroughExp_PaysEveryTenthRewardOnceAndSettlesAp()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var levels = new HeroLevelService(save, b);
            int events = 0;
            levels.HeroLeveledUp += level => events++;
            double gem = save.gem;
            int stones = save.breakStones, gear = save.gearTickets, skill = save.skillTickets, pet = save.petTickets;
            while (save.heroLevel < TopLevel)
                levels.AddExp(HeroLevelService.ExpRequired(b, save.heroLevel) - save.heroExp);

            Assert.AreEqual(TopLevel, save.heroLevel);
            Assert.AreEqual(TopLevel - 1, events);
            int rewards = TopLevel / b.LEVEL_REWARD_EVERY;
            int big = TopLevel / b.LEVEL_REWARD_BIG_EVERY;
            Assert.AreEqual(gem + rewards * b.LEVEL_REWARD_GEM + big * b.LEVEL_REWARD_BIG_GEM, save.gem, 1e-9);
            Assert.AreEqual(stones + rewards * b.LEVEL_REWARD_STONES, save.breakStones);
            Assert.AreEqual(gear + rewards * b.LEVEL_REWARD_GEAR_TICKETS, save.gearTickets);
            Assert.AreEqual(skill + big * b.LEVEL_REWARD_BIG_TICKETS, save.skillTickets);
            Assert.AreEqual(pet + big * b.LEVEL_REWARD_BIG_TICKETS, save.petTickets);
            Assert.AreEqual(b.AP_PER_LEVEL * (TopLevel - 1), HeroAp.Spent(save));
            Assert.AreEqual(0, HeroAp.Unspent(b, save));
            Assert.IsTrue(new JobService(save, b).CanAdvance, "level 100 may take the first job");
        }

        [Test]
        public void Save_UnknownJobIdOrJobAheadOfLevel_FallsBackWithoutBreakingTheLoadout()
        {
            var b = new BalanceValues();

            // An unknown id reads as the beginner and the first job can still be taken.
            var save = SaveDataV2.CreateNew();
            save.jobId = "no_such_job";
            save.heroLevel = b.JOB_LV_1;
            SkillBook.EnsureStarters(save, b);
            var skills = new SkillService(save, b);
            Assert.AreEqual(JobCatalog.Beginner, new JobService(save, b).Current.Id);
            Assert.IsFalse(JobService.EnsureLineSkills(save, skills, b), "a beginner needs no repair");
            Assert.IsTrue(JobService.CanUse(save, SkillCatalog.Find(SkillCatalog.PowerStrike)));
            Assert.IsTrue(new JobService(save, b).TryAdvance("archer").Ok);
            Assert.AreEqual("archer", save.jobId);

            // A third job on a level-1 save (an old test save) still loads: commons granted, loadout usable, ultimate set.
            foreach (JobDef job in JobCatalog.All)
            {
                var old = SaveDataV2.CreateNew();
                old.jobId = job.Id;
                SkillBook.EnsureStarters(old, b);
                var oldSkills = new SkillService(old, b);
                oldSkills.AutoEquip();
                JobService.EnsureLineSkills(old, oldSkills, b);
                for (int s = 0; s < SkillService.SlotCount(b); s++)
                {
                    SkillDef def = SkillCatalog.Find(SkillBook.EquippedAt(old, s));
                    if (def != null) Assert.IsTrue(JobService.CanUse(old, def), job.Id + " keeps " + def.Id);
                }

                Assert.IsFalse(JobService.EnsureLineSkills(old, oldSkills, b), job.Id + " repair is idempotent");
                HeroStats stats = CombatLoadout.ComputeStats(b, old);
                AssertFinite(stats.Hp, job.Id + " HP");
                StageRunner runner = Runner(b, old, stats, 3);
                Assert.AreSame(job.Ultimate, runner.Skills.DefAt(runner.Skills.UltimateSlot), job.Id);
                int masteries = 0;
                for (JobDef j = job; j.Tier > 0; j = JobCatalog.Find(j.Parent)) masteries += j.Mastery.Length;
                TalentEffects effects = JobService.Effects(old);
                double sum = effects.AtkPct + effects.HpPct + effects.DefPct + effects.CritPoints + effects.CritDamage + effects.AtkSpdPct
                    + effects.BossDamagePct + effects.DamageTakenPct + effects.SkillDamagePct + effects.CooldownPct + effects.DotPct;
                if (masteries > 0) Assert.Greater(sum, 0d, job.Id + " mastery adds nothing");
                else Assert.AreEqual(0d, sum, 1e-12, job.Id);
                Assert.AreEqual(job.Tier >= JobCatalog.MaxTier, new JobService(old, b).IsMax, job.Id);
            }
        }
    }
}
