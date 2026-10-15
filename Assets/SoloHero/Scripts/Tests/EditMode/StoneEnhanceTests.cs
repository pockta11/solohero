using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-160 (L-09): Epic and better gear at +10 climbs to +20 with break stones; promotion never takes them away.</summary>
    public sealed class StoneEnhanceTests
    {
        private static SaveDataV2 With(BalanceValues b, GearGrade grade, int level, out string id)
        {
            var save = SaveDataV2.CreateNew();
            id = GachaCatalog.IdOf(EquipmentSlot.Sword, grade);
            EquipmentLevels.AddOwned(save, id);
            EquipmentLevels.Set(save, id, level);
            return save;
        }

        [Test]
        public void Enhance_EpicAtTen_SpendsStonesLevelByLevel_UpToTheCap()
        {
            var b = new BalanceValues();
            SaveDataV2 save = With(b, GearGrade.Epic, b.EQUIP_MAX_LEVEL, out string id);
            Assert.AreEqual(b.EQUIP_STONE_COST, StoneEnhance.Cost(b, b.EQUIP_MAX_LEVEL));
            Assert.AreEqual(FailReason.NotEnoughStones, StoneEnhance.TryEnhance(b, save, id).Reason);

            save.breakStones = 10000;
            int spent = 0;
            for (int level = b.EQUIP_MAX_LEVEL; level < b.EQUIP_STONE_MAX_LEVEL; level++)
            {
                spent += StoneEnhance.Cost(b, level);
                Assert.IsTrue(StoneEnhance.TryEnhance(b, save, id).Ok, "level " + level);
            }

            Assert.AreEqual(b.EQUIP_STONE_MAX_LEVEL, EquipmentLevels.Get(save, id));
            Assert.AreEqual(b.EQUIP_STONE_COST * 55, spent, "1 + 2 + ... + 10 steps of EQUIP_STONE_COST");
            Assert.AreEqual(10000 - spent, save.breakStones);
            Assert.AreEqual(FailReason.MaxLevel, StoneEnhance.TryEnhance(b, save, id).Reason);
            Assert.IsFalse(StoneEnhance.CanEnhance(b, save, id));
        }

        [Test]
        public void Enhance_NeedsTheDuplicateCap_AndEpicOrBetter()
        {
            var b = new BalanceValues();
            SaveDataV2 epic = With(b, GearGrade.Epic, b.EQUIP_MAX_LEVEL - 1, out string epicId);
            epic.breakStones = 1000;
            Assert.AreEqual(FailReason.Locked, StoneEnhance.TryEnhance(b, epic, epicId).Reason, "duplicates take it to +10 first");

            SaveDataV2 rare = With(b, GearGrade.Rare, b.EQUIP_MAX_LEVEL, out string rareId);
            rare.breakStones = 1000;
            Assert.AreEqual(FailReason.Locked, StoneEnhance.TryEnhance(b, rare, rareId).Reason, "rare gear promotes instead");
            Assert.AreEqual(1000, rare.breakStones);
        }

        [Test]
        public void StoneLevels_RaiseTheOwnedBonus_AndDuplicatesStillRefund()
        {
            var b = new BalanceValues();
            SaveDataV2 save = With(b, GearGrade.Legendary, b.EQUIP_MAX_LEVEL, out string id);
            double before = EquipmentBonus.OwnedAtkBonus(b, save);
            save.breakStones = StoneEnhance.Cost(b, b.EQUIP_MAX_LEVEL);
            Assert.IsTrue(StoneEnhance.TryEnhance(b, save, id).Ok);
            Assert.AreEqual(before * (1d + b.EQUIP_ENHANCE_GAIN), EquipmentBonus.OwnedAtkBonus(b, save), 1e-9);
        }

        [Test]
        public void Promotion_IntoAStoneLevelledCopy_KeepsItsLevel()
        {
            var b = new BalanceValues();
            SaveDataV2 save = With(b, GearGrade.Rare, b.EQUIP_MAX_LEVEL, out string rare);
            string epic = GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Epic);
            EquipmentLevels.AddOwned(save, epic);
            EquipmentLevels.Set(save, epic, 14);
            save.gold = 1e9;

            Assert.IsTrue(EquipPromotion.TryPromote(b, save, rare).Ok);
            Assert.AreEqual(14, EquipmentLevels.Get(save, epic), "stone levels are never lost");
        }
    }
}
