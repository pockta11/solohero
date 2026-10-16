using System;
using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Events;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-173 (L-19): pickup banners run from the event schedule and only reshape the pick inside a grade.</summary>
    public sealed class PickupTests
    {
        private const long Start = 1_800_000_000L;
        private const long Day = 86400L;

        private sealed class FakeClock : IClock
        {
            public long Utc = Start + 3600;

            public long UtcNowSeconds => Utc;

            public DateTime LocalNow => DateTimeOffset.FromUnixTimeSeconds(Utc).UtcDateTime;
        }

        private sealed class FixedPickup : IPickupSource
        {
            public PickupBanner Pet;
            public PickupBanner Skill;

            public PickupBanner ActivePickup(SummonKind kind) => kind == SummonKind.Pet ? Pet : kind == SummonKind.Skill ? Skill : null;

            public long PickupSecondsLeft(SummonKind kind) => ActivePickup(kind) != null ? Day : 0L;
        }

        [Test]
        public void Catalog_PetBannersForEpicAndUp_SkillBannersCoverEveryLine()
        {
            Assert.IsNotNull(PickupCatalog.Find("pickup_pet_phoenix"));
            Assert.IsNull(PickupCatalog.Find("pickup_pet_slime"), "common pets have no banner");
            Assert.IsNotNull(PickupCatalog.Find("pickup_pet_azure"), "D-176: the star whale shares the Ancient grade");
            Assert.IsNotNull(PickupCatalog.Find("pickup_pet_starwhale"));
            Assert.IsNull(PickupCatalog.Find("launch"));
            PickupBanner skill = PickupCatalog.Find("pickup_skill_1");
            Assert.IsNotNull(skill);
            Assert.AreEqual(SummonKind.Skill, skill.Kind);
            Assert.AreEqual(3, skill.Targets.Length, "one legendary of each line");
            foreach (string id in skill.Targets)
            {
                SkillDef def = SkillCatalog.Find(id);
                Assert.AreEqual(Grade.Legendary, def.Grade, id);
                Assert.Less(def.JobTier, 3, id);
            }
        }

        [Test]
        public void Schedule_PickupRunsBesideTheEvent_PerSummonKind()
        {
            var entries = new List<ScheduleEntry>
            {
                new ScheduleEntry { Id = "ev", Template = "launch", StartUtc = Start, EndUtc = Start + 14 * Day },
                new ScheduleEntry { Id = "pk1", Template = "pickup_pet_dragon", StartUtc = Start, EndUtc = Start + 7 * Day },
                new ScheduleEntry { Id = "pk2", Template = "pickup_pet_phoenix", StartUtc = Start + Day, EndUtc = Start + 7 * Day },
                new ScheduleEntry { Id = "pk3", Template = "pickup_skill_2", StartUtc = Start, EndUtc = Start + 3 * Day },
            };
            var clock = new FakeClock();
            var events = new EventService(new BalanceValues(), SaveDataV2.CreateNew(), clock);
            events.SetSchedule(entries);

            Assert.AreEqual("ev", events.Active.Id, "a pickup never takes the event's place");
            Assert.AreEqual("pickup_pet_dragon", events.ActivePickup(SummonKind.Pet).Id);
            Assert.AreEqual("pickup_skill_2", events.ActivePickup(SummonKind.Skill).Id);
            Assert.IsNull(events.ActivePickup(SummonKind.Gear));

            clock.Utc = Start + 2 * Day;
            Assert.AreEqual("pickup_pet_phoenix", events.ActivePickup(SummonKind.Pet).Id, "latest start wins");
            clock.Utc = Start + 4 * Day;
            Assert.IsNull(events.ActivePickup(SummonKind.Skill));
            Assert.AreEqual(3 * Day, events.PickupSecondsLeft(SummonKind.Pet));
            clock.Utc = Start + 7 * Day;
            Assert.IsNull(events.ActivePickup(SummonKind.Pet));
        }

        [Test]
        public void PickIndex_TargetTakesItsShare_OthersSplitTheRest()
        {
            string[] pool = { "a", "b", "c", "d" };
            var banner = new PickupBanner { Id = "pickup_test", Kind = SummonKind.Pet, Targets = new[] { "c" } };
            var rng = new SystemRandom(new Random(4));
            var counts = new int[pool.Length];
            const int n = 40000;
            for (int i = 0; i < n; i++) counts[Pickup.PickIndex(pool, s => s, banner, 70d, rng)]++;

            Assert.AreEqual(0.7, counts[2] / (double)n, 0.015);
            for (int i = 0; i < pool.Length; i++)
                if (i != 2) Assert.AreEqual(0.1, counts[i] / (double)n, 0.015, pool[i]);
            Assert.AreEqual(1.0, Pickup.TargetRate(2d, 50d), 1e-12);
            Assert.AreEqual(1.0 / 3, Pickup.OtherRate(2d, 50d, 4), 1e-12);
        }

        [Test]
        public void PickIndex_TargetNotInPool_StaysUniform()
        {
            string[] pool = { "a", "b" };
            var banner = new PickupBanner { Id = "pickup_test", Kind = SummonKind.Pet, Targets = new[] { "z" } };
            var rng = new SystemRandom(new Random(2));
            int a = 0;
            for (int i = 0; i < 20000; i++) a += Pickup.PickIndex(pool, s => s, banner, 50d, rng) == 0 ? 1 : 0;
            Assert.AreEqual(0.5, a / 20000d, 0.02);
        }

        [Test]
        public void PetSummon_DuringAPickup_GradeRatesHold_TargetLeadsItsGrade()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            PetService.EnsureOwned(save);
            save.highestStage = b.PET_UNLOCK_STAGE;
            save.gold = 1e12;
            save.petPullCount = 1_000_000; // summon level maxed: every grade open
            var pickup = new FixedPickup { Pet = PickupCatalog.Find("pickup_pet_owl") };
            var summon = new PetSummonService(b, GearTableValues.FromBalance(b), new SystemRandom(new Random(8)), pickup);

            int epic = 0, owl = 0;
            for (int k = 0; k < 400; k++)
            {
                foreach (PetPullItem item in summon.TryPullTen(save).Items)
                {
                    if (item.Grade != GearGrade.Epic) continue;
                    epic++;
                    if (item.Id == "owl") owl++;
                }
            }

            Assert.Greater(epic, 100);
            Assert.AreEqual(b.PICKUP_SHARE / 100d, owl / (double)epic, 0.08);
        }

        [Test]
        public void SkillSummon_PickupOfTheHerosLine_IsTheOneBoosted()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.gold = 1e13;
            save.heroLevel = b.JOB_LV_1;
            save.jobId = "mage";
            save.skillPullCount = 1_000_000;
            PickupBanner banner = PickupCatalog.Find("pickup_skill_1");
            string target = null;
            foreach (string id in banner.Targets)
                if (SkillCatalog.Find(id).Line == JobLine.Mage) target = id;
            Assert.IsNotNull(target);
            var summon = new SkillSummonService(b, GachaTableValues.FromBalance(b), new SystemRandom(new Random(6)), new FixedPickup { Skill = banner });

            int legend = 0, hit = 0;
            for (int k = 0; k < 600; k++)
            {
                foreach (SkillPullItem item in summon.TryPullTen(save).Items)
                {
                    if (item.Grade != Grade.Legendary) continue;
                    legend++;
                    if (item.Id == target) hit++;
                }
            }

            Assert.Greater(legend, 60);
            Assert.AreEqual(b.PICKUP_SHARE / 100d, hit / (double)legend, 0.12);
        }
    }
}
