using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    /// <summary>
    /// D-141 stat window actions: spend AP by hand, switch auto allocation on or off, and reset (gems) to re-spend.
    /// Switching auto on spends the unspent points in the auto split and keeps what was placed by hand.
    /// </summary>
    public sealed class ApService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public event Action Changed;

        public ApService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public int Unspent => HeroAp.Unspent(_balance, _data);

        public bool Manual => _data.apManual;

        public ApPoints Points => ApPoints.From(_data);

        public int ResetCost => _balance.AP_RESET_GEM;

        /// <summary>Manual mode: puts up to <paramref name="count"/> unspent points into <paramref name="stat"/>.</summary>
        public Result TryAdd(ApStat stat, int count = 1)
        {
            int left = Unspent;
            if (left <= 0) return Result.Fail(FailReason.NoApPoints);
            if (count > left) count = left;
            if (count <= 0) return Result.Fail(FailReason.NoApPoints);
            if (stat == ApStat.Main) _data.apMain += count;
            else _data.apVit += count;
            Done();
            return Result.Success;
        }

        public void SetManual(bool manual)
        {
            if (_data.apManual == manual) return;
            _data.apManual = manual;
            HeroAp.Settle(_balance, _data);
            Done();
        }

        /// <summary>Takes every point back for AP_RESET_GEM gems; auto mode re-spends them in the auto split at once.</summary>
        public Result TryReset()
        {
            if (HeroAp.Spent(_data) <= 0) return Result.Fail(FailReason.NoApPoints);
            if (_data.gem < _balance.AP_RESET_GEM) return Result.Fail(FailReason.NotEnoughGem);
            _data.gem -= _balance.AP_RESET_GEM;
            _data.apMain = 0;
            _data.apVit = 0;
            HeroAp.Settle(_balance, _data);
            Done();
            return Result.Success;
        }

        private void Done()
        {
            Changed?.Invoke();
            _save?.RequestSave();
        }
    }
}
