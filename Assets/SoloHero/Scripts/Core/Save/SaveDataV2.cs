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

        /// <summary>D-102 companions: the equipped id ("" none; every save starts with the slime) and levels index-aligned with CompanionCatalog.</summary>
        public string companionEquipped = "slime";
        public List<int> companionLevels = new List<int>();

        public int upgradeHp;
        public int upgradeAtk;
        public int upgradeDef;
        public int upgradeSpd;

        public string equippedSword = "";
        public string equippedHelm = "";
        public string equippedArmor = "";
        public string equippedBoots = "";
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

        public int tutorialStep;

        public bool bgmMuted;
        public bool sfxMuted;
        public bool lowEffectMode;
        public bool fps30Mode;
        public bool skillManualMode;

        public int rebirthCount;
        public double soul;
        public int permGoldLevel;
        public int permAtkLevel;
        public int permOfflineLevel;

        public int dataVersion = CurrentVersion;

        public static SaveDataV2 CreateNew() => new SaveDataV2();
    }
}
