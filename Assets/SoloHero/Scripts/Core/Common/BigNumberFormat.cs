using System;
using System.Globalization;

namespace SoloHero.Core.Common
{
    public static class BigNumberFormat
    {
        private const double Thousand = 1000d;
        private const int FixedSuffixCount = 4;

        private static readonly string[] FixedSuffixes = { "K", "M", "B", "T" };

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0";

            bool negative = value < 0d;
            double abs = Math.Abs(value);

            if (abs < Thousand)
            {
                double asInteger = Math.Round(abs, MidpointRounding.AwayFromZero);
                if (asInteger < Thousand)
                {
                    string digits = ((long)asInteger).ToString(CultureInfo.InvariantCulture);
                    return negative ? "-" + digits : digits;
                }

                return FormatScaled(1.0d, 0, negative);
            }

            int tier = -1;
            double scaled = abs;
            while (scaled >= Thousand)
            {
                scaled /= Thousand;
                tier++;
            }

            double mantissa = Math.Round(scaled, 1, MidpointRounding.AwayFromZero);
            if (mantissa >= Thousand)
            {
                mantissa = 1.0d;
                tier++;
            }

            return FormatScaled(mantissa, tier, negative);
        }

        private static string FormatScaled(double mantissa, int tier, bool negative)
        {
            string mantissaText = mantissa.ToString("0.0", CultureInfo.InvariantCulture);
            string suffix = SuffixForTier(tier);
            string body = mantissaText + suffix;
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
