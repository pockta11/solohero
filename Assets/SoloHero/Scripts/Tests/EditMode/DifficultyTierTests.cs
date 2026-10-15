using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-153 (L-02 / L-03): tiers name and colour stages, and the first clear of a tier's last boss opens the next.</summary>
    public sealed class DifficultyTierTests
    {
        private const string TablePath = "Assets/SoloHero/Data/Strings/strings_ko.txt";

        [TearDown]
        public void Reset() => Strings.Load(null);

        [Test]
        public void Tier_EveryTenChapters_NamedUpToAbyssThenNumbered()
        {
            var b = new BalanceValues();

            Assert.AreEqual(0, DifficultyTier.OfChapter(b, 1));
            Assert.AreEqual(0, DifficultyTier.OfChapter(b, 10));
            Assert.AreEqual(1, DifficultyTier.OfChapter(b, 11));
            Assert.AreEqual(4, DifficultyTier.OfChapter(b, 50));
            Assert.AreEqual(5, DifficultyTier.OfChapter(b, 51));
            Assert.AreEqual(1, DifficultyTier.Of(b, 101));
            Assert.AreEqual(0, DifficultyTier.Of(b, 100));
            Assert.AreEqual(3, DifficultyTier.ChapterInTier(b, 13));
            Assert.AreEqual(10, DifficultyTier.ChapterInTier(b, 20));
            Assert.AreEqual(101, DifficultyTier.FirstStage(b, 1));
            Assert.IsTrue(DifficultyTier.IsLastBoss(b, 100));
            Assert.IsTrue(DifficultyTier.IsLastBoss(b, 300));
            Assert.IsFalse(DifficultyTier.IsLastBoss(b, 90));

            Assert.AreEqual("tier.name.4", DifficultyTier.NameKey(4));
            Assert.AreEqual("tier.name.4", DifficultyTier.NameKey(7));
            Assert.AreEqual(0, DifficultyTier.Rank(4));
            Assert.AreEqual(2, DifficultyTier.Rank(5));
            Assert.AreEqual("IV", DifficultyTier.Roman(4));
            Assert.AreEqual("XIV", DifficultyTier.Roman(14));
        }

        [Test]
        public void Labels_NormalTierPlain_LaterTiersNamed()
        {
            Strings.Load(new Dictionary<string, string>
            {
                { "tier.name.1", "Hard" }, { "tier.name.2", "Hell" }, { "tier.name.4", "Abyss" },
                { "tier.stage", "{0} {1}-{2}" }, { "tier.chapter", "{0} ch.{1}" }, { "stage.chapter", "ch.{0}" }
            });
            var b = new BalanceValues();

            Assert.AreEqual("3-7", DifficultyTier.StageLabel(b, 27));
            Assert.AreEqual("10-10", DifficultyTier.StageLabel(b, 100));
            Assert.AreEqual("Hard 1-1", DifficultyTier.StageLabel(b, 101));
            Assert.AreEqual("Hard 3-7", DifficultyTier.StageLabel(b, 127));
            Assert.AreEqual("Abyss II 1-1", DifficultyTier.StageLabel(b, 501));
            Assert.AreEqual("ch.3", DifficultyTier.ChapterLabel(b, 3));
            Assert.AreEqual("Hell ch.2", DifficultyTier.ChapterLabel(b, 22));
        }

        [Test]
        public void Table_NamesEveryTierAndFormatsItsLabels()
        {
            Dictionary<string, string> table = Strings.ParseTsv(File.ReadAllText(TablePath));
            for (int tier = 0; tier < DifficultyTier.NamedTiers; tier++)
                Assert.IsTrue(table.ContainsKey(DifficultyTier.NameKey(tier)), DifficultyTier.NameKey(tier));
            for (int tier = 1; tier < DifficultyTier.NamedTiers; tier++)
                Assert.IsTrue(table.ContainsKey("tier.boss_prefix." + tier), "tier.boss_prefix." + tier);
            StringAssert.Contains("{2}", table["tier.stage"]);
            StringAssert.Contains("{1}", table["tier.chapter"]);
        }

        [Test]
        public void FirstClearOfTierBoss_PaysGemsAndTickets_Once()
        {
            var b = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            var reward = new StageReward(data, b);

            reward.ApplyClear(90);
            Assert.AreEqual(b.CHAPTER_CLEAR_GEM, data.gem, 1e-9);
            Assert.AreEqual(0, data.gearTickets);

            reward.ApplyClear(100);
            Assert.AreEqual(2 * b.CHAPTER_CLEAR_GEM + b.TIER_CLEAR_GEM, data.gem, 1e-9);
            Assert.AreEqual(b.TIER_CLEAR_GEAR_TICKETS, data.gearTickets);
            Assert.AreEqual(b.TIER_CLEAR_SKILL_TICKETS, data.skillTickets);
            Assert.AreEqual(b.TIER_CLEAR_PET_TICKETS, data.petTickets);

            reward.ApplyClear(100);
            Assert.AreEqual(2 * b.CHAPTER_CLEAR_GEM + b.TIER_CLEAR_GEM, data.gem, 1e-9);
            Assert.AreEqual(b.TIER_CLEAR_GEAR_TICKETS, data.gearTickets);
        }
    }
}
