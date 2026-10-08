using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// D-100 daily dungeon entries: DUNGEON_DAILY_TICKETS per dungeon per device-local day (like the ad counters).
    /// An entry is spent only when the runner actually starts the run, and that spend is saved at once. D-143: they
    /// open once stage DUNGEON_UNLOCK_STAGE is cleared, and every finished run pays DUNGEON_STONES breakthrough stones.
    /// </summary>
    public sealed class DungeonService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;

        public DungeonService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        public int DailyTickets => _balance.DUNGEON_DAILY_TICKETS;

        public static bool IsUnlocked(BalanceValues balance, SaveDataV2 data) =>
            balance != null && data != null && data.highestStage >= balance.DUNGEON_UNLOCK_STAGE;

        public bool Unlocked => IsUnlocked(_balance, _data);

        public int Remaining(DungeonKind kind)
        {
            RollDay();
            int left = _balance.DUNGEON_DAILY_TICKETS - Used(kind);
            return left < 0 ? 0 : left;
        }

        /// <summary>What one kill pays right now (gold or EXP) at the farming stage, for the entry card.</summary>
        public double RewardPerKill(DungeonKind kind)
        {
            int g = _data.farmingStage < 1 ? 1 : _data.farmingStage;
            return kind == DungeonKind.Gold ? Formulas.DungeonGoldPerKill(_balance, g) : Formulas.DungeonExpPerKill(_balance, g);
        }

        public Result TryEnter(DungeonKind kind, StageRunner runner)
        {
            if (kind == DungeonKind.None || runner == null || !Unlocked) return Result.Fail(FailReason.Locked);
            if (Remaining(kind) <= 0) return Result.Fail(FailReason.DailyLimit);
            if (!runner.StartDungeon(kind)) return Result.Fail(FailReason.Busy);
            if (kind == DungeonKind.Gold) _data.dungeonGoldUsed++;
            else _data.dungeonExpUsed++;
            _save?.RequestSave();
            return Result.Success;
        }

        private int Used(DungeonKind kind) => kind == DungeonKind.Gold ? _data.dungeonGoldUsed : _data.dungeonExpUsed;

        private void RollDay()
        {
            string today = DayKey.Today(_clock);
            if (!DayKey.IsNewDay(_data.dungeonDate, today)) return;
            _data.dungeonDate = today;
            _data.dungeonGoldUsed = 0;
            _data.dungeonExpUsed = 0;
        }
    }
}
