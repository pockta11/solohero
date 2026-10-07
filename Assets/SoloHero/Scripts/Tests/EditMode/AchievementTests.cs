using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-118 achievements: tiers read lifetime totals, claim in order for gems, and claim-all sweeps every tier.</summary>
    public sealed class AchievementTests
    {
        [Test]
        public void Catalog_TargetsGrowAndEveryTierPays()
        {
            var ids = new HashSet<string>();
            foreach (AchievementDef def in AchievementCatalog.All)
            {
                Assert.IsTrue(ids.Add(def.Id), def.Id);
                Assert.AreEqual(def.Targets.Length, def.Gems.Length, def.Id);
                for (int t = 0; t < def.Tiers; t++)
                {
                    Assert.Greater(def.Gems[t], 0d, def.Id);
                    if (t > 0) Assert.Greater(def.Targets[t], def.Targets[t - 1], def.Id);
                }
            }
        }

        [Test]
        public void Claim_OneTierAtATime_GivesGems_UntilFinished()
        {
            var save = SaveDataV2.CreateNew();
            var service = new AchievementService(save);
            int kills = System.Array.FindIndex(AchievementCatalog.All, d => d.Kind == AchievementKind.Kills);
            AchievementDef def = AchievementCatalog.All[kills];

            Assert.AreEqual(FailReason.Locked, service.TryClaim(kills).Reason);
            save.totalKills = def.Targets[1];
            Assert.IsTrue(service.TryClaim(kills).Ok);
            Assert.AreEqual(def.Gems[0], save.gem, 1e-9);
            Assert.AreEqual(1, service.Claimed(kills));
            Assert.AreEqual(def.Targets[1], service.Target(kills));
            Assert.IsTrue(service.Claimable(kills), "the second tier is also done");

            save.totalKills = long.MaxValue / 2;
            service.ClaimAll();
            Assert.IsTrue(service.Finished(kills));
            Assert.IsFalse(service.Claimable(kills));
            Assert.AreEqual(FailReason.Locked, service.TryClaim(kills).Reason);
        }

        [Test]
        public void Progress_ReadsBestOfAnyRun_AndCollections()
        {
            var save = SaveDataV2.CreateNew();
            save.highestStage = 12;
            save.bestStageEver = 57;
            save.heroLevel = 3;
            save.bestHeroLevel = 41;
            save.petOwned.Add("slime");
            save.petOwned.Add("wisp");

            Assert.AreEqual(57, AchievementService.Progress(save, AchievementKind.BestStage));
            Assert.AreEqual(41, AchievementService.Progress(save, AchievementKind.HeroLevel));
            Assert.AreEqual(2, AchievementService.Progress(save, AchievementKind.PetsOwned));
        }

        [Test]
        public void StageKills_CountForAchievements()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            SoloHero.Core.Pets.PetService.EnsureOwned(save);
            var runner = new SoloHero.Core.Stage.StageRunner(b, new SystemRandom(new System.Random(3)),
                new SoloHero.Core.Growth.HeroStats(1e6, 1e4, 1e4, b.ATKSPD_BASE, 0d), save);
            SoloHero.Core.Stage.CombatLoadout.Apply(runner, b, save);
            runner.Begin(1);
            for (int i = 0; i < 1200; i++) runner.Tick(0.05f);
            Assert.Greater(save.totalKills, 0L);
        }
    }
}
