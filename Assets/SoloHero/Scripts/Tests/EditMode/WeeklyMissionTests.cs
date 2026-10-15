using System;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-155 (L-11): weekly missions count the same actions as the daily ones over a Monday-to-Sunday week.</summary>
    public sealed class WeeklyMissionTests
    {
        private sealed class FakeClock : IClock
        {
            // 2026-10-14 is a Wednesday.
            public DateTime Local = new DateTime(2026, 10, 14, 10, 0, 0);

            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => Local;
        }

        private static int IndexOf(MissionKind kind)
        {
            for (int i = 0; i < WeeklyMissionCatalog.Count; i++)
                if (WeeklyMissionCatalog.All[i].Kind == kind) return i;
            return -1;
        }

        [Test]
        public void WeekOf_IsTheMonday()
        {
            Assert.AreEqual("2026-10-12", DayKey.WeekOf(new DateTime(2026, 10, 12, 0, 0, 0)));
            Assert.AreEqual("2026-10-12", DayKey.WeekOf(new DateTime(2026, 10, 14, 23, 0, 0)));
            Assert.AreEqual("2026-10-12", DayKey.WeekOf(new DateTime(2026, 10, 18, 23, 59, 0)));
            Assert.AreEqual("2026-10-19", DayKey.WeekOf(new DateTime(2026, 10, 19, 0, 0, 1)));
        }

        [Test]
        public void Record_FillsDailyAndWeekly_WeeklyKeepsAcrossDays()
        {
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var daily = new DailyService(new BalanceValues(), save, clock);
            int kill = IndexOf(MissionKind.Kill);

            daily.Record(MissionKind.Kill, 300);
            clock.Local = clock.Local.AddDays(1);
            daily.Record(MissionKind.Kill, 300);

            Assert.AreEqual(600, daily.WeeklyProgress(kill));
            Assert.AreEqual(MissionCatalog.All[0].Target, daily.Progress(0), "the daily kill mission caps at its target");
        }

        [Test]
        public void ClaimWeekly_PaysGemsTicketsStones_OnceAWeek_ResetsMonday()
        {
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var daily = new DailyService(new BalanceValues(), save, clock);
            int summon = IndexOf(MissionKind.Summon);
            MissionDef def = WeeklyMissionCatalog.All[summon];

            Assert.AreEqual(FailReason.Locked, daily.TryClaimWeekly(summon).Reason);
            daily.Record(MissionKind.Summon, def.Target);
            Assert.IsTrue(daily.WeeklyClaimable(summon));
            Assert.IsTrue(daily.TryClaimWeekly(summon).Ok);
            Assert.AreEqual(def.Gems, save.gem, 1e-9);
            Assert.AreEqual(def.GearTickets, save.gearTickets);
            Assert.AreEqual(FailReason.DailyLimit, daily.TryClaimWeekly(summon).Reason);

            clock.Local = new DateTime(2026, 10, 19, 0, 5, 0);
            Assert.AreEqual(0, daily.WeeklyProgress(summon));
            Assert.IsFalse(daily.WeeklyClaimed(summon));
        }

        [Test]
        public void AllChest_OpensWhenTheOthersAreClaimed()
        {
            var save = SaveDataV2.CreateNew();
            var daily = new DailyService(new BalanceValues(), save, new FakeClock());
            int all = IndexOf(MissionKind.All);
            for (int i = 0; i < WeeklyMissionCatalog.Count; i++)
            {
                if (i == all) continue;
                MissionDef def = WeeklyMissionCatalog.All[i];
                daily.Record(def.Kind, def.Target);
                Assert.IsTrue(daily.TryClaimWeekly(i).Ok, def.Id);
            }

            int stonesBefore = save.breakStones;
            Assert.IsTrue(daily.TryClaimWeekly(all).Ok);
            Assert.AreEqual(WeeklyMissionCatalog.All[all].SkillTickets, save.skillTickets);
            Assert.AreEqual(stonesBefore, save.breakStones);
        }
    }
}
