using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-078: skill collection, slots, level-up, owned effect and skill summon.</summary>
    public sealed class SkillCollectionTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;
            public void RequestSave() => RequestCount++;
        }

        private sealed class ScriptedRandom : IRandom
        {
            private readonly double _unit;
            private readonly int _index;

            public ScriptedRandom(double unit, int index)
            {
                _unit = unit;
                _index = index;
            }

            public double NextDouble() => _unit;
            public int Next(int maxExclusive) => _index % maxExclusive;
        }

        private static SkillSummonService Summon(BalanceValues b, double unit, int index = 0) =>
            new SkillSummonService(b, GachaTableValues.FromBalance(b), new ScriptedRandom(unit, index));

        [Test]
        public void EnsureStarters_NewSave_OwnsAndEquipsThreeStarters()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();

            Assert.IsTrue(SkillBook.EnsureStarters(save, b));

            Assert.AreEqual(3, save.ownedSkills.Count);
            Assert.AreEqual(b.SKILL_SLOT_COUNT, save.equippedSkills.Count);
            for (int i = 0; i < 3; i++) Assert.AreEqual(SkillCatalog.StarterIds[i], save.equippedSkills[i]);
            Assert.AreEqual("", save.equippedSkills[3]);
            Assert.IsFalse(SkillBook.EnsureStarters(save, b), "second run changes nothing");
        }

        [Test]
        public void EnsureStarters_OldSave_KeepsFixedSkillLevels()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.skillLevel1 = 4;
            save.skillLevel2 = 0;
            save.skillLevel3 = 7;

            SkillBook.EnsureStarters(save, b);

            Assert.AreEqual(4, SkillBook.GetLevel(save, SkillCatalog.PowerStrike));
            Assert.AreEqual(1, SkillBook.GetLevel(save, SkillCatalog.Whirlwind));
            Assert.AreEqual(7, SkillBook.GetLevel(save, SkillCatalog.BattleCry));
        }

        [Test]
        public void EnsureStarters_ClearsUnknownAndDuplicateSlots()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            SkillBook.EnsureStarters(save, b);
            save.equippedSkills[3] = "no_such_skill";
            save.equippedSkills[4] = SkillCatalog.PowerStrike;

            Assert.IsTrue(SkillBook.EnsureStarters(save, b));

            Assert.AreEqual("", save.equippedSkills[3]);
            Assert.AreEqual("", save.equippedSkills[4]);
            Assert.AreEqual(SkillCatalog.PowerStrike, save.equippedSkills[0]);
        }

        [Test]
        public void TryLevelUp_PaysGradeCostAndSaves()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var requester = new FakeSaveRequester();
            var service = new SkillService(save, b, requester);
            double cost = Formulas.SkillUpgradeCost(b, Grade.Common, 1);
            save.gold = cost;

            Assert.IsTrue(service.TryLevelUp(SkillCatalog.PowerStrike).Ok);

            Assert.AreEqual(2, service.Level(SkillCatalog.PowerStrike));
            Assert.AreEqual(0d, save.gold, 1e-9);
            Assert.AreEqual(1, requester.RequestCount);
            Assert.AreEqual(b.SKILL_UPG_BASE_C, cost, 1e-9);
        }

        [Test]
        public void TryLevelUp_NotOwnedNotEnoughGoldOrMax_Fails()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var service = new SkillService(save, b);

            Assert.AreEqual(FailReason.Locked, service.TryLevelUp("meteor").Reason);
            save.gold = 0d;
            Assert.AreEqual(FailReason.NotEnoughGold, service.TryLevelUp(SkillCatalog.PowerStrike).Reason);
            SkillBook.SetLevel(save, SkillCatalog.PowerStrike, b.SKILL_MAX_LEVEL);
            save.gold = 1e12;
            Assert.AreEqual(FailReason.MaxLevel, service.TryLevelUp(SkillCatalog.PowerStrike).Reason);
        }

        [Test]
        public void TryEquip_FirstEmptyUnlockedSlot_ThenSlotsFull()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.SKILL_UNLOCK_LV_4;
            var service = new SkillService(save, b);
            SkillBook.AddOwned(save, "meteor");
            SkillBook.AddOwned(save, "thunder");

            Assert.IsTrue(service.TryEquip("meteor").Ok);
            Assert.AreEqual(3, SkillBook.SlotOf(save, "meteor"));
            Assert.AreEqual(FailReason.SlotsFull, service.TryEquip("thunder").Reason);

            Assert.IsTrue(service.TryEquip("thunder", 0).Ok);
            Assert.AreEqual("thunder", SkillBook.EquippedAt(save, 0));
            Assert.AreEqual(-1, SkillBook.SlotOf(save, SkillCatalog.PowerStrike));
        }

        [Test]
        public void TryEquip_LockedSlotOrUnowned_ReturnsLocked()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var service = new SkillService(save, b);
            SkillBook.AddOwned(save, "meteor");

            Assert.AreEqual(FailReason.Locked, service.TryEquip("meteor", 5).Reason);
            Assert.AreEqual(FailReason.Locked, service.TryEquip("judgement", 0).Reason);
        }

        [Test]
        public void TryEquip_EquippedSkillToOtherSlot_Swaps()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = 99;
            var service = new SkillService(save, b);

            Assert.IsTrue(service.TryEquip(SkillCatalog.BattleCry, 0).Ok);

            Assert.AreEqual(SkillCatalog.BattleCry, SkillBook.EquippedAt(save, 0));
            Assert.AreEqual(SkillCatalog.PowerStrike, SkillBook.EquippedAt(save, 2));
        }

        [Test]
        public void AutoEquip_PicksHighestGradeThenLevelIntoUnlockedSlots()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.SKILL_UNLOCK_LV_2;
            var service = new SkillService(save, b);
            SkillBook.AddOwned(save, "judgement");
            SkillBook.SetLevel(save, SkillCatalog.BattleCry, 5);

            service.AutoEquip();

            Assert.AreEqual("judgement", SkillBook.EquippedAt(save, 0));
            Assert.AreEqual(SkillCatalog.BattleCry, SkillBook.EquippedAt(save, 1));
            Assert.AreEqual("", SkillBook.EquippedAt(save, 2), "locked slots stay empty");
        }

        [Test]
        public void OwnedAtkBonus_SumsEveryOwnedSkillWithLevelScale()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            SkillBook.EnsureStarters(save, b);
            SkillBook.AddOwned(save, "judgement", 10);

            double expected = 3d * b.SKILL_OWNED_ATK_C / 100d + b.SKILL_OWNED_ATK_L / 100d * 1.9d;
            Assert.AreEqual(expected, SkillService.OwnedAtkBonus(b, save), 1e-9);

            HeroStats with = CombatLoadout.ComputeStats(b, save);
            save.ownedSkills.Clear();
            save.ownedSkillLevels.Clear();
            HeroStats without = CombatLoadout.ComputeStats(b, save);
            Assert.AreEqual(without.Atk * (1d + expected), with.Atk, 1e-9);
        }

        [Test]
        public void Apply_LockedSlotsCarryNoSkill()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            SkillBook.EnsureStarters(save, b);
            var runner = new StageRunner(b, new SystemRandom(new System.Random(1)), CombatLoadout.ComputeStats(b, save), save);

            CombatLoadout.Apply(runner, b, save);
            Assert.IsNotNull(runner.Skills.DefAt(0));
            Assert.IsNull(runner.Skills.DefAt(1));
            Assert.IsNull(runner.Skills.DefAt(2));

            save.heroLevel = b.SKILL_UNLOCK_LV_3;
            CombatLoadout.Apply(runner, b, save);
            Assert.AreEqual(SkillCatalog.Whirlwind, runner.Skills.DefAt(1).Id);
            Assert.AreEqual(SkillCatalog.BattleCry, runner.Skills.DefAt(2).Id);
        }

        [Test]
        public void Summon_NewSkill_OwnedAtLevelOneAndAutoEquippedIntoEmptySlot()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.heroLevel = b.SKILL_UNLOCK_LV_4;
            save.gold = b.SKILL_SUMMON_COST_SINGLE;
            SkillSummonService summon = Summon(b, 0.995d);

            SkillSummonResult r = summon.TryPull(save);

            Assert.IsTrue(r.Status.Ok);
            Assert.AreEqual(Grade.Legendary, r.Items[0].Grade);
            Assert.IsTrue(r.Items[0].WasNew);
            Assert.IsTrue(r.Items[0].AutoEquipped);
            Assert.AreEqual(r.Items[0].Id, SkillBook.EquippedAt(save, 3));
            Assert.AreEqual(0d, save.gold, 1e-9);
            Assert.AreEqual(0, save.skillPityCount, "a Legendary resets pity");
        }

        [Test]
        public void Summon_Duplicate_LevelsUpThenRefundsAtMax()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            SkillSummonService summon = Summon(b, 0.0d);
            string id = SkillCatalog.OfGrade(Grade.Common)[0].Id;
            SkillBook.EnsureStarters(save, b);
            int before = SkillBook.GetLevel(save, id);
            save.gold = b.SKILL_SUMMON_COST_SINGLE;

            SkillPullItem dup = summon.TryPull(save).Items[0];
            Assert.IsFalse(dup.WasNew);
            Assert.AreEqual(before + 1, SkillBook.GetLevel(save, id));

            SkillBook.SetLevel(save, id, b.SKILL_MAX_LEVEL);
            save.gold = b.SKILL_SUMMON_COST_SINGLE;
            SkillPullItem refund = summon.TryPull(save).Items[0];
            Assert.AreEqual(b.SKILL_REFUND_C, refund.RefundGold, 1e-9);
            Assert.AreEqual(b.SKILL_REFUND_C, save.gold, 1e-9);
        }

        [Test]
        public void Summon_PityCeilingGuaranteesLegendaryWithOwnCounter()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.skillPityCount = b.GACHA_PITY - 1;
            save.pityCount = 5;
            save.gold = b.SKILL_SUMMON_COST_SINGLE;

            SkillPullItem item = Summon(b, 0.0d).TryPull(save).Items[0];

            Assert.AreEqual(Grade.Legendary, item.Grade);
            Assert.AreEqual(0, save.skillPityCount);
            Assert.AreEqual(5, save.pityCount, "equipment pity is separate");
        }

        [Test]
        public void Summon_TenAndGem_ChargeAndReturnTenItems()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            SkillSummonService summon = Summon(b, 0.5d, 3);
            save.gold = b.SKILL_SUMMON_COST_TEN - 1d;
            Assert.AreEqual(FailReason.NotEnoughGold, summon.TryPullTen(save).Status.Reason);

            save.gold = b.SKILL_SUMMON_COST_TEN;
            Assert.AreEqual(10, summon.TryPullTen(save).Items.Length);
            save.gem = b.SKILL_SUMMON_COST_TEN_GEM;
            Assert.AreEqual(10, summon.TryPullTenWithGem(save).Items.Length);
            Assert.AreEqual(0d, save.gem, 1e-9);
            Assert.AreEqual(20, save.skillPullCount);
        }
    }
}
