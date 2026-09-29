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
