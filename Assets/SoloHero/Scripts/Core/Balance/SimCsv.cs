using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SoloHero.Core.Balance
{
    /// <summary>Text output of the simulator: per-stage and per-day CSV plus a markdown summary of the checks.</summary>
    public static class SimCsv
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Stages(SimReport r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("g,label,boss,day,play_min,wall_hours,attempts,fails,clear_s,stall_min,hero_level,upg_hp,upg_atk,upg_def,upg_spd,skills_owned,skill_level_sum,skill_pulls,sword,helm,armor,boots,pulls,gold,earned_total,spent_total");
            for (int i = 0; i < r.Stages.Count; i++)
            {
                SimStageRow s = r.Stages[i];
                sb.Append(s.G).Append(',')
                    .Append(BalanceChecks.Label(s.G)).Append(',')
                    .Append(s.IsBoss ? 1 : 0).Append(',')
                    .Append(s.Day).Append(',')
                    .Append(N(s.PlaySeconds / 60d)).Append(',')
                    .Append(N(s.WallHours)).Append(',')
                    .Append(s.Attempts).Append(',')
                    .Append(s.Fails).Append(',')
                    .Append(N(s.ClearSeconds)).Append(',')
                    .Append(N(s.StuckPlaySeconds / 60d)).Append(',')
                    .Append(s.HeroLevel).Append(',')
                    .Append(s.UpgradeHp).Append(',')
                    .Append(s.UpgradeAtk).Append(',')
                    .Append(s.UpgradeDef).Append(',')
                    .Append(s.UpgradeSpd).Append(',')
                    .Append(s.SkillsOwned).Append(',')
                    .Append(s.SkillLevelSum).Append(',')
                    .Append(s.SkillPulls).Append(',')
                    .Append(Grade(s.SwordGrade)).Append(',')
                    .Append(Grade(s.HelmGrade)).Append(',')
                    .Append(Grade(s.ArmorGrade)).Append(',')
                    .Append(Grade(s.BootsGrade)).Append(',')
                    .Append(s.Pulls).Append(',')
                    .Append(N(s.Gold)).Append(',')
                    .Append(N(s.EarnedTotal)).Append(',')
                    .Append(N(s.SpentTotal)).AppendLine();
            }

            return sb.ToString();
        }

        public static string Days(SimReport r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("day,highest,label,hero_level,play_min,earned,earned_stage,earned_offline,earned_refund,earned_booster,spent,spent_upgrade,spent_gacha,spent_skill,spent_pet,gold_pulls,gem_pulls,normal_attempts,normal_fails,boss_attempts,boss_fails,gold_end,gem_end");
            for (int i = 0; i < r.Days.Count; i++)
            {
                SimDayRow d = r.Days[i];
                sb.Append(d.Day).Append(',')
                    .Append(d.HighestStage).Append(',')
                    .Append(BalanceChecks.Label(d.HighestStage)).Append(',')
                    .Append(d.HeroLevel).Append(',')
                    .Append(N(d.PlaySeconds / 60d)).Append(',')
                    .Append(N(d.Earned)).Append(',')
                    .Append(N(d.EarnedStage)).Append(',')
                    .Append(N(d.EarnedOffline)).Append(',')
                    .Append(N(d.EarnedRefund)).Append(',')
                    .Append(N(d.EarnedBooster)).Append(',')
                    .Append(N(d.Spent)).Append(',')
                    .Append(N(d.SpentUpgrade)).Append(',')
                    .Append(N(d.SpentGacha)).Append(',')
                    .Append(N(d.SpentSkill)).Append(',')
                    .Append(N(d.SpentPet)).Append(',')
                    .Append(d.GoldPulls).Append(',')
                    .Append(d.GemPulls).Append(',')
                    .Append(d.NormalAttempts).Append(',')
                    .Append(d.NormalFails).Append(',')
                    .Append(d.BossAttempts).Append(',')
                    .Append(d.BossFails).Append(',')
                    .Append(N(d.GoldEnd)).Append(',')
                    .Append(N(d.GemEnd)).AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>Markdown table of checks for each run, one column per run.</summary>
        public static string Summary(IList<SimReport> runs, IList<List<SimCheck>> checks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Check | Target |" + Header(runs));
            sb.Append("|---|---|");
            for (int i = 0; i < runs.Count; i++) sb.Append("---|");
            sb.AppendLine();

            if (checks.Count == 0) return sb.ToString();
            for (int c = 0; c < checks[0].Count; c++)
            {
                SimCheck first = checks[0][c];
                sb.Append("| ").Append(first.Id).Append(' ').Append(first.Title).Append(" | ").Append(first.Target).Append(" |");
                for (int r = 0; r < checks.Count; r++)
                {
                    SimCheck check = checks[r][c];
                    sb.Append(' ').Append(Mark(check.Status)).Append(' ').Append(check.Measured).Append(" |");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.Append("Days (highest cleared g):");
            for (int r = 0; r < runs.Count; r++)
            {
                sb.AppendLine();
                sb.Append("- ").Append(runs[r].Name).Append(" seed ").Append(runs[r].Seed).Append(": ");
                for (int d = 0; d < runs[r].Days.Count; d++)
                {
                    if (d > 0) sb.Append(" / ");
                    sb.Append(BalanceChecks.Label(runs[r].Days[d].HighestStage));
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>D-151 (L-04): days at which the long-term summary lists the highest stage.</summary>
        public static readonly int[] LongTermDays = { 1, 7, 14, 30, 60, 90, 120, 180, 240, 365 };

        /// <summary>
        /// D-151 (L-04) markdown for a long-term run: per persona the range (min, median, max over runs) of the highest
        /// stage, level and tower at <see cref="LongTermDays"/>, the axis end days, then every run's checks.
        /// </summary>
        public static string LongTerm(IList<SimReport> runs, IList<List<SimCheck>> checks)
        {
            var sb = new StringBuilder();
            var personas = new List<string>();
            for (int i = 0; i < runs.Count; i++)
            {
                if (!personas.Contains(runs[i].Name)) personas.Add(runs[i].Name);
            }

            foreach (string persona in personas)
            {
                var group = new List<SimReport>();
                var groupChecks = new List<List<SimCheck>>();
                for (int i = 0; i < runs.Count; i++)
                {
                    if (runs[i].Name != persona) continue;
                    group.Add(runs[i]);
                    groupChecks.Add(checks[i]);
                }

                sb.AppendLine("## " + persona + " (" + group.Count + " runs)");
                sb.AppendLine();
                sb.AppendLine("| Day | Highest stage (min / median / max) | Hero level | Tower floor |");
                sb.AppendLine("|---|---|---|---|");
                foreach (int day in LongTermDays)
                {
                    var stages = new List<double>();
                    var levels = new List<double>();
                    var floors = new List<double>();
                    foreach (SimReport r in group)
                    {
                        if (day > r.Days.Count) continue;
                        SimDayRow d = r.Days[day - 1];
                        stages.Add(d.HighestStage);
                        levels.Add(d.HeroLevel);
                        floors.Add(d.TowerFloor);
                    }

                    if (stages.Count == 0) continue;
                    sb.Append("| ").Append(day).Append(" | ")
                        .Append(BalanceChecks.Label((int)Min(stages))).Append(" / ").Append(BalanceChecks.Label((int)Math.Round(MedianOf(stages))))
                        .Append(" / ").Append(BalanceChecks.Label((int)Max(stages))).Append(" | ")
                        .Append(Min(levels)).Append(" - ").Append(Max(levels)).Append(" | ")
                        .Append(Min(floors)).Append(" - ").Append(Max(floors)).AppendLine(" |");
                }

                sb.AppendLine();
                var stalls = new List<double>();
                foreach (SimReport r in group) stalls.Add(LongTermChecks.LongestStallDays(r));
                sb.Append("Longest wait for a new stage: median ").Append(N(MedianOf(stalls))).Append(" days, max ").Append(N(Max(stalls))).AppendLine(" days.");
                sb.AppendLine();
                sb.Append("Axis end day (median of the runs that ended it, runs): ");
                var names = new List<string>();
                foreach (SimReport r in group)
                {
                    foreach (string axis in r.AxisCapDays.Keys)
                        if (!names.Contains(axis)) names.Add(axis);
                }

                var parts = new List<KeyValuePair<double, string>>();
                foreach (string axis in names)
                {
                    var days = new List<double>();
                    foreach (SimReport r in group)
                    {
                        if (r.AxisCapDays.TryGetValue(axis, out int day)) days.Add(day);
                    }

                    double median = MedianOf(days);
                    parts.Add(new KeyValuePair<double, string>(median, axis + " d" + N(median) + " (" + days.Count + "/" + group.Count + ")"));
                }

                parts.Sort((a, b) => a.Key.CompareTo(b.Key));
                for (int i = 0; i < parts.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(parts[i].Value);
                }

                sb.AppendLine(parts.Count == 0 ? "none." : ".");
                sb.AppendLine();
                sb.AppendLine("| Check | Target |" + RunHeader(group));
                sb.Append("|---|---|");
                for (int i = 0; i < group.Count; i++) sb.Append("---|");
                sb.AppendLine();
                if (groupChecks.Count > 0)
                {
                    for (int c = 0; c < groupChecks[0].Count; c++)
                    {
                        SimCheck first = groupChecks[0][c];
                        sb.Append("| ").Append(first.Id).Append(' ').Append(first.Title).Append(" | ").Append(first.Target).Append(" |");
                        for (int r = 0; r < groupChecks.Count; r++)
                            sb.Append(' ').Append(Mark(groupChecks[r][c].Status)).Append(' ').Append(groupChecks[r][c].Measured).Append(" |");
                        sb.AppendLine();
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string RunHeader(IList<SimReport> runs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < runs.Count; i++)
                sb.Append(' ').Append(runs[i].Job).Append(" #").Append(runs[i].Seed).Append(" |");
            return sb.ToString();
        }

        private static double MedianOf(List<double> values)
        {
            if (values.Count == 0) return 0d;
            var copy = new List<double>(values);
            copy.Sort();
            int n = copy.Count;
            return n % 2 == 1 ? copy[n / 2] : (copy[n / 2 - 1] + copy[n / 2]) / 2d;
        }

        private static double Min(List<double> values)
        {
            double m = double.MaxValue;
            for (int i = 0; i < values.Count; i++) m = Math.Min(m, values[i]);
            return m;
        }

        private static double Max(List<double> values)
        {
            double m = double.MinValue;
            for (int i = 0; i < values.Count; i++) m = Math.Max(m, values[i]);
            return m;
        }

        private static string Header(IList<SimReport> runs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < runs.Count; i++)
                sb.Append(' ').Append(runs[i].Name).Append(" #").Append(runs[i].Seed).Append(" |");
            return sb.ToString();
        }

        private static string Mark(SimCheckStatus status)
        {
            switch (status)
            {
                case SimCheckStatus.Pass: return "PASS";
                case SimCheckStatus.Fail: return "FAIL";
                default: return "INFO";
            }
        }

        private static string Grade(int grade)
        {
            switch (grade)
            {
                case 0: return "C";
                case 1: return "U";
                case 2: return "R";
                case 3: return "E";
                case 4: return "L";
                case 5: return "M";
                case 6: return "A";
                default: return "-";
            }
        }

        private static string N(double v) => v.ToString("0.##", Inv);
    }
}
