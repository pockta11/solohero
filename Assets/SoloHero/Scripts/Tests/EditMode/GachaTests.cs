using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class GachaTests
    {
        [Test]
        public void Rates_DefaultTable_SumsTo100()
        {
            GachaTableValues table = GachaTableValues.FromBalance(new BalanceValues());

            Assert.AreEqual(100d, table.RatesSum, 1e-9);
        }

        [Test]
        public void TryPull_PityAt100_ForcesLegendary()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            data.pityCount = balance.GACHA_PITY - 1;

            // Slot only — grade roll is skipped on pity.
            var rng = new ScriptedRandom(doubles: new double[0], ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.IsTrue(result.RequestSave);
            Assert.AreEqual(1, result.Items.Length);
            Assert.AreEqual(Grade.Legendary, result.Items[0].Grade);
            Assert.AreEqual(0, data.pityCount);
        }

        [Test]
        public void TryPull_NaturalLegendary_ResetsPity()
        {
            BalanceValues balance = new BalanceValues();
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = balance.GACHA_COST_SINGLE;
            data.pityCount = 40;

            // Slot Sword, then unit sample in Legendary band [0.98, 1).
            var rng = new ScriptedRandom(doubles: new[] { 0.99 }, ints: new[] { 0 });
            GachaService service = CreateService(balance, rng);

            GachaBatchResult result = service.TryPull(data);

            Assert.IsTrue(result.Status.Ok);
            Assert.AreEqual(Grade.Legendary, result.Items[0].Grade);
            Assert.AreEqual(0, data.pityCount);
        }

        [Test]
        public void TryPull_EnoughGold_Costs500()
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
        public void TryPullTen_EnoughGold_Costs4500AndRollsTen()
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

            // Empty queues — fail path must not roll.
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
        public void TryPull_Duplicate_RefundsInsteadOfSecondCopy()
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
            Assert.AreEqual(balance.REFUND_C, result.Items[0].RefundGold, 1e-9);
            Assert.AreEqual(1, data.ownedEquipment.Count);
            Assert.AreEqual(balance.REFUND_C, data.gold, 1e-9);
        }

        [Test]
        public void PickGrade_Boundaries_MapToExpectedGrades()
        {
            GachaTableValues table = GachaTableValues.FromBalance(new BalanceValues());

            Assert.AreEqual(Grade.Common, table.PickGrade(0.0));
            Assert.AreEqual(Grade.Rare, table.PickGrade(0.55));
            Assert.AreEqual(Grade.Epic, table.PickGrade(0.88));
            Assert.AreEqual(Grade.Legendary, table.PickGrade(0.98));
            Assert.AreEqual(Grade.Legendary, table.PickGrade(0.999));
        }

        private static GachaService CreateService(BalanceValues balance, IRandom rng)
        {
            return new GachaService(
                balance,
                GachaTableValues.FromBalance(balance),
                rng,
                BuildCatalog(balance));
        }

        private static GachaEquipmentDef[] BuildCatalog(BalanceValues balance)
        {
            var list = new List<GachaEquipmentDef>(16);
            EquipmentSlot[] slots =
            {
                EquipmentSlot.Sword, EquipmentSlot.Helm, EquipmentSlot.Armor, EquipmentSlot.Boots
            };
            Grade[] grades = { Grade.Common, Grade.Rare, Grade.Epic, Grade.Legendary };

            for (int s = 0; s < slots.Length; s++)
            {
                for (int g = 0; g < grades.Length; g++)
                {
                    Grade grade = grades[g];
                    list.Add(new GachaEquipmentDef(
                        "Equipment_" + slots[s] + "_" + grade,
                        slots[s],
                        grade,
                        RefundFor(balance, grade)));
                }
            }

            return list.ToArray();
        }

        private static double RefundFor(BalanceValues balance, Grade grade)
        {
            switch (grade)
            {
                case Grade.Common: return balance.REFUND_C;
                case Grade.Rare: return balance.REFUND_R;
                case Grade.Epic: return balance.REFUND_E;
                case Grade.Legendary: return balance.REFUND_L;
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
