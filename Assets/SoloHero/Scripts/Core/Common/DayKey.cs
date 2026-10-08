using System;
using System.Globalization;

namespace SoloHero.Core.Common
{
    /// <summary>
    /// Local calendar days for the daily resets (ad counters, attendance, missions, dungeons, shop), stored as
    /// "yyyy-MM-dd". A day rolls over only when today is later than the stored day (D-133): turning the clock back
    /// never reopens a day that was already used, and a day claimed early with a clock moved forward is not paid again
    /// when it really comes.
    /// </summary>
    public static class DayKey
    {
        public static string Of(DateTime local) => local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static string Today(IClock clock) => Of(clock.LocalNow);

        /// <summary>True when <paramref name="today"/> is a later day than <paramref name="stored"/> ("" = never used).</summary>
        public static bool IsNewDay(string stored, string today) =>
            string.IsNullOrEmpty(stored) || string.CompareOrdinal(today, stored) > 0;
    }
}
