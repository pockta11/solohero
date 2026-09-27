using System;
using System.Collections.Generic;
using System.Globalization;
using SoloHero.Core.Config;

namespace SoloHero.Core.Balance
{
    public enum SimCheckStatus
    {
        Pass,
        Fail,
        Info
    }

    public sealed class SimCheck
    {
        public string Id;
        public string Title;
        public string Target;
        public string Measured;
        public SimCheckStatus Status;
    }

    /// <summary>
    /// GDD "Number Balancing" target curve and V-1..V-7 as numeric checks over one <see cref="SimReport"/>.
    /// Ranges that the GDD states only in words are pinned here; change them here, not in callers.
    /// </summary>
    public static class BalanceChecks
    {
        public const int Day1Min = 10;
        public const int Day1Max = 19; // GDD "a day = half to one chapter": chapter 2 boss not yet beaten
        public const int Day3Min = 20;
        public const int Day3Max = 29;
        public const int Day7Min = 40;
        public const int Day7Max = 49;
        public const double StageSecondsMin = 20d;
        public const double StageSecondsMax = 30d;
        public const double StageSecondsShareMin = 0.7d;
        public const double GachaShareMin = 0.30d;
        public const double GachaShareMax = 0.50d;
        public const double OfflineShareMin = 0.20d;
        public const double OfflineShareMax = 0.40d;
        public const int WallStageMin = 17;
        public const int WallStageMax = 20;
        public const double BossExtraFarmMin = 0.25d;
        public const double BossExtraFarmMax = 0.60d;
        public const double NormalFailRateMin = 0.01d;
        public const double NormalFailRateMax = 0.10d;
        public const double DailySpendMin = 0.90d;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static List<SimCheck> Evaluate(SimReport r, BalanceValues b)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (b == null) throw new ArgumentNullException(nameof(b));

            var list = new List<SimCheck>();
            list.Add(FirstSession(r));
            list.Add(DayCheckpoint(r, "V-1a", 1, Day1Min, Day1Max, "1-10 boss beaten, chapter 2 entered"));
            list.Add(DayCheckpoint(r, "V-1b", 3, Day3Min, Day3Max, "chapter 3 entered"));
            list.Add(DayCheckpoint(r, "V-1c", 7, Day7Min, Day7Max, "chapter 5 entered"));
            list.Add(StageDuration(r));
            list.Add(GachaShare(r));
            list.Add(OfflineShare(r, b));
            list.Add(GachaPace(r));
            list.Add(FirstWall(r));
            list.Add(LongWall(r, b));
            list.Add(BossExtraFarm(r, b));
            list.Add(NormalFailRate(r));
            list.Add(DailySpend(r));
            return list;
        }

        public static int CountFailed(List<SimCheck> checks)
        {
            int n = 0;
            for (int i = 0; i < checks.Count; i++)
            {
                if (checks[i].Status == SimCheckStatus.Fail) n++;
            }

            return n;
        }

        private static SimCheck FirstSession(SimReport r)
        {
            bool ok = r.FirstFiveMinutesStage >= 5 && r.FirstFiveMinutesUpgrades >= 3 && r.FirstFiveMinutesPulls >= 1;
            return Make("FS-5", "First session, 5 minutes", "stage >= 1-5, upgrades >= 3, pulls >= 1",
                "stage " + r.FirstFiveMinutesStage + ", upgrades " + r.FirstFiveMinutesUpgrades + ", pulls " + r.FirstFiveMinutesPulls, ok);
        }

        private static SimCheck DayCheckpoint(SimReport r, string id, int day, int min, int max, string meaning)
        {
            if (r.Days.Count < day)
                return Info(id, "Day " + day + " checkpoint", "needs " + day + " simulated days", "not simulated");

            int highest = r.Days[day - 1].HighestStage;
            return Make(id, "Day " + day + ": " + meaning, "highest cleared g in [" + min + ", " + max + "]",
                "g " + highest + " (" + Label(highest) + ")", highest >= min && highest <= max);
        }

        private static SimCheck StageDuration(SimReport r)
        {
            var samples = new List<double>();
            for (int i = 0; i < r.Stages.Count; i++)
            {
                if (!r.Stages[i].IsBoss) samples.Add(r.Stages[i].ClearSeconds);
            }

            if (samples.Count == 0)
                return Make("V-2", "Normal stage duration at the frontier", "median 20-30 s", "no clears", false);

            int inside = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i] >= StageSecondsMin && samples[i] <= StageSecondsMax) inside++;
            }

            double median = Median(samples);
            double share = (double)inside / samples.Count;
            bool ok = median >= StageSecondsMin && median <= StageSecondsMax && share >= StageSecondsShareMin;
            return Make("V-2", "Normal stage duration at the frontier (first clear)",
                "median in [20, 30] s and >= 70% of stages inside",
                "median " + F1(median) + " s, inside " + Pct(share), ok);
        }

        private static SimCheck GachaShare(SimReport r)
        {
            double upgrade = 0d, gacha = 0d, skill = 0d;
            for (int i = 0; i < r.Days.Count; i++)
            {
                upgrade += r.Days[i].SpentUpgrade;
                gacha += r.Days[i].SpentGacha;
                skill += r.Days[i].SpentSkill;
            }

            double total = upgrade + gacha + skill;
            double share = total > 0d ? gacha / total : 0d;
            return Make("V-3", "Gacha share of all gold spent", "30% - 50%",
                Pct(share) + " (upgrade " + Pct(Safe(upgrade, total)) + ", skill " + Pct(Safe(skill, total)) + ")",
                share >= GachaShareMin && share <= GachaShareMax);
        }

        private static SimCheck OfflineShare(SimReport r, BalanceValues b)
        {
            double offline = 0d, earned = 0d;
            for (int i = 0; i < r.Days.Count; i++)
            {
                offline += r.Days[i].EarnedOffline;
                earned += r.Days[i].Earned;
            }

            var minutes = new List<double>();
            for (int i = 0; i < r.OfflineClaims.Count; i++)
            {
                SimOfflineClaim c = r.OfflineClaims[i];
                double baseGold = c.Doubled ? c.Gold / b.OFFLINE_AD_MULT : c.Gold;
                if (c.ElapsedSeconds >= b.OFFLINE_CAP && c.OnlineGoldPerMinute > 0d)
                    minutes.Add(baseGold / c.OnlineGoldPerMinute);
            }

            double share = earned > 0d ? offline / earned : 0d;
            string perClaim = minutes.Count > 0 ? "; a 6 h claim = " + F1(Median(minutes)) + " online min" : "";
            return Make("V-4", "Offline share of all gold earned (online stays the main income)", "20% - 40%",
                Pct(share) + perClaim, share >= OfflineShareMin && share <= OfflineShareMax);
        }

        private static SimCheck GachaPace(SimReport r)
        {
            string epic = r.FirstEpicPlaySeconds < 0d ? "none" : "day " + r.FirstEpicDay + " (" + F1(r.FirstEpicPlaySeconds / 60d) + " play min)";
            string legend = r.FirstLegendaryPlaySeconds < 0d ? "none" : "day " + r.FirstLegendaryDay + " (" + F1(r.FirstLegendaryPlaySeconds / 60d) + " play min)";
            return Info("V-5", "Equipment pace: first Epic+ / first Legendary", "reported; rate accuracy is GachaTests", "Epic " + epic + ", Legendary " + legend);
        }

        private static SimCheck FirstWall(SimReport r)
        {
            SimStageRow boss = r.FindStage(10);
            if (boss == null)
                return Make("V-6a", "First wall at the 1-10 boss", "1-2 fails before the first clear", "1-10 not cleared", false);

            return Make("V-6a", "First wall at the 1-10 boss", "1-2 fails before the first clear",
                boss.Fails + " fails, " + F1(boss.StuckPlaySeconds / 60d) + " play min from first try", boss.Fails >= 1 && boss.Fails <= 2);
        }

        private static SimCheck LongWall(SimReport r, BalanceValues b)
        {
            int limit = b.STAGES_PER_CHAPTER * 2;
            int worstG = 0;
            double worst = -1d;
            for (int i = 0; i < r.Stages.Count; i++)
            {
                SimStageRow row = r.Stages[i];
                if (row.G > limit) continue;
                if (row.StuckPlaySeconds > worst)
                {
                    worst = row.StuckPlaySeconds;
                    worstG = row.G;
                }
            }

            if (worstG == 0)
                return Make("V-6b", "Longest stall in chapters 1-2", "at 2-7 .. 2-10", "chapters 1-2 not cleared", false);

            return Make("V-6b", "Longest stall in chapters 1-2", "at 2-7 .. 2-10",
                Label(worstG) + ", " + F1(worst / 60d) + " play min", worstG >= WallStageMin && worstG <= WallStageMax);
        }

        private static SimCheck BossExtraFarm(SimReport r, BalanceValues b)
        {
            var ratios = new List<double>();
            double previousBossClear = 0d;
            for (int i = 0; i < r.Stages.Count; i++)
            {
                SimStageRow row = r.Stages[i];
                if (!row.IsBoss) continue;
                double firstTry = row.PlaySeconds - row.StuckPlaySeconds;
                double approach = firstTry - previousBossClear;
                if (approach > 0d) ratios.Add(row.StuckPlaySeconds / approach);
                previousBossClear = row.PlaySeconds;
            }

            if (ratios.Count == 0)
                return Make("E9-04", "Extra farming to break a boss", "about 40% (25% - 60%)", "no boss cleared", false);

            double median = Median(ratios);
            return Make("E9-04", "Extra farming to break a boss (stall time / time to reach the boss)", "median 25% - 60%",
                "median " + Pct(median) + " over " + ratios.Count + " bosses", median >= BossExtraFarmMin && median <= BossExtraFarmMax);
        }

        private static SimCheck NormalFailRate(SimReport r)
        {
            int attempts = 0, fails = 0;
            for (int i = 0; i < r.Days.Count; i++)
            {
                attempts += r.Days[i].NormalAttempts;
                fails += r.Days[i].NormalFails;
            }

            double rate = attempts > 0 ? (double)fails / attempts : 0d;
            return Make("V-7", "Normal stage fail rate (survival is a real constraint)", "1% - 10% of attempts",
                Pct(rate) + " (" + fails + " / " + attempts + ")", rate >= NormalFailRateMin && rate <= NormalFailRateMax);
        }

        private static SimCheck DailySpend(SimReport r)
        {
            double worst = double.MaxValue;
            int worstDay = 0;
            for (int i = 0; i < r.Days.Count; i++)
            {
                SimDayRow d = r.Days[i];
                if (d.Earned <= 0d) continue;
                double share = d.Spent / d.Earned;
                if (share < worst)
                {
                    worst = share;
                    worstDay = d.Day;
                }
            }

            if (worstDay == 0)
                return Make("E9-05", "Gold spent within 24 h", ">= 90% every day", "no earnings", false);

            return Make("E9-05", "Gold spent within 24 h", ">= 90% every day",
                "lowest " + Pct(worst) + " on day " + worstDay, worst >= DailySpendMin);
        }

        public static string Label(int g) => ((g - 1) / 10 + 1) + "-" + ((g - 1) % 10 + 1);

        private static double Median(List<double> values)
        {
            var copy = new List<double>(values);
            copy.Sort();
            int n = copy.Count;
            return n % 2 == 1 ? copy[n / 2] : (copy[n / 2 - 1] + copy[n / 2]) / 2d;
        }

        private static double Safe(double part, double total) => total > 0d ? part / total : 0d;

        private static string F1(double v) => v.ToString("0.0", Inv);

        private static string Pct(double v) => (v * 100d).ToString("0.0", Inv) + "%";

        private static SimCheck Make(string id, string title, string target, string measured, bool ok) =>
            new SimCheck { Id = id, Title = title, Target = target, Measured = measured, Status = ok ? SimCheckStatus.Pass : SimCheckStatus.Fail };

        private static SimCheck Info(string id, string title, string target, string measured) =>
            new SimCheck { Id = id, Title = title, Target = target, Measured = measured, Status = SimCheckStatus.Info };
    }
}
