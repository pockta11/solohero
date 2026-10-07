namespace SoloHero.Core.Progression
{
    /// <summary>What an achievement counts. Every kind reads a lifetime total the save already keeps.</summary>
    public enum AchievementKind
    {
        Kills = 0,
        BossKills = 1,

        /// <summary>Best stage of any run.</summary>
        BestStage = 2,

        /// <summary>Best hero level of any run.</summary>
        HeroLevel = 3,
        GearPulls = 4,
        SkillPulls = 5,
        PetPulls = 6,
        GearOwned = 7,
        PetsOwned = 8,
        SkillsOwned = 9
    }

    public sealed class AchievementDef
    {
        public string Id { get; init; } = "";
        public AchievementKind Kind { get; init; }

        /// <summary>Targets of the tiers in order; tier i is complete when the count reaches Targets[i].</summary>
        public long[] Targets { get; init; } = new long[0];

        /// <summary>Gems for each tier, index-aligned with <see cref="Targets"/>.</summary>
        public double[] Gems { get; init; } = new double[0];

        public string NameKey => "achieve.name." + Id;
        public int Tiers => Targets.Length;
    }

    /// <summary>
    /// D-118 achievements (genre: tiered lifetime goals): ten tracks of growing targets, gems for every tier. The index
    /// is saved (achievementTiers, the tiers claimed), so only append.
    /// </summary>
    public static class AchievementCatalog
    {
        private static readonly double[] Six = { 20, 30, 50, 80, 120, 200 };

        public static readonly AchievementDef[] All =
        {
            new AchievementDef { Id = "kills", Kind = AchievementKind.Kills, Targets = new long[] { 100, 1000, 5000, 20000, 100000, 500000 }, Gems = Six },
            new AchievementDef { Id = "bosses", Kind = AchievementKind.BossKills, Targets = new long[] { 1, 5, 10, 20, 40, 80 }, Gems = Six },
            new AchievementDef
            {
                Id = "stage", Kind = AchievementKind.BestStage,
                Targets = new long[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
                Gems = new double[] { 30, 30, 50, 50, 80, 80, 120, 120, 200, 200 }
            },
            new AchievementDef
            {
                Id = "level", Kind = AchievementKind.HeroLevel, Targets = new long[] { 10, 20, 30, 40, 50, 60, 70 },
                Gems = new double[] { 20, 30, 50, 80, 100, 150, 200 }
            },
            new AchievementDef { Id = "gear_pulls", Kind = AchievementKind.GearPulls, Targets = new long[] { 50, 200, 500, 1000, 2000, 5000 }, Gems = Six },
            new AchievementDef
            {
                Id = "skill_pulls", Kind = AchievementKind.SkillPulls, Targets = new long[] { 10, 50, 150, 400, 1000 },
                Gems = new double[] { 20, 30, 50, 80, 150 }
            },
            new AchievementDef
            {
                Id = "pet_pulls", Kind = AchievementKind.PetPulls, Targets = new long[] { 10, 50, 150, 400, 1000 },
                Gems = new double[] { 20, 30, 50, 80, 150 }
            },
            new AchievementDef { Id = "gear_owned", Kind = AchievementKind.GearOwned, Targets = new long[] { 10, 20, 30, 40, 50, 56 }, Gems = Six },
            new AchievementDef
            {
                Id = "pets_owned", Kind = AchievementKind.PetsOwned, Targets = new long[] { 4, 8, 12, 16 },
                Gems = new double[] { 30, 60, 120, 200 }
            },
            new AchievementDef { Id = "skills_owned", Kind = AchievementKind.SkillsOwned, Targets = new long[] { 5, 10, 20, 30, 40, 51 }, Gems = Six },
        };

        public static int Count => All.Length;
    }
}
