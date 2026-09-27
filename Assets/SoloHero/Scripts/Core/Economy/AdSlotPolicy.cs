using System;
using System.Globalization;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>
    /// Rewarded ad slots (E6-09..E6-13): daily limits per slot, reset at device-local midnight, and the reward
    /// each slot grants. A slot is counted only after <see cref="AdOutcome.Rewarded"/>; closing early or a load
    /// failure costs nothing and the normal path (plain claim, no booster) always stays available.
    /// </summary>
    public sealed class AdSlotPolicy
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;

        public AdSlotPolicy(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        public double GemReward => _balance.AD_GEM_REWARD;

        public int DailyLimit(AdSlot slot)
        {
            switch (slot)
            {
                case AdSlot.OfflineDouble: return _balance.AD_OFFLINE_DAILY;
                case AdSlot.Gem: return _balance.AD_GEM_DAILY;
                case AdSlot.GoldBooster: return _balance.AD_BOOSTER_DAILY;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public int Remaining(AdSlot slot)
        {
            RollDay();
            int left = DailyLimit(slot) - Used(slot);
            return left < 0 ? 0 : left;
        }

        public Result CanUse(AdSlot slot)
        {
            if (Remaining(slot) <= 0) return Result.Fail(FailReason.DailyLimit);
            if (slot == AdSlot.GoldBooster && BoosterSecondsLeft > 0) return Result.Fail(FailReason.Busy);
            return Result.Success;
        }

        /// <summary>
        /// Applies the result of one ad. Only <see cref="AdOutcome.Rewarded"/> grants and counts. For
        /// <see cref="AdSlot.OfflineDouble"/> the caller claims the doubled gold itself (OfflineClaim) after Ok.
        /// </summary>
        public Result Complete(AdSlot slot, AdOutcome outcome)
        {
            if (outcome != AdOutcome.Rewarded) return Result.Fail(FailReason.AdUnavailable);
            Result can = CanUse(slot);
            if (!can.Ok) return can;

            switch (slot)
            {
                case AdSlot.Gem:
                    _data.gem += _balance.AD_GEM_REWARD;
                    break;
                case AdSlot.GoldBooster:
                    _data.goldBoosterEndUtc = _clock.UtcNowSeconds + (long)Math.Round(_balance.AD_BOOSTER_SECONDS);
                    break;
            }

            SetUsed(slot, Used(slot) + 1);
            _save?.RequestSave();
            return Result.Success;
        }

        public long BoosterSecondsLeft
        {
            get
            {
                long left = _data.goldBoosterEndUtc - _clock.UtcNowSeconds;
                return left > 0 ? left : 0;
            }
        }

        /// <summary>Stage-clear gold multiplier right now (A-3 gold booster).</summary>
        public double StageGoldMultiplier => BoosterSecondsLeft > 0 ? _balance.AD_BOOSTER_GOLD_MULT : 1d;

        private void RollDay()
        {
            string today = _clock.LocalNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (_data.adCountResetDate == today) return;
            _data.adCountResetDate = today;
            _data.adCountA1 = 0;
            _data.adCountA2 = 0;
            _data.adCountA3 = 0;
        }

        private int Used(AdSlot slot)
        {
            switch (slot)
            {
                case AdSlot.OfflineDouble: return _data.adCountA1;
                case AdSlot.Gem: return _data.adCountA2;
                default: return _data.adCountA3;
            }
        }

        private void SetUsed(AdSlot slot, int value)
        {
            switch (slot)
            {
                case AdSlot.OfflineDouble: _data.adCountA1 = value; break;
                case AdSlot.Gem: _data.adCountA2 = value; break;
                default: _data.adCountA3 = value; break;
            }
        }
    }
}
