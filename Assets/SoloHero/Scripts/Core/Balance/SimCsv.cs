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
            sb.AppendLine("day,highest,label,hero_level,play_min,earned,earned_stage,earned_offline,earned_refund,earned_booster,spent,spent_upgrade,spent_gacha,spent_skill,gold_pulls,gem_pulls,normal_attempts,normal_fails,boss_attempts,boss_fails,gold_end,gem_end");
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
                case 1: return "R";
                case 2: return "E";
                case 3: return "L";
                default: return "-";
            }
        }

        private static string N(double v) => v.ToString("0.##", Inv);
    }
}
