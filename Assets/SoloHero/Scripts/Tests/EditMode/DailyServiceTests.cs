using System;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class DailyServiceTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime Local = new DateTime(2026, 10, 1, 10, 0, 0);

            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => Local;
        }

        private static int IndexOf(MissionKind kind)
        {
            for (int i = 0; i < MissionCatalog.Count; i++)
                if (MissionCatalog.All[i].Kind == kind) return i;
            return -1;
        }

        [Test]
        public void TryClaimAttendance_OncePerLocalDay_CyclesGemsGoldAndDaySeven()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.farmingStage = 12;
            var clock = new FakeClock();
            var daily = new DailyService(b, save, clock);

            Assert.IsTrue(daily.AttendanceAvailable);
            Assert.AreEqual(1, daily.AttendanceDay);
            Assert.IsTrue(daily.TryClaimAttendance().Ok);
            Assert.AreEqual(b.ATTENDANCE_GEM, save.gem);
            Assert.AreEqual(FailReason.DailyLimit, daily.TryClaimAttendance().Reason);

            clock.Local = clock.Local.AddDays(1);
            Assert.AreEqual(2, daily.AttendanceDay);
            Assert.IsTrue(daily.TryClaimAttendance().Ok);
            Assert.AreEqual(Math.Floor(Formulas.StageGold(b, 12) * b.ATTENDANCE_GOLD_STAGES), save.gold);

            for (int day = 3; day <= 7; day++)
            {
                clock.Local = clock.Local.AddDays(1);
                Assert.IsTrue(daily.TryClaimAttendance().Ok);
            }

            Assert.AreEqual(b.ATTENDANCE_GEM * 3 + b.ATTENDANCE_DAY7_GEM, save.gem);
            Assert.AreEqual(7, daily.AttendanceClaimedInCycle);
            clock.Local = clock.Local.AddDays(3);
            // D-166: the board goes on to its second week; a missed day does not reset it.
            Assert.AreEqual(8, daily.AttendanceDay, "a missed day does not reset; the board goes on to day 8");
            Assert.AreEqual(8, daily.AttendanceWeekStart);
            Assert.AreEqual(0, daily.AttendanceClaimedInCycle);
        }

        [Test]
        public void MonthlyBoard_WeekEndsPayTickets_Day28Restarts_AndTotalsPayMilestones()
        {
            // D-166 (L-11): a 28-day board, tickets on days 14 / 21 / 28, milestones at 30, 60, ... days attended.
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.farmingStage = 5;
            var clock = new FakeClock();
            var daily = new DailyService(b, save, clock);
            int gearTickets = 0;
            for (int day = 1; day <= 30; day++)
            {
                Assert.AreEqual((day - 1) % DailyService.BoardDays + 1, daily.AttendanceDay, "day " + day);
                Assert.IsTrue(daily.TryClaimAttendance().Ok);
                Assert.AreEqual(day == 30 ? 30 : 0, daily.LastMilestone, "day " + day);
                if (day == 14) gearTickets += 10;
                if (day == 28) gearTickets += 20;
                if (day == 30) gearTickets += AttendanceCatalog.At(30).Reward.GearTickets;
                Assert.AreEqual(gearTickets, save.gearTickets, "day " + day);
                clock.Local = clock.Local.AddDays(1);
            }

            Assert.AreEqual(b.ATTENDANCE_DAY28_GEM, daily.AttendanceGems(28));
            Assert.AreEqual(b.ATTENDANCE_DAY7_GEM, daily.AttendanceGems(21));
            Assert.AreEqual(0d, daily.AttendanceGold(14), "week ends pay gems, not gold");
            Assert.AreEqual(3, daily.AttendanceDay, "the board restarts after day 28");
            Assert.AreEqual(60, AttendanceCatalog.Next(30).Days);
            Assert.IsNull(AttendanceCatalog.Next(365));
        }

        [Test]
        public void Missions_RecordClaimAndAllBonus_ResetAtMidnight()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var daily = new DailyService(b, save, clock);
            int kill = IndexOf(MissionKind.Kill);
            int all = IndexOf(MissionKind.All);

            Assert.AreEqual(FailReason.Locked, daily.TryClaimMission(kill).Reason);
            daily.Record(MissionKind.Kill, 1000);
            Assert.AreEqual(MissionCatalog.All[kill].Target, daily.Progress(kill), "progress caps at the target");
            Assert.IsTrue(daily.TryClaimMission(kill).Ok);
            Assert.AreEqual(MissionCatalog.All[kill].Gems, save.gem);
            Assert.AreEqual(FailReason.DailyLimit, daily.TryClaimMission(kill).Reason);

            daily.Record(MissionKind.StageClear, 99);
            daily.Record(MissionKind.Upgrade, 99);
            daily.Record(MissionKind.Summon, 99);
            daily.Record(MissionKind.WatchAd, 1);
            for (int i = 0; i < MissionCatalog.Count; i++)
                if (i != all && i != kill) Assert.IsTrue(daily.TryClaimMission(i).Ok);
            Assert.IsTrue(daily.Claimable(all));
            Assert.IsTrue(daily.TryClaimMission(all).Ok);
            Assert.AreEqual(0, daily.ClaimableCount - (daily.AttendanceAvailable ? 1 : 0));

            clock.Local = clock.Local.AddDays(1);
            Assert.AreEqual(0, daily.Progress(kill));
            Assert.IsFalse(daily.Claimed(kill));
        }
    }
}
