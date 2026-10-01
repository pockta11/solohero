namespace SoloHero.Core.Daily
{
    public enum MissionKind
    {
        Kill = 0,
        StageClear = 1,
        Upgrade = 2,
        Summon = 3,
        WatchAd = 4,

        /// <summary>Claim every other mission of the day.</summary>
        All = 5
    }

    public sealed class MissionDef
    {
        public string Id { get; init; } = "";
        public MissionKind Kind { get; init; }
        public int Target { get; init; } = 1;
        public double Gems { get; init; }

        public string NameKey => "mission.name." + Id;
    }

    /// <summary>
    /// Daily missions (D-099, genre "daily quests"): five everyday actions plus a bonus for clearing them all. Like
    /// SkillCatalog this is the content table; the index is saved (missionProgress / missionClaimed), so only append.
    /// </summary>
    public static class MissionCatalog
    {
        public static readonly MissionDef[] All =
        {
            new MissionDef { Id = "kill", Kind = MissionKind.Kill, Target = 100, Gems = 10 },
            new MissionDef { Id = "clear", Kind = MissionKind.StageClear, Target = 10, Gems = 10 },
            new MissionDef { Id = "upgrade", Kind = MissionKind.Upgrade, Target = 10, Gems = 10 },
            new MissionDef { Id = "summon", Kind = MissionKind.Summon, Target = 10, Gems = 20 },
            new MissionDef { Id = "ad", Kind = MissionKind.WatchAd, Target = 1, Gems = 20 },
            new MissionDef { Id = "all", Kind = MissionKind.All, Target = 5, Gems = 30 },
        };

        public static int Count => All.Length;
    }
}
