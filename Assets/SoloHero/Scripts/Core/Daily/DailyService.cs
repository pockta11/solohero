using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Daily
{
    /// <summary>
    /// Attendance and daily missions (D-099). Both roll over at device-local midnight like the ad counters.
    /// Attendance is cumulative (missing a day does not reset it): gems on odd days, the current farming stage's clear
    /// gold x ATTENDANCE_GOLD_STAGES on even days and ATTENDANCE_DAY7_GEM on each week's last day. D-166 (L-11): the
    /// cycle is a 28-day board (<see cref="AttendanceCatalog"/>: day 28 pays ATTENDANCE_DAY28_GEM, days 14 / 21 / 28 add
    /// tickets) and the total days attended pay milestones.
    /// Missions count everyday actions (recorded by the game) and pay gems when claimed; the last one pays a bonus
    /// for claiming all the others. Progress changes do not request a save on their own; claims do (currency).
    /// D-155: weekly missions count the same actions over a week (Monday 00:00 local) and also pay tickets and stones.
    /// </summary>
    public sealed class DailyService
    {
        /// <summary>Days of one week of the board (the cells the popup shows at a time).</summary>
        public const int CycleDays = AttendanceCatalog.WeekDays;

        /// <summary>D-166: days of the whole board.</summary>
        public const int BoardDays = AttendanceCatalog.BoardDays;

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

        /// <summary>The board day (1..28) the next claim pays, or today's claimed day once claimed.</summary>
        public int AttendanceDay =>
            AttendanceAvailable || _data.attendanceCount <= 0 ? _data.attendanceCount % BoardDays + 1 : (_data.attendanceCount - 1) % BoardDays + 1;

        /// <summary>D-166: the first board day of the week the popup shows (1, 8, 15 or 22).</summary>
        public int AttendanceWeekStart => (AttendanceDay - 1) / CycleDays * CycleDays + 1;

        /// <summary>D-166: the total days attended (the milestones count these).</summary>
        public int AttendanceTotal => _data.attendanceCount;

        /// <summary>Board days already claimed in this round of the board (0..28).</summary>
        private int ClaimedOnBoard
        {
            get
            {
                if (_data.attendanceCount <= 0) return 0;
                int inBoard = _data.attendanceCount % BoardDays;
                if (inBoard == 0) return AttendanceAvailable ? 0 : BoardDays;
                return inBoard;
            }
        }

        /// <summary>How many days of the shown week are already claimed (0..7).</summary>
        public int AttendanceClaimedInCycle => Math.Max(0, Math.Min(CycleDays, ClaimedOnBoard - (AttendanceWeekStart - 1)));

        /// <summary>Gems of board day <paramref name="day"/> (1..28): odd days, and more on each week's last day.</summary>
        /// <summary>D-181: the VIP pack multiplies attendance and daily mission rewards while it runs.</summary>
        public double VipMultiplier => Economy.VipPerks.DailyMultiplier(_balance, _data, _clock.UtcNowSeconds);

        public double AttendanceGems(int day)
        {
            double gems = day == BoardDays ? _balance.ATTENDANCE_DAY28_GEM
                : day % CycleDays == 0 ? _balance.ATTENDANCE_DAY7_GEM
                : day % 2 == 1 ? _balance.ATTENDANCE_GEM : 0d;
            return gems * VipMultiplier;
        }

        public double AttendanceGold(int day) =>
            day % 2 == 0 && day % CycleDays != 0
                ? Math.Floor(Formulas.StageGold(_balance, FarmStage) * _balance.ATTENDANCE_GOLD_STAGES * VipMultiplier)
                : 0d;

        /// <summary>Gems the daily mission at <paramref name="index"/> pays right now (VIP included).</summary>
        public double MissionGems(int index) =>
            index >= 0 && index < MissionCatalog.Count ? MissionCatalog.All[index].Gems * VipMultiplier : 0d;

        /// <summary>D-166: the total attended that the last claim reached as a milestone (0 when it reached none).</summary>
        public int LastMilestone { get; private set; }

        public Result TryClaimAttendance()
        {
            if (!AttendanceAvailable) return Result.Fail(FailReason.DailyLimit);
            int day = _data.attendanceCount % BoardDays + 1;
            _data.gem += AttendanceGems(day);
            _data.gold += AttendanceGold(day);
            AttendanceCatalog.WeekEndExtra(day)?.PayInto(_balance, _data);
            _data.attendanceCount++;
            _data.attendanceDate = Today;
            // D-166: the total days attended pay their milestone once, on the claim that reaches it.
            AttendanceMilestone milestone = AttendanceCatalog.At(_data.attendanceCount);
            milestone?.Reward.PayInto(_balance, _data);
            LastMilestone = milestone != null ? milestone.Days : 0;
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

        /// <summary>Rewards waiting to be collected (attendance counts as one, D-155 weekly missions too), for the menu badge.</summary>
        public int ClaimableCount
        {
            get
            {
                int n = AttendanceAvailable ? 1 : 0;
                for (int i = 0; i < MissionCatalog.Count; i++)
                    if (Claimable(i)) n++;
                for (int i = 0; i < WeeklyMissionCatalog.Count; i++)
                    if (WeeklyClaimable(i)) n++;
                return n;
            }
        }

        /// <summary>D-156: every recorded action, for the running event's missions.</summary>
        public event Action<MissionKind, int> Recorded;

        /// <summary>D-163: a daily (false) or weekly (true) mission was claimed, for the season pass XP.</summary>
        public event Action<bool> MissionClaimed;

        /// <summary>Counts an action toward today's and this week's missions of that kind.</summary>
        public void Record(MissionKind kind, int amount)
        {
            if (amount <= 0 || kind == MissionKind.All) return;
            Recorded?.Invoke(kind, amount);
            RollMissions();
            RollWeekly();
            bool moved = Move(MissionCatalog.All, _data.missionProgress, kind, amount);
            moved |= Move(WeeklyMissionCatalog.All, _data.weeklyProgress, kind, amount);
            if (moved) Changed?.Invoke();
        }

        public Result TryClaimMission(int index)
        {
            if (index < 0 || index >= MissionCatalog.Count) return Result.Fail(FailReason.Locked);
            if (Claimed(index)) return Result.Fail(FailReason.DailyLimit);
            if (!Complete(index)) return Result.Fail(FailReason.Locked);
            _data.gem += MissionGems(index);
            _data.missionClaimed[index] = true;
            BumpAll(MissionCatalog.All, _data.missionProgress, _data.missionClaimed);
            MissionClaimed?.Invoke(false);
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        private static bool Move(MissionDef[] catalog, List<int> progress, MissionKind kind, int amount)
        {
            bool moved = false;
            for (int i = 0; i < catalog.Length; i++)
            {
                MissionDef def = catalog[i];
                if (def.Kind != kind || progress[i] >= def.Target) continue;
                progress[i] = Math.Min(def.Target, progress[i] + amount);
                moved = true;
            }

            return moved;
        }

        /// <summary>The "claim all the others" mission counts the claimed ones.</summary>
        private static void BumpAll(MissionDef[] catalog, List<int> progress, List<bool> claimed)
        {
            int count = 0;
            int all = -1;
            for (int i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Kind == MissionKind.All) all = i;
                else if (claimed[i]) count++;
            }

            if (all >= 0) progress[all] = Math.Min(catalog[all].Target, count);
        }

        // ------------------------------------------------------------------ weekly missions (D-155)

        public int WeeklyProgress(int index)
        {
            RollWeekly();
            return index >= 0 && index < _data.weeklyProgress.Count ? _data.weeklyProgress[index] : 0;
        }

        public bool WeeklyClaimed(int index)
        {
            RollWeekly();
            return index >= 0 && index < _data.weeklyClaimed.Count && _data.weeklyClaimed[index];
        }

        public bool WeeklyComplete(int index) =>
            index >= 0 && index < WeeklyMissionCatalog.Count && WeeklyProgress(index) >= WeeklyMissionCatalog.All[index].Target;

        public bool WeeklyClaimable(int index) => WeeklyComplete(index) && !WeeklyClaimed(index);

        /// <summary>Pays a finished weekly mission: gems, summon tickets and breakthrough stones.</summary>
        public Result TryClaimWeekly(int index)
        {
            if (index < 0 || index >= WeeklyMissionCatalog.Count) return Result.Fail(FailReason.Locked);
            if (WeeklyClaimed(index)) return Result.Fail(FailReason.DailyLimit);
            if (!WeeklyComplete(index)) return Result.Fail(FailReason.Locked);
            MissionDef def = WeeklyMissionCatalog.All[index];
            _data.gem += def.Gems;
            _data.breakStones += def.Stones;
            ShopService.AddTickets(_data, SummonKind.Gear, def.GearTickets);
            ShopService.AddTickets(_data, SummonKind.Skill, def.SkillTickets);
            ShopService.AddTickets(_data, SummonKind.Pet, def.PetTickets);
            _data.weeklyClaimed[index] = true;
            BumpAll(WeeklyMissionCatalog.All, _data.weeklyProgress, _data.weeklyClaimed);
            MissionClaimed?.Invoke(true);
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        private void RollWeekly()
        {
            int n = WeeklyMissionCatalog.Count;
            while (_data.weeklyProgress.Count < n) _data.weeklyProgress.Add(0);
            while (_data.weeklyClaimed.Count < n) _data.weeklyClaimed.Add(false);
            string week = DayKey.ThisWeek(_clock);
            if (!DayKey.IsNewDay(_data.weeklyDate, week)) return;
            _data.weeklyDate = week;
            for (int i = 0; i < n; i++)
            {
                _data.weeklyProgress[i] = 0;
                _data.weeklyClaimed[i] = false;
            }
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
