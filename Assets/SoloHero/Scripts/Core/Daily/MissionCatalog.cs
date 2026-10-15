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
        All = 5,

        /// <summary>D-155: a daily dungeon run started.</summary>
        Dungeon = 6,

        /// <summary>D-155: an infinite tower climb started.</summary>
        Tower = 7
    }

    public sealed class MissionDef
    {
        public string Id { get; init; } = "";
        public MissionKind Kind { get; init; }
        public int Target { get; init; } = 1;
        public double Gems { get; init; }

        /// <summary>D-155: weekly missions also pay summon tickets and breakthrough stones.</summary>
        public int GearTickets { get; init; }
        public int SkillTickets { get; init; }
        public int PetTickets { get; init; }
        public int Stones { get; init; }

        public int Tickets => GearTickets + SkillTickets + PetTickets;

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

    /// <summary>
    /// D-155 (L-11) weekly missions (genre "weekly quests"): bigger targets over a week that rolls over Monday 00:00,
    /// paying gems, summon tickets and breakthrough stones, plus a chest for clearing them all. Index saved
    /// (weeklyProgress / weeklyClaimed), so only append. Same six rows as the daily list in the popup.
    /// </summary>
    public static class WeeklyMissionCatalog
    {
        public static readonly MissionDef[] All =
        {
            new MissionDef { Id = "w_kill", Kind = MissionKind.Kill, Target = 5000, Gems = 50 },
            new MissionDef { Id = "w_clear", Kind = MissionKind.StageClear, Target = 150, Gems = 50 },
            new MissionDef { Id = "w_summon", Kind = MissionKind.Summon, Target = 500, Gems = 50, GearTickets = 3 },
            new MissionDef { Id = "w_dungeon", Kind = MissionKind.Dungeon, Target = 10, Gems = 30, Stones = 20 },
            new MissionDef { Id = "w_tower", Kind = MissionKind.Tower, Target = 7, Gems = 50, Stones = 10 },
            new MissionDef { Id = "w_all", Kind = MissionKind.All, Target = 5, Gems = 150, GearTickets = 10, SkillTickets = 2, PetTickets = 2 },
        };

        public static int Count => All.Length;
    }
}
