using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-172: the x100 pull is ten ten-pulls at the ten-pull price, all or nothing, on the same counters.</summary>
    public sealed class BulkSummonTests
    {
        private static IRandom Rng(int seed) => new SystemRandom(new System.Random(seed));

        [Test]
        public void Gear_Hundred_CostsTenTenPulls_CountsEveryPull()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.gold = b.GACHA_COST_TEN * 10 + 5;
            var gacha = new GachaService(b, GearTableValues.FromBalance(b), Rng(3), GachaCatalog.Standard(b));

            GachaBatchResult result = gacha.TryPullTens(save, b.SUMMON_BULK_PULLS / 10);

            Assert.IsTrue(result.Status.Ok);
            Assert.AreEqual(100, result.Items.Length);
            Assert.AreEqual(100, save.totalPullCount);
            Assert.GreaterOrEqual(save.gold, 5d, "only refunds can add gold back");
        }

        [Test]
        public void Gear_Hundred_ShortOfGold_PullsNothing()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.gold = b.GACHA_COST_TEN * 10 - 1;
            var gacha = new GachaService(b, GearTableValues.FromBalance(b), Rng(3), GachaCatalog.Standard(b));

            GachaBatchResult result = gacha.TryPullTens(save, 10);

            Assert.IsFalse(result.Status.Ok);
            Assert.AreEqual(FailReason.NotEnoughGold, result.Status.Reason);
            Assert.AreEqual(b.GACHA_COST_TEN * 10 - 1, save.gold);
            Assert.AreEqual(0, save.totalPullCount);
        }

        [Test]
        public void Pet_And_Skill_Hundred_UseTheirOwnPrices()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            PetService.EnsureOwned(save);
            save.highestStage = b.PET_UNLOCK_STAGE;
            save.heroLevel = b.JOB_LV_1;
            save.jobId = "warrior";
            save.gold = b.PET_SUMMON_COST_TEN * 10 + b.SKILL_SUMMON_COST_TEN * 10;
            var pets = new PetSummonService(b, GearTableValues.FromBalance(b), Rng(5));
            var skills = new SkillSummonService(b, GachaTableValues.FromBalance(b), Rng(5));

            PetSummonResult pet = pets.TryPullTens(save, 10);
            SkillSummonResult skill = skills.TryPullTens(save, 10);

            Assert.IsTrue(pet.Status.Ok);
            Assert.IsTrue(skill.Status.Ok);
            Assert.AreEqual(100, pet.Items.Length);
            Assert.AreEqual(100, skill.Items.Length);
            Assert.AreEqual(100, save.petPullCount);
            Assert.AreEqual(100, save.skillPullCount);
        }

        [Test]
        public void Tickets_SpendUpToTheBatch()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.gearTickets = 130;
            var gacha = new GachaService(b, GearTableValues.FromBalance(b), Rng(9), GachaCatalog.Standard(b));

            Assert.AreEqual(100, gacha.TryPullTickets(save, b.SUMMON_TICKET_MAX).Items.Length);
            Assert.AreEqual(30, save.gearTickets);
            Assert.AreEqual(30, gacha.TryPullTickets(save, b.SUMMON_TICKET_MAX).Items.Length);
            Assert.AreEqual(0, save.gearTickets);
        }
    }
}
