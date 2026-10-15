using System;
using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Events;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-156 (L-16): a scheduled event's attendance, missions and token exchange, all tied to its id.</summary>
    public sealed class EventServiceTests
    {
        private const long Start = 1_800_000_000L;
        private const long Day = 86400L;

        private sealed class FakeClock : IClock
        {
            public long Utc = Start + 3600;

            public long UtcNowSeconds => Utc;

            public DateTime LocalNow => DateTimeOffset.FromUnixTimeSeconds(Utc).UtcDateTime;
        }

        private static List<ScheduleEntry> Schedule(string id, string template = "launch", long start = Start, long days = 14) =>
            new List<ScheduleEntry> { new ScheduleEntry { Id = id, Template = template, StartUtc = start, EndUtc = start + days * Day } };

        private static int MissionIndex(EventTemplate t, MissionKind kind)
        {
            for (int i = 0; i < t.Missions.Length; i++)
                if (t.Missions[i].Kind == kind) return i;
            return -1;
        }

        [Test]
        public void Schedule_PicksTheRunningKnownEvent_LatestStartWins()
        {
            var entries = new List<ScheduleEntry>
            {
                new ScheduleEntry { Id = "old", Template = "launch", StartUtc = Start - 30 * Day, EndUtc = Start - Day },
                new ScheduleEntry { Id = "a", Template = "launch", StartUtc = Start, EndUtc = Start + 14 * Day },
                new ScheduleEntry { Id = "b", Template = "festival", StartUtc = Start + Day, EndUtc = Start + 8 * Day },
                new ScheduleEntry { Id = "future", Template = "no_such_template", StartUtc = Start, EndUtc = Start + 30 * Day },
            };

            Assert.IsNull(EventSchedule.ActiveAt(entries, Start - 1));
            Assert.AreEqual("a", EventSchedule.ActiveAt(entries, Start + 10).Id);
            Assert.AreEqual("b", EventSchedule.ActiveAt(entries, Start + 2 * Day).Id);
            Assert.AreEqual("a", EventSchedule.ActiveAt(entries, Start + 9 * Day).Id);
            Assert.IsNull(EventSchedule.ActiveAt(entries, Start + 14 * Day));
        }

        [Test]
        public void NoEvent_NothingRunsOrPays()
        {
            var save = SaveDataV2.CreateNew();
            var events = new EventService(new BalanceValues(), save, new FakeClock());

            Assert.IsFalse(events.Running);
            Assert.AreEqual(FailReason.EventEnded, events.TryClaimAttendance().Reason);
            events.Record(MissionKind.Kill, 100);
            Assert.AreEqual(0, events.ClaimableCount);
        }

        [Test]
        public void Attendance_OneClaimPerLocalDay_InOrder()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var events = new EventService(b, save, clock);
            events.SetSchedule(Schedule("e1"));
            EventTemplate t = events.Template;

            Assert.IsTrue(events.AttendanceAvailable);
            Assert.IsTrue(events.TryClaimAttendance().Ok);
            Assert.AreEqual(t.Attendance[0].Gems, save.gem, 1e-9);
            Assert.AreEqual(FailReason.DailyLimit, events.TryClaimAttendance().Reason);

            clock.Utc += 3 * Day;
            Assert.IsTrue(events.TryClaimAttendance().Ok);
            Assert.AreEqual(t.Attendance[1].GearTickets, save.gearTickets);
            Assert.AreEqual(2, events.AttendanceClaimed);
        }

        [Test]
        public void Missions_PayTokens_ShopSpendsThem_WithLimits()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var events = new EventService(b, save, new FakeClock());
            events.SetSchedule(Schedule("e1"));
            EventTemplate t = events.Template;
            int kill = MissionIndex(t, MissionKind.Kill);

            events.Record(MissionKind.Kill, (int)t.Missions[kill].Target);
            Assert.IsTrue(events.MissionClaimable(kill));
            Assert.IsTrue(events.TryClaimMission(kill).Ok);
            Assert.AreEqual(t.Missions[kill].Tokens, events.Tokens);
            Assert.AreEqual(FailReason.DailyLimit, events.TryClaimMission(kill).Reason);

            save.eventTokens = 1000;
            EventShopItem gear = t.Shop[0];
            for (int i = 0; i < gear.Limit; i++) Assert.IsTrue(events.TryBuy(0).Ok);
            Assert.AreEqual(FailReason.MaxLevel, events.TryBuy(0).Reason);
            Assert.AreEqual(gear.Limit * gear.Reward.GearTickets, save.gearTickets);
            Assert.AreEqual(1000 - gear.Limit * gear.Cost, events.Tokens);

            save.eventTokens = 0;
            Assert.AreEqual(FailReason.NotEnoughTokens, events.TryBuy(1).Reason);
        }

        [Test]
        public void ANewEvent_StartsOver_AndDropsTheOldTokens()
        {
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var events = new EventService(new BalanceValues(), save, clock);
            events.SetSchedule(Schedule("e1"));
            Assert.IsTrue(events.TryClaimAttendance().Ok);
            save.eventTokens = 55;
            Assert.IsTrue(events.MarkJoined());
            Assert.IsFalse(events.MarkJoined());

            events.SetSchedule(Schedule("e2", "festival", Start + 20 * Day));
            clock.Utc = Start + 21 * Day;

            Assert.AreEqual("e2", events.Active.Id);
            Assert.AreEqual(0, events.Tokens);
            Assert.AreEqual(0, events.AttendanceClaimed);
            Assert.IsTrue(events.MarkJoined());
        }

        [Test]
        public void DailyActions_ReachTheEvent()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var daily = new DailyService(b, save, clock);
            var events = new EventService(b, save, clock);
            daily.Recorded += events.Record;
            events.SetSchedule(Schedule("e1"));
            int clear = MissionIndex(events.Template, MissionKind.StageClear);

            daily.Record(MissionKind.StageClear, 7);
            Assert.AreEqual(7, events.MissionProgress(clear));
        }
    }
}
