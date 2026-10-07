using NUnit.Framework;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// E5-14 / GDD gacha rule 8: 100,000 real pulls match the disclosed table within 1 %p; pity always holds.
    /// D-113: the seven-grade gear table and its 200-pull ceiling.
    /// </summary>
    public sealed class GachaVerifierTests
    {
        [Test]
        public void HundredThousandPulls_MatchDisclosedRates_AndPityHolds()
        {
            var b = new BalanceValues();
            // D-123: the pity counts once the summon level opens Legendary; verify at that level.
            int level = GearTableValues.FromBalance(b).OpenLevel(GearTableValues.PityGrade);

            GachaVerifier.Report report = GachaVerifier.Run(b, 100000, seed: 20260928, level: level);

            Assert.That(report.WorstGapP, Is.LessThanOrEqualTo(GachaVerifier.ToleranceP), report.ToMarkdown(b.GEAR_PITY));
            Assert.That(report.LongestDryStreak, Is.LessThan(b.GEAR_PITY), "a Legendary or better always comes within the ceiling");
            Assert.That(report.EffectivePercent(GearGrade.Legendary), Is.GreaterThan(report.Disclosed[(int)GearGrade.Legendary]), "pity only adds Legendaries");
            Assert.That(report.NaturalPercent(GearGrade.Mythic), Is.LessThan(0.2d), "Mythic stays extremely rare");
            Assert.IsTrue(report.Passed(b.GEAR_PITY));
        }
    }
}
