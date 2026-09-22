using System;
using System.Collections.Generic;

namespace SoloHero.Core.Save
{
    public static class MigrationV1ToV2
    {
        public const int V1MaxUpgradeLevel = 50;
        public const int V1StagesPerChapter = 5;
        public const int V1BaseCostHp = 100;
        public const int V1BaseCostAtk = 150;
        public const int V1BaseCostDef = 150;
        public const int V1BaseCostSpd = 200;

        public static SaveDataV2 Convert(PlayerDataV1 source, IReadOnlyCollection<string> knownEquipmentIds)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = source.gold
                + Refund(V1BaseCostHp, source.upgradeHpLevel)
                + Refund(V1BaseCostAtk, source.upgradeAtkLevel)
                + Refund(V1BaseCostDef, source.upgradeDefLevel)
                + Refund(V1BaseCostSpd, source.upgradeSpdLevel);
            data.upgradeHp = 0;
            data.upgradeAtk = 0;
            data.upgradeDef = 0;
            data.upgradeSpd = 0;

            int g = (source.chapter - 1) * V1StagesPerChapter + source.stageNumber;
            if (g < 1) g = 1;
            data.highestStage = g;
            data.farmingStage = g;
            data.retreatMode = false;

            data.equippedSword = KeepId(source.equippedWeapon, knownEquipmentIds);
            data.equippedHelm = KeepId(source.equippedHelmet, knownEquipmentIds);
            data.equippedArmor = KeepId(source.equippedArmor, knownEquipmentIds);
            data.equippedBoots = KeepId(source.equippedBoots, knownEquipmentIds);
            data.ownedEquipment = SplitOwned(source.ownedEquipmentCsv, knownEquipmentIds);

            data.pityCount = source.gachaPullCount;
            data.totalPullCount = source.gachaPullCount;
            data.lastQuitTimeUtc = source.lastQuitTimeUtc;
            data.dataVersion = SaveDataV2.CurrentVersion;
            return data;
        }

        public static double Refund(int baseCost, int level)
        {
            if (level <= 0 || baseCost <= 0) return 0d;
            if (level > V1MaxUpgradeLevel) level = V1MaxUpgradeLevel;
            return baseCost * (double)level * (level + 1) / 2d;
        }

        private static string KeepId(string id, IReadOnlyCollection<string> knownEquipmentIds)
        {
            if (string.IsNullOrEmpty(id)) return "";
            if (knownEquipmentIds != null && !ContainsId(knownEquipmentIds, id)) return "";
            return id;
        }

        private static List<string> SplitOwned(string csv, IReadOnlyCollection<string> knownEquipmentIds)
        {
            var owned = new List<string>();
            if (string.IsNullOrEmpty(csv)) return owned;

            string[] parts = csv.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (knownEquipmentIds != null && !ContainsId(knownEquipmentIds, id)) continue;
                owned.Add(id);
            }

            return owned;
        }

        private static bool ContainsId(IReadOnlyCollection<string> knownEquipmentIds, string id)
        {
            foreach (string known in knownEquipmentIds)
            {
                if (known == id) return true;
            }

            return false;
        }
    }
}
