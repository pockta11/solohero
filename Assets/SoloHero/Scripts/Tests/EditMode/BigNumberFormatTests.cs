using NUnit.Framework;
using SoloHero.Core.Common;

namespace SoloHero.Tests.EditMode
{
    public sealed class BigNumberFormatTests
    {
        [Test]
        public void Format_NineHundredNinetyNine_ShowsInteger()
        {
            Assert.AreEqual("999", BigNumberFormat.Format(999d));
        }

        [Test]
        public void Format_Thousand_ShowsOneDecimalK()
        {
            Assert.AreEqual("1.0K", BigNumberFormat.Format(1000d));
        }

        [Test]
        public void Format_TwelveThousandFourHundred_ShowsOneDecimalK()
        {
            Assert.AreEqual("12.4K", BigNumberFormat.Format(12400d));
        }

        [Test]
        public void Format_OneMillion_ShowsOneDecimalM()
        {
            Assert.AreEqual("1.0M", BigNumberFormat.Format(1_000_000d));
        }

        [Test]
        public void Format_OneTrillion_ShowsOneDecimalT()
        {
            Assert.AreEqual("1.0T", BigNumberFormat.Format(1e12));
        }

        [Test]
        public void Format_OneQuadrillion_ShowsOneDecimalAa()
        {
            Assert.AreEqual("1.0aa", BigNumberFormat.Format(1e15));
        }

        [Test]
        public void Format_Negative_KeepsLeadingMinus()
        {
            Assert.AreEqual("-12.4K", BigNumberFormat.Format(-12400d));
        }

        [Test]
        public void Format_NaN_ReturnsZero()
        {
            Assert.AreEqual("0", BigNumberFormat.Format(double.NaN));
        }
    }
}
