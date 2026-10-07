using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-116 equipment promotion: +10 items buy the next grade at +5, up to Epic; the source restarts at +0.</summary>
    public sealed class EquipPromotionTests
    {
        private static SaveDataV2 WithMaxed(BalanceValues b, GearGrade grade, bool equip)
        {
            var save = SaveDataV2.CreateNew();
            string id = GachaCatalog.IdOf(EquipmentSlot.Sword, grade);
            EquipmentLevels.AddOwned(save, id);
            EquipmentLevels.Set(save, id, b.EQUIP_MAX_LEVEL);
            if (equip) save.equippedSword = id;
            return save;
        }

        [Test]
        public void Promote_MaxedCommon_GivesUncommonAtFive_SourceBackToZero_EquipFollows()
        {
            var b = new BalanceValues();
            SaveDataV2 save = WithMaxed(b, GearGrade.Common, equip: true);
            string common = GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Common);
            string uncommon = GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Uncommon);
            double cost = EquipPromotion.Cost(b, GearGrade.Common);
            save.gold = cost;

            Assert.IsTrue(EquipPromotion.TryPromote(b, save, common).Ok);

            Assert.AreEqual(0d, save.gold, 1e-9);
            Assert.AreEqual(0, EquipmentLevels.Get(save, common));
            Assert.IsTrue(save.ownedEquipment.Contains(common), "the source stays in the collection");
            Assert.AreEqual(b.EQUIP_PROMOTE_ENHANCE, EquipmentLevels.Get(save, uncommon));
            Assert.AreEqual(uncommon, save.equippedSword);
        }

        [Test]
        public void Promote_OwnedTarget_GainsLevelsCapped_EquippedOtherItemStays()
        {
            var b = new BalanceValues();
            SaveDataV2 save = WithMaxed(b, GearGrade.Uncommon, equip: false);
            string rare = GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Rare);
            string epic = GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Epic);
            EquipmentLevels.AddOwned(save, rare);
            EquipmentLevels.Set(save, rare, 8);
            EquipmentLevels.AddOwned(save, epic);
            save.equippedSword = epic;
            save.gold = 1e9;

            Assert.IsTrue(EquipPromotion.TryPromote(b, save, GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Uncommon)).Ok);

            Assert.AreEqual(b.EQUIP_MAX_LEVEL, EquipmentLevels.Get(save, rare), "8 + 5 caps at the max");
            Assert.AreEqual(epic, save.equippedSword);
        }

        [Test]
        public void Promote_Refused_BelowMax_AtEpic_UnownedOrPoor()
        {
            var b = new BalanceValues();
            SaveDataV2 save = WithMaxed(b, GearGrade.Epic, equip: false);
            save.gold = 1e9;
            Assert.AreEqual(FailReason.Locked, EquipPromotion.TryPromote(b, save, GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Epic)).Reason,
                "Legendary and above only come from the summon");

            string common = GachaCatalog.IdOf(EquipmentSlot.Helm, GearGrade.Common);
            EquipmentLevels.AddOwned(save, common);
            EquipmentLevels.Set(save, common, b.EQUIP_MAX_LEVEL - 1);
            Assert.AreEqual(FailReason.Locked, EquipPromotion.TryPromote(b, save, common).Reason);
            Assert.AreEqual(FailReason.Locked, EquipPromotion.TryPromote(b, save, GachaCatalog.IdOf(EquipmentSlot.Ring, GearGrade.Common)).Reason);

            EquipmentLevels.Set(save, common, b.EQUIP_MAX_LEVEL);
            save.gold = EquipPromotion.Cost(b, GearGrade.Common) - 1d;
            Assert.AreEqual(FailReason.NotEnoughGold, EquipPromotion.TryPromote(b, save, common).Reason);
        }

        [Test]
        public void PromoteAll_LowestFirst_ChainsWhileGoldLasts()
        {
            var b = new BalanceValues();
            SaveDataV2 save = WithMaxed(b, GearGrade.Common, equip: true);
            string uncommon = GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Uncommon);
            EquipmentLevels.AddOwned(save, uncommon);
            EquipmentLevels.Set(save, uncommon, 7);
            save.gold = EquipPromotion.Cost(b, GearGrade.Common) + EquipPromotion.Cost(b, GearGrade.Uncommon);

            EquipPromotion.TotalCost(b, save, out int before);
            Assert.AreEqual(1, before, "only the common is at the max now");
            int made = EquipPromotion.PromoteAll(b, save, out double spent);

            // Common -> Uncommon (7 + 5 caps at 10, now promotable) -> Rare at +5.
            Assert.AreEqual(2, made);
            Assert.AreEqual(save.gold, 0d, 1e-9);
            Assert.AreEqual(EquipPromotion.Cost(b, GearGrade.Common) + EquipPromotion.Cost(b, GearGrade.Uncommon), spent, 1e-9);
            Assert.AreEqual(b.EQUIP_PROMOTE_ENHANCE, EquipmentLevels.Get(save, GachaCatalog.IdOf(EquipmentSlot.Sword, GearGrade.Rare)));
        }
    }
}
