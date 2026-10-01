using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    public sealed class PromotionTests
    {
        [Test]
        public void TryPromote_NeedsLevelAndGold_RaisesTierAndStats()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 20;
            var promotion = new PromotionService(save, b);

            save.heroLevel = b.PROMOTE_LV_1 - 1;
            save.gold = 1e12;
            Assert.AreEqual(FailReason.Locked, promotion.TryPromote().Reason);

            save.heroLevel = b.PROMOTE_LV_1;
            save.gold = promotion.Cost(1) - 1d;
            Assert.AreEqual(FailReason.NotEnoughGold, promotion.TryPromote().Reason);

            HeroStats before = CombatLoadout.ComputeStats(b, save);
            save.gold = promotion.Cost(1);
            int promoted = 0;
            promotion.Promoted += tier => promoted = tier;
            Assert.IsTrue(promotion.TryPromote().Ok);
            Assert.AreEqual(1, promotion.Tier);
            Assert.AreEqual(1, promoted);
            Assert.AreEqual(0d, save.gold, 1e-9);

            HeroStats after = CombatLoadout.ComputeStats(b, save);
            Assert.AreEqual(before.Hp * b.PROMOTE_STAT_MULT, after.Hp, before.Hp * 1e-9);
            Assert.AreEqual(before.Atk * b.PROMOTE_STAT_MULT, after.Atk, before.Atk * 1e-9);
            Assert.AreEqual(before.Def * b.PROMOTE_STAT_MULT, after.Def, before.Def * 1e-9);
            Assert.AreEqual(before.AtkSpd, after.AtkSpd, 1e-9);
        }

        [Test]
        public void TryPromote_AtTierFour_ReturnsMaxLevel()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = 999;
            save.gold = 1e30;
            var promotion = new PromotionService(save, b);
            for (int i = 0; i < PromotionService.MaxTier; i++) Assert.IsTrue(promotion.TryPromote().Ok);
            Assert.IsTrue(promotion.IsMax);
            Assert.AreEqual(FailReason.MaxLevel, promotion.TryPromote().Reason);
            Assert.AreEqual(b.PROMOTE_COST_STAGES * 2d, promotion.Cost(2) / Formulas.StageGold(b, 1), 1e-6);
        }
    }
}
