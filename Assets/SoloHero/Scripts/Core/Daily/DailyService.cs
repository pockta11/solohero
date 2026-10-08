using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Daily
{
    /// <summary>
    /// Attendance and daily missions (D-099). Both roll over at device-local midnight like the ad counters.
    /// Attendance is cumulative (missing a day does not reset it): a 7-day cycle of gems on odd days, the current
    /// farming stage's clear gold x ATTENDANCE_GOLD_STAGES on even days and ATTENDANCE_DAY7_GEM on day 7.
    /// Missions count everyday actions (recorded by the game) and pay gems when claimed; the last one pays a bonus
    /// for claiming all the others. Progress changes do not request a save on their own; claims do (currency).
    /// </summary>
    public sealed class DailyService
    {
        public const int CycleDays = 7;

        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;

        public DailyService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        /// <summary>Raised after progress moves or a reward is claimed.</summary>
        public event Action Changed;

        private string Today => DayKey.Today(_clock);

        // ------------------------------------------------------------------ attendance

        public bool AttendanceAvailable => DayKey.IsNewDay(_data.attendanceDate, Today);

        /// <summary>The cycle day (1..7) the next claim pays, or today's claimed day once claimed.</summary>
        public int AttendanceDay => AttendanceAvailable ? _data.attendanceCount % CycleDays + 1 : (_data.attendanceCount - 1) % CycleDays + 1;

        /// <summary>How many days of the current cycle are already claimed (0..7).</summary>
        public int AttendanceClaimedInCycle
        {
            get
            {
                if (_data.attendanceCount <= 0) return 0;
                int inCycle = _data.attendanceCount % CycleDays;
                if (inCycle == 0) return AttendanceAvailable ? 0 : CycleDays;
                return inCycle;
            }
        }

        public double AttendanceGems(int day)
        {
            if (day == CycleDays) return _balance.ATTENDANCE_DAY7_GEM;
            return day % 2 == 1 ? _balance.ATTENDANCE_GEM : 0d;
        }

        public double AttendanceGold(int day) =>
            day % 2 == 0 && day != CycleDays ? Math.Floor(Formulas.StageGold(_balance, FarmStage) * _balance.ATTENDANCE_GOLD_STAGES) : 0d;

        public Result TryClaimAttendance()
        {
            if (!AttendanceAvailable) return Result.Fail(FailReason.DailyLimit);
            int day = _data.attendanceCount % CycleDays + 1;
            _data.gem += AttendanceGems(day);
            _data.gold += AttendanceGold(day);
            _data.attendanceCount++;
            _data.attendanceDate = Today;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        private int FarmStage => _data.farmingStage < 1 ? 1 : _data.farmingStage;

        // ------------------------------------------------------------------ missions

        public int Progress(int index)
        {
            RollMissions();
            return index >= 0 && index < _data.missionProgress.Count ? _data.missionProgress[index] : 0;
        }

        public bool Claimed(int index)
        {
            RollMissions();
            return index >= 0 && index < _data.missionClaimed.Count && _data.missionClaimed[index];
        }

        public bool Complete(int index) => index >= 0 && index < MissionCatalog.Count && Progress(index) >= MissionCatalog.All[index].Target;

        public bool Claimable(int index) => Complete(index) && !Claimed(index);

        /// <summary>Rewards waiting to be collected (attendance counts as one), for the menu badge.</summary>
        public int ClaimableCount
        {
            get
            {
                int n = AttendanceAvailable ? 1 : 0;
                for (int i = 0; i < MissionCatalog.Count; i++)
                    if (Claimable(i)) n++;
                return n;
            }
        }

        public void Record(MissionKind kind, int amount)
        {
            if (amount <= 0 || kind == MissionKind.All) return;
            RollMissions();
            bool moved = false;
            for (int i = 0; i < MissionCatalog.Count; i++)
            {
                MissionDef def = MissionCatalog.All[i];
                if (def.Kind != kind || _data.missionProgress[i] >= def.Target) continue;
                _data.missionProgress[i] = Math.Min(def.Target, _data.missionProgress[i] + amount);
                moved = true;
            }

            if (moved) Changed?.Invoke();
        }

        public Result TryClaimMission(int index)
        {
            if (index < 0 || index >= MissionCatalog.Count) return Result.Fail(FailReason.Locked);
            if (Claimed(index)) return Result.Fail(FailReason.DailyLimit);
            if (!Complete(index)) return Result.Fail(FailReason.Locked);
            MissionDef def = MissionCatalog.All[index];
            _data.gem += def.Gems;
            _data.missionClaimed[index] = true;
            BumpAllMission();
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        private void BumpAllMission()
        {
            int claimed = 0;
            int all = -1;
            for (int i = 0; i < MissionCatalog.Count; i++)
            {
                if (MissionCatalog.All[i].Kind == MissionKind.All) all = i;
                else if (_data.missionClaimed[i]) claimed++;
            }

            if (all >= 0) _data.missionProgress[all] = Math.Min(MissionCatalog.All[all].Target, claimed);
        }

        private void RollMissions()
        {
            int n = MissionCatalog.Count;
            while (_data.missionProgress.Count < n) _data.missionProgress.Add(0);
            while (_data.missionClaimed.Count < n) _data.missionClaimed.Add(false);
            string today = Today;
            if (!DayKey.IsNewDay(_data.missionDate, today)) return;
            _data.missionDate = today;
            for (int i = 0; i < n; i++)
            {
                _data.missionProgress[i] = 0;
                _data.missionClaimed[i] = false;
            }
        }
    }
}
