using SoloHero.Core.Daily;
using SoloHero.Core.Economy;

namespace SoloHero.Core.Events
{
    /// <summary>D-156: an event mission counts an action over the whole event and pays event tokens.</summary>
    public sealed class EventMission
    {
        public string Id { get; init; } = "";
        public MissionKind Kind { get; init; }
        public long Target { get; init; } = 1;
        public int Tokens { get; init; }

        public string NameKey => "event.mission." + Id;
    }

    /// <summary>D-156: an exchange shop item - event tokens for a reward, bought up to Limit times an event.</summary>
    public sealed class EventShopItem
    {
        public string Id { get; init; } = "";
        public int Cost { get; init; }
        public int Limit { get; init; } = 1;
        public RewardBundle Reward { get; init; } = new RewardBundle();

        public string NameKey => "event.shop." + Id;
    }

    /// <summary>
    /// D-156 (L-16): an event's content - an attendance board (one reward a day, missed days are not lost), missions that
    /// pay tokens, and the token exchange shop. Templates ship in the app; the schedule (which template runs when) comes
    /// from the server, so a new event is a data change, not an update.
    /// </summary>
    public sealed class EventTemplate
    {
        public string Id { get; init; } = "";
        public RewardBundle[] Attendance { get; init; } = new RewardBundle[0];
        public EventMission[] Missions { get; init; } = new EventMission[0];
        public EventShopItem[] Shop { get; init; } = new EventShopItem[0];

        public string NameKey => "event.name." + Id;
    }

    /// <summary>
    /// D-156 event templates. The progress lists in the save are index-aligned with a template's tables, and a new event
    /// id resets them, so a template may change between events but not while one of its events runs.
    /// </summary>
    public static class EventCatalog
    {
        public static readonly EventTemplate[] All =
        {
            new EventTemplate
            {
                Id = "launch",
                Attendance = new[]
                {
                    new RewardBundle { Gems = 100 },
                    new RewardBundle { GearTickets = 10 },
                    new RewardBundle { Tokens = 20 },
                    new RewardBundle { SkillTickets = 3 },
                    new RewardBundle { Tokens = 30 },
                    new RewardBundle { PetTickets = 3 },
                    new RewardBundle { Gems = 300, GearTickets = 10 },
                },
                Missions = new[]
                {
                    new EventMission { Id = "kill", Kind = MissionKind.Kill, Target = 3000, Tokens = 30 },
                    new EventMission { Id = "clear", Kind = MissionKind.StageClear, Target = 100, Tokens = 30 },
                    new EventMission { Id = "summon", Kind = MissionKind.Summon, Target = 300, Tokens = 30 },
                    new EventMission { Id = "dungeon", Kind = MissionKind.Dungeon, Target = 10, Tokens = 20 },
                    new EventMission { Id = "tower", Kind = MissionKind.Tower, Target = 5, Tokens = 20 },
                    new EventMission { Id = "upgrade", Kind = MissionKind.Upgrade, Target = 100, Tokens = 20 },
                },
                Shop = new[]
                {
                    new EventShopItem { Id = "gear10", Cost = 40, Limit = 3, Reward = new RewardBundle { GearTickets = 10 } },
                    new EventShopItem { Id = "skill3", Cost = 40, Limit = 2, Reward = new RewardBundle { SkillTickets = 3 } },
                    new EventShopItem { Id = "pet3", Cost = 40, Limit = 2, Reward = new RewardBundle { PetTickets = 3 } },
                    new EventShopItem { Id = "gem300", Cost = 50, Limit = 2, Reward = new RewardBundle { Gems = 300 } },
                    new EventShopItem { Id = "stone50", Cost = 30, Limit = 3, Reward = new RewardBundle { Stones = 50 } },
                    new EventShopItem { Id = "gold", Cost = 20, Limit = 5, Reward = new RewardBundle { GoldStages = 300 } },
                },
            },
            new EventTemplate
            {
                Id = "festival",
                Attendance = new[]
                {
                    new RewardBundle { Tokens = 20 },
                    new RewardBundle { Gems = 100 },
                    new RewardBundle { Tokens = 20 },
                    new RewardBundle { GearTickets = 10 },
                    new RewardBundle { Tokens = 30 },
                    new RewardBundle { Stones = 50 },
                    new RewardBundle { Gems = 200, SkillTickets = 3, PetTickets = 3 },
                },
                Missions = new[]
                {
                    new EventMission { Id = "kill", Kind = MissionKind.Kill, Target = 5000, Tokens = 30 },
                    new EventMission { Id = "clear", Kind = MissionKind.StageClear, Target = 150, Tokens = 30 },
                    new EventMission { Id = "summon", Kind = MissionKind.Summon, Target = 500, Tokens = 30 },
                    new EventMission { Id = "dungeon", Kind = MissionKind.Dungeon, Target = 14, Tokens = 30 },
                    new EventMission { Id = "tower", Kind = MissionKind.Tower, Target = 7, Tokens = 30 },
                    new EventMission { Id = "ad", Kind = MissionKind.WatchAd, Target = 10, Tokens = 30 },
                },
                Shop = new[]
                {
                    new EventShopItem { Id = "gear10", Cost = 40, Limit = 4, Reward = new RewardBundle { GearTickets = 10 } },
                    new EventShopItem { Id = "skill3", Cost = 40, Limit = 3, Reward = new RewardBundle { SkillTickets = 3 } },
                    new EventShopItem { Id = "pet3", Cost = 40, Limit = 3, Reward = new RewardBundle { PetTickets = 3 } },
                    new EventShopItem { Id = "gem300", Cost = 50, Limit = 2, Reward = new RewardBundle { Gems = 300 } },
                    new EventShopItem { Id = "stone50", Cost = 30, Limit = 3, Reward = new RewardBundle { Stones = 50 } },
                    new EventShopItem { Id = "gold", Cost = 20, Limit = 5, Reward = new RewardBundle { GoldStages = 300 } },
                },
            },
        };

        public static EventTemplate Find(string id)
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
