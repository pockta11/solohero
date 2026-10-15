using NUnit.Framework;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-154 (L-08): every tenth level pays on the spot, every fiftieth more; achievement tracks only grew.</summary>
    public sealed class LevelRewardTests
    {
        [Test]
        public void For_EveryTenthLevel_BiggerOnTheFiftieth()
        {
            var b = new BalanceValues();

            Assert.IsFalse(LevelReward.For(b, 1).Any);
            Assert.IsFalse(LevelReward.For(b, 15).Any);
            LevelRewardPay ten = LevelReward.For(b, 10);
            Assert.AreEqual(b.LEVEL_REWARD_GEM, ten.Gems);
            Assert.AreEqual(b.LEVEL_REWARD_GEAR_TICKETS, ten.GearTickets);
            Assert.AreEqual(b.LEVEL_REWARD_STONES, ten.Stones);
            Assert.AreEqual(0, ten.SkillTickets);
            LevelRewardPay fifty = LevelReward.For(b, 50);
            Assert.AreEqual(b.LEVEL_REWARD_GEM + b.LEVEL_REWARD_BIG_GEM, fifty.Gems);
            Assert.AreEqual(b.LEVEL_REWARD_BIG_TICKETS, fifty.SkillTickets);
            Assert.AreEqual(b.LEVEL_REWARD_BIG_TICKETS, fifty.PetTickets);
        }

        [Test]
        public void LevelUp_PaysEachRewardLevelOnce()
        {
            var b = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            var levels = new HeroLevelService(data, b);
            double toTen = 0d;
            for (int l = 1; l < 10; l++) toTen += HeroLevelService.ExpRequired(b, l);

            levels.AddExp(toTen - 1d);
            Assert.AreEqual(9, data.heroLevel);
            Assert.AreEqual(0d, data.gem, 1e-9);

            levels.AddExp(2d);
            Assert.AreEqual(10, data.heroLevel);
            Assert.AreEqual(b.LEVEL_REWARD_GEM, data.gem, 1e-9);
            Assert.AreEqual(b.LEVEL_REWARD_GEAR_TICKETS, data.gearTickets);
            Assert.AreEqual(b.LEVEL_REWARD_STONES, data.breakStones);

            levels.AddExp(1d);
            Assert.AreEqual(b.LEVEL_REWARD_GEM, data.gem, 1e-9);
        }

        [Test]
        public void Achievements_D118TiersKept_YearTiersAppended()
        {
            AchievementDef stage = AchievementCatalog.All[2];
            AchievementDef level = AchievementCatalog.All[3];

            Assert.AreEqual("stage", stage.Id);
            Assert.AreEqual(100, stage.Targets[9]);
            Assert.AreEqual(500, stage.Targets[stage.Tiers - 1]);
            Assert.AreEqual("level", level.Id);
            Assert.AreEqual(70, level.Targets[6]);
            Assert.AreEqual(200, level.Targets[level.Tiers - 1]);
        }
    }
}
