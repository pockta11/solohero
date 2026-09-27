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

        private static readonly string[] LegacyMaterials = { "iron", "steel", "magic", "divine" };

        /// <summary>
        /// Maps a 3D-era equipment id (asset name or SO id, e.g. "Iron_Sword" / "divine_crown") to the v2 catalog id.
        /// Material gives the grade (iron C, steel R, magic E, divine L), the item kind gives the slot.
        /// </summary>
        public static string MapLegacyId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string[] parts = id.ToLowerInvariant().Split('_');
            if (parts.Length != 2) return "";

            int grade = Array.IndexOf(LegacyMaterials, parts[0]);
            if (grade < 0) return "";

            string slot;
            switch (parts[1])
            {
                case "sword":
                case "blade":
                    slot = "Sword";
                    break;
                case "helm":
                case "crown":
                    slot = "Helm";
                    break;
                case "armor":
                case "robe":
                    slot = "Armor";
                    break;
                case "boots":
                    slot = "Boots";
                    break;
                default:
                    return "";
            }

            return "Equipment_" + slot + "_" + ((Gacha.Grade)grade);
        }

        private static string KeepId(string id, IReadOnlyCollection<string> knownEquipmentIds)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string mapped = MapLegacyId(id);
            if (mapped.Length > 0) return mapped;
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
                string id = KeepId(parts[i], knownEquipmentIds);
                if (id.Length == 0 || owned.Contains(id)) continue;
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
