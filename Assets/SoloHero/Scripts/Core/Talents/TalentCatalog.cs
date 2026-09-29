namespace SoloHero.Core.Talents
{
    /// <summary>
    /// The talent tree (D-087): three branches of four tiers. Tier t opens after t x TALENT_TIER_STEP points in the
    /// same branch. This table is the single source for talent numbers; the GDD mirrors it.
    /// </summary>
    public static class TalentCatalog
    {
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

        public const int BranchCount = 3;
        public const int TierCount = 4;

        public static TalentDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }

            return null;
        }
    }
}
