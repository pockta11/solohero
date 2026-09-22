using NUnit.Framework;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class SaveDataV2Tests
    {
        [Test]
        public void CreateNew_SetsNewUserDefaults()
        {
            SaveDataV2 data = SaveDataV2.CreateNew();

            Assert.AreEqual(0d, data.gold);
            Assert.AreEqual(0d, data.gem);
            Assert.AreEqual(1, data.highestStage);
            Assert.AreEqual(1, data.farmingStage);
            Assert.IsFalse(data.retreatMode);
            Assert.IsNotNull(data.chapterFirstClearFlags);
            Assert.AreEqual(0, data.chapterFirstClearFlags.Count);
            Assert.AreEqual(1, data.heroLevel);
            Assert.AreEqual(0d, data.heroExp);
            Assert.AreEqual(0, data.upgradeHp);
            Assert.AreEqual(0, data.upgradeAtk);
            Assert.AreEqual(0, data.upgradeDef);
            Assert.AreEqual(0, data.upgradeSpd);
            Assert.AreEqual("", data.equippedSword);
            Assert.AreEqual("", data.equippedHelm);
            Assert.AreEqual("", data.equippedArmor);
            Assert.AreEqual("", data.equippedBoots);
            Assert.IsNotNull(data.ownedEquipment);
            Assert.AreEqual(0, data.ownedEquipment.Count);
            Assert.AreEqual(0, data.pityCount);
            Assert.AreEqual(0, data.totalPullCount);
            Assert.AreEqual(0, data.skillLevel1);
            Assert.AreEqual(0, data.skillLevel2);
            Assert.AreEqual(0, data.skillLevel3);
            Assert.AreEqual(0L, data.lastQuitTimeUtc);
            Assert.AreEqual(0, data.adCountA1);
            Assert.AreEqual(0, data.adCountA2);
            Assert.AreEqual(0, data.adCountA3);
            Assert.AreEqual("", data.adCountResetDate);
            Assert.AreEqual(0L, data.goldBoosterEndUtc);
            Assert.AreEqual(0, data.tutorialStep);
            Assert.IsFalse(data.bgmMuted);
            Assert.IsFalse(data.sfxMuted);
            Assert.IsFalse(data.lowEffectMode);
            Assert.IsFalse(data.fps30Mode);
            Assert.AreEqual(0, data.rebirthCount);
            Assert.AreEqual(0d, data.soul);
            Assert.AreEqual(0, data.permGoldLevel);
            Assert.AreEqual(0, data.permAtkLevel);
            Assert.AreEqual(0, data.permOfflineLevel);
            Assert.AreEqual(SaveDataV2.CurrentVersion, data.dataVersion);
        }
    }
}
