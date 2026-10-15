using System;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Pass;
using SoloHero.Core.Pets;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;
using SoloHero.Core.Talents;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-163 free season pass and D-164 collection codex.</summary>
    public sealed class PassAndCollectionTests
    {
        private sealed class FakeClock : IClock
        {
            public long Utc;

            public long UtcNowSeconds => Utc;

            public DateTime LocalNow => DateTimeOffset.FromUnixTimeSeconds(Utc).LocalDateTime;
        }

        [Test]
        public void Pass_XpFromMissionsAndKills_ClaimsReachedLevels()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock { Utc = b.PASS_EPOCH_UTC + 10L * 86400L };
            var pass = new PassService(b, save, clock);
            Assert.AreEqual(0, pass.Season);
            Assert.AreEqual(18L * 86400L, pass.SecondsLeft);
            Assert.AreEqual(FailReason.Locked, pass.TryClaimAll(out _).Reason, "nothing reached yet");

            pass.OnMissionClaimed(false);
            pass.OnMissionClaimed(true);
            Assert.AreEqual(b.PASS_XP_DAILY + b.PASS_XP_WEEKLY, pass.Xp);
            pass.Record(MissionKind.Kill, b.PASS_KILLS_PER_XP * 3 + 1);
            pass.Record(MissionKind.Summon, 500);
            Assert.AreEqual(b.PASS_XP_DAILY + b.PASS_XP_WEEKLY + 3, pass.Xp, "kills turn into XP, other actions do not");
            pass.Record(MissionKind.Kill, b.PASS_KILLS_PER_XP - 1);
            Assert.AreEqual(b.PASS_XP_DAILY + b.PASS_XP_WEEKLY + 4, pass.Xp, "the carried kill counts");

            pass.AddXp(b.PASS_XP_PER_LEVEL * 3);
            Assert.AreEqual(3, pass.Level);
            int gearBefore = save.gearTickets;
            Assert.IsTrue(pass.TryClaimAll(out PassPayout paid).Ok);
            Assert.AreEqual(3, pass.Claimed);
            Assert.AreEqual(PassCatalog.At(2).GearTickets, save.gearTickets - gearBefore);
            Assert.AreEqual(PassCatalog.At(3).Gems, paid.Gems);
            Assert.AreEqual(FailReason.Locked, pass.TryClaimAll(out _).Reason);

            pass.AddXp(int.MaxValue / 2);
            Assert.AreEqual(PassCatalog.Levels, pass.Level, "capped at the last level");
            Assert.AreEqual(pass.MaxXp, pass.Xp);
        }

        [Test]
        public void Pass_NewSeason_PaysUnclaimedLevelsAndStartsOver()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock { Utc = b.PASS_EPOCH_UTC + 5L * 86400L };
            var pass = new PassService(b, save, clock);
            pass.AddXp(b.PASS_XP_PER_LEVEL * 2);
            double gems = save.gem;

            clock.Utc += (long)b.PASS_SEASON_DAYS * 86400L;
            Assert.AreEqual(1, pass.Season);
            Assert.AreEqual(0, pass.Xp, "a new season starts over");
            Assert.AreEqual(0, pass.Claimed);
            int expected = PassCatalog.At(1).Gems + PassCatalog.At(2).Gems;
            Assert.AreEqual(gems + expected, save.gem, 1e-9, "the reached levels were paid at the rollover");
            Assert.AreEqual(1, save.passSeason);
        }

        [Test]
        public void Pass_TableHasThirtyLevels_EveryTenthIsBig()
        {
            Assert.AreEqual(30, PassCatalog.Levels);
            for (int level = 1; level <= PassCatalog.Levels; level++)
            {
                Assert.IsNotNull(PassCatalog.At(level));
                if (PassCatalog.IsBig(level)) Assert.Greater(PassCatalog.At(level).Gems, 0, "level " + level);
            }

            Assert.IsNull(PassCatalog.At(0));
            Assert.IsNull(PassCatalog.At(31));
        }

        [Test]
        public void Collection_CompleteGearGrade_AddsItsAtkThroughTheTalentChannel()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            CollectionSet rareGear = Array.Find(CollectionCatalog.All, s => s.Id == "gear_r");
            for (int s = 0; s < GachaCatalog.SlotCount - 1; s++)
                EquipmentLevels.AddOwned(save, GachaCatalog.IdOf((EquipmentSlot)s, GearGrade.Rare));
            CollectionCatalog.Progress(save, rareGear, out int owned, out int total);
            Assert.AreEqual(GachaCatalog.SlotCount - 1, owned);
            Assert.AreEqual(GachaCatalog.SlotCount, total);
            Assert.IsFalse(CollectionCatalog.IsComplete(save, rareGear));
            double atkBefore = CombatLoadout.Effects(b, save).AtkPct;

            EquipmentLevels.AddOwned(save, GachaCatalog.IdOf((EquipmentSlot)(GachaCatalog.SlotCount - 1), GearGrade.Rare));
            Assert.IsTrue(CollectionCatalog.IsComplete(save, rareGear));
            Assert.AreEqual(atkBefore + rareGear.Amount, CombatLoadout.Effects(b, save).AtkPct, 1e-9);
            Assert.AreEqual(rareGear.Amount, CollectionCatalog.Total(save, TalentStat.AtkPct), 1e-9);
        }

        [Test]
        public void Collection_PetAndSkillSets_CountTheirGradeAndTheHerosLine()
        {
            var save = SaveDataV2.CreateNew();
            CollectionSet commonPets = Array.Find(CollectionCatalog.All, s => s.Id == "pet_c");
            CollectionCatalog.Progress(save, commonPets, out _, out int petTotal);
            int expectedPets = 0;
            foreach (PetDef pet in PetCatalog.All)
                if (pet.Grade == GearGrade.Common) expectedPets++;
            Assert.AreEqual(expectedPets, petTotal);

            CollectionSet commonSkills = Array.Find(CollectionCatalog.All, s => s.Id == "skill_c");
            CollectionCatalog.Progress(save, commonSkills, out _, out int beginnerTotal);
            Assert.AreEqual(0, beginnerTotal, "the beginner has no skill sets");
            save.jobId = "mage";
            CollectionCatalog.Progress(save, commonSkills, out int owned, out int total);
            Assert.AreEqual(SkillCatalog.OfLine(SoloHero.Core.Jobs.JobLine.Mage, Grade.Common).Length, total);
            Assert.AreEqual(0, owned);
            foreach (SkillDef def in SkillCatalog.OfLine(SoloHero.Core.Jobs.JobLine.Mage, Grade.Common)) SkillBook.AddOwned(save, def.Id);
            Assert.IsTrue(CollectionCatalog.IsComplete(save, commonSkills));
            Assert.AreEqual(1, CollectionCatalog.CompleteCount(save));
        }
    }
}
