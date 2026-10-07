using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class GachaTests
    {
        [Test]
        public void Rates_DefaultTables_SumTo100()
        {
            Assert.AreEqual(100d, GearTableValues.FromBalance(new BalanceValues()).RatesSum, 1e-9);
            Assert.AreEqual(100d, GachaTableValues.FromBalance(new BalanceValues()).RatesSum, 1e-9);
        }

        [Test]
        public void GearRates_EveryGradeRarerThanTheOneBelow_TopTiersExtremelyRare()
        {
            // D-113: seven grades, each rarer than the last; Mythic and Ancient together stay under 0.1 %.
            GearTableValues table = GearTableValues.FromBalance(new BalanceValues());

            Assert.AreEqual(GachaCatalog.GradeCount, table.Rates.Length);
            for (int g = 1; g < table.Rates.Length; g++)
                Assert.Less(table.Rates[g], table.Rates[g - 1], ((GearGrade)g).ToString());
            Assert.Less(table.Rate(GearGrade.Mythic) + table.Rate(GearGrade.Ancient), 0.1d);
            Assert.Less(table.Rate(GearGrade.Legendary), 1d);
        }

        [Test]
        public void TryPull_PityCeiling_ForcesLegendary()
        {
            BalanceValues balance = AllGradesOpen();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            data.pityCount = balance.GEAR_PITY - 1;

            // Slot only - grade roll is skipped on pity.
            var rng = new ScriptedRandom(doubles: new double[0], ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.IsTrue(result.RequestSave);
            Assert.AreEqual(1, result.Items.Length);
            Assert.AreEqual(GearGrade.Legendary, result.Items[0].Grade);
            Assert.AreEqual(0, data.pityCount);
        }

        [Test]
        public void TryPull_NaturalLegendary_ResetsPity()
        {
            BalanceValues balance = AllGradesOpen();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            data.pityCount = 40;

            // Slot Sword, then unit sample in the Legendary band [0.995, 0.9995).
            var rng = new ScriptedRandom(doubles: new[] { 0.996 }, ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.AreEqual(GearGrade.Legendary, result.Items[0].Grade);
            Assert.AreEqual(0, data.pityCount);
        }

        [TestCase(0.9996, GearGrade.Mythic)]
        [TestCase(0.99999, GearGrade.Ancient)]
        public void TryPull_NaturalAboveLegendary_AlsoResetsPity(double sample, GearGrade expected)
        {
            BalanceValues balance = AllGradesOpen();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            data.pityCount = 150;
            GachaService service = CreateService(balance, new ScriptedRandom(doubles: new[] { sample }, ints: new[] { 1 }));

            GachaBatchResult result = service.TryPull(data);

            Assert.AreEqual(expected, result.Items[0].Grade);
            Assert.AreEqual(EquipmentSlot.Helm, result.Items[0].Slot);
            Assert.AreEqual(0, data.pityCount);
        }

        [Test]
        public void TryPull_NaturalEpic_KeepsPityCounting()
        {
            BalanceValues balance = AllGradesOpen();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            data.pityCount = 40;
            GachaService service = CreateService(balance, new ScriptedRandom(doubles: new[] { 0.98 }, ints: new[] { 0 }));

            GachaBatchResult result = service.TryPull(data);

            Assert.AreEqual(GearGrade.Epic, result.Items[0].Grade);
            Assert.AreEqual(41, data.pityCount);
        }

        [Test]
        public void TryPull_HigherGrade_ReplacesEquippedLowerGrade()
        {
            // The seven-grade order decides auto-equip: an Uncommon replaces a Common, a Common never replaces it back.
            BalanceValues balance = AllGradesOpen();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE * 2d;
            EquipmentLevels.AddOwned(data, "Equipment_Sword_Common");
            data.equippedSword = "Equipment_Sword_Common";
            GachaService service = CreateService(balance, new ScriptedRandom(doubles: new[] { 0.7, 0.0 }, ints: new[] { 0, 0 }));

            GachaPullItem uncommon = service.TryPull(data).Items[0];
            GachaPullItem common = service.TryPull(data).Items[0];

            Assert.AreEqual(GearGrade.Uncommon, uncommon.Grade);
            Assert.IsTrue(uncommon.AutoEquipped);
            Assert.IsFalse(common.AutoEquipped);
            Assert.AreEqual("Equipment_Sword_Uncommon", data.equippedSword);
        }

        [Test]
        public void TryPull_EnoughGold_CostsSinglePrice()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = 1000d;

            var rng = new ScriptedRandom(doubles: new[] { 0.0 }, ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.AreEqual(1000d - balance.GACHA_COST_SINGLE, data.gold, 1e-9);
            Assert.AreEqual(1, data.totalPullCount);
        }

        [Test]
        public void TryPullTen_EnoughGold_CostsTenPriceAndRollsTen()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = 10000d;

            var doubles = new double[10];
            var ints = new int[10];
            for (int i = 0; i < 10; i++)
            {
                ints[i] = i % 4;
                if (i < 4) doubles[i] = 0.0;
                else if (i < 8) doubles[i] = 0.60;
                else doubles[i] = 0.90;
            }

            var rng = new ScriptedRandom(doubles, ints);
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPullTen(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.AreEqual(10, result.Items.Length);
            Assert.AreEqual(10000d - balance.GACHA_COST_TEN, data.gold, 1e-9);
            Assert.AreEqual(10, data.totalPullCount);
        }

        [Test]
        public void TryPull_NotEnoughGold_ReturnsFailWithNoMutation()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE - 1d;
            data.pityCount = 12;
            data.totalPullCount = 3;
            data.ownedEquipment.Add("Equipment_Sword_Common");

            var rng = new ScriptedRandom(doubles: new[] { 0.0 }, ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsFalse(result.Status.Ok);
            Assert.AreEqual(FailReason.NotEnoughGold, result.Status.Reason);
            Assert.IsFalse(result.RequestSave);
            Assert.AreEqual(0, result.Items.Length);
            Assert.AreEqual(balance.GACHA_COST_SINGLE - 1d, data.gold, 1e-9);
            Assert.AreEqual(12, data.pityCount);
            Assert.AreEqual(3, data.totalPullCount);
            Assert.AreEqual(1, data.ownedEquipment.Count);
        }

        [Test]
        public void TryPullTenWithGem_NotEnoughGem_ReturnsFailWithNoMutation()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = 1000d;
            data.gem = balance.GACHA_COST_TEN_GEM - 1d;
            data.pityCount = 12;
            data.totalPullCount = 3;
            data.ownedEquipment.Add("Equipment_Sword_Common");
            data.equippedSword = "Equipment_Sword_Common";

            // Empty queues - fail path must not roll.
            var rng = new ScriptedRandom(doubles: new double[0], ints: new int[0]);
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPullTenWithGem(data);

            Assert.IsFalse(result.Status.Ok);
            Assert.AreEqual(FailReason.NotEnoughGem, result.Status.Reason);
            Assert.IsFalse(result.RequestSave);
            Assert.AreEqual(0, result.Items.Length);
            Assert.AreEqual(1000d, data.gold, 1e-9);
            Assert.AreEqual(balance.GACHA_COST_TEN_GEM - 1d, data.gem, 1e-9);
            Assert.AreEqual(12, data.pityCount);
            Assert.AreEqual(3, data.totalPullCount);
            Assert.AreEqual(1, data.ownedEquipment.Count);
            Assert.AreEqual("Equipment_Sword_Common", data.equippedSword);
        }

        [Test]
        public void TryPullTenWithGem_EnoughGem_Costs200AndRollsTen()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = 0d;
            data.gem = balance.GACHA_COST_TEN_GEM;

            var doubles = new double[10];
            var ints = new int[10];
            for (int i = 0; i < 10; i++)
            {
                ints[i] = i % 4;
                if (i < 4) doubles[i] = 0.0;
                else if (i < 8) doubles[i] = 0.60;
                else doubles[i] = 0.90;
            }

            var rng = new ScriptedRandom(doubles, ints);
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPullTenWithGem(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.AreEqual(10, result.Items.Length);
            Assert.AreEqual(0d, data.gold, 1e-9);
            Assert.AreEqual(0d, data.gem, 1e-9);
            Assert.AreEqual(10, data.totalPullCount);
        }

        [Test]
        public void TryPull_Duplicate_EnhancesOwnedCopy()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            string id = "Equipment_Sword_Common";
            data.ownedEquipment.Add(id);

            var rng = new ScriptedRandom(doubles: new[] { 0.0 }, ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.IsTrue(result.Items[0].WasDuplicate);
            Assert.AreEqual(1, result.Items[0].EnhancedLevel);
            Assert.AreEqual(0d, result.Items[0].RefundGold, 1e-9);
            Assert.AreEqual(1, data.ownedEquipment.Count);
            Assert.AreEqual(1, EquipmentLevels.Get(data, id));
            Assert.AreEqual(0d, data.gold, 1e-9);
        }

        [Test]
        public void TryPull_DuplicateAtMaxLevel_RefundsGold()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            string id = "Equipment_Sword_Common";
            EquipmentLevels.AddOwned(data, id);
            EquipmentLevels.Set(data, id, balance.EQUIP_MAX_LEVEL);

            var rng = new ScriptedRandom(doubles: new[] { 0.0 }, ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Items[0].WasDuplicate);
            Assert.AreEqual(0, result.Items[0].EnhancedLevel);
            Assert.AreEqual(balance.REFUND_C, result.Items[0].RefundGold, 1e-9);
            Assert.AreEqual(balance.EQUIP_MAX_LEVEL, EquipmentLevels.Get(data, id));
            Assert.AreEqual(balance.REFUND_C, data.gold, 1e-9);
        }

        [Test]
        public void PickGrade_SkillTableBoundaries_MapToExpectedGrades()
        {
            GachaTableValues table = GachaTableValues.FromBalance(new BalanceValues());

            Assert.AreEqual(Grade.Common, table.PickGrade(0.0));
            Assert.AreEqual(Grade.Rare, table.PickGrade(0.55));
            Assert.AreEqual(Grade.Epic, table.PickGrade(0.88));
            Assert.AreEqual(Grade.Legendary, table.PickGrade(0.98));
            Assert.AreEqual(Grade.Legendary, table.PickGrade(0.999));
        }

        // Cumulative gear bands: C [0, 60) U [60, 88) R [88, 97) E [97, 99.5) L [99.5, 99.95) M [99.95, 99.995) A [99.995, 100).
        [TestCase(0.0, GearGrade.Common)]
        [TestCase(0.5999, GearGrade.Common)]
        [TestCase(0.6001, GearGrade.Uncommon)]
        [TestCase(0.8799, GearGrade.Uncommon)]
        [TestCase(0.8801, GearGrade.Rare)]
        [TestCase(0.9699, GearGrade.Rare)]
        [TestCase(0.9701, GearGrade.Epic)]
        [TestCase(0.9949, GearGrade.Epic)]
        [TestCase(0.9951, GearGrade.Legendary)]
        [TestCase(0.9994, GearGrade.Legendary)]
        [TestCase(0.9996, GearGrade.Mythic)]
        [TestCase(0.99994, GearGrade.Mythic)]
        [TestCase(0.99996, GearGrade.Ancient)]
        [TestCase(0.9999999, GearGrade.Ancient)]
        public void PickGrade_GearTableBands_MapToExpectedGrades(double sample, GearGrade expected)
        {
            Assert.AreEqual(expected, GearTableValues.FromBalance(new BalanceValues()).PickGrade(sample));
        }

        [Test]
        public void Pull_ManyPulls_EverySlotIncludingAccessoriesDrops()
        {
            // D-109: the slot is uniform over all 8 slots, and a new accessory auto-equips into its own field.
            var balance = new BalanceValues();
            var data = SaveDataV2.CreateNew();
            data.gold = 1e9;
            GachaService service = CreateService(balance, new SystemRandom(new System.Random(7)));
            var seen = new int[GachaCatalog.SlotCount];

            for (int i = 0; i < 40; i++)
            {
                GachaBatchResult r = service.TryPullTen(data);
                Assert.IsTrue(r.Status.Ok);
                foreach (GachaPullItem item in r.Items) seen[(int)item.Slot]++;
            }

            for (int s = 0; s < seen.Length; s++)
                Assert.Greater(seen[s], 25, ((EquipmentSlot)s).ToString());
            Assert.IsNotEmpty(data.equippedRing);
            Assert.IsNotEmpty(data.equippedEarring);
            Assert.AreEqual(EquipmentSlot.Ring, GachaCatalog.Standard(balance)[(int)EquipmentSlot.Ring * GachaCatalog.GradeCount].Slot);
        }

        /// <summary>D-121 / D-123 gates off, so a new summon uses the base table's bands (the gates have their own tests).</summary>
        internal static BalanceValues AllGradesOpen() => new BalanceValues
        {
            SUMMON_OPEN_U = 1, SUMMON_OPEN_R = 1, SUMMON_OPEN_E = 1, SUMMON_OPEN_L = 1, SUMMON_OPEN_M = 1, SUMMON_OPEN_A = 1,
            SKILL_SUMMON_OPEN_R = 1, SKILL_SUMMON_OPEN_E = 1, SKILL_SUMMON_OPEN_L = 1
        };

        private static GachaService CreateService(BalanceValues balance, IRandom rng)
        {
            return new GachaService(
                balance,
                GearTableValues.FromBalance(balance),
                rng,
                BuildCatalog(balance));
        }

        private static GachaEquipmentDef[] BuildCatalog(BalanceValues balance)
        {
            var list = new List<GachaEquipmentDef>(56);
            EquipmentSlot[] slots =
            {
                EquipmentSlot.Sword, EquipmentSlot.Helm, EquipmentSlot.Armor, EquipmentSlot.Boots,
                EquipmentSlot.Gloves, EquipmentSlot.Necklace, EquipmentSlot.Ring, EquipmentSlot.Earring
            };
            GearGrade[] grades =
            {
                GearGrade.Common, GearGrade.Uncommon, GearGrade.Rare, GearGrade.Epic, GearGrade.Legendary, GearGrade.Mythic,
                GearGrade.Ancient
            };

            for (int s = 0; s < slots.Length; s++)
            {
                for (int g = 0; g < grades.Length; g++)
                {
                    GearGrade grade = grades[g];
                    list.Add(new GachaEquipmentDef(
                        "Equipment_" + slots[s] + "_" + grade,
                        slots[s],
                        grade,
                        RefundFor(balance, grade)));
                }
            }

            return list.ToArray();
        }

        private static double RefundFor(BalanceValues balance, GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.Common: return balance.REFUND_C;
                case GearGrade.Uncommon: return balance.REFUND_U;
                case GearGrade.Rare: return balance.REFUND_R;
                case GearGrade.Epic: return balance.REFUND_E;
                case GearGrade.Legendary: return balance.REFUND_L;
                case GearGrade.Mythic: return balance.REFUND_M;
                case GearGrade.Ancient: return balance.REFUND_A;
                default: return 0d;
            }
        }

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

            public int Next(int maxExclusive) => _ints.Dequeue();
        }
    }
}
