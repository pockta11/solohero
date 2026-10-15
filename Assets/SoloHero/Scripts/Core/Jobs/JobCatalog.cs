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

        /// <summary>0 beginner, 1 first job, 2 second job, 3 third job (D-157).</summary>
        public int Tier { get; init; }

        /// <summary>The job this one advances from ("" for the first jobs, which come from the beginner).</summary>
        public string Parent { get; init; } = "";

        /// <summary>Null for the beginner, who keeps the BASIC_SKILL_* flash slash.</summary>
        public MainAttack Main { get; init; }

        /// <summary>Passive stats of this job alone; a job also keeps the mastery of every job it advanced from.</summary>
        public Mastery[] Mastery { get; init; } = new Mastery[0];

        /// <summary>Second and third jobs: the job-only skill in the ultimate slot (a third job's replaces its parent's).</summary>
        public SkillDef Ultimate { get; init; }

        /// <summary>Hero sheet entity under Art/Hero for this job's look.</summary>
        public string Look { get; init; } = "knight";

        public string NameKey => "job.name." + Id;
        public string MainKey => "job.main." + Id;
        public string DescKey => "job.desc." + Id;
    }

    /// <summary>
    /// D-104 jobs, MapleStory-style: the beginner, three first jobs at JOB_LV_1 and two second jobs for each at
    /// JOB_LV_2. D-157 (L-05): one third job for each second job at JOB_LV_3, a straight succession with a stronger
    /// main attack and ultimate and one more mastery. Content table like SkillCatalog; ids are saved
    /// (SaveDataV2.jobId), so never rename one. New jobs go at the end (the job looks are index-aligned).
    /// </summary>
    public static class JobCatalog
    {
        public const string Beginner = "beginner";

        /// <summary>The last job tier (D-157: third jobs).</summary>
        public const int MaxTier = 3;

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

            // D-157 (L-05) third jobs, one per second job: the main attack and the ultimate replace the parent's, the
            // mastery stacks on the first and second job's.
            new JobDef
            {
                Id = "paladin", Line = JobLine.Warrior, Tier = 3, Parent = "knight", Look = "knight5",
                Main = new MainAttack { Targets = 6, Mult = 3.2, Range = 2.8, Tint = 0xFFF0A0, VfxScale = 1.65f },
                Mastery = new[] { new Mastery(TalentStat.HpPct, 0.15), new Mastery(TalentStat.DamageTakenPct, 0.05) },
                Ultimate = new SkillDef
                {
                    Id = "ult_sanctuary", Grade = Grade.Legendary, Line = JobLine.Warrior, Kind = SkillKind.Area,
                    Cooldown = 25f, Range = 3.5, DamageMult = 13.0, ShieldPercent = 40, ShieldSeconds = 8f, HealPercent = 10,
                    Vfx = "holy", VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.2f, Sound = "thunder",
                },
            },
            new JobDef
            {
                Id = "warlord", Line = JobLine.Warrior, Tier = 3, Parent = "berserker", Look = "knight6",
                Main = new MainAttack { Targets = 5, Mult = 3.9, Range = 2.6, Tint = 0xFF4040, VfxScale = 1.65f },
                Mastery = new[] { new Mastery(TalentStat.AtkPct, 0.08), new Mastery(TalentStat.CritDamage, 0.2) },
                Ultimate = new SkillDef
                {
                    Id = "ult_warlord_wrath", Grade = Grade.Legendary, Line = JobLine.Warrior, Kind = SkillKind.Area,
                    Cooldown = 28f, Range = 3.2, DamageMult = 10.0, Buff = SkillBuff.Atk, BuffAmount = 80,
                    BuffSeconds = 10f, HealPercent = 20, Vfx = "aura", VfxAt = SkillVfxAt.Hero, VfxScale = 1.8f,
                    Tint = 0xFF2030, Sound = "cry",
                },
            },
            new JobDef
            {
                Id = "archpyro", Line = JobLine.Mage, Tier = 3, Parent = "pyro", Look = "jobarchpyro",
                Main = new MainAttack { Targets = 4, Mult = 2.9, Range = 4.8, DotPercent = 40, DotSeconds = 2f, Vfx = "fire", VfxScale = 1.05f },
                Mastery = new[] { new Mastery(TalentStat.AtkPct, 0.06), new Mastery(TalentStat.DotPct, 0.25) },
                Ultimate = new SkillDef
                {
                    Id = "ult_meteor_storm", Grade = Grade.Legendary, Line = JobLine.Mage, Kind = SkillKind.Area,
                    Cooldown = 24f, Range = 5.5, DamageMult = 4.2, Waves = 5, WaveInterval = 0.2f, DotPercent = 150,
                    DotSeconds = 4f, Vfx = "meteor", VfxAt = SkillVfxAt.Front, VfxScale = 1.6f, Sound = "fire",
                },
            },
            new JobDef
            {
                Id = "archcryo", Line = JobLine.Mage, Tier = 3, Parent = "cryo", Look = "jobarchcryo",
                Main = new MainAttack { Targets = 5, Mult = 2.3, Range = 4.8, StunSeconds = 0.3f, Vfx = "ice", VfxScale = 1.05f },
                Mastery = new[] { new Mastery(TalentStat.SkillDamagePct, 0.08), new Mastery(TalentStat.CooldownPct, 0.04) },
                Ultimate = new SkillDef
                {
                    Id = "ult_frozen_thunder", Grade = Grade.Legendary, Line = JobLine.Mage, Kind = SkillKind.Area,
                    Cooldown = 26f, Range = 6.5, DamageMult = 3.6, Waves = 6, WaveInterval = 0.2f, StunSeconds = 2f,
                    Vfx = "bolt", VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.3f, Tint = 0xA0E8FF, Sound = "thunder",
                },
            },
            new JobDef
            {
                Id = "bowmaster", Line = JobLine.Archer, Tier = 3, Parent = "ranger", Look = "jobbowmaster",
                Main = new MainAttack { Targets = 4, Hits = 2, Mult = 1.65, Range = 5.2, Vfx = "spear", Tint = 0xFFE890, VfxScale = 1f },
                Mastery = new[] { new Mastery(TalentStat.AtkSpdPct, 0.08), new Mastery(TalentStat.AtkPct, 0.05) },
                Ultimate = new SkillDef
                {
                    Id = "ult_arrow_storm", Grade = Grade.Legendary, Line = JobLine.Archer, Kind = SkillKind.Area,
                    Cooldown = 22f, Range = 6.5, DamageMult = 1.9, Waves = 12, WaveInterval = 0.1f, MarkPercent = 15,
                    MarkSeconds = 5f, Vfx = "swords", VfxAt = SkillVfxAt.Front, VfxScale = 1.5f, Tint = 0xFFE890,
                    Sound = "whoosh",
                },
            },
            new JobDef
            {
                Id = "marksman", Line = JobLine.Archer, Tier = 3, Parent = "sniper", Look = "jobmarksman",
                Main = new MainAttack { Targets = 1, Mult = 7.5, Range = 6.5, Vfx = "spear", Tint = 0xFFD0A0, VfxScale = 1.45f },
                Mastery = new[] { new Mastery(TalentStat.CritDamage, 0.35), new Mastery(TalentStat.BossDamagePct, 0.1) },
                Ultimate = new SkillDef
                {
                    Id = "ult_true_shot", Grade = Grade.Legendary, Line = JobLine.Archer, Kind = SkillKind.Strike,
                    Cooldown = 22f, Range = 9.0, DamageMult = 62.0, MarkPercent = 25, MarkSeconds = 6f, Vfx = "spear",
                    VfxScale = 1.9f, Tint = 0xFFC060, Sound = "thunder",
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
