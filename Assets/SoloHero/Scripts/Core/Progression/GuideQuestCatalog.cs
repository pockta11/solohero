namespace SoloHero.Core.Progression
{
    /// <summary>What a guide quest counts. Every kind reads totals the save already keeps, so nothing is tracked twice.</summary>
    public enum GuideKind
    {
        /// <summary>Sum of the four upgrade lane levels.</summary>
        UpgradeTotal = 0,

        /// <summary>Highest cleared global stage.</summary>
        ClearStage = 1,

        HeroLevel = 2,

        /// <summary>Gear summons made (the welcome gift counts).</summary>
        GearPulls = 3,

        SkillPulls = 4,

        /// <summary>Skill slots holding a skill.</summary>
        EquipSkills = 5,

        /// <summary>Job tier reached (1 first job, 2 second job).</summary>
        JobTier = 6,

        /// <summary>Talent ranks learned.</summary>
        Talents = 7,

        /// <summary>Different gear items owned.</summary>
        OwnedGear = 8
    }

    public sealed class GuideQuestDef
    {
        public GuideKind Kind { get; init; }
        public int Target { get; init; }
        public double Gems { get; init; }

        /// <summary>Gold reward as this many clears of the current farming stage (keeps up with the economy).</summary>
        public double GoldStages { get; init; }
    }

    /// <summary>
    /// D-111 guide quests (genre "main quest" bar): one quest at a time, front-loaded so the first hours always show
    /// the next step - upgrade, summon, beat the boss, advance the job. After the hand-made chain the quests repeat
    /// in a cycle with growing targets, so the bar never runs dry. The index is saved (SaveDataV2.guideQuest).
    /// </summary>
    public static class GuideQuestCatalog
    {
        private static GuideQuestDef Q(GuideKind kind, int target, double gems = 0d, double goldStages = 0d) =>
            new GuideQuestDef { Kind = kind, Target = target, Gems = gems, GoldStages = goldStages };

        public static readonly GuideQuestDef[] Chain =
        {
            Q(GuideKind.UpgradeTotal, 1, gems: 20),
            Q(GuideKind.ClearStage, 3, goldStages: 5),
            Q(GuideKind.UpgradeTotal, 5, gems: 20),
            Q(GuideKind.GearPulls, 3, gems: 30),
            Q(GuideKind.ClearStage, 5, goldStages: 8),
            Q(GuideKind.HeroLevel, 5, gems: 20),
            Q(GuideKind.EquipSkills, 2, gems: 20),
            Q(GuideKind.ClearStage, 10, gems: 50),
            Q(GuideKind.UpgradeTotal, 15, goldStages: 10),
            Q(GuideKind.HeroLevel, 10, gems: 30),
            Q(GuideKind.JobTier, 1, gems: 50),
            Q(GuideKind.SkillPulls, 3, gems: 30),
            Q(GuideKind.EquipSkills, 3, gems: 20),
            Q(GuideKind.ClearStage, 15, goldStages: 12),
            Q(GuideKind.OwnedGear, 10, gems: 30),
            Q(GuideKind.Talents, 3, gems: 20),
            Q(GuideKind.ClearStage, 20, gems: 80),
            Q(GuideKind.UpgradeTotal, 40, goldStages: 15),
            Q(GuideKind.HeroLevel, 20, gems: 30),
            Q(GuideKind.GearPulls, 30, gems: 50),
            Q(GuideKind.ClearStage, 25, goldStages: 15),
            Q(GuideKind.EquipSkills, 5, gems: 30),
            Q(GuideKind.ClearStage, 30, gems: 80),
            Q(GuideKind.HeroLevel, 30, gems: 50),
            Q(GuideKind.JobTier, 2, gems: 100),
            Q(GuideKind.ClearStage, 35, goldStages: 20),
            Q(GuideKind.OwnedGear, 20, gems: 50),
            Q(GuideKind.ClearStage, 40, gems: 100),
        };

        /// <summary>Kinds of the endless cycle after the chain, one step each.</summary>
        private static readonly GuideKind[] Cycle =
        {
            GuideKind.ClearStage, GuideKind.UpgradeTotal, GuideKind.GearPulls, GuideKind.HeroLevel
        };

        /// <summary>The quest at <paramref name="index"/>: the chain first, then the cycle with growing targets.</summary>
        public static GuideQuestDef At(int index)
        {
            if (index < 0) index = 0;
            if (index < Chain.Length) return Chain[index];
            int k = index - Chain.Length;
            int round = k / Cycle.Length + 1;
            switch (Cycle[k % Cycle.Length])
            {
                case GuideKind.ClearStage: return Q(GuideKind.ClearStage, 40 + 5 * round, gems: 80);
                case GuideKind.UpgradeTotal: return Q(GuideKind.UpgradeTotal, 40 + 15 * round, goldStages: 20);
                case GuideKind.GearPulls: return Q(GuideKind.GearPulls, 30 + 20 * round, gems: 50);
                default: return Q(GuideKind.HeroLevel, 30 + 3 * round, gems: 40);
            }
        }

        /// <summary>The cycle's first quest index (for tests and tooling).</summary>
        public static int CycleStart => Chain.Length;

        public static int CycleLength => Cycle.Length;
    }
}
