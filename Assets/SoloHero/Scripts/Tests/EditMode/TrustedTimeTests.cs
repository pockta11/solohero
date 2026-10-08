using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core.Boot;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Economy;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-133: trusted time, offline gold on return, and daily resets that a moved device clock cannot repeat.</summary>
    public sealed class TrustedTimeTests
    {
        private sealed class DeviceClock : IClock
        {
            public long Utc = 1_800_000_000L;
            public DateTime Local = new DateTime(2026, 10, 8, 10, 0, 0);

            public long UtcNowSeconds => Utc;

            public DateTime LocalNow => Local;
        }

        private sealed class Monotonic
        {
            public double Seconds = 500d;

            public double Read() => Seconds;
        }

        [Test]
        public void UtcNowSeconds_DeviceClockMovedAfterStart_IgnoresTheMove()
        {
            var device = new DeviceClock();
            var mono = new Monotonic();
            var clock = new TrustedClock(device, mono.Read);

            device.Utc += 6 * 3600;
            mono.Seconds += 10d;

            Assert.AreEqual(1_800_000_010L, clock.UtcNowSeconds);
        }

        [Test]
        public void SyncServer_Offset_AnchorsToServerTimeAndRaisesOnce()
        {
            var device = new DeviceClock();
            var mono = new Monotonic();
            var clock = new TrustedClock(device, mono.Read);
            int raised = 0;
            clock.ServerTimeKnown += () => raised++;

            Assert.IsFalse(clock.IsServerTime);
            Assert.IsFalse(clock.IsTrusted);
            clock.SyncServer(-3_600_000d);
            mono.Seconds += 5d;
            clock.SyncServer(-3_600_000d);

            Assert.IsTrue(clock.IsServerTime);
            Assert.IsTrue(clock.IsTrusted);
            Assert.AreEqual(1, raised);
            Assert.AreEqual(1_800_000_000L - 3600L, clock.UtcNowSeconds);
        }

        [Test]
        public void UtcAt_EarlierReading_IsMeasuredFromTheServerAnchor()
        {
            var device = new DeviceClock();
            var mono = new Monotonic();
            var clock = new TrustedClock(device, mono.Read);
            double cameBack = mono.Seconds;

            mono.Seconds += 40d;
            device.Utc += 40;
            clock.SyncServer(1_000d);

            Assert.AreEqual(1_800_000_001L, clock.UtcAt(cameBack));
        }

        [Test]
        public void IsTrusted_NoServerExpected_TrustsTheDeviceClock()
        {
            var clock = new TrustedClock(new DeviceClock(), new Monotonic().Read) { ServerExpected = false };

            Assert.IsTrue(clock.IsTrusted);
            Assert.IsFalse(clock.IsServerTime);
        }

        [Test]
        public void IsNewDay_OnlyLaterDaysRollOver()
        {
            Assert.IsTrue(DayKey.IsNewDay("", "2026-10-08"));
            Assert.IsTrue(DayKey.IsNewDay("2026-10-07", "2026-10-08"));
            Assert.IsFalse(DayKey.IsNewDay("2026-10-08", "2026-10-08"));
            Assert.IsFalse(DayKey.IsNewDay("2026-10-09", "2026-10-08"));
            Assert.IsTrue(DayKey.IsNewDay("2026-12-31", "2027-01-01"));
        }

        [Test]
        public void TryClaimAttendance_ClockMovedForwardThenBack_PaysTheEarlyDayOnlyOnce()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new DeviceClock();
            var daily = new DailyService(b, save, clock);
            Assert.IsTrue(daily.TryClaimAttendance().Ok);

            clock.Local = clock.Local.AddDays(1);
            Assert.IsTrue(daily.TryClaimAttendance().Ok);
            clock.Local = clock.Local.AddDays(-1);
            Assert.IsFalse(daily.AttendanceAvailable);
            clock.Local = clock.Local.AddDays(1);

            Assert.IsFalse(daily.AttendanceAvailable);
            Assert.AreEqual(2, save.attendanceCount);
        }

        [Test]
        public void Remaining_ClockTurnedBack_DoesNotRefillAdCounters()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new DeviceClock();
            var policy = new AdSlotPolicy(b, save, clock);
            Assert.IsTrue(policy.Complete(AdSlot.Gem, AdOutcome.Rewarded).Ok);

            clock.Local = clock.Local.AddDays(-1);

            Assert.AreEqual(b.AD_GEM_DAILY - 1, policy.Remaining(AdSlot.Gem));
        }

        [Test]
        public void BoosterSecondsLeft_ClockTurnedBack_StaysWithinOneBooster()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new DeviceClock();
            var policy = new AdSlotPolicy(b, save, clock);
            Assert.IsTrue(policy.Complete(AdSlot.GoldBooster, AdOutcome.Rewarded).Ok);
            Assert.IsTrue(policy.Complete(AdSlot.BattleSpeed, AdOutcome.Rewarded).Ok);

            clock.Utc -= 24 * 3600;

            Assert.AreEqual((long)Math.Round(b.AD_BOOSTER_SECONDS), policy.BoosterSecondsLeft);
            Assert.AreEqual((long)Math.Round(b.AD_SPEED_SECONDS), policy.SpeedSecondsLeft);
        }

        [Test]
        public void Apply_ShortAbsence_PaysAtOnceAndMovesQuitTime()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.lastQuitTimeUtc = 1000;

            OfflineReward reward = OfflineReturn.Apply(b, save, 1030, serverTime: true);

            Assert.IsTrue(reward.GrantNow);
            Assert.AreEqual(b.STAGE_GOLD_BASE / b.OFFLINE_DIVISOR * 30d, save.gold, 1e-9);
            Assert.AreEqual(1030L, save.lastQuitTimeUtc);
        }

        [Test]
        public void Apply_PopupAbsence_LeavesGoldAndQuitTimeForTheClaim()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.lastQuitTimeUtc = 1000;

            OfflineReward reward = OfflineReturn.Apply(b, save, 1000 + 3600, serverTime: true);

            Assert.IsTrue(reward.ShowPopup);
            Assert.AreEqual(0d, save.gold);
            Assert.AreEqual(1000L, save.lastQuitTimeUtc);
        }

        [Test]
        public void StampLeave_DeviceClockTurnedBack_KeepsTheLaterStamp()
        {
            var save = SaveDataV2.CreateNew();
            save.lastQuitTimeUtc = 5000;

            OfflineReturn.StampLeave(save, 4000, serverTime: false);
            Assert.AreEqual(5000L, save.lastQuitTimeUtc);

            OfflineReturn.StampLeave(save, 6000, serverTime: false);
            Assert.AreEqual(6000L, save.lastQuitTimeUtc);

            OfflineReturn.StampLeave(save, 4000, serverTime: true);
            Assert.AreEqual(4000L, save.lastQuitTimeUtc);
        }

        [Test]
        public void DeviceClock_ForwardClaimThenBack_CannotPayTwice()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.lastQuitTimeUtc = 10_000;

            // Clock moved 6 h forward: paid once (the claim stamps the future time).
            OfflineReward first = OfflineReturn.Apply(b, save, 10_000 + b.OFFLINE_CAP, serverTime: false);
            Assert.IsTrue(first.ShowPopup);
            Assert.IsTrue(new OfflineClaim(b).Apply(save, first, 10_000 + b.OFFLINE_CAP, adDoubled: false).Ok);
            OfflineReturn.StampLeave(save, 10_000 + b.OFFLINE_CAP + 60, serverTime: false);

            // Clock back to the real time, then forward again by the same amount: nothing new to pay.
            OfflineReward back = OfflineReturn.Apply(b, save, 10_100, serverTime: false);
            OfflineReturn.StampLeave(save, 10_160, serverTime: false);
            OfflineReward again = OfflineReturn.Apply(b, save, 10_000 + b.OFFLINE_CAP + 60, serverTime: false);

            Assert.AreEqual(0d, back.Gold);
            Assert.AreEqual(0d, again.Gold);
            Assert.IsFalse(again.ShowPopup);
        }

        [Test]
        public void Run_SignedInWithoutServerTime_DefersTheOfflineReward()
        {
            var stored = SaveDataV2.CreateNew();
            stored.lastQuitTimeUtc = 1_800_000_000L - 3600;
            var store = new MemoryStore { Data = stored };
            var clock = new TrustedClock(new DeviceClock(), new Monotonic().Read);

            BootReport report = BootFlow.RunAsync(
                new FixedAuth("uid"),
                userId => new SaveService(store, new PassSerializer(store), debounceMs: 5000),
                new BalanceValues(),
                clock).GetAwaiter().GetResult();

            Assert.IsTrue(report.OfflineDeferred);
            Assert.IsFalse(report.Offline.ShowPopup);
            Assert.AreEqual(1_800_000_000L - 3600, report.Data.lastQuitTimeUtc);
            Assert.AreEqual(0d, report.Data.gold);
        }

        [Test]
        public void Run_LocalMode_PaysOnTheDeviceClock()
        {
            var stored = SaveDataV2.CreateNew();
            stored.lastQuitTimeUtc = 1_800_000_000L - 3600;
            var store = new MemoryStore { Data = stored };
            var clock = new TrustedClock(new DeviceClock(), new Monotonic().Read);

            BootReport report = BootFlow.RunAsync(
                new FixedAuth("local"),
                userId => new SaveService(store, new PassSerializer(store), debounceMs: 5000),
                new BalanceValues(),
                clock).GetAwaiter().GetResult();

            Assert.IsFalse(report.OfflineDeferred);
            Assert.IsTrue(report.Offline.ShowPopup);
            Assert.AreEqual(3600L, report.Offline.CountedSeconds);
        }

        private sealed class FixedAuth : IAuthGateway
        {
            private readonly string _userId;
            public FixedAuth(string userId) { _userId = userId; }
            public Task<string> SignInAsync() => Task.FromResult(_userId);
        }

        /// <summary>Holds the save object itself; the serializer below hands it back unchanged.</summary>
        private sealed class MemoryStore : ISaveStore
        {
            public SaveDataV2 Data;

            public Task<string> LoadJsonAsync() => Task.FromResult(Data != null ? "save" : null);

            public Task SaveJsonAsync(string json) => Task.CompletedTask;
        }

        private sealed class PassSerializer : ISaveSerializer
        {
            private readonly MemoryStore _store;
            public PassSerializer(MemoryStore store) { _store = store; }
            public string ToJson(SaveDataV2 data) => "save";
            public SaveDataV2 FromV2Json(string json) => _store.Data;
            public PlayerDataV1 FromV1Json(string json) => null;
        }
    }
}
