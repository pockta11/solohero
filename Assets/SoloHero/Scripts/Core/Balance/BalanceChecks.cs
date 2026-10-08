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
        // D-145 slower curve: day 1 just past the first boss (2-1 .. 2-7), day 3 in chapter 3, day 7 in chapter 4.
        public const int Day1Min = 11;
        public const int Day1Max = 17;
        public const int Day3Min = 21;
        public const int Day3Max = 29;
        public const int Day7Min = 31;
        public const int Day7Max = 40;
        // D-110: enemies walk in, so the hero walks 10-30% of a stage instead of 30-45%; the same fight fits in
        // 12-25 s (was 20-30 s while the hero had to reach every enemy).
        public const double StageSecondsMin = 12d;
        public const double StageSecondsMax = 25d;
        public const double StageSecondsShareMin = 0.7d;

        private static readonly string StageSecondsTarget =
            "median in [" + StageSecondsMin.ToString(CultureInfo.InvariantCulture) + ", " + StageSecondsMax.ToString(CultureInfo.InvariantCulture) + "] s";
        public const double PullParityMin = 0.5d;
        public const double PullParityMax = 2.0d;
        public const double OfflineShareMin = 0.20d;
        public const double OfflineShareMax = 0.40d;
        public const int WallStageMin = 17;
        public const int WallStageMax = 20;
        public const double BossExtraFarmMin = 0.25d;
        public const double BossExtraFarmMax = 0.60d;
        public const double NormalFailRateMin = 0.01d;
        public const double NormalFailRateMax = 0.10d;
        public const double DailySpendMin = 0.90d;

        /// <summary>D-124: a boss is a wall of HP against the clock; most fails should be time-outs.</summary>
        public const double BossTimeoutShareMin = 0.80d;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static List<SimCheck> Evaluate(SimReport r, BalanceValues b)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (b == null) throw new ArgumentNullException(nameof(b));

            var list = new List<SimCheck>();
            list.Add(FirstSession(r));
            list.Add(DayCheckpoint(r, "V-1a", 1, Day1Min, Day1Max, "1-10 boss beaten, early chapter 2"));
            list.Add(DayCheckpoint(r, "V-1b", 3, Day3Min, Day3Max, "chapter 3"));
            list.Add(DayCheckpoint(r, "V-1c", 7, Day7Min, Day7Max, "chapter 4"));
            list.Add(StageDuration(r));
            list.Add(GachaValueParity(r));
            list.Add(GachaShare(r));
            list.Add(OfflineShare(r, b));
            list.Add(GachaPace(r));
            list.Add(PetPace(r));
            list.Add(FirstWall(r));
            list.Add(LongWall(r, b));
            list.Add(BossExtraFarm(r, b));
            list.Add(BossFailCause(r));
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
                return Make("V-2", "Normal stage duration at the frontier", StageSecondsTarget, "no clears", false);

            int inside = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i] >= StageSecondsMin && samples[i] <= StageSecondsMax) inside++;
            }

            double median = Median(samples);
            double share = (double)inside / samples.Count;
            bool ok = median >= StageSecondsMin && median <= StageSecondsMax && share >= StageSecondsShareMin;
            return Make("V-2", "Normal stage duration at the frontier (first clear)",
                StageSecondsTarget + " and >= " + Pct(StageSecondsShareMin) + " of stages inside",
                "median " + F1(median) + " s, inside " + Pct(share), ok);
        }

        /// <summary>
        /// V-3 design intent: gacha competes with upgrades for gold. A value-optimal player is indifferent near parity,
        /// so the pass/fail check is value per gold (V-3a); the spend share (V-3b) is player taste and is reported only,
        /// to be confirmed with analytics (E6-15). D-063.
        /// </summary>
        /// <summary>
        /// D-123: reported only. On the summon ladder a low level gives Common only, so one pull is worth little now; the
        /// summon level carries the value, which a one-step comparison cannot see.
        /// </summary>
        private static SimCheck GachaValueParity(SimReport r)
        {
            return Info("V-3a", "Gacha value per gold vs best upgrade (median over spending decisions)", "reported (was 0.5x - 2.0x before D-123)",
                F2(r.PullValueParity) + "x");
        }

        private static SimCheck GachaShare(SimReport r)
        {
            double upgrade = 0d, gacha = 0d, skill = 0d, pet = 0d;
            for (int i = 0; i < r.Days.Count; i++)
            {
                upgrade += r.Days[i].SpentUpgrade;
                gacha += r.Days[i].SpentGacha;
                skill += r.Days[i].SpentSkill;
                pet += r.Days[i].SpentPet;
            }

            double total = upgrade + gacha + skill + pet;
            double share = total > 0d ? gacha / total : 0d;
            return Info("V-3b", "Gacha share of all gold spent (player taste; confirm with analytics)", "30% - 50% in live data",
                Pct(share) + " (upgrade " + Pct(Safe(upgrade, total)) + ", skill " + Pct(Safe(skill, total)) + ", pet " + Pct(Safe(pet, total)) + ")");
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

        /// <summary>D-114: how far the pet collection got by the end of the run.</summary>
        private static SimCheck PetPace(SimReport r)
        {
            int pulls = 0;
            for (int i = 0; i < r.Days.Count; i++) pulls += r.Days[i].PetPulls;
            return Info("V-5b", "Pet pace: pets owned, equipped pet at the end", "reported",
                r.PetsOwned + " owned after " + pulls + " pet pulls, equipped " + r.PetEquipped + " Lv " + r.PetLevel + " +" + r.PetEnhance);
        }

        private static SimCheck GachaPace(SimReport r)
        {
            string epic = r.FirstEpicPlaySeconds < 0d ? "none" : "day " + r.FirstEpicDay + " (" + F1(r.FirstEpicPlaySeconds / 60d) + " play min)";
            string legend = r.FirstLegendaryPlaySeconds < 0d ? "none" : "day " + r.FirstLegendaryDay + " (" + F1(r.FirstLegendaryPlaySeconds / 60d) + " play min)";
            return Info("V-5", "Equipment pace: first Epic+ / first Legendary", "reported; rate accuracy is GachaTests",
                "Epic " + epic + ", Legendary " + legend + ", promotions " + r.Promotions);
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

        private static SimCheck BossFailCause(SimReport r)
        {
            int fails = 0, deaths = 0;
            for (int i = 0; i < r.Days.Count; i++)
            {
                fails += r.Days[i].BossFails;
                deaths += r.Days[i].BossDeaths;
            }

            double timeouts = fails > 0 ? (double)(fails - deaths) / fails : 1d;
            return Make("V-8", "Boss fails come from the timer, not from the hero falling", ">= 80% time-outs",
                Pct(timeouts) + " time-outs (" + (fails - deaths) + " / " + fails + ")", timeouts >= BossTimeoutShareMin);
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

        /// <summary>
        /// GDD: gold earned is spent within 24 h. For every day d >= 2, everything spent up to the end of day d
        /// must cover at least 90% of everything earned up to the end of day d - 1 (a one-day lag, no day-edge noise).
        /// </summary>
        private static SimCheck DailySpend(SimReport r)
        {
            if (r.Days.Count < 2)
                return Info("E9-05", "Gold spent within 24 h", ">= 90% of the previous day's total", "needs 2 days");

            double worst = double.MaxValue;
            int worstDay = 0;
            double earnedBefore = r.Days[0].Earned;
            double spent = r.Days[0].Spent;
            for (int i = 1; i < r.Days.Count; i++)
            {
                spent += r.Days[i].Spent;
                if (earnedBefore > 0d)
                {
                    double share = spent / earnedBefore;
                    if (share < worst)
                    {
                        worst = share;
                        worstDay = r.Days[i].Day;
                    }
                }

                earnedBefore += r.Days[i].Earned;
            }

            return Make("E9-05", "Gold spent within 24 h (spent by day d vs earned by day d-1)", ">= 90% every day",
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

        private static string F2(double v) => v.ToString("0.00", Inv);

        private static string Pct(double v) => (v * 100d).ToString("0.0", Inv) + "%";

        private static SimCheck Make(string id, string title, string target, string measured, bool ok) =>
            new SimCheck { Id = id, Title = title, Target = target, Measured = measured, Status = ok ? SimCheckStatus.Pass : SimCheckStatus.Fail };

        private static SimCheck Info(string id, string title, string target, string measured) =>
            new SimCheck { Id = id, Title = title, Target = target, Measured = measured, Status = SimCheckStatus.Info };
    }
}
