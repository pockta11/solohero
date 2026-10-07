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
    /// disclosed per-pull rate applies to natural draws; the effective share (with pity) is reported too. D-113: the
    /// seven gear grades; a dry streak ends on a Legendary or better.
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

            public double NaturalPercent(GearGrade g) => NaturalPulls > 0 ? 100d * Natural[(int)g] / NaturalPulls : 0d;

            public double EffectivePercent(GearGrade g) => Pulls > 0 ? 100d * Total[(int)g] / Pulls : 0d;

            public double WorstGapP
            {
                get
                {
                    double worst = 0d;
                    for (int g = 0; g < Natural.Length; g++)
                        worst = Math.Max(worst, Math.Abs(NaturalPercent((GearGrade)g) - Disclosed[g]));
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
                    var grade = (GearGrade)g;
                    sb.Append("| ").Append(grade).Append(" | ")
                        .Append(Disclosed[g].ToString("0.000", c)).Append(" | ")
                        .Append(NaturalPercent(grade).ToString("0.000", c)).Append(" | ")
                        .Append((NaturalPercent(grade) - Disclosed[g]).ToString("+0.000;-0.000", c)).Append(" | ")
                        .Append(EffectivePercent(grade).ToString("0.000", c)).Append(" |\n");
                }

                sb.Append("\nPulls ").Append(Pulls.ToString("N0", c))
                    .Append(", pity-forced Legendaries ").Append(PityForced.ToString("N0", c))
                    .Append(", longest run without a Legendary or better ").Append(LongestDryStreak)
                    .Append(" (ceiling ").Append(pityCeiling).Append(").\n")
                    .Append("Result: ").Append(Passed(pityCeiling) ? "PASS" : "FAIL")
                    .Append(" (worst gap ").Append(WorstGapP.ToString("0.00", c)).Append(" %p, tolerance ")
                    .Append(ToleranceP.ToString("0.0", c)).Append(" %p)\n");
                return sb.ToString();
            }
        }

        /// <param name="level">D-115 summon level held for the whole run (its own disclosed table).</param>
        public static Report Run(BalanceValues balance, int pulls, int seed, int level = 1)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            GearTableValues table = GearTableValues.FromBalance(balance);
            var service = new GachaService(balance, table, new SystemRandom(new Random(seed)), GachaCatalog.Standard(balance));
            SaveDataV2 data = SaveDataV2.CreateNew();
            var report = new Report { Pulls = pulls };
            GearTableValues disclosed = table.AtLevel(level);
            for (int g = 0; g < report.Disclosed.Length; g++) report.Disclosed[g] = disclosed.Rates[g];
            int levelPulls = SummonLevel.PullsFor(balance, SummonKind.Gear, level);

            int dry = 0;
            for (int i = 0; i < pulls; i++)
            {
                data.gold = balance.GACHA_COST_SINGLE;
                data.totalPullCount = levelPulls;
                bool forced = disclosed.PityOpen && data.pityCount + 1 >= table.PityCeiling;
                GachaBatchResult result = service.TryPull(data);
                GearGrade grade = result.Items[0].Grade;
                report.Total[(int)grade]++;
                if (forced) report.PityForced++;
                else report.Natural[(int)grade]++;

                if (grade >= GearTableValues.PityGrade)
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
