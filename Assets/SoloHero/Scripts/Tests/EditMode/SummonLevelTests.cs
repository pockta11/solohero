using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-115 summon level: levels from cumulative pulls, boosted tables that still sum to 100, services use them.</summary>
    public sealed class SummonLevelTests
    {
        private sealed class ScriptedRandom : IRandom
        {
            private readonly Queue<double> _doubles;

            public ScriptedRandom(params double[] doubles)
            {
                _doubles = new Queue<double>(doubles);
            }

            public double NextDouble() => _doubles.Dequeue();

            public int Next(int maxExclusive) => 0;
        }

        [Test]
        public void Level_RisesWithCumulativePulls_UpToTheCap()
        {
            var b = new BalanceValues();
            Assert.AreEqual(1, SummonLevel.LevelOf(b, SummonKind.Gear, 0));
            int step = b.SUMMON_LV_STEP_GEAR;
            Assert.AreEqual(1, SummonLevel.LevelOf(b, SummonKind.Gear, step - 1));
            Assert.AreEqual(2, SummonLevel.LevelOf(b, SummonKind.Gear, step));
            Assert.AreEqual(3, SummonLevel.LevelOf(b, SummonKind.Gear, step * 3));
            Assert.AreEqual(b.SUMMON_LV_MAX, SummonLevel.LevelOf(b, SummonKind.Gear, 1000000));
            Assert.AreEqual(2, SummonLevel.LevelOf(b, SummonKind.Pet, b.SUMMON_LV_STEP_PET));

            var save = SaveDataV2.CreateNew();
            save.skillPullCount = SummonLevel.PullsFor(b, SummonKind.Skill, 4);
            Assert.AreEqual(4, SummonLevel.Of(b, save, SummonKind.Skill));
        }

        [Test]
        public void Tables_EveryLevelSumsTo100_GoodGradesRiseAndCommonFalls()
        {
            var b = new BalanceValues();
            GearTableValues gear = GearTableValues.FromBalance(b);
            GachaTableValues skill = GachaTableValues.FromBalance(b);
            for (int level = 2; level <= b.SUMMON_LV_MAX; level++)
            {
                GearTableValues g = gear.AtLevel(level);
                GearTableValues prev = gear.AtLevel(level - 1);
                Assert.AreEqual(100d, g.RatesSum, 1e-9, "gear level " + level);
                Assert.Less(g.Rates[0], prev.Rates[0]);
                for (int i = 1; i < g.Rates.Length; i++)
                {
                    // D-121 / D-123: a grade not open yet stays at 0, it appears at its level, then the boosted ones
                    // (Rare and up) rise every level while Uncommon keeps its rate.
                    int open = gear.OpenLevel((GearGrade)i);
                    if (open > level) Assert.AreEqual(0d, g.Rates[i], 1e-12);
                    else if (open == level) Assert.Greater(g.Rates[i], 0d);
                    else if (i >= SummonLevel.FirstBoostedGrade) Assert.Greater(g.Rates[i], prev.Rates[i]);
                    else Assert.AreEqual(prev.Rates[i], g.Rates[i], 1e-12, "uncommon is not boosted");
                }

                GachaTableValues s = skill.AtLevel(level);
                GachaTableValues sPrev = skill.AtLevel(level - 1);
                Assert.AreEqual(100d, s.RatesSum, 1e-9, "skill level " + level);
                Assert.AreEqual(level >= skill.OpenLevel(Grade.Rare) ? skill.RateR : 0d, s.RateR, 1e-12, "rare skills are not boosted");
                if (level > skill.OpenLevel(Grade.Epic)) Assert.Greater(s.RateE, sPrev.RateE);
            }

            Assert.AreEqual(100d, gear.AtLevel(1).RatesSum, 1e-9);
            Assert.AreSame(gear.AtLevel(5), gear.AtLevel(5), "cached");
            Assert.AreSame(gear.AtLevel(b.SUMMON_LV_MAX), gear.AtLevel(b.SUMMON_LV_MAX + 5), "capped");
        }

        [Test]
        public void GearPull_UsesTheLevelBeforeThePull()
        {
            var b = new BalanceValues();
            // 94 % is a Common at level 1 (D-123: only Common is open) but in the Epic band at the top level, where every
            // grade is open, Common has shrunk and every boundary moved down (Epic from about 93.8 %).
            const double sample = 0.94;
            var low = SaveDataV2.CreateNew();
            low.gold = b.GACHA_COST_SINGLE;
            var service = new GachaService(b, GearTableValues.FromBalance(b), new ScriptedRandom(sample), GachaCatalog.Standard(b));
            Assert.AreEqual(GearGrade.Common, service.TryPull(low).Items[0].Grade);

            var high = SaveDataV2.CreateNew();
            high.gold = b.GACHA_COST_SINGLE;
            high.totalPullCount = SummonLevel.PullsFor(b, SummonKind.Gear, b.SUMMON_LV_MAX);
            service = new GachaService(b, GearTableValues.FromBalance(b), new ScriptedRandom(sample), GachaCatalog.Standard(b));
            Assert.AreEqual(GearGrade.Epic, service.TryPull(high).Items[0].Grade);
        }

        [Test]
        public void Ladder_NewSummonIsCommonOnly_EachLevelOpensTheNextGrade()
        {
            // D-123 (MapleStory Idle): level 1 gives Common only, level 2 adds Uncommon, level 3 Rare, and so on.
            var b = new BalanceValues();
            GearTableValues gear = GearTableValues.FromBalance(b);
            Assert.AreEqual(100d, gear.AtLevel(1).Rate(GearGrade.Common), 1e-9);
            for (int g = 1; g < GearGrades.Count; g++)
            {
                int open = gear.OpenLevel((GearGrade)g);
                Assert.AreEqual(g + 1, open, (GearGrade)g + " opens one level after the grade below");
                Assert.AreEqual(0d, gear.AtLevel(open - 1).Rates[g], 1e-12);
                Assert.Greater(gear.AtLevel(open).Rates[g], 0d);
                Assert.AreEqual(g, SummonLevel.GradeOpeningAt(gear.OpenLevels, open));
            }

            GachaTableValues skill = GachaTableValues.FromBalance(b);
            Assert.AreEqual(100d, skill.AtLevel(1).RateC, 1e-9);
            for (int g = 1; g < 4; g++)
            {
                Assert.AreEqual(g + 1, skill.OpenLevel((Grade)g));
                Assert.AreEqual(0d, skill.AtLevel(g).Rates[g], 1e-12);
                Assert.Greater(skill.AtLevel(g + 1).Rates[g], 0d);
            }
        }

        [Test]
        public void Ladder_EachLevelTakesHundredsOfGearPulls()
        {
            // User: "the summon level needs hundreds of pulls"; the gear steps grow 100, 200, 300, ...
            var b = new BalanceValues();
            for (int level = 2; level <= b.SUMMON_LV_MAX; level++)
            {
                int step = SummonLevel.PullsFor(b, SummonKind.Gear, level) - SummonLevel.PullsFor(b, SummonKind.Gear, level - 1);
                Assert.GreaterOrEqual(step, 100, "level " + level);
            }
        }

        [Test]
        public void Pity_CountsOnlyOnceItsGradeIsOpen()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gold = b.GACHA_COST_TEN * 2d;
            var service = new GachaService(b, GearTableValues.FromBalance(b), new SystemRandom(new System.Random(4)), GachaCatalog.Standard(b));

            service.TryPullTen(save);
            Assert.AreEqual(0, save.pityCount, "no pity on a summon that cannot give a Legendary yet");

            save.totalPullCount = SummonLevel.PullsFor(b, SummonKind.Gear, GearTableValues.FromBalance(b).OpenLevel(GearTableValues.PityGrade));
            service.TryPullTen(save);
            Assert.Greater(save.pityCount + CountLegendaryOrBetter(save), 0, "counting from the level that opens Legendary");
        }

        private static int CountLegendaryOrBetter(SaveDataV2 save)
        {
            int n = 0;
            foreach (string id in save.ownedEquipment)
            {
                if (id.EndsWith("_Legendary") || id.EndsWith("_Mythic") || id.EndsWith("_Ancient")) n++;
            }

            return n;
        }

        [Test]
        public void GearPull_NewSummon_TopSampleIsCommon()
        {
            // D-123: no Ancient (or anything above Common) by luck on a new summon.
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gold = b.GACHA_COST_SINGLE;
            var service = new GachaService(b, GearTableValues.FromBalance(b), new ScriptedRandom(0.99999), GachaCatalog.Standard(b));
            Assert.AreEqual(GearGrade.Common, service.TryPull(save).Items[0].Grade);
        }

        [Test]
        public void Verifier_AtAHighLevel_MatchesThatLevelsTable()
        {
            var b = new BalanceValues();
            GachaVerifier.Report report = GachaVerifier.Run(b, 60000, seed: 99, level: 6);
            Assert.That(report.WorstGapP, Is.LessThanOrEqualTo(GachaVerifier.ToleranceP), report.ToMarkdown(b.GEAR_PITY));
            Assert.AreEqual(GearTableValues.FromBalance(b).AtLevel(6).Rate(GearGrade.Rare), report.Disclosed[(int)GearGrade.Rare], 1e-12);
        }

        [Test]
        public void GearRefunds_AtTheTopLevel_StayUnderHalfTheTenPullPrice()
        {
            // A maxed collection at the top summon level refunds every duplicate; pulling must still cost gold.
            var b = new BalanceValues();
            GearTableValues table = GearTableValues.FromBalance(b).AtLevel(b.SUMMON_LV_MAX);
            double expected = 0d;
            for (int g = 0; g < GearGrades.Count; g++)
                expected += table.Rates[g] / 100d * GachaCatalog.RefundOf(b, (GearGrade)g);
            Assert.Less(expected, b.GACHA_COST_TEN / 10d * 0.5d);
        }
    }
}
