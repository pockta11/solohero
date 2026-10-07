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

        /// <param name="goldMult">D-117: the permanent rebirth boosts (RebirthService.OfflineMult).</param>
        public static OfflineReward Compute(BalanceValues balance, int farmingStage, long lastQuitTimeUtc, long nowUtc, double goldMult = 1d)
        {
            if (lastQuitTimeUtc <= 0 || balance.OFFLINE_DIVISOR == 0d) return new OfflineReward(0d, false, false, false);

            long elapsed = nowUtc - lastQuitTimeUtc;
            if (elapsed < 0 || elapsed > (long)balance.OFFLINE_CAP * 2L)
                return new OfflineReward(0d, false, false, true);

            long counted = elapsed > balance.OFFLINE_CAP ? balance.OFFLINE_CAP : elapsed;
            int stage = farmingStage < 1 ? 1 : farmingStage;
            double gold = Formulas.StageGold(balance, stage) / balance.OFFLINE_DIVISOR * counted * (goldMult > 0d ? goldMult : 1d);
            bool showPopup = elapsed >= balance.OFFLINE_MIN_SECONDS;
            return new OfflineReward(gold, showPopup, !showPopup && counted > 0, false, counted);
        }
    }
}
