using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Balance;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-151 (L-04): the 365-day checks read stalls and checkpoints off a report; no long run needed here.</summary>
    public sealed class LongTermChecksTests
    {
        private static SimReport Report(string persona, params int[] highestByDay)
        {
            var r = new SimReport { Name = persona, Seed = 1, Job = "knight" };
            for (int i = 0; i < highestByDay.Length; i++)
                r.Days.Add(new SimDayRow { Day = i + 1, HighestStage = highestByDay[i] });
            return r;
        }

        /// <summary>A year whose highest stage follows <paramref name="stageOn"/> (day -> g), tower 10 floors behind.</summary>
        private static SimReport Year(string persona, System.Func<int, int> stageOn)
        {
            var r = new SimReport { Name = persona, Seed = 1, Job = "knight" };
            for (int d = 1; d <= 365; d++)
            {
                int g = stageOn(d);
                r.Days.Add(new SimDayRow { Day = d, HighestStage = g, TowerFloor = g - 10 });
            }

            return r;
        }

        private static SimCheck Find(List<SimCheck> checks, string id)
        {
            foreach (SimCheck c in checks)
                if (c.Id == id) return c;
            Assert.Fail("missing " + id);
            return null;
        }

        [Test]
        public void LongestStall_CountsDaysWithoutANewStage_OpenEndIncluded()
        {
            Assert.AreEqual(3, LongTermChecks.LongestStallDays(Report("no-ads", 1, 2, 2, 2, 5, 5, 5, 5, 6)));
            Assert.AreEqual(4, LongTermChecks.LongestStallDays(Report("no-ads", 1, 2, 2, 2, 2, 2)));
        }

        [Test]
        public void NewStageDaysTrailing_SlidesAThirtyDayWindow()
        {
            var days = new int[40];
            for (int i = 0; i < days.Length; i++) days[i] = i < 10 ? i + 1 : 10;
            int[] trailing = LongTermChecks.NewStageDaysTrailing(Report("no-ads", days));
            Assert.AreEqual(10, trailing[9]);
            Assert.AreEqual(10, trailing[29]);
            Assert.AreEqual(9, trailing[30]);
            Assert.AreEqual(0, trailing[39]);
        }

        [Test]
        public void Evaluate_NoAdsOnTarget_PassesCheckpointsAndStalls()
        {
            // Chapter 7 on day 30, 13 on day 90, 18 on day 180, 25 on day 365, a new stage every two or three days late.
            SimReport r = Year(LongTermChecks.NoAds, d => d <= 30 ? 2 * d + 5 : d <= 90 ? 35 + d : d <= 180 ? 125 + (d - 90) * 50 / 90 : 175 + (d - 180) * 75 / 185);
            List<SimCheck> checks = LongTermChecks.Evaluate(r);

            foreach (string id in new[] { "V-L1a", "V-L1b", "V-L1c", "V-L1d", "V-L2a", "V-L4" })
                Assert.AreEqual(SimCheckStatus.Pass, Find(checks, id).Status, id + ": " + Find(checks, id).Measured);
            Assert.AreEqual(SimCheckStatus.Info, Find(checks, "V-L2b").Status);
            Assert.AreEqual(SimCheckStatus.Info, Find(checks, "V-L2c").Status);
            Assert.AreEqual(SimCheckStatus.Info, Find(checks, "V-L3").Status);
        }

        [Test]
        public void Evaluate_NoAdsWall_FailsTheYearAndTheStall()
        {
            SimReport r = Year(LongTermChecks.NoAds, d => d <= 30 ? 2 * d + 5 : System.Math.Min(109, 65 + d - 30));
            List<SimCheck> checks = LongTermChecks.Evaluate(r);

            Assert.AreEqual(SimCheckStatus.Fail, Find(checks, "V-L1d").Status);
            Assert.AreEqual(SimCheckStatus.Fail, Find(checks, "V-L2a").Status);
        }

        [Test]
        public void Evaluate_Heavy_ReportsTheStallWithoutGrading()
        {
            SimReport r = Year(LongTermChecks.Heavy, d => System.Math.Min(330, 40 + d));
            List<SimCheck> checks = LongTermChecks.Evaluate(r);

            Assert.AreEqual(SimCheckStatus.Info, Find(checks, "V-L2a").Status);
            StringAssert.Contains("75 days", Find(checks, "V-L2b").Measured);
        }

        [Test]
        public void LongestStall_CountsOnlyTheDaysInTheWindow()
        {
            // New stages on days 1-10, then nothing until day 40.
            var days = new int[60];
            for (int i = 0; i < days.Length; i++) days[i] = i < 10 ? i + 1 : i < 39 ? 10 : 11;
            SimReport r = Report("no-ads", days);

            Assert.AreEqual(29, LongTermChecks.LongestStallDays(r));
            Assert.AreEqual(10, LongTermChecks.LongestStallDays(r, 30, 45));
            Assert.AreEqual(20, LongTermChecks.LongestStallDays(r, 1, 30));
        }

        [Test]
        public void AxisEnds_MarksAxesEndingBeforeTheTargetDay()
        {
            SimReport r = Report(LongTermChecks.NoAds, 1, 2, 3);
            r.AxisCapDays.Add(SimAxes.GearSummon, 40);
            r.AxisCapDays.Add(SimAxes.Talents, 120);
            SimCheck check = Find(LongTermChecks.Evaluate(r), "V-L3");

            StringAssert.Contains("1 before day 90", check.Measured);
            StringAssert.Contains(SimAxes.GearSummon + " d40!", check.Measured);
            StringAssert.Contains(SimAxes.Talents + " d120", check.Measured);
        }

        [Test]
        public void SimAxes_NewSave_NothingCapped_FullGearLadder_Capped()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            var capped = new List<string>();
            SimAxes.AddCapped(b, save, capped);
            Assert.IsEmpty(capped);

            save.totalPullCount = SummonLevel.PullsFor(b, SummonKind.Gear, b.SUMMON_LV_MAX);
            SimAxes.AddCapped(b, save, capped);
            CollectionAssert.AreEqual(new[] { SimAxes.GearSummon }, capped);
        }

        [Test]
        public void Heavy_PlaysThreeHoursWithAds()
        {
            SimSettings s = SimSettings.Heavy(2);
            double minutes = 0d;
            foreach (SimSession session in s.DailySessions) minutes += session.Minutes;

            Assert.AreEqual(180d, minutes, 1e-9);
            Assert.IsTrue(s.UseAds);
            Assert.AreEqual(LongTermChecks.Heavy, s.Name);
            Assert.AreEqual(2, s.Seed);
        }

        [Test]
        public void LongTermSummary_ListsEveryPersonaAndCheck()
        {
            var runs = new List<SimReport>
            {
                Year(LongTermChecks.NoAds, d => 30 + d / 2),
                Year(LongTermChecks.Heavy, d => 60 + d)
            };
            var checks = new List<List<SimCheck>> { LongTermChecks.Evaluate(runs[0]), LongTermChecks.Evaluate(runs[1]) };
            string md = SimCsv.LongTerm(runs, checks);

            StringAssert.Contains("## no-ads (1 runs)", md);
            StringAssert.Contains("## heavy (1 runs)", md);
            StringAssert.Contains("V-L4", md);
            StringAssert.Contains("| 365 | ", md);
        }
    }
}
