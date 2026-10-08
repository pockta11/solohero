using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>
    /// D-133: offline gold when the player comes back - a cold start (BootFlow) or a return from the background
    /// (BootSequence) - and the quit stamp when they leave. Both measure trusted time (TrustedClock).
    /// </summary>
    public static class OfflineReturn
    {
        /// <summary>
        /// Computes the reward and applies the parts that need no popup: a short absence is paid at once and a future
        /// quit time is reset. Returns the reward; when <see cref="OfflineReward.ShowPopup"/> the caller shows the popup
        /// and the claim moves the quit time. The caller requests the save when gold or the quit time changed.
        /// </summary>
        public static OfflineReward Apply(BalanceValues balance, SaveDataV2 data, long nowUtc, bool serverTime)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (data == null) throw new ArgumentNullException(nameof(data));

            OfflineReward offline = OfflineReward.Compute(balance, data.farmingStage, data.lastQuitTimeUtc, nowUtc, serverTime);
            if (offline.GrantNow)
            {
                data.gold += offline.Gold;
                data.lastQuitTimeUtc = nowUtc;
            }
            else if (offline.ResetQuitTime)
            {
                data.lastQuitTimeUtc = nowUtc;
            }

            return offline;
        }

        /// <summary>
        /// The quit stamp. With the server's clock it is now; on the device clock alone it never moves back, so a
        /// clock turned back cannot reopen hours that were already paid.
        /// </summary>
        public static void StampLeave(SaveDataV2 data, long nowUtc, bool serverTime)
        {
            if (data == null) return;
            if (serverTime || nowUtc > data.lastQuitTimeUtc) data.lastQuitTimeUtc = nowUtc;
        }
    }
}
