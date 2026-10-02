using SoloHero.Core.Gacha;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Jobs
{
    /// <summary>Weapon lines of the first job advancement (D-104). None = beginner and the line-free starter skills.</summary>
    public enum JobLine
    {
        None = 0,
        Warrior = 1,
        Mage = 2,
        Archer = 3
    }

    /// <summary>
    /// A job's main attack, replacing the beginner flash slash (D-093): it fires on the attack-speed timer at the
    /// nearest <see cref="Targets"/> enemies within <see cref="Range"/>, <see cref="Hits"/> times each, for
    /// ATK x <see cref="Mult"/> per hit with its own crit roll, plus an optional burn or stun.
    /// </summary>
    public sealed class MainAttack
    {
        public int Targets { get; init; } = 3;
        public int Hits { get; init; } = 1;
        public double Mult { get; init; } = 3.0;
        public double Range { get; init; } = 2.4;
        public double DotPercent { get; init; }
        public float DotSeconds { get; init; }
        public float StunSeconds { get; init; }

        /// <summary>View hint: VFX clip played in front of the hero on each swing ("wave" = the flash slash).</summary>
        public string Vfx { get; init; } = "wave";

        /// <summary>View hint: RGB tint of <see cref="Vfx"/>; 0 = untinted.</summary>
        public uint Tint { get; init; }

        public float VfxScale { get; init; } = 1.35f;
    }

    public readonly struct Mastery
    {
        public readonly TalentStat Stat;
        public readonly double Amount;

        public Mastery(TalentStat stat, double amount)
        {
            Stat = stat;
            Amount = amount;
        }
    }

    public sealed class JobDef
    {
        public string Id { get; init; } = "";
        public JobLine Line { get; init; }

        /// <summary>0 beginner, 1 first job, 2 second job.</summary>
        public int Tier { get; init; }

        /// <summary>The job this one advances from ("" for the first jobs, which come from the beginner).</summary>
        public string Parent { get; init; } = "";

        /// <summary>Null for the beginner, who keeps the BASIC_SKILL_* flash slash.</summary>
        public MainAttack Main { get; init; }

        /// <summary>Passive stats of this job alone; a second job also keeps its first job's mastery.</summary>
        public Mastery[] Mastery { get; init; } = new Mastery[0];

        /// <summary>Second jobs: the job-only skill in the ultimate slot.</summary>
        public SkillDef Ultimate { get; init; }

        /// <summary>Hero sheet entity under Art/Hero for this job's look.</summary>
        public string Look { get; init; } = "knight";

        public string NameKey => "job.name." + Id;
        public string MainKey => "job.main." + Id;
        public string DescKey => "job.desc." + Id;
    }

    /// <summary>
    /// D-104 jobs, MapleStory-style: the beginner, three first jobs at JOB_LV_1 and two second jobs for each at
    /// JOB_LV_2. Content table like SkillCatalog; ids are saved (SaveDataV2.jobId), so never rename one.
    /// </summary>
    public static class JobCatalog
    {
        public const string Beginner = "beginner";

        public static readonly JobDef[] All =
        {
            new JobDef { Id = Beginner, Tier = 0, Look = "knight" },

            // First jobs
            new JobDef
            {
                Id = "warrior", Line = JobLine.Warrior, Tier = 1, Look = "knight1",
                Main = new MainAttack { Targets = 4, Mult = 2.6, Range = 2.4, Tint = 0xA8C8FF },
                Mastery = new[] { new Mastery(TalentStat.HpPct, 0.1), new Mastery(TalentStat.DefPct, 0.1) },
            },
            new JobDef
            {
                Id = "mage", Line = JobLine.Mage, Tier = 1, Look = "jobmage",
                Main = new MainAttack { Targets = 2, Mult = 2.8, Range = 4.0, Vfx = "bolt", Tint = 0xC890FF, VfxScale = 1f },
                Mastery = new[] { new Mastery(TalentStat.AtkPct, 0.03), new Mastery(TalentStat.HpPct, 0.03) },
            },
            new JobDef
            {
                Id = "archer", Line = JobLine.Archer, Tier = 1, Look = "jobarcher",
                Main = new MainAttack { Targets = 2, Hits = 2, Mult = 1.4, Range = 5.0, Vfx = "spear", Tint = 0xD8F0B0, VfxScale = 0.8f },
                Mastery = new[] { new Mastery(TalentStat.CritPoints, 5), new Mastery(TalentStat.AtkSpdPct, 0.05) },
            },

            // Second jobs
            new JobDef
            {
                Id = "knight", Line = JobLine.Warrior, Tier = 2, Parent = "warrior", Look = "knight2",
                Main = new MainAttack { Targets = 5, Mult = 2.6, Range = 2.6, Tint = 0xFFE080, VfxScale = 1.5f },
                Mastery = new[] { new Mastery(TalentStat.HpPct, 0.12), new Mastery(TalentStat.DefPct, 0.15) },
                Ultimate = new SkillDef
                {
                    Id = "ult_guardian_cross", Grade = Grade.Legendary, Line = JobLine.Warrior, Kind = SkillKind.Area,
                    Cooldown = 25f, Range = 3.0, DamageMult = 9.0, ShieldPercent = 30, ShieldSeconds = 8f,
                    Vfx = "holy", VfxAt = SkillVfxAt.EachTarget, Sound = "thunder",
                },
            },
            new JobDef
            {
                Id = "berserker", Line = JobLine.Warrior, Tier = 2, Parent = "warrior", Look = "knight4",
                Main = new MainAttack { Targets = 4, Mult = 3.2, Range = 2.4, Tint = 0xFF6050, VfxScale = 1.5f },
                Mastery = new[] { new Mastery(TalentStat.AtkPct, 0.07), new Mastery(TalentStat.CritDamage, 0.15) },
                Ultimate = new SkillDef
                {
                    Id = "ult_blood_rage", Grade = Grade.Legendary, Line = JobLine.Warrior, Kind = SkillKind.Area,
                    Cooldown = 28f, Range = 3.0, DamageMult = 7.0, Buff = SkillBuff.Atk, BuffAmount = 60,
                    BuffSeconds = 10f, HealPercent = 15, Vfx = "aura", VfxAt = SkillVfxAt.Hero, VfxScale = 1.6f,
                    Tint = 0xFF4040, Sound = "cry",
                },
            },
            new JobDef
            {
                Id = "pyro", Line = JobLine.Mage, Tier = 2, Parent = "mage", Look = "jobpyro",
                Main = new MainAttack { Targets = 3, Mult = 2.4, Range = 4.5, DotPercent = 30, DotSeconds = 2f, Vfx = "fire", VfxScale = 0.9f },
                Mastery = new[] { new Mastery(TalentStat.AtkPct, 0.05), new Mastery(TalentStat.DotPct, 0.2) },
                Ultimate = new SkillDef
                {
                    Id = "ult_inferno", Grade = Grade.Legendary, Line = JobLine.Mage, Kind = SkillKind.Area,
                    Cooldown = 24f, Range = 5.0, DamageMult = 3.5, Waves = 4, WaveInterval = 0.2f, DotPercent = 120,
                    DotSeconds = 4f, Vfx = "breath", VfxAt = SkillVfxAt.Front, VfxScale = 1.3f, Sound = "fire",
                },
            },
            new JobDef
            {
                Id = "cryo", Line = JobLine.Mage, Tier = 2, Parent = "mage", Look = "jobcryo",
                Main = new MainAttack { Targets = 4, Mult = 1.9, Range = 4.5, StunSeconds = 0.25f, Vfx = "ice", VfxScale = 0.9f },
                Mastery = new[] { new Mastery(TalentStat.SkillDamagePct, 0.05), new Mastery(TalentStat.CooldownPct, 0.03) },
                Ultimate = new SkillDef
                {
                    Id = "ult_blizzard", Grade = Grade.Legendary, Line = JobLine.Mage, Kind = SkillKind.Area,
                    Cooldown = 26f, Range = 6.0, DamageMult = 3.0, Waves = 5, WaveInterval = 0.2f, StunSeconds = 1.5f,
                    Vfx = "ice", VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.3f, Sound = "ice",
                },
            },
            new JobDef
            {
                Id = "ranger", Line = JobLine.Archer, Tier = 2, Parent = "archer", Look = "jobranger",
                Main = new MainAttack { Targets = 3, Hits = 2, Mult = 1.35, Range = 5.0, Vfx = "spear", Tint = 0xC8F0A0, VfxScale = 0.9f },
                Mastery = new[] { new Mastery(TalentStat.AtkSpdPct, 0.08), new Mastery(TalentStat.CritPoints, 3) },
                Ultimate = new SkillDef
                {
                    Id = "ult_arrow_rain", Grade = Grade.Legendary, Line = JobLine.Archer, Kind = SkillKind.Area,
                    Cooldown = 22f, Range = 6.0, DamageMult = 1.6, Waves = 10, WaveInterval = 0.1f, Vfx = "swords",
                    VfxAt = SkillVfxAt.Front, VfxScale = 1.3f, Tint = 0xC8F0A0, Sound = "whoosh",
                },
            },
            new JobDef
            {
                Id = "sniper", Line = JobLine.Archer, Tier = 2, Parent = "archer", Look = "jobsniper",
                Main = new MainAttack { Targets = 1, Mult = 6.0, Range = 6.0, Vfx = "spear", Tint = 0xFFF0C0, VfxScale = 1.3f },
                Mastery = new[] { new Mastery(TalentStat.CritDamage, 0.3), new Mastery(TalentStat.CritPoints, 5) },
                Ultimate = new SkillDef
                {
                    Id = "ult_death_shot", Grade = Grade.Legendary, Line = JobLine.Archer, Kind = SkillKind.Strike,
                    Cooldown = 22f, Range = 8.0, DamageMult = 45.0, Vfx = "spear", VfxScale = 1.6f, Sound = "thunder",
                },
            },
        };

        public static int Count => All.Length;

        /// <summary>Icon id of a job's main attack in the skill icon set ("main_" + job id).</summary>
        public static string MainIconId(JobDef job) => "main_" + job.Id;

        /// <summary>The job with this id; the beginner for "" or an unknown id.</summary>
        public static JobDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return All[0];
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return All[0];
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return i;
            return 0;
        }
    }
}
