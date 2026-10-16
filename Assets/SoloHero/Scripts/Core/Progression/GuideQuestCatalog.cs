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
        OwnedGear = 8,

        /// <summary>D-114 pet summons made.</summary>
        PetPulls = 9
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
    /// D-113: gear pull and collection targets sit above the welcome ten-pull, since pulls got cheap and there are 56 items.
    /// D-180: the chain is twice as dense (60 steps, 2-3 stages or a few upgrades apart) with smaller rewards each, so a
    /// new player claims something every couple of minutes; the gem and gold totals of the old 30-step chain stay
    /// (about 1,000 gems and 110 stage clears of gold up to 4-10).
    /// </summary>
    public static class GuideQuestCatalog
    {
        private static GuideQuestDef Q(GuideKind kind, int target, double gems = 0d, double goldStages = 0d) =>
            new GuideQuestDef { Kind = kind, Target = target, Gems = gems, GoldStages = goldStages };

        public static readonly GuideQuestDef[] Chain =
        {
            // First minutes: upgrade, clear, the welcome summons. Gold and the first gems come early (the first five
            // minutes must buy three upgrades - the gem gold pack does it - and the first boss must fall within two tries:
            // BalanceChecks FS-5 / V-6a).
            Q(GuideKind.UpgradeTotal, 1, gems: 20),
            Q(GuideKind.ClearStage, 2, goldStages: 3),
            Q(GuideKind.UpgradeTotal, 2, gems: 20),
            Q(GuideKind.GearPulls, 11, gems: 20),
            Q(GuideKind.ClearStage, 4, goldStages: 4),
            Q(GuideKind.HeroLevel, 3, gems: 10),
            Q(GuideKind.UpgradeTotal, 4, goldStages: 4),
            Q(GuideKind.EquipSkills, 2, gems: 10),
            Q(GuideKind.ClearStage, 6, goldStages: 4),
            Q(GuideKind.GearPulls, 15, gems: 15),
            Q(GuideKind.HeroLevel, 5, gems: 10),
            Q(GuideKind.UpgradeTotal, 7, goldStages: 4),
            Q(GuideKind.ClearStage, 8, goldStages: 5),
            Q(GuideKind.OwnedGear, 8, gems: 15),
            Q(GuideKind.UpgradeTotal, 10, goldStages: 4),
            // The first boss, the pet summon and the first job.
            Q(GuideKind.ClearStage, 10, gems: 40),
            Q(GuideKind.PetPulls, 1, gems: 15),
            Q(GuideKind.HeroLevel, 8, gems: 10),
            Q(GuideKind.PetPulls, 3, gems: 15),
            Q(GuideKind.ClearStage, 12, goldStages: 5),
            Q(GuideKind.HeroLevel, 10, gems: 20),
            Q(GuideKind.JobTier, 1, gems: 50),
            Q(GuideKind.SkillPulls, 1, gems: 15),
            Q(GuideKind.EquipSkills, 3, gems: 15),
            Q(GuideKind.UpgradeTotal, 20, goldStages: 5),
            Q(GuideKind.ClearStage, 15, goldStages: 5),
            Q(GuideKind.SkillPulls, 5, gems: 20),
            Q(GuideKind.OwnedGear, 15, gems: 20),
            Q(GuideKind.Talents, 1, gems: 10),
            Q(GuideKind.HeroLevel, 15, gems: 20),
            Q(GuideKind.Talents, 3, gems: 15),
            Q(GuideKind.ClearStage, 18, goldStages: 6),
            Q(GuideKind.UpgradeTotal, 30, goldStages: 6),
            Q(GuideKind.GearPulls, 30, gems: 20),
            // Chapter 2 boss, more slots, the summons as a habit.
            Q(GuideKind.ClearStage, 20, gems: 50),
            Q(GuideKind.HeroLevel, 20, gems: 20),
            Q(GuideKind.EquipSkills, 4, gems: 15),
            Q(GuideKind.PetPulls, 10, gems: 20),
            Q(GuideKind.UpgradeTotal, 40, goldStages: 6),
            Q(GuideKind.ClearStage, 23, goldStages: 8),
            Q(GuideKind.GearPulls, 60, gems: 30),
            Q(GuideKind.OwnedGear, 22, gems: 20),
            Q(GuideKind.SkillPulls, 10, gems: 20),
            Q(GuideKind.ClearStage, 25, goldStages: 6),
            Q(GuideKind.HeroLevel, 25, gems: 20),
            Q(GuideKind.PetPulls, 30, gems: 30),
            Q(GuideKind.Talents, 10, gems: 20),
            Q(GuideKind.UpgradeTotal, 55, goldStages: 8),
            Q(GuideKind.EquipSkills, 5, gems: 20),
            Q(GuideKind.ClearStage, 28, goldStages: 8),
            // Chapter 3 boss and the second job.
            Q(GuideKind.ClearStage, 30, gems: 50),
            Q(GuideKind.HeroLevel, 30, gems: 30),
            Q(GuideKind.JobTier, 2, gems: 80),
            Q(GuideKind.GearPulls, 100, gems: 30),
            Q(GuideKind.ClearStage, 33, goldStages: 6),
            Q(GuideKind.OwnedGear, 30, gems: 30),
            Q(GuideKind.UpgradeTotal, 70, goldStages: 8),
            Q(GuideKind.ClearStage, 35, goldStages: 8),
            Q(GuideKind.Talents, 20, gems: 20),
            Q(GuideKind.ClearStage, 40, gems: 80),
        };

        /// <summary>Kinds of the endless cycle after the chain, one step each (D-180: smaller steps, six kinds).</summary>
        private static readonly GuideKind[] Cycle =
        {
            GuideKind.ClearStage, GuideKind.UpgradeTotal, GuideKind.GearPulls, GuideKind.HeroLevel, GuideKind.SkillPulls, GuideKind.PetPulls
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
                case GuideKind.ClearStage: return Q(GuideKind.ClearStage, 40 + 3 * round, gems: 40);
                case GuideKind.UpgradeTotal: return Q(GuideKind.UpgradeTotal, 70 + 10 * round, goldStages: 10);
                case GuideKind.GearPulls: return Q(GuideKind.GearPulls, 100 + 30 * round, gems: 30);
                case GuideKind.SkillPulls: return Q(GuideKind.SkillPulls, 10 + 10 * round, gems: 20);
                case GuideKind.PetPulls: return Q(GuideKind.PetPulls, 30 + 10 * round, gems: 20);
                default: return Q(GuideKind.HeroLevel, 30 + 2 * round, gems: 20);
            }
        }

        /// <summary>The cycle's first quest index (for tests and tooling).</summary>
        public static int CycleStart => Chain.Length;

        public static int CycleLength => Cycle.Length;
    }
}
