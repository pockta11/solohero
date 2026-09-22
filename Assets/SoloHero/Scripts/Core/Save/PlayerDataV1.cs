using System;

namespace SoloHero.Core.Save
{
    [Serializable]
    public sealed class PlayerDataV1
    {
        public long gold;
        public int chapter = 1;
        public long lastQuitTimeUtc;
        public int stageKillsCurrent;
        public int gachaPullCount;
        public string equippedWeapon = "";
        public string equippedHelmet = "";
        public string equippedArmor = "";
        public string equippedBoots = "";
        public string ownedEquipmentCsv = "";
        public int stageNumber = 1;
        public int upgradeHpLevel;
        public int upgradeAtkLevel;
        public int upgradeDefLevel;
        public int upgradeSpdLevel;
        public int dataVersion = 1;
    }
}
