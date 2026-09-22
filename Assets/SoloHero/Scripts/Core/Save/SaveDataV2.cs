using System.Collections.Generic;

namespace SoloHero.Core.Save
{
    public sealed class SaveDataV2
    {
        public const int CurrentVersion = 2;

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

        public int pityCount;
        public int totalPullCount;

        public int skillLevel1;
        public int skillLevel2;
        public int skillLevel3;

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

        public int rebirthCount;
        public double soul;
        public int permGoldLevel;
        public int permAtkLevel;
        public int permOfflineLevel;

        public int dataVersion = CurrentVersion;

        public static SaveDataV2 CreateNew() => new SaveDataV2();
    }
}
