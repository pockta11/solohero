using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    public sealed class OfflineClaim
    {
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public OfflineClaim(BalanceValues balance, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public Result Apply(SaveDataV2 save, OfflineReward reward, long now, bool adDoubled)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (!reward.ShowPopup)
                return Result.Fail(FailReason.Busy);

            double mult = adDoubled ? _balance.OFFLINE_AD_MULT : 1d;
            save.gold += reward.Gold * mult;
            save.lastQuitTimeUtc = now;
            _save?.RequestSave();
            return Result.Success;
        }
    }
}
