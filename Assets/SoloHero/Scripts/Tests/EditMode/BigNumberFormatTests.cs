using NUnit.Framework;
using SoloHero.Core.Common;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-127: under 1,000 the whole number, then three significant digits with K, M, B, T, aa, ... and no trailing zeros.</summary>
    public sealed class BigNumberFormatTests
    {
        [TestCase(0d, "0")]
        [TestCase(7d, "7")]
        [TestCase(999d, "999")]
        [TestCase(999.9d, "999")]
        [TestCase(1000d, "1K")]
        [TestCase(1150d, "1.15K")]
        [TestCase(1350d, "1.35K")]
        [TestCase(1359d, "1.35K")]
        [TestCase(12400d, "12.4K")]
        [TestCase(45000d, "45K")]
        [TestCase(123456d, "123K")]
        [TestCase(999999d, "999K")]
        [TestCase(1_000_000d, "1M")]
        [TestCase(1_234_567d, "1.23M")]
        [TestCase(2.5e9, "2.5B")]
        [TestCase(1e12, "1T")]
        [TestCase(1e15, "1aa")]
        [TestCase(3.21e18, "3.21ab")]
        public void Format_ShortensWithThreeSignificantDigits(double value, string expected)
        {
            Assert.AreEqual(expected, BigNumberFormat.Format(value));
        }

        [Test]
        public void Format_Negative_KeepsLeadingMinus()
        {
            Assert.AreEqual("-12.4K", BigNumberFormat.Format(-12400d));
            Assert.AreEqual("-5", BigNumberFormat.Format(-5d));
        }

        [Test]
        public void Format_NaN_ReturnsZero()
        {
            Assert.AreEqual("0", BigNumberFormat.Format(double.NaN));
        }
    }
}
