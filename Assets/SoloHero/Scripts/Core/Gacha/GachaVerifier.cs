using System;
using System.Globalization;
using System.Text;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Gacha
{
    /// <summary>
    /// Rate disclosure check (E5-14, GDD gacha rule 8): runs the real <see cref="GachaService"/> for N single pulls and
    /// compares the measured grade shares with the disclosed table. Pity-forced Legendaries are counted apart - the
    /// disclosed per-pull rate applies to natural draws; the effective Legendary share (with pity) is reported too.
    /// </summary>
    public static class GachaVerifier
    {
        public const double ToleranceP = 1.0;

        public sealed class Report
        {
            public int Pulls;
            public int PityForced;
            public readonly int[] Natural = new int[GachaCatalog.GradeCount];
            public readonly int[] Total = new int[GachaCatalog.GradeCount];
            public readonly double[] Disclosed = new double[GachaCatalog.GradeCount];
            public int LongestDryStreak;

            public int NaturalPulls => Pulls - PityForced;

            public double NaturalPercent(Grade g) => NaturalPulls > 0 ? 100d * Natural[(int)g] / NaturalPulls : 0d;

            public double EffectivePercent(Grade g) => Pulls > 0 ? 100d * Total[(int)g] / Pulls : 0d;

            public double WorstGapP
            {
                get
                {
                    double worst = 0d;
                    for (int g = 0; g < Natural.Length; g++)
                        worst = Math.Max(worst, Math.Abs(NaturalPercent((Grade)g) - Disclosed[g]));
                    return worst;
                }
            }

            public bool Passed(int pityCeiling) => WorstGapP <= ToleranceP && LongestDryStreak < pityCeiling;

            public string ToMarkdown(int pityCeiling)
            {
                var sb = new StringBuilder();
                var c = CultureInfo.InvariantCulture;
                sb.Append("| Grade | Disclosed % | Natural draws % | Gap %p | With pity % |\n|---|---|---|---|---|\n");
                for (int g = 0; g < Natural.Length; g++)
                {
                    var grade = (Grade)g;
                    sb.Append("| ").Append(grade).Append(" | ")
                        .Append(Disclosed[g].ToString("0.00", c)).Append(" | ")
                        .Append(NaturalPercent(grade).ToString("0.00", c)).Append(" | ")
                        .Append((NaturalPercent(grade) - Disclosed[g]).ToString("+0.00;-0.00", c)).Append(" | ")
                        .Append(EffectivePercent(grade).ToString("0.00", c)).Append(" |\n");
                }

                sb.Append("\nPulls ").Append(Pulls.ToString("N0", c))
                    .Append(", pity-forced Legendaries ").Append(PityForced.ToString("N0", c))
                    .Append(", longest run without a Legendary ").Append(LongestDryStreak)
                    .Append(" (ceiling ").Append(pityCeiling).Append(").\n")
                    .Append("Result: ").Append(Passed(pityCeiling) ? "PASS" : "FAIL")
                    .Append(" (worst gap ").Append(WorstGapP.ToString("0.00", c)).Append(" %p, tolerance ")
                    .Append(ToleranceP.ToString("0.0", c)).Append(" %p)\n");
                return sb.ToString();
            }
        }

        public static Report Run(BalanceValues balance, int pulls, int seed)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            GachaTableValues table = GachaTableValues.FromBalance(balance);
            var service = new GachaService(balance, table, new SystemRandom(new Random(seed)), GachaCatalog.Standard(balance));
            SaveDataV2 data = SaveDataV2.CreateNew();
            var report = new Report { Pulls = pulls };
            report.Disclosed[(int)Grade.Common] = balance.GACHA_RATE_C;
            report.Disclosed[(int)Grade.Rare] = balance.GACHA_RATE_R;
            report.Disclosed[(int)Grade.Epic] = balance.GACHA_RATE_E;
            report.Disclosed[(int)Grade.Legendary] = balance.GACHA_RATE_L;

            int dry = 0;
            for (int i = 0; i < pulls; i++)
            {
                data.gold = balance.GACHA_COST_SINGLE;
                bool forced = data.pityCount + 1 >= table.PityCeiling;
                GachaBatchResult result = service.TryPull(data);
                Grade grade = result.Items[0].Grade;
                report.Total[(int)grade]++;
                if (forced) report.PityForced++;
                else report.Natural[(int)grade]++;

                if (grade == Grade.Legendary)
                {
                    dry = 0;
                }
                else
                {
                    dry++;
                    if (dry > report.LongestDryStreak) report.LongestDryStreak = dry;
                }
            }

            return report;
        }
    }
}
