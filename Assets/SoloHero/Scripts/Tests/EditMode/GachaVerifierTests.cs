using NUnit.Framework;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;

namespace SoloHero.Tests.EditMode
{
    /// <summary>E5-14 / GDD gacha rule 8: 100,000 real pulls match the disclosed table within 1 %p; pity always holds.</summary>
    public sealed class GachaVerifierTests
    {
        [Test]
        public void HundredThousandPulls_MatchDisclosedRates_AndPityHolds()
        {
            var b = new BalanceValues();

            GachaVerifier.Report report = GachaVerifier.Run(b, 100000, seed: 20260928);

            Assert.That(report.WorstGapP, Is.LessThanOrEqualTo(GachaVerifier.ToleranceP), report.ToMarkdown(b.GACHA_PITY));
            Assert.That(report.LongestDryStreak, Is.LessThan(b.GACHA_PITY), "a Legendary always comes within the ceiling");
            Assert.That(report.EffectivePercent(Grade.Legendary), Is.GreaterThan(b.GACHA_RATE_L), "pity only adds Legendaries");
            Assert.IsTrue(report.Passed(b.GACHA_PITY));
        }
    }
}
