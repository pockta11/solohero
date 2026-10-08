using System.Collections.Generic;

namespace SoloHero.Core.Save
{
    public sealed class SaveDataV2
    {
        public const int CurrentVersion = 2;

        /// <summary>
        /// Increments on every save request (E9-08). On load the copy with the higher revision wins, so a remote copy
        /// that missed the last writes (app killed before the upload) never overwrites a newer local backup.
        /// </summary>
        public long saveRevision;

        public double gold;
        public double gem;

        public int highestStage = 1;
        public int farmingStage = 1;
        public bool retreatMode;
        public List<bool> chapterFirstClearFlags = new List<bool>();

        public int heroLevel = 1;
        public double heroExp;

        /// <summary>D-101 promotion tier, superseded by jobs (D-104); kept only so old saves load. Unused.</summary>
        public int promotionTier;

        /// <summary>D-104 job id from JobCatalog; "" is the beginner.</summary>
        public string jobId = "";

        /// <summary>
        /// D-114 pets (the D-102 companions; these two keep their save names): the equipped pet id ("" none; every save
        /// starts with the slime) and gold levels index-aligned with PetCatalog.
        /// </summary>
        public string companionEquipped = "slime";
        public List<int> companionLevels = new List<int>();

        /// <summary>
        /// D-114: pets obtained (an older save starts empty and PetService.EnsureOwned fills it with the pets its cleared
        /// stages had unlocked), duplicate enhance levels index-aligned with PetCatalog, and the pet summon's own pity
        /// and pull counters.
        /// </summary>
        public List<string> petOwned = new List<string>();
        public List<int> petEnhance = new List<int>();
        public int petPityCount;
        public int petPullCount;

        public int upgradeHp;
        public int upgradeAtk;
        public int upgradeDef;
        public int upgradeSpd;

        /// <summary>D-142 crit rate and crit damage lanes (open with hero levels).</summary>
        public int upgradeCrit;
        public int upgradeCritDmg;

        /// <summary>
        /// D-143 limit breaks per lane, index-aligned with UpgradeLane (LaneRules.EnsureBreaks fills a save from before
        /// them as broken up to its levels), and the breakthrough stones held.
        /// </summary>
        public List<int> laneBreaks = new List<int>();
        public int breakStones;

        /// <summary>
        /// D-141 AP put into the main stat and vitality. apManual false = auto: new points are split at once
        /// (HeroAp.Settle), which also hands an older save the points of every past level.
        /// </summary>
        public int apMain;
        public int apVit;
        public bool apManual;

        public string equippedSword = "";
        public string equippedHelm = "";
        public string equippedArmor = "";
        public string equippedBoots = "";

        /// <summary>D-109 accessories ("" empty; saves from before D-109 load with all four empty).</summary>
        public string equippedGloves = "";
        public string equippedNecklace = "";
        public string equippedRing = "";
        public string equippedEarring = "";
        public List<string> ownedEquipment = new List<string>();
        public List<int> ownedEquipmentLevels = new List<int>();

        public int pityCount;
        public int totalPullCount;

        /// <summary>Levels of the three fixed skills before D-078. Read once by SkillBook.EnsureStarters, then unused.</summary>
        public int skillLevel1;
        public int skillLevel2;
        public int skillLevel3;

        /// <summary>Skill collection (D-078): owned ids and their levels, index-aligned.</summary>
        public List<string> ownedSkills = new List<string>();
        public List<int> ownedSkillLevels = new List<int>();

        /// <summary>One entry per skill slot in cast order; "" is an empty slot.</summary>
        public List<string> equippedSkills = new List<string>();

        public List<string> talentIds = new List<string>();
        public List<int> talentRanks = new List<int>();

        public int skillPityCount;
        public int skillPullCount;

        public long lastQuitTimeUtc;

        public int adCountA1;
        public int adCountA2;
        public int adCountA3;

        /// <summary>D-120: free gear and pet summons watched today.</summary>
        public int adCountA4;
        public int adCountA5;

        /// <summary>D-129: speed boosters watched today and when the running one ends (UTC seconds).</summary>
        public int adCountA6;
        public long speedBoostEndUtc;
        public string adCountResetDate = "";

        public long goldBoosterEndUtc;

        /// <summary>D-099 attendance: days claimed in total (the 7-day cycle is count % 7) and the last claim's local date.</summary>
        public int attendanceCount;
        public string attendanceDate = "";

        /// <summary>D-099 daily missions, index-aligned with MissionCatalog; reset when missionDate is not today.</summary>
        public string missionDate = "";
        public List<int> missionProgress = new List<int>();
        public List<bool> missionClaimed = new List<bool>();

        /// <summary>D-100 daily dungeon entries used today (gold / EXP); reset when dungeonDate is not today.</summary>
        public string dungeonDate = "";
        public int dungeonGoldUsed;
        public int dungeonExpUsed;

        /// <summary>D-128 summon tickets (one pull each) and today's shop purchases, index-aligned with ShopItem.</summary>
        public int gearTickets;
        public int skillTickets;
        public int petTickets;
        public string shopDate = "";
        public List<bool> shopBought = new List<bool>();

        /// <summary>D-130: the highest infinite-tower floor cleared.</summary>
        public int towerFloor;

        public int tutorialStep;

        /// <summary>D-111: index of the current guide quest in GuideQuestCatalog.</summary>
        public int guideQuest;

        public bool bgmMuted;
        public bool sfxMuted;
        public bool lowEffectMode;
        public bool fps30Mode;
        public bool skillManualMode;

        /// <summary>D-119: reminders switched off in the settings, and whether Android's permission was asked.</summary>
        public bool notificationsOff;
        public bool notifyAsked;

        /// <summary>D-118: lifetime enemy and boss kills, and the achievement tiers claimed (index-aligned with AchievementCatalog).</summary>
        public long totalKills;
        public int bossKills;
        public List<int> achievementTiers = new List<int>();

        public int dataVersion = CurrentVersion;

        public static SaveDataV2 CreateNew() => new SaveDataV2();
    }
}
