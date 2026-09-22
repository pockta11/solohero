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

        public OfflineReward(double gold, bool showPopup, bool grantNow, bool resetQuitTime)
        {
            Gold = gold;
            ShowPopup = showPopup;
            GrantNow = grantNow;
            ResetQuitTime = resetQuitTime;
        }

        public static OfflineReward Compute(BalanceValues balance, int farmingStage, long lastQuitTimeUtc, long nowUtc)
        {
            if (lastQuitTimeUtc <= 0 || balance.OFFLINE_DIVISOR == 0d) return new OfflineReward(0d, false, false, false);

            long elapsed = nowUtc - lastQuitTimeUtc;
            if (elapsed < 0 || elapsed > (long)balance.OFFLINE_CAP * 2L)
                return new OfflineReward(0d, false, false, true);

            long counted = elapsed > balance.OFFLINE_CAP ? balance.OFFLINE_CAP : elapsed;
            int stage = farmingStage < 1 ? 1 : farmingStage;
            double gold = Formulas.StageGold(balance, stage) / balance.OFFLINE_DIVISOR * counted;
            bool showPopup = elapsed >= balance.OFFLINE_MIN_SECONDS;
            return new OfflineReward(gold, showPopup, !showPopup && counted > 0, false);
        }
    }
}
