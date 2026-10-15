using SoloHero.Core.Jobs;

namespace SoloHero.Core.Talents
{
    /// <summary>
    /// The talent tree (D-087): three branches of four tiers. Tier t opens after t x TALENT_TIER_STEP points in the
    /// same branch. This table is the single source for talent numbers; the GDD mirrors it.
    /// D-158 (L-07): every job line also has a job talent page of <see cref="JobNodesPerLine"/> nodes (6 ranks, 2 points
    /// a rank = 60 points, Lv 80~140 after the 78-point tree), open from the third job. Ids are saved; never rename one.
    /// </summary>
    public static class TalentCatalog
    {
        /// <summary>D-158: the job tier that opens the job talent page.</summary>
        public const int JobPageTier = 3;

        public const int JobNodesPerLine = 5;

        /// <summary>Execute: extra damage against enemies under this HP fraction.</summary>
        public const double ExecuteThreshold = 0.3;
        public const double ExecuteBonus = 0.5;

        /// <summary>Last Stand: once per stage a lethal hit leaves the hero alive with this HP fraction.</summary>
        public const double LastStandHealPct = 0.3;

        /// <summary>Overload: every Nth skill cast deals this damage multiplier.</summary>
        public const int OverloadEvery = 4;
        public const double OverloadMult = 2.0;

        public static readonly TalentDef[] All =
        {
            new TalentDef { Id = "sharpness", Branch = TalentBranch.Might, Tier = 0, Column = 0, Stat = TalentStat.AtkPct, PerRank = 0.02 },
            new TalentDef { Id = "precision", Branch = TalentBranch.Might, Tier = 0, Column = 1, Stat = TalentStat.CritPoints, PerRank = 1 },
            new TalentDef { Id = "ferocity", Branch = TalentBranch.Might, Tier = 1, Column = 0, Stat = TalentStat.CritDamage, PerRank = 0.08 },
            new TalentDef { Id = "swiftness", Branch = TalentBranch.Might, Tier = 1, Column = 1, Stat = TalentStat.AtkSpdPct, PerRank = 0.02 },
            new TalentDef { Id = "giant_slayer", Branch = TalentBranch.Might, Tier = 2, Column = 0, Stat = TalentStat.BossDamagePct, PerRank = 0.05 },
            new TalentDef { Id = "execute", Branch = TalentBranch.Might, Tier = 3, Column = 0, MaxRank = 1, Stat = TalentStat.Execute, PerRank = 1 },

            new TalentDef { Id = "vitality", Branch = TalentBranch.Guard, Tier = 0, Column = 0, Stat = TalentStat.HpPct, PerRank = 0.03 },
            new TalentDef { Id = "iron_hide", Branch = TalentBranch.Guard, Tier = 0, Column = 1, Stat = TalentStat.DefPct, PerRank = 0.03 },
            new TalentDef { Id = "endurance", Branch = TalentBranch.Guard, Tier = 1, Column = 0, Stat = TalentStat.DamageTakenPct, PerRank = 0.02 },
            new TalentDef { Id = "mending", Branch = TalentBranch.Guard, Tier = 1, Column = 1, Stat = TalentStat.HealPct, PerRank = 0.1 },
            new TalentDef { Id = "fortitude", Branch = TalentBranch.Guard, Tier = 2, Column = 0, Stat = TalentStat.HpPct, PerRank = 0.05 },
            new TalentDef { Id = "last_stand", Branch = TalentBranch.Guard, Tier = 3, Column = 0, MaxRank = 1, Stat = TalentStat.LastStand, PerRank = 1 },

            new TalentDef { Id = "focus", Branch = TalentBranch.Arcane, Tier = 0, Column = 0, Stat = TalentStat.SkillDamagePct, PerRank = 0.04 },
            new TalentDef { Id = "haste", Branch = TalentBranch.Arcane, Tier = 0, Column = 1, Stat = TalentStat.CooldownPct, PerRank = 0.02 },
            new TalentDef { Id = "persistence", Branch = TalentBranch.Arcane, Tier = 1, Column = 0, Stat = TalentStat.BuffDurationPct, PerRank = 0.08 },
            new TalentDef { Id = "venom", Branch = TalentBranch.Arcane, Tier = 1, Column = 1, Stat = TalentStat.DotPct, PerRank = 0.1 },
            new TalentDef { Id = "mastery", Branch = TalentBranch.Arcane, Tier = 2, Column = 0, Stat = TalentStat.SkillDamagePct, PerRank = 0.06 },
            new TalentDef { Id = "overload", Branch = TalentBranch.Arcane, Tier = 3, Column = 0, MaxRank = 1, Stat = TalentStat.Overload, PerRank = 1 },
        };

        /// <summary>D-158 job talent pages, <see cref="JobNodesPerLine"/> per line in row order.</summary>
        public static readonly TalentDef[] Job =
        {
            J("w_blade_mastery", JobLine.Warrior, 0, TalentStat.AtkPct, 0.03),
            J("w_iron_body", JobLine.Warrior, 1, TalentStat.HpPct, 0.04),
            J("w_valor", JobLine.Warrior, 2, TalentStat.CritDamage, 0.08),
            J("w_crushing", JobLine.Warrior, 3, TalentStat.BossDamagePct, 0.04),
            J("w_bulwark", JobLine.Warrior, 4, TalentStat.DamageTakenPct, 0.02),

            J("m_arcane_power", JobLine.Mage, 0, TalentStat.SkillDamagePct, 0.04),
            J("m_wisdom", JobLine.Mage, 1, TalentStat.AtkPct, 0.03),
            J("m_elemental", JobLine.Mage, 2, TalentStat.DotPct, 0.08),
            J("m_spell_haste", JobLine.Mage, 3, TalentStat.CooldownPct, 0.015),
            J("m_mana_shield", JobLine.Mage, 4, TalentStat.HpPct, 0.04),

            J("a_focus", JobLine.Archer, 0, TalentStat.AtkPct, 0.03),
            J("a_sharp_eyes", JobLine.Archer, 1, TalentStat.CritPoints, 1),
            J("a_marksmanship", JobLine.Archer, 2, TalentStat.CritDamage, 0.08),
            J("a_quick_draw", JobLine.Archer, 3, TalentStat.AtkSpdPct, 0.02),
            J("a_hunter", JobLine.Archer, 4, TalentStat.BossDamagePct, 0.04),
        };

        public const int BranchCount = 3;
        public const int TierCount = 4;

        private static readonly TalentDef[][] JobByLine = BuildJobByLine();

        private static TalentDef J(string id, JobLine line, int row, TalentStat stat, double perRank) =>
            new TalentDef { Id = id, Line = line, Tier = row, MaxRank = 6, Cost = 2, Stat = stat, PerRank = perRank };

        /// <summary>A node of the tree or of a job page; null for an unknown id.</summary>
        public static TalentDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }

            for (int i = 0; i < Job.Length; i++)
            {
                if (Job[i].Id == id) return Job[i];
            }

            return null;
        }

        /// <summary>D-158: the job page of <paramref name="line"/> in row order (empty for the beginner).</summary>
        public static TalentDef[] JobNodes(JobLine line)
        {
            int l = (int)line;
            return l >= 0 && l < JobByLine.Length ? JobByLine[l] : JobByLine[0];
        }

        /// <summary>Points that fill the shared tree and one job page.</summary>
        public static int FullPoints()
        {
            int sum = 0;
            for (int i = 0; i < All.Length; i++) sum += All[i].MaxRank * All[i].Cost;
            int page = 0;
            for (int l = 1; l < JobByLine.Length; l++)
            {
                int line = 0;
                foreach (TalentDef def in JobByLine[l]) line += def.MaxRank * def.Cost;
                if (line > page) page = line;
            }

            return sum + page;
        }

        private static TalentDef[][] BuildJobByLine()
        {
            var result = new TalentDef[4][];
            for (int l = 0; l < result.Length; l++)
            {
                var list = new System.Collections.Generic.List<TalentDef>();
                foreach (TalentDef def in Job)
                    if ((int)def.Line == l) list.Add(def);
                list.Sort((a, b) => a.Tier.CompareTo(b.Tier));
                result[l] = list.ToArray();
            }

            return result;
        }
    }
}
