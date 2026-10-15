using System;
using System.Collections.Generic;
using System.Text;

namespace SoloHero.Core.Balance
{
    /// <summary>
    /// D-151 (L-04) checks over a 365-day run: the target curve per persona (content roadmap section 3), stalls, the
    /// growth axes that end, and the tower's distance behind the stages. The persona is the run's
    /// <see cref="SimReport.Name"/> ("no-ads", "ads", "heavy").
    /// </summary>
    public static class LongTermChecks
    {
        public const string NoAds = "no-ads";
        public const string Ads = "ads";
        public const string Heavy = "heavy";

        /// <summary>Checkpoint days of the target curve: one month, three months, six months, a year.</summary>
        public static readonly int[] CheckpointDays = { 30, 90, 180, 365 };

        // Chapter of the highest cleared stage at each checkpoint day (inclusive range), per persona. No-ads is the design
        // target (roadmap section 3); ads and heavy are bands around the 12-run medians of the D-151 curve, to catch
        // regressions. D-167: the heavy band moved up with the P1 axes (a three-hour player reaches the third job in a week).
        private static readonly int[] NoAdsMin = { 7, 12, 16, 23 };
        private static readonly int[] NoAdsMax = { 8, 15, 20, 29 };
        private static readonly int[] AdsMin = { 10, 16, 23, 26 };
        private static readonly int[] AdsMax = { 12, 19, 26, 30 };
        private static readonly int[] HeavyMin = { 18, 23, 26, 28 };
        private static readonly int[] HeavyMax = { 20, 25, 28, 31 };

        /// <summary>
        /// G-1: a 30-minute player never waits longer than this many days for a new stage while the launch content lasts
        /// (up to <see cref="StallGradedUntil"/>). Later the capped axes are done and only the upgrade lanes grow, so the
        /// chapter bosses turn into month-long walls until the post-launch axes (roadmap L-06, L-09, L-10) arrive; that half
        /// is reported.
        /// </summary>
        public const int LongestStallMax = 14;
        public const int StallGradedUntil = 180;

        /// <summary>
        /// Reported only: new-stage days in the last 30 from <see cref="StallWindowFrom"/> on. A chapter boss is a one- to
        /// two-week gate after which the nine normal stages fall in a day or two, so even the runs with no long stall show
        /// 2-8 here; the roadmap's "8 of 30" is a goal for the recurring content, not the stages.
        /// </summary>
        public const int NewStageDaysGoal = 8;
        public const int StallWindowFrom = 90;

        /// <summary>G-3: a capped axis should not end before this day (30-minute players) or this day (heavy).</summary>
        public const int AxisEndMinLight = 90;
        public const int AxisEndMinHeavy = 21;

        /// <summary>The tower floor stays this many floors behind the highest stage (from day 90 on).</summary>
        public const int TowerGapMin = 5;
        public const int TowerGapMax = 25;

        public static List<SimCheck> Evaluate(SimReport r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            var list = new List<SimCheck>();
            int[] min, max;
            TargetOf(r.Name, out min, out max);
            string[] ids = { "V-L1a", "V-L1b", "V-L1c", "V-L1d" };
            for (int i = 0; i < CheckpointDays.Length; i++)
                list.Add(Checkpoint(r, ids[i], CheckpointDays[i], min, max, i));
            bool light = r.Name != Heavy;
            bool graded = r.Name == NoAds;
            list.Add(LongestStall(r, graded));
            list.Add(LateStall(r));
            list.Add(NewStageDays(r));
            list.Add(AxisEnds(r, light ? AxisEndMinLight : AxisEndMinHeavy));
            list.Add(TowerGap(r));
            return list;
        }

        /// <summary>The persona's target chapters at <see cref="CheckpointDays"/>; null arrays for an unknown persona.</summary>
        public static void TargetOf(string persona, out int[] minChapter, out int[] maxChapter)
        {
            switch (persona)
            {
                case NoAds: minChapter = NoAdsMin; maxChapter = NoAdsMax; return;
                case Ads: minChapter = AdsMin; maxChapter = AdsMax; return;
                case Heavy: minChapter = HeavyMin; maxChapter = HeavyMax; return;
                default: minChapter = null; maxChapter = null; return;
            }
        }

        public static int ChapterOf(int g) => g < 1 ? 0 : (g - 1) / 10 + 1;

        /// <summary>Highest cleared stage at the end of <paramref name="day"/>, or -1 when the run is shorter.</summary>
        public static int HighestOn(SimReport r, int day) => day >= 1 && day <= r.Days.Count ? r.Days[day - 1].HighestStage : -1;

        /// <summary>
        /// The longest run of days without a new highest stage (an open one at the end counts too), counting only the days
        /// from <paramref name="fromDay"/> to <paramref name="toDay"/>.
        /// </summary>
        public static int LongestStallDays(SimReport r, int fromDay = 1, int toDay = int.MaxValue)
        {
            int previous = 0, run = 0, longest = 0;
            for (int i = 0; i < r.Days.Count; i++)
            {
                SimDayRow d = r.Days[i];
                bool isNew = d.HighestStage > previous;
                if (isNew) previous = d.HighestStage;
                if (d.Day < fromDay || d.Day > toDay) continue;
                run = isNew ? 0 : run + 1;
                longest = Math.Max(longest, run);
            }

            return longest;
        }

        /// <summary>For every day, how many of the last 30 days (that day included) cleared a new highest stage.</summary>
        public static int[] NewStageDaysTrailing(SimReport r, int window = 30)
        {
            var result = new int[r.Days.Count];
            var isNew = new bool[r.Days.Count];
            int previous = 0;
            for (int i = 0; i < r.Days.Count; i++)
            {
                isNew[i] = r.Days[i].HighestStage > previous;
                if (isNew[i]) previous = r.Days[i].HighestStage;
            }

            int count = 0;
            for (int i = 0; i < r.Days.Count; i++)
            {
                if (isNew[i]) count++;
                if (i >= window && isNew[i - window]) count--;
                result[i] = count;
            }

            return result;
        }

        private static SimCheck Checkpoint(SimReport r, string id, int day, int[] min, int[] max, int index)
        {
            string title = "Day " + day + ": chapter of the highest stage";
            int g = HighestOn(r, day);
            if (g < 0) return Info(id, title, "needs " + day + " simulated days", "not simulated");
            string measured = "g " + g + " (" + BalanceChecks.Label(g) + ")";
            if (min == null) return Info(id, title, "no target for " + r.Name, measured);
            int chapter = ChapterOf(g);
            return Make(id, title, "chapter " + min[index] + " - " + max[index], measured, chapter >= min[index] && chapter <= max[index]);
        }

        private static SimCheck LongestStall(SimReport r, bool graded)
        {
            string title = "Longest wait for a new stage, days 1-" + StallGradedUntil;
            int longest = LongestStallDays(r, 1, StallGradedUntil);
            string target = "<= " + LongestStallMax + " days" + (graded ? "" : " (graded for no-ads)");
            string measured = longest + " days";
            return graded ? Make("V-L2a", title, target, measured, longest <= LongestStallMax) : Info("V-L2a", title, target, measured);
        }

        private static SimCheck LateStall(SimReport r)
        {
            string title = "Longest wait for a new stage after day " + StallGradedUntil;
            if (r.Days.Count <= StallGradedUntil) return Info("V-L2b", title, "reported", "needs " + (StallGradedUntil + 1) + " simulated days");
            return Info("V-L2b", title, "reported (the post-launch axes shorten it)", LongestStallDays(r, StallGradedUntil + 1) + " days");
        }

        private static SimCheck NewStageDays(SimReport r)
        {
            string title = "New-stage days in the last 30, lowest from day " + StallWindowFrom;
            string target = "reported (goal " + NewStageDaysGoal + " with the recurring content)";
            int[] trailing = NewStageDaysTrailing(r);
            if (trailing.Length < StallWindowFrom) return Info("V-L2c", title, target, "needs " + StallWindowFrom + " simulated days");
            int lowest = int.MaxValue, lowestDay = 0;
            for (int i = StallWindowFrom - 1; i < trailing.Length; i++)
            {
                if (trailing[i] >= lowest) continue;
                lowest = trailing[i];
                lowestDay = i + 1;
            }

            string measured = lowest + " (day " + lowestDay + "), " + trailing[trailing.Length - 1] + " on the last day";
            return Info("V-L2c", title, target, measured);
        }

        /// <summary>Reported only: the day each capped axis ended, the early ones marked.</summary>
        private static SimCheck AxisEnds(SimReport r, int minDay)
        {
            var entries = new List<KeyValuePair<string, int>>(r.AxisCapDays);
            entries.Sort((a, b) => a.Value != b.Value ? a.Value.CompareTo(b.Value) : string.CompareOrdinal(a.Key, b.Key));
            var sb = new StringBuilder();
            int early = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(entries[i].Key).Append(" d").Append(entries[i].Value);
                if (entries[i].Value >= minDay) continue;
                sb.Append('!');
                early++;
            }

            string measured = early + " before day " + minDay + (entries.Count > 0 ? ": " + sb : "");
            return Info("V-L3", "Capped growth axes and the day each ended (! = before the G-3 day)", "none before day " + minDay, measured);
        }

        private static SimCheck TowerGap(SimReport r)
        {
            string title = "Tower floor behind the highest stage (days 90, 180, 365)";
            string target = TowerGapMin + " - " + TowerGapMax + " floors";
            var sb = new StringBuilder();
            bool ok = true;
            int counted = 0;
            for (int i = 1; i < CheckpointDays.Length; i++)
            {
                int day = CheckpointDays[i];
                if (day > r.Days.Count) continue;
                SimDayRow d = r.Days[day - 1];
                int gap = d.HighestStage - d.TowerFloor;
                if (counted++ > 0) sb.Append(", ");
                sb.Append("d").Append(day).Append(' ').Append(gap);
                if (gap < TowerGapMin || gap > TowerGapMax) ok = false;
            }

            if (counted == 0) return Info("V-L4", title, target, "needs " + CheckpointDays[1] + " simulated days");
            return Make("V-L4", title, target, sb.ToString(), ok);
        }

        /// <summary>Median of the highest cleared stage on <paramref name="day"/> over runs of one persona.</summary>
        public static double MedianHighest(IList<SimReport> runs, string persona, int day)
        {
            var values = new List<double>();
            for (int i = 0; i < runs.Count; i++)
            {
                if (runs[i].Name != persona) continue;
                int g = HighestOn(runs[i], day);
                if (g >= 0) values.Add(g);
            }

            if (values.Count == 0) return -1d;
            values.Sort();
            int n = values.Count;
            return n % 2 == 1 ? values[n / 2] : (values[n / 2 - 1] + values[n / 2]) / 2d;
        }

        private static SimCheck Make(string id, string title, string target, string measured, bool ok) =>
            new SimCheck { Id = id, Title = title, Target = target, Measured = measured, Status = ok ? SimCheckStatus.Pass : SimCheckStatus.Fail };

        private static SimCheck Info(string id, string title, string target, string measured) =>
            new SimCheck { Id = id, Title = title, Target = target, Measured = measured, Status = SimCheckStatus.Info };
    }
}
