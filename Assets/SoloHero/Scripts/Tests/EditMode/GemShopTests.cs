using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>E6-02: 50 gems = farming stage gold x 100.</summary>
    public sealed class GemShopTests
    {
        private sealed class Counter : ISaveRequester
        {
            public int Count;

            public void RequestSave() => Count++;
        }

        [Test]
        public void GoldPack_PaysFarmingStageGoldTimes100()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gem = 60;
            save.farmingStage = 12;
            var counter = new Counter();
            var shop = new GemShop(b, counter);
            double expected = System.Math.Floor(Formulas.StageGold(b, 12) * b.GEM_GOLD_PACK_STAGES);

            Assert.IsTrue(shop.TryBuyGoldPack(save).Ok);

            Assert.AreEqual(expected, save.gold, 1e-6);
            Assert.AreEqual(60 - b.GEM_GOLD_PACK_COST, save.gem, 1e-9);
            Assert.AreEqual(1, counter.Count);
        }

        [Test]
        public void GoldPack_NotEnoughGems_FailsWithoutChange()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gem = b.GEM_GOLD_PACK_COST - 1;

            Result r = new GemShop(b).TryBuyGoldPack(save);

            Assert.IsFalse(r.Ok);
            Assert.AreEqual(FailReason.NotEnoughGem, r.Reason);
            Assert.AreEqual(0d, save.gold);
            Assert.AreEqual(b.GEM_GOLD_PACK_COST - 1, save.gem, 1e-9);
        }
    }
}
