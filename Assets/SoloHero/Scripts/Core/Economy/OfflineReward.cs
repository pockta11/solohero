using SoloHero.Core;
using SoloHero.Core.Config;

namespace SoloHero.Core.Economy
{
    public readonly struct OfflineReward
    {
        public readonly double Gold;
        public readonly bool ShowPopup;
        public readonly bool GrantNow;
        public readonly bool ResetQuitTime;

        /// <summary>Offline seconds that earned gold (capped at OFFLINE_CAP) - the popup's cap gauge.</summary>
        public readonly long CountedSeconds;

        public OfflineReward(double gold, bool showPopup, bool grantNow, bool resetQuitTime, long countedSeconds = 0)
        {
            Gold = gold;
            ShowPopup = showPopup;
            GrantNow = grantNow;
            ResetQuitTime = resetQuitTime;
            CountedSeconds = countedSeconds;
        }

        /// <summary>
        /// Offline gold for the time between the stored quit and now, capped at OFFLINE_CAP (D-133: an absence longer
        /// than twice the cap pays the cap - the clock is trusted, so a long absence is no longer read as tampering).
        /// A quit time in the future pays nothing: with <paramref name="serverTime"/> it is reset to now (it came from a
        /// wrong device clock); on the device clock alone (local mode) it is kept, so moving the clock back and forth
        /// cannot pay the same hours twice.
        /// </summary>
        public static OfflineReward Compute(BalanceValues balance, int farmingStage, long lastQuitTimeUtc, long nowUtc, bool serverTime = true) =>
            Compute(balance, farmingStage, lastQuitTimeUtc, nowUtc, serverTime, balance.OFFLINE_CAP);

        /// <summary>The same with an explicit cap in seconds (D-181: the VIP pack raises it).</summary>
        public static OfflineReward Compute(BalanceValues balance, int farmingStage, long lastQuitTimeUtc, long nowUtc, bool serverTime, int capSeconds)
        {
            if (lastQuitTimeUtc <= 0 || balance.OFFLINE_DIVISOR == 0d) return new OfflineReward(0d, false, false, false);

            long elapsed = nowUtc - lastQuitTimeUtc;
            if (elapsed < 0)
                return new OfflineReward(0d, false, false, serverTime);

            long cap = capSeconds > 0 ? capSeconds : balance.OFFLINE_CAP;
            long counted = elapsed > cap ? cap : elapsed;
            int stage = farmingStage < 1 ? 1 : farmingStage;
            double gold = Formulas.StageGold(balance, stage) / balance.OFFLINE_DIVISOR * counted;
            bool showPopup = elapsed >= balance.OFFLINE_MIN_SECONDS;
            return new OfflineReward(gold, showPopup, !showPopup && counted > 0, false, counted);
        }
    }
}
