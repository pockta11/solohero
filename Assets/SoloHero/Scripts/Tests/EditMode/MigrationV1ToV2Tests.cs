using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class MigrationV1ToV2Tests
    {
        [Test]
        public void Convert_NoUpgrades_CopiesGoldAndStartsStagesAtOne()
        {
            var source = new PlayerDataV1 { gold = 40, chapter = 1, stageNumber = 1 };

            SaveDataV2 data = MigrationV1ToV2.Convert(source, null);

            Assert.AreEqual(40d, data.gold);
            Assert.AreEqual(1, data.highestStage);
            Assert.AreEqual(1, data.farmingStage);
            Assert.IsFalse(data.retreatMode);
            Assert.AreEqual(0, data.upgradeHp);
            Assert.AreEqual(SaveDataV2.CurrentVersion, data.dataVersion);
        }

        [Test]
        public void Convert_UpgradeLevels_RefundsSpentGoldAndClearsLevels()
        {
            var source = new PlayerDataV1
            {
                gold = 10,
                upgradeHpLevel = 2,
                upgradeAtkLevel = 1,
                upgradeDefLevel = 0,
                upgradeSpdLevel = 50
            };

            SaveDataV2 data = MigrationV1ToV2.Convert(source, null);

            double expected = 10d
                + MigrationV1ToV2.Refund(MigrationV1ToV2.V1BaseCostHp, 2)
                + MigrationV1ToV2.Refund(MigrationV1ToV2.V1BaseCostAtk, 1)
                + MigrationV1ToV2.Refund(MigrationV1ToV2.V1BaseCostSpd, 50);
            Assert.AreEqual(expected, data.gold, 1e-9);
            Assert.AreEqual(300d, MigrationV1ToV2.Refund(100, 2), 1e-9);
            Assert.AreEqual(0, data.upgradeHp);
            Assert.AreEqual(0, data.upgradeAtk);
            Assert.AreEqual(0, data.upgradeDef);
            Assert.AreEqual(0, data.upgradeSpd);
        }

        [Test]
        public void Convert_LevelAboveCap_RefundsOnlyThroughFifty()
        {
            Assert.AreEqual(
                MigrationV1ToV2.Refund(MigrationV1ToV2.V1BaseCostHp, 50),
                MigrationV1ToV2.Refund(MigrationV1ToV2.V1BaseCostHp, 80),
                1e-9);
            Assert.AreEqual(0d, MigrationV1ToV2.Refund(100, -3));
        }

        [Test]
        public void Convert_ChapterAndStage_PreservesFiveStageIndex()
        {
            var source = new PlayerDataV1 { chapter = 2, stageNumber = 5 };

            SaveDataV2 data = MigrationV1ToV2.Convert(source, null);

            Assert.AreEqual(10, data.highestStage);
            Assert.AreEqual(10, data.farmingStage);
        }

        [Test]
        public void Convert_StageBelowOne_ClampsToOne()
        {
            var source = new PlayerDataV1 { chapter = 1, stageNumber = 0 };

            SaveDataV2 data = MigrationV1ToV2.Convert(source, null);

            Assert.AreEqual(1, data.highestStage);
        }

        [Test]
        public void Convert_SlotsAndPity_RenamesAndCopiesCount()
        {
            var source = new PlayerDataV1
            {
                equippedWeapon = "Iron_Sword",
                equippedHelmet = "Iron_Helm",
                equippedArmor = "Iron_Armor",
                equippedBoots = "Iron_Boots",
                gachaPullCount = 7,
                lastQuitTimeUtc = 1234
            };

            SaveDataV2 data = MigrationV1ToV2.Convert(source, null);

            Assert.AreEqual("Iron_Sword", data.equippedSword);
            Assert.AreEqual("Iron_Helm", data.equippedHelm);
            Assert.AreEqual("Iron_Armor", data.equippedArmor);
            Assert.AreEqual("Iron_Boots", data.equippedBoots);
            Assert.AreEqual(7, data.pityCount);
            Assert.AreEqual(7, data.totalPullCount);
            Assert.AreEqual(1234L, data.lastQuitTimeUtc);
        }

        [Test]
        public void Convert_UnknownEquipment_BecomesEmptyWhenCatalogIsProvided()
        {
            var known = new HashSet<string> { "Iron_Sword" };
            var source = new PlayerDataV1
            {
                equippedWeapon = "Iron_Sword",
                equippedHelmet = "Gone_Helm",
                ownedEquipmentCsv = "Iron_Sword|Gone_Helm|"
            };

            SaveDataV2 data = MigrationV1ToV2.Convert(source, known);

            Assert.AreEqual("Iron_Sword", data.equippedSword);
            Assert.AreEqual("", data.equippedHelm);
            CollectionAssert.AreEqual(new[] { "Iron_Sword" }, data.ownedEquipment);
        }
    }
}
