using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Events
{
    /// <summary>
    /// D-156 (L-16) the running event: its attendance board (one claim a local day, a missed day just waits), its missions
    /// (actions counted over the whole event, paying tokens) and its token exchange. Progress in the save belongs to one
    /// event id; when another event runs the progress starts over and unspent tokens of the old one are gone. Times come
    /// from the trusted clock, so moving the device clock does not open or stretch an event.
    /// </summary>
    public sealed class EventService : IPickupSource
    {
        private static readonly ScheduleEntry[] None = new ScheduleEntry[0];

        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;
        private IReadOnlyList<ScheduleEntry> _schedule = None;

        public EventService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        /// <summary>Raised when the schedule arrives, progress moves or something is claimed or bought.</summary>
        public event Action Changed;

        public void SetSchedule(IReadOnlyList<ScheduleEntry> entries)
        {
            _schedule = entries ?? None;
            Changed?.Invoke();
        }

        /// <summary>The running event, or null.</summary>
        public ScheduleEntry Active => EventSchedule.ActiveAt(_schedule, _clock.UtcNowSeconds);

        public EventTemplate Template => Sync();

        public bool Running => Active != null;

        public long SecondsLeft
        {
            get
            {
                ScheduleEntry active = Active;
                return active == null ? 0L : Math.Max(0L, active.EndUtc - _clock.UtcNowSeconds);
            }
        }

        /// <summary>D-173: the pickup banner of a summon running now (beside any event), or null.</summary>
        public PickupBanner ActivePickup(SummonKind kind)
        {
            ScheduleEntry entry = EventSchedule.PickupAt(_schedule, _clock.UtcNowSeconds, kind);
            return entry != null ? PickupCatalog.Find(entry.Template) : null;
        }

        public long PickupSecondsLeft(SummonKind kind)
        {
            ScheduleEntry entry = EventSchedule.PickupAt(_schedule, _clock.UtcNowSeconds, kind);
            return entry == null ? 0L : Math.Max(0L, entry.EndUtc - _clock.UtcNowSeconds);
        }

        /// <summary>True the first time it is asked during an event (for the event_join analytics event).</summary>
        public bool MarkJoined()
        {
            if (Sync() == null || _data.eventJoined) return false;
            _data.eventJoined = true;
            _save?.RequestSave();
            return true;
        }

        // ------------------------------------------------------------------ attendance

        public int AttendanceClaimed => Sync() != null ? _data.eventAttendCount : 0;

        public bool AttendanceAvailable
        {
            get
            {
                EventTemplate t = Sync();
                return t != null && _data.eventAttendCount < t.Attendance.Length && DayKey.IsNewDay(_data.eventAttendDate, DayKey.Today(_clock));
            }
        }

        public Result TryClaimAttendance()
        {
            EventTemplate t = Sync();
            if (t == null) return Result.Fail(FailReason.EventEnded);
            if (!AttendanceAvailable) return Result.Fail(FailReason.DailyLimit);
            t.Attendance[_data.eventAttendCount].PayInto(_balance, _data);
            _data.eventAttendCount++;
            _data.eventAttendDate = DayKey.Today(_clock);
            Done();
            return Result.Success;
        }

        // ------------------------------------------------------------------ missions

        public long MissionProgress(int index)
        {
            EventTemplate t = Sync();
            return t != null && index >= 0 && index < t.Missions.Length ? _data.eventMissionProgress[index] : 0L;
        }

        public bool MissionClaimed(int index)
        {
            EventTemplate t = Sync();
            return t != null && index >= 0 && index < t.Missions.Length && _data.eventMissionClaimed[index];
        }

        public bool MissionClaimable(int index)
        {
            EventTemplate t = Sync();
            return t != null && index >= 0 && index < t.Missions.Length && !_data.eventMissionClaimed[index]
                && _data.eventMissionProgress[index] >= t.Missions[index].Target;
        }

        public Result TryClaimMission(int index)
        {
            EventTemplate t = Sync();
            if (t == null) return Result.Fail(FailReason.EventEnded);
            if (index < 0 || index >= t.Missions.Length) return Result.Fail(FailReason.Locked);
            if (_data.eventMissionClaimed[index]) return Result.Fail(FailReason.DailyLimit);
            if (!MissionClaimable(index)) return Result.Fail(FailReason.Locked);
            _data.eventTokens += t.Missions[index].Tokens;
            _data.eventMissionClaimed[index] = true;
            Done();
            return Result.Success;
        }

        /// <summary>Counts an action toward the running event's missions (DailyService forwards every action).</summary>
        public void Record(MissionKind kind, int amount)
        {
            if (amount <= 0) return;
            EventTemplate t = Sync();
            if (t == null) return;
            bool moved = false;
            for (int i = 0; i < t.Missions.Length; i++)
            {
                EventMission m = t.Missions[i];
                if (m.Kind != kind || _data.eventMissionProgress[i] >= m.Target) continue;
                _data.eventMissionProgress[i] = Math.Min(m.Target, _data.eventMissionProgress[i] + amount);
                moved = true;
            }

            if (moved) Changed?.Invoke();
        }

        // ------------------------------------------------------------------ exchange

        public int Tokens => Sync() != null ? _data.eventTokens : 0;

        public int Bought(int index)
        {
            EventTemplate t = Sync();
            return t != null && index >= 0 && index < t.Shop.Length ? _data.eventShopBought[index] : 0;
        }

        public bool CanBuy(int index)
        {
            EventTemplate t = Sync();
            return t != null && index >= 0 && index < t.Shop.Length && _data.eventShopBought[index] < t.Shop[index].Limit
                && _data.eventTokens >= t.Shop[index].Cost;
        }

        public Result TryBuy(int index)
        {
            EventTemplate t = Sync();
            if (t == null) return Result.Fail(FailReason.EventEnded);
            if (index < 0 || index >= t.Shop.Length) return Result.Fail(FailReason.Locked);
            EventShopItem item = t.Shop[index];
            if (_data.eventShopBought[index] >= item.Limit) return Result.Fail(FailReason.MaxLevel);
            if (_data.eventTokens < item.Cost) return Result.Fail(FailReason.NotEnoughTokens);
            _data.eventTokens -= item.Cost;
            _data.eventShopBought[index]++;
            item.Reward.PayInto(_balance, _data);
            Done();
            return Result.Success;
        }

        /// <summary>Rewards waiting (attendance, finished missions, affordable items), for the menu badge.</summary>
        public int ClaimableCount
        {
            get
            {
                EventTemplate t = Sync();
                if (t == null) return 0;
                int n = AttendanceAvailable ? 1 : 0;
                for (int i = 0; i < t.Missions.Length; i++)
                    if (MissionClaimable(i)) n++;
                return n;
            }
        }

        private void Done()
        {
            Changed?.Invoke();
            _save?.RequestSave();
        }

        /// <summary>The running event's template with the save's progress made to belong to it; null when none runs.</summary>
        private EventTemplate Sync()
        {
            ScheduleEntry active = Active;
            if (active == null) return null;
            EventTemplate t = EventCatalog.Find(active.Template);
            if (t == null) return null;
            if (_data.eventId != active.Id)
            {
                _data.eventId = active.Id;
                _data.eventAttendCount = 0;
                _data.eventAttendDate = "";
                _data.eventMissionProgress.Clear();
                _data.eventMissionClaimed.Clear();
                _data.eventShopBought.Clear();
                _data.eventTokens = 0;
                _data.eventJoined = false;
            }

            while (_data.eventMissionProgress.Count < t.Missions.Length) _data.eventMissionProgress.Add(0L);
            while (_data.eventMissionClaimed.Count < t.Missions.Length) _data.eventMissionClaimed.Add(false);
            while (_data.eventShopBought.Count < t.Shop.Length) _data.eventShopBought.Add(0);
            return t;
        }
    }
}
