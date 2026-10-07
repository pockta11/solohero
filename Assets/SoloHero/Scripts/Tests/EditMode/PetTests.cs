using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-114 pets (D-102 companions): catalog, ownership and migration, levels, summon, owned effect, combat.</summary>
    public sealed class PetTests
    {
        private sealed class FixedRandom : IRandom
        {
            public double NextDouble() => 0.99d;

            public int Next(int maxExclusive) => 0;
        }

        /// <summary>Hands out queued values; Next answers come modulo the bound.</summary>
        private sealed class ScriptedRandom : IRandom
        {
            private readonly Queue<double> _doubles;
            private readonly Queue<int> _ints;

            public ScriptedRandom(double[] doubles, int[] ints)
            {
                _doubles = new Queue<double>(doubles);
                _ints = new Queue<int>(ints);
            }

            public double NextDouble() => _doubles.Dequeue();

            public int Next(int maxExclusive) => _ints.Dequeue() % maxExclusive;
        }

        /// <summary>A new save (the slime only) that has since beaten the first boss, so the summon is open.</summary>
        private static SaveDataV2 Unlocked(BalanceValues b)
        {
            SaveDataV2 save = SaveDataV2.CreateNew();
            PetService.EnsureOwned(save);
            save.highestStage = b.PET_UNLOCK_STAGE;
            return save;
        }

        private static PetSummonService Summon(BalanceValues b, IRandom rng) =>
            new PetSummonService(b, GearTableValues.FromBalance(b), rng);

        [Test]
        public void Catalog_SixteenPets_FewerAtEachHigherGrade_KeepsTheD102Indices()
        {
            Assert.AreEqual(16, PetCatalog.Count);
            int[] perGrade = { 3, 3, 3, 3, 2, 1, 1 };
            for (int g = 0; g < GearGrades.Count; g++)
                Assert.AreEqual(perGrade[g], PetCatalog.OfGrade((GearGrade)g).Length, ((GearGrade)g).ToString());

            var ids = new HashSet<string>();
            foreach (PetDef def in PetCatalog.All)
            {
                Assert.IsTrue(ids.Add(def.Id), def.Id);
                Assert.Greater(def.AttackMult, 0d, def.Id);
                Assert.Greater(def.Interval, 0f, def.Id);
            }

            // Saves index the levels by catalog order: the four D-102 companions stay first.
            Assert.AreEqual(new[] { "slime", "wisp", "owl", "dragon" },
                new[] { PetCatalog.All[0].Id, PetCatalog.All[1].Id, PetCatalog.All[2].Id, PetCatalog.All[3].Id });
        }

        [Test]
        public void Catalog_HigherGradesHitHarder()
        {
            for (int g = 1; g < GearGrades.Count; g++)
            {
                double best = 0d, worstAbove = double.MaxValue;
                foreach (PetDef def in PetCatalog.OfGrade((GearGrade)(g - 1))) best = System.Math.Max(best, def.DamagePerSecond);
                foreach (PetDef def in PetCatalog.OfGrade((GearGrade)g)) worstAbove = System.Math.Min(worstAbove, def.DamagePerSecond);
                Assert.Greater(worstAbove, best * 0.95d, ((GearGrade)g).ToString());
            }
        }

        [Test]
        public void EnsureOwned_NewSave_OwnsAndEquipsTheSlime()
        {
            SaveDataV2 save = SaveDataV2.CreateNew();

            PetService.EnsureOwned(save);

            CollectionAssert.AreEqual(new[] { "slime" }, save.petOwned);
            Assert.AreEqual("slime", save.companionEquipped);
        }

        [Test]
        public void EnsureOwned_OldSave_KeepsUnlockedCompanionsTheirLevelsAndTheEquippedOne()
        {
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.highestStage = 25;
            save.companionEquipped = "owl";
            save.companionLevels = new List<int> { 4, 7, 3 };
            save.petOwned = new List<string>();

            PetService.EnsureOwned(save);

            CollectionAssert.AreEqual(new[] { "slime", "wisp", "owl" }, save.petOwned);
            Assert.AreEqual("owl", save.companionEquipped);
            Assert.AreEqual(7, PetService.Level(save, PetCatalog.IndexOf("wisp")));

            // Runs once: later stage clears do not hand out pets any more.
            save.highestStage = 40;
            PetService.EnsureOwned(save);
            Assert.IsFalse(save.petOwned.Contains("dragon"));
        }

        [Test]
        public void Pets_EquipOnlyOwned_LevelWithGold()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Unlocked(b);
            var pets = new PetService(save, b);
            int wisp = PetCatalog.IndexOf("wisp");

            Assert.AreEqual(FailReason.Locked, pets.TryEquip(wisp).Reason);
            Assert.AreEqual(FailReason.Locked, pets.TryLevelUp(wisp).Reason);
            save.petOwned.Add("wisp");
            Assert.IsTrue(pets.TryEquip(wisp).Ok);
            Assert.AreEqual(wisp, pets.EquippedIndex);

            double cost = pets.LevelCost(wisp);
            Assert.AreEqual(Formulas.PetLevelCost(b, GearGrade.Rare, 1), cost);
            Assert.AreEqual(FailReason.NotEnoughGold, pets.TryLevelUp(wisp).Reason);
            save.gold = cost;
            Assert.IsTrue(pets.TryLevelUp(wisp).Ok);
            Assert.AreEqual(2, pets.Level(wisp));
            Assert.AreEqual(0d, save.gold, 1e-9);
        }

        [Test]
        public void OwnedBonus_EveryOwnedPet_ScalesWithEnhanceAndLevel()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Unlocked(b);
            save.petOwned.Add("owl");
            int owl = PetCatalog.IndexOf("owl");
            PetService.SetEnhance(save, owl, 2);
            while (save.companionLevels.Count <= owl) save.companionLevels.Add(1);
            save.companionLevels[owl] = 3;

            double expected = (b.PET_OWNED_ATK_C
                + b.PET_OWNED_ATK_E * System.Math.Pow(1d + b.PET_ENHANCE_GAIN, 2) * (1d + b.PET_LEVEL_OWNED_GAIN * 2)) / 100d;

            Assert.AreEqual(expected, PetService.OwnedAtkBonus(b, save), 1e-12);
            double bare = CombatLoadout.ComputeStats(b, SaveDataV2.CreateNew()).Atk;
            Assert.Greater(CombatLoadout.ComputeStats(b, save).Atk, bare);
        }

        [Test]
        public void Summon_LockedUntilTheFirstBoss()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.gold = 1e9;
            save.gem = 1e9;
            PetSummonService summon = Summon(b, new FixedRandom());

            Assert.AreEqual(FailReason.Locked, summon.TryPull(save).Status.Reason);
            Assert.AreEqual(FailReason.Locked, summon.TryPullTen(save).Status.Reason);
            Assert.AreEqual(FailReason.Locked, summon.TryPullTenWithGem(save).Status.Reason);
            Assert.AreEqual(1e9, save.gold, 1e-6);
        }

        [Test]
        public void Summon_Costs_AndCountsItsOwnPity()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Unlocked(b);
            save.gold = b.PET_SUMMON_COST_TEN + b.PET_SUMMON_COST_SINGLE;
            save.pityCount = 7;
            PetSummonService summon = Summon(b, new SystemRandom(new System.Random(3)));

            Assert.IsTrue(summon.TryPullTen(save).Status.Ok);
            Assert.IsTrue(summon.TryPull(save).Status.Ok);

            Assert.AreEqual(0d, save.gold, 1e-6);
            Assert.AreEqual(11, save.petPullCount);
            Assert.AreEqual(7, save.pityCount, "gear pity untouched");
            Assert.AreEqual(FailReason.NotEnoughGold, summon.TryPull(save).Status.Reason);
        }

        [Test]
        public void Summon_PityCeiling_GivesALegendaryPet_AndResets()
        {
            BalanceValues b = GachaTests.AllGradesOpen();
            SaveDataV2 save = Unlocked(b);
            save.gold = b.PET_SUMMON_COST_SINGLE;
            save.petPityCount = b.GEAR_PITY - 1;

            PetSummonResult r = Summon(b, new ScriptedRandom(new double[0], new[] { 1 })).TryPull(save);

            Assert.AreEqual(GearGrade.Legendary, r.Items[0].Grade);
            Assert.AreEqual(PetCatalog.OfGrade(GearGrade.Legendary)[1].Id, r.Items[0].Id);
            Assert.AreEqual(0, save.petPityCount);
        }

        [Test]
        public void Summon_NewPetOutranking_IsEquipped_DuplicateEnhances_MaxRefunds()
        {
            BalanceValues b = GachaTests.AllGradesOpen();
            SaveDataV2 save = Unlocked(b);
            save.gold = b.PET_SUMMON_COST_SINGLE * 3;
            // Uncommon band (0.6 - 0.88): the first uncommon pet, three times.
            PetSummonService summon = Summon(b, new ScriptedRandom(new[] { 0.7, 0.7, 0.7 }, new[] { 0, 0, 0 }));
            string id = PetCatalog.OfGrade(GearGrade.Uncommon)[0].Id;
            int index = PetCatalog.IndexOf(id);

            PetPullItem first = summon.TryPull(save).Items[0];
            Assert.IsTrue(first.WasNew);
            Assert.IsTrue(first.AutoEquipped, "an uncommon outranks the common slime");
            Assert.AreEqual(id, save.companionEquipped);

            PetPullItem second = summon.TryPull(save).Items[0];
            Assert.IsFalse(second.WasNew);
            Assert.AreEqual(1, second.EnhancedLevel);
            Assert.AreEqual(1, PetService.Enhance(save, index));

            PetService.SetEnhance(save, index, b.PET_MAX_ENHANCE);
            PetPullItem third = summon.TryPull(save).Items[0];
            Assert.AreEqual(b.PET_REFUND_U, third.RefundGold, 1e-9);
            Assert.AreEqual(b.PET_REFUND_U, save.gold, 1e-9);
        }

        [Test]
        public void Summon_LowerGradeNewPet_DoesNotReplaceTheEquippedOne()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Unlocked(b);
            save.petOwned.Add("owl");
            save.companionEquipped = "owl";
            save.gold = b.PET_SUMMON_COST_SINGLE;

            PetPullItem item = Summon(b, new ScriptedRandom(new[] { 0.0 }, new[] { 1 })).TryPull(save).Items[0];

            Assert.AreEqual(GearGrade.Common, item.Grade);
            Assert.IsFalse(item.AutoEquipped);
            Assert.AreEqual("owl", save.companionEquipped);
        }

        [Test]
        public void Refunds_StayBelowThePullPriceOnAverage()
        {
            // Guards the D-114 money-printer bug: a maxed collection must not pay out more than a pull costs, even at
            // the top summon level (D-115), where the rare grades with the big refunds come twice as often.
            var b = new BalanceValues();
            GearTableValues table = GearTableValues.FromBalance(b).AtLevel(b.SUMMON_LV_MAX);
            double expected = 0d;
            for (int g = 0; g < GearGrades.Count; g++)
                expected += table.Rates[g] / 100d * Formulas.PetRefund(b, (GearGrade)g);
            Assert.Less(expected, b.PET_SUMMON_COST_TEN / 10d * 0.5d);
        }

        [Test]
        public void EquippedPet_AttacksInCombat_WithItsHitKind()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            PetService.EnsureOwned(save);
            var runner = new StageRunner(b, new FixedRandom(), new HeroStats(1e6, 1d, 1e4, b.ATKSPD_BASE, 0d), save);
            CombatLoadout.Apply(runner, b, save);
            int petHits = 0;
            runner.World.HitLanded += (target, amount, kind) => { if (kind == HitKind.Pet) petHits++; };
            runner.Begin(1);
            for (int i = 0; i < 200; i++) runner.Tick(0.05f);
            Assert.Greater(petHits, 2, "the slime attacks every 2 s once enemies are in range");
        }

        [Test]
        public void UnownedEquippedId_DoesNotFight()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.companionEquipped = "dragon";
            var runner = new StageRunner(b, new FixedRandom(), new HeroStats(1e6, 1d, 1e4, b.ATKSPD_BASE, 0d), save);

            CombatLoadout.Apply(runner, b, save);

            Assert.IsNull(runner.Pet.Def);
        }
    }
}
