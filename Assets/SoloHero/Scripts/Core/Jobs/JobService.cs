using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Jobs
{
    /// <summary>
    /// D-104 job advancement. Advancing is free, gated by hero level (JOB_LV_1 / JOB_LV_2), and permanent. The job
    /// decides the main attack, the passive mastery (added to the talent effects), the ultimate (second jobs) and
    /// which skills may be equipped. Each advancement also multiplies HP / ATK / DEF by JOB_STAT_MULT (StatAggregator).
    /// D-107: the beginner uses only the line-free starter skills and a job only its own line's; the first job hands
    /// out its line's common skills so the slots are never empty right after advancing.
    /// </summary>
    public sealed class JobService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public JobService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public event Action<JobDef> Advanced;

        public JobDef Current => JobCatalog.Find(_data.jobId);

        public int Tier => Current.Tier;

        public static int TierOf(SaveDataV2 data) => data == null ? 0 : JobCatalog.Find(data.jobId).Tier;

        public static JobLine LineOf(SaveDataV2 data) => data == null ? JobLine.None : JobCatalog.Find(data.jobId).Line;

        /// <summary>Level needed for the next advancement; int.MaxValue once the second job is reached.</summary>
        public int NextLevel => NextLevelFor(_balance, Tier);

        public static int NextLevelFor(BalanceValues balance, int tier) =>
            tier == 0 ? balance.JOB_LV_1 : tier == 1 ? balance.JOB_LV_2 : int.MaxValue;

        public bool IsMax => Tier >= 2;

        public bool CanAdvance => !IsMax && _data.heroLevel >= NextLevel;

        /// <summary>The jobs the next advancement can pick from.</summary>
        public JobDef[] Choices() => ChoicesFrom(Current);

        public static JobDef[] ChoicesFrom(JobDef current)
        {
            int n = 0;
            for (int i = 0; i < JobCatalog.All.Length; i++)
                if (IsChoice(JobCatalog.All[i], current)) n++;
            var result = new JobDef[n];
            n = 0;
            for (int i = 0; i < JobCatalog.All.Length; i++)
                if (IsChoice(JobCatalog.All[i], current)) result[n++] = JobCatalog.All[i];
            return result;
        }

        private static bool IsChoice(JobDef def, JobDef current) =>
            def.Tier == current.Tier + 1 && (current.Tier == 0 ? def.Parent == "" : def.Parent == current.Id);

        public Result TryAdvance(string jobId)
        {
            if (IsMax) return Result.Fail(FailReason.MaxLevel);
            if (_data.heroLevel < NextLevel) return Result.Fail(FailReason.Locked);
            JobDef next = JobCatalog.Find(jobId);
            if (next.Id != jobId || !IsChoice(next, Current)) return Result.Fail(FailReason.Locked);
            _data.jobId = next.Id;
            if (next.Tier == 1) GrantLineCommons(_data, next.Line);
            Advanced?.Invoke(next);
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>D-107: a skill is usable only by its own line; the line-free starters only by the beginner.</summary>
        public static bool CanUse(SaveDataV2 data, SkillDef def)
        {
            if (def == null) return false;
            return def.Line == LineOf(data);
        }

        /// <summary>
        /// D-107 save repair, run once at boot: a hero who advanced before D-107 never got the line's commons and may
        /// still have beginner starters equipped. Grants the commons and, when any equipped skill is unusable, re-runs
        /// auto-equip so the slots hold the job's own skills. True when something changed.
        /// </summary>
        public static bool EnsureLineSkills(SaveDataV2 data, SkillService skills, BalanceValues balance)
        {
            if (data == null || skills == null || balance == null) return false;
            JobLine line = LineOf(data);
            if (line == JobLine.None) return false;
            int before = data.ownedSkills.Count;
            GrantLineCommons(data, line);
            bool changed = data.ownedSkills.Count != before;
            for (int s = 0; s < SkillService.SlotCount(balance); s++)
            {
                SkillDef def = SkillCatalog.Find(SkillBook.EquippedAt(data, s));
                if (def == null || CanUse(data, def)) continue;
                skills.AutoEquip();
                return true;
            }

            return changed;
        }

        /// <summary>D-107: the common-grade skills of a line, added to the book if missing (level 1).</summary>
        public static void GrantLineCommons(SaveDataV2 data, JobLine line)
        {
            foreach (SkillDef def in SkillCatalog.All)
                if (def.Line == line && def.Grade == Gacha.Grade.Common && !SkillBook.IsOwned(data, def.Id))
                    SkillBook.AddOwned(data, def.Id);
        }

        /// <summary>Talent effects plus the mastery of the job (and of its first job, for a second job).</summary>
        public static TalentEffects Effects(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var effects = new TalentEffects();
            TalentService.AddTo(effects, data);
            JobDef job = JobCatalog.Find(data.jobId);
            AddMastery(effects, job);
            if (job.Tier == 2) AddMastery(effects, JobCatalog.Find(job.Parent));
            return effects;
        }

        private static void AddMastery(TalentEffects effects, JobDef job)
        {
            for (int i = 0; i < job.Mastery.Length; i++) effects.Add(job.Mastery[i].Stat, job.Mastery[i].Amount);
        }
    }
}
