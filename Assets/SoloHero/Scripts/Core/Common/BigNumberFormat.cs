using System;
using System.Globalization;

namespace SoloHero.Core.Common
{
    /// <summary>
    /// Short numbers for the UI (D-127, genre style): under 1,000 the whole number; from 1,000 three significant digits
    /// with a unit and no trailing zeros - 1K, 1.35K, 12.3K, 123K, 1.23M - then B, T and aa, ab, ... for every further
    /// thousand. Digits are cut, never rounded up, so a shown amount is never more than the real one.
    /// </summary>
    public static class BigNumberFormat
    {
        private const double Thousand = 1000d;
        private const int FixedSuffixCount = 4;

        // Guards the cut against binary fractions (1.15 is stored as 1.1499...).
        private const double CutEpsilon = 1e-7;

        private static readonly string[] FixedSuffixes = { "K", "M", "B", "T" };

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0";

            bool negative = value < 0d;
            double abs = Math.Abs(value);

            if (abs < Thousand)
            {
                string digits = ((long)Math.Floor(abs + CutEpsilon)).ToString(CultureInfo.InvariantCulture);
                return negative && digits != "0" ? "-" + digits : digits;
            }

            int tier = -1;
            double scaled = abs;
            while (scaled >= Thousand)
            {
                scaled /= Thousand;
                tier++;
            }

            // Three significant digits: 1.23 / 12.3 / 123.
            double step = scaled >= 100d ? 1d : scaled >= 10d ? 10d : 100d;
            double mantissa = Math.Floor(scaled * step + CutEpsilon) / step;
            string body = mantissa.ToString("0.##", CultureInfo.InvariantCulture) + SuffixForTier(tier);
            return negative ? "-" + body : body;
        }

        private static string SuffixForTier(int tier)
        {
            if (tier < FixedSuffixCount)
                return FixedSuffixes[tier];

            int letterIndex = tier - FixedSuffixCount;
            char high = (char)('a' + (letterIndex / 26));
            char low = (char)('a' + (letterIndex % 26));
            return new string(new[] { high, low });
        }
    }
}
