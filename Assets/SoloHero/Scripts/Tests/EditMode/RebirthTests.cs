using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;

namespace SoloHero.Tests.EditMode
{
    public sealed class RebirthTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;

            public void RequestSave() => RequestCount++;
        }

        [Test]
        public void SoulGain_FloorGDivFive()
        {
            Assert.AreEqual(0d, RebirthService.SoulGain(4));
            Assert.AreEqual(1d, RebirthService.SoulGain(5));
            Assert.AreEqual(10d, RebirthService.SoulGain(50));
            Assert.AreEqual(10d, RebirthService.SoulGain(54));
            Assert.AreEqual(11d, RebirthService.SoulGain(55));
        }

        [Test]
        public void MinStage_ChapterFiveBoss_IsFifty()
        {
            var balance = new BalanceValues();
            Assert.AreEqual(50, RebirthService.MinStage(balance));
        }

        [Test]
        public void TryRebirth_BelowMinStage_FailsLockedWithoutMutation()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 49;
            data.gold = 1000d;
            data.heroLevel = 20;
            data.soul = 3d;
            data.rebirthCount = 1;
            var save = new FakeSaveRequester();
            var service = new RebirthService(new BalanceValues(), save);

            Result result = service.TryRebirth(data);

            Assert.IsFalse(result.Ok);
            Assert.AreEqual(FailReason.Locked, result.Reason);
            Assert.AreEqual(49, data.highestStage);
            Assert.AreEqual(1000d, data.gold);
            Assert.AreEqual(20, data.heroLevel);
            Assert.AreEqual(3d, data.soul);
            Assert.AreEqual(1, data.rebirthCount);
            Assert.AreEqual(0, save.RequestCount);
        }

        [Test]
        public void TryRebirth_AtChapter5Boss_AddsSoulResetsProgressKeepsMeta()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 50;
            data.farmingStage = 40;
            data.gold = 9999d;
            data.gem = 120d;
            data.heroLevel = 30;
            data.heroExp = 40d;
            data.upgradeHp = 5;
            data.upgradeAtk = 6;
            data.upgradeDef = 7;
            data.upgradeSpd = 8;
            SkillBook.EnsureStarters(data, new BalanceValues());
            SkillBook.SetLevel(data, SkillCatalog.PowerStrike, 3);
            data.soul = 2d;
            data.rebirthCount = 0;
            data.permGoldLevel = 1;
            data.permAtkLevel = 2;
            data.permOfflineLevel = 3;
            data.pityCount = 77;
            data.totalPullCount = 200;
            data.equippedSword = "sword_c";
            data.ownedEquipment.Add("sword_c");
            data.ownedEquipment.Add("helm_r");
            var save = new FakeSaveRequester();
            var service = new RebirthService(new BalanceValues(), save);

            Result result = service.TryRebirth(data);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(2d + 10d, data.soul);
            Assert.AreEqual(1, data.rebirthCount);

            Assert.AreEqual(0d, data.gold);
            Assert.AreEqual(1, data.heroLevel);
            Assert.AreEqual(0d, data.heroExp);
            Assert.AreEqual(0, data.upgradeHp);
            Assert.AreEqual(0, data.upgradeAtk);
            Assert.AreEqual(0, data.upgradeDef);
            Assert.AreEqual(0, data.upgradeSpd);
            Assert.AreEqual(3, SkillBook.GetLevel(data, SkillCatalog.PowerStrike), "skills survive rebirth (D-078)");
            Assert.AreEqual(1, data.highestStage);
            Assert.AreEqual(1, data.farmingStage);

            Assert.AreEqual(120d, data.gem);
            Assert.AreEqual(77, data.pityCount);
            Assert.AreEqual(200, data.totalPullCount);
            Assert.AreEqual(1, data.permGoldLevel);
            Assert.AreEqual(2, data.permAtkLevel);
            Assert.AreEqual(3, data.permOfflineLevel);
            Assert.AreEqual("sword_c", data.equippedSword);
            Assert.AreEqual(2, data.ownedEquipment.Count);
            Assert.AreEqual(1, save.RequestCount);
        }

        [Test]
        public void TryRebirth_NullSaveRequester_StillSucceeds()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 50;
            var service = new RebirthService(new BalanceValues());

            Result result = service.TryRebirth(data);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(10d, data.soul);
            Assert.AreEqual(1, data.rebirthCount);
        }

        [Test]
        public void TryUpgradePerm_CostsBasePlusLevelSoul_AndBoosts()
        {
            var b = new BalanceValues();
            var data = SaveDataV2.CreateNew();
            data.soul = RebirthService.Cost(b, 0) + RebirthService.Cost(b, 1);
            var save = new FakeSaveRequester();
            var service = new RebirthService(b, save);

            Assert.IsTrue(service.TryUpgradePerm(data, RebirthService.PermLane.Atk).Ok);
            Assert.IsTrue(service.TryUpgradePerm(data, RebirthService.PermLane.Atk).Ok);

            Assert.AreEqual(0d, data.soul, 1e-9);
            Assert.AreEqual(2, data.permAtkLevel);
            Assert.AreEqual(2, save.RequestCount);
            Assert.AreEqual(System.Math.Pow(1d + b.REBIRTH_ATK_GAIN, 2), RebirthService.AtkMult(b, data), 1e-12);
            Assert.AreEqual(FailReason.NotEnoughSoul, service.TryUpgradePerm(data, RebirthService.PermLane.Gold).Reason);
        }

        [Test]
        public void TryRebirth_KeepsBestStageForUnlocks_AndClearsTalents()
        {
            var data = SaveDataV2.CreateNew();
            data.highestStage = 57;
            data.heroLevel = 44;
            data.talentIds.Add("sharpness");
            data.talentRanks.Add(3);

            Assert.IsTrue(new RebirthService(new BalanceValues()).TryRebirth(data).Ok);

            Assert.AreEqual(57, data.bestStageEver);
            Assert.AreEqual(57, RebirthService.BestStage(data));
            Assert.AreEqual(44, data.bestHeroLevel);
            Assert.AreEqual(0, data.talentIds.Count);
            Assert.IsTrue(SoloHero.Core.Pets.PetSummonService.IsUnlocked(new BalanceValues(), data), "the pet summon stays open");
        }
    }
}
