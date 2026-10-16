using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>
    /// D-181 VIP pack perks, read wherever they apply (AdSlotPolicy speed, DailyService rewards, OfflineReturn cap,
    /// the ad gateway). Active while the save's vipUntilUtc lies ahead of the trusted clock.
    /// </summary>
    public static class VipPerks
    {
        public const long DaySeconds = 86400L;

        public static bool IsActive(SaveDataV2 data, long nowUtc) => data != null && data.vipUntilUtc > nowUtc;

        public static bool IsActive(SaveDataV2 data, IClock clock) => clock != null && IsActive(data, clock.UtcNowSeconds);

        /// <summary>Whole days left, rounded up (0 when inactive).</summary>
        public static int DaysLeft(SaveDataV2 data, long nowUtc)
        {
            if (!IsActive(data, nowUtc)) return 0;
            long left = data.vipUntilUtc - nowUtc;
            return (int)((left + DaySeconds - 1) / DaySeconds);
        }

        /// <summary>Attendance and daily mission reward multiplier right now.</summary>
        public static double DailyMultiplier(BalanceValues balance, SaveDataV2 data, long nowUtc) =>
            IsActive(data, nowUtc) && balance.VIP_DAILY_MULT > 1d ? balance.VIP_DAILY_MULT : 1d;

        /// <summary>Offline gold cap in seconds right now.</summary>
        public static int OfflineCap(BalanceValues balance, SaveDataV2 data, long nowUtc) =>
            IsActive(data, nowUtc) && balance.VIP_OFFLINE_CAP > balance.OFFLINE_CAP ? balance.VIP_OFFLINE_CAP : balance.OFFLINE_CAP;

        /// <summary>Extends the pack by VIP_DAYS from its end, or from now once it ran out.</summary>
        public static void Extend(BalanceValues balance, SaveDataV2 data, long nowUtc)
        {
            long from = data.vipUntilUtc > nowUtc ? data.vipUntilUtc : nowUtc;
            data.vipUntilUtc = from + balance.VIP_DAYS * DaySeconds;
        }
    }
}
