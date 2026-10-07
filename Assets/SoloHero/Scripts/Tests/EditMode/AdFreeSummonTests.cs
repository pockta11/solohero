using System;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-120 ad summons: own daily counters, no reward from the policy itself, and free pulls that move pity and counters.</summary>
    public sealed class AdFreeSummonTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime Local = new DateTime(2026, 10, 7, 10, 0, 0);

            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => Local;
        }

        [Test]
        public void FreeSummonSlots_CountSeparately_AndResetAtMidnight()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var policy = new AdSlotPolicy(b, save, clock);

            for (int i = 0; i < b.AD_FREE_GEAR_DAILY; i++)
                Assert.IsTrue(policy.Complete(AdSlot.FreeGearSummon, AdOutcome.Rewarded).Ok);

            Assert.AreEqual(FailReason.DailyLimit, policy.CanUse(AdSlot.FreeGearSummon).Reason);
            Assert.AreEqual(b.AD_FREE_PET_DAILY, policy.Remaining(AdSlot.FreePetSummon));
            Assert.AreEqual(b.AD_BOOSTER_DAILY, policy.Remaining(AdSlot.GoldBooster), "the booster keeps its own count");
            Assert.AreEqual(0d, save.gem, 1e-9, "the policy grants nothing; the panel makes the pulls");
            Assert.AreEqual(0d, save.gold, 1e-9);

            clock.Local = clock.Local.AddDays(1);
            Assert.AreEqual(b.AD_FREE_GEAR_DAILY, policy.Remaining(AdSlot.FreeGearSummon));
        }

        [Test]
        public void GearPullFree_CostsNothing_AndAdvancesCountersAndPity()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gold = 123d;
            var service = new GachaService(b, GearTableValues.FromBalance(b), new SystemRandom(new Random(5)), GachaCatalog.Standard(b));

            GachaBatchResult r = service.PullFree(save, b.AD_FREE_SUMMON_PULLS);

            Assert.IsTrue(r.Status.Ok);
            Assert.AreEqual(b.AD_FREE_SUMMON_PULLS, r.Items.Length);
            Assert.AreEqual(123d, save.gold, 1e-9);
            Assert.AreEqual(b.AD_FREE_SUMMON_PULLS, save.totalPullCount);
        }

        [Test]
        public void PetPullFree_NeedsTheOpenSummon_ThenCostsNothing()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            PetService.EnsureOwned(save);
            var service = new PetSummonService(b, GearTableValues.FromBalance(b), new SystemRandom(new Random(9)));

            Assert.AreEqual(FailReason.Locked, service.TryPullFree(save, b.AD_FREE_SUMMON_PULLS).Status.Reason);

            save.highestStage = b.PET_UNLOCK_STAGE;
            PetSummonResult r = service.TryPullFree(save, b.AD_FREE_SUMMON_PULLS);

            Assert.IsTrue(r.Status.Ok);
            Assert.AreEqual(b.AD_FREE_SUMMON_PULLS, r.Items.Length);
            Assert.AreEqual(0d, save.gold, 1e-9);
            Assert.AreEqual(b.AD_FREE_SUMMON_PULLS, save.petPullCount);
        }
    }
}
