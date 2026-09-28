using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Balance;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    public sealed class BalanceSimulatorTests
    {
        private static SimSettings OneDay(int seed)
        {
            SimSettings s = SimSettings.NoAds(seed);
            s.Days = 1;
            return s;
        }

        [Test]
        public void Run_SameSeed_SameResult()
        {
            var balance = new BalanceValues();

            SimReport a = BalanceSimulator.Run(balance, OneDay(3));
            SimReport b = BalanceSimulator.Run(balance, OneDay(3));

            Assert.AreEqual(a.FinalHighestStage, b.FinalHighestStage);
            Assert.AreEqual(a.Stages.Count, b.Stages.Count);
            Assert.AreEqual(a.Days[0].Earned, b.Days[0].Earned, 1e-6);
            Assert.AreEqual(a.Days[0].Spent, b.Days[0].Spent, 1e-6);
        }

        [Test]
        public void Run_OneDay_ClearsFirstStageNearTargetDuration()
        {
            SimReport r = BalanceSimulator.Run(new BalanceValues(), OneDay(1));

            SimStageRow first = r.FindStage(1);
            Assert.IsNotNull(first);
            Assert.AreEqual(1, first.Attempts);
            Assert.That(first.ClearSeconds, Is.InRange(15d, 30d));
            Assert.AreEqual(1, r.Days.Count);
            Assert.Greater(r.Days[0].EarnedStage, 0d);
            Assert.AreEqual(30d * 60d, r.TotalPlaySeconds, 1d);
        }

        [Test]
        public void Run_OneDay_RecordsOfflineClaimsBetweenSessions()
        {
            SimReport r = BalanceSimulator.Run(new BalanceValues(), OneDay(1));

            Assert.AreEqual(3, r.OfflineClaims.Count);
            Assert.Greater(r.Days[0].EarnedOffline, 0d);
        }

        [Test]
        public void Evaluate_Report_ReturnsEveryCheckOnce()
        {
            var balance = new BalanceValues();
            SimReport r = BalanceSimulator.Run(balance, OneDay(1));

            List<SimCheck> checks = BalanceChecks.Evaluate(r, balance);

            var ids = new HashSet<string>();
            for (int i = 0; i < checks.Count; i++)
                Assert.IsTrue(ids.Add(checks[i].Id), "duplicate id " + checks[i].Id);
            Assert.IsTrue(ids.Contains("V-1a"));
            Assert.IsTrue(ids.Contains("V-7"));
            Assert.AreEqual(SimCheckStatus.Info, checks.Find(c => c.Id == "V-1c").Status);
        }

        [Test]
        public void Stages_Csv_HasHeaderAndOneLinePerFirstClear()
        {
            SimReport r = BalanceSimulator.Run(new BalanceValues(), OneDay(1));

            string csv = SimCsv.Stages(r);

            string[] lines = csv.TrimEnd().Split('\n');
            Assert.AreEqual(r.Stages.Count + 1, lines.Length);
            StringAssert.StartsWith("g,label,boss", lines[0]);
        }

        [Test]
        public void Standard_Catalog_HasSixteenUniqueIdsWithGradeRefunds()
        {
            var balance = new BalanceValues();

            GachaEquipmentDef[] defs = GachaCatalog.Standard(balance);

            Assert.AreEqual(16, defs.Length);
            var ids = new HashSet<string>();
            for (int i = 0; i < defs.Length; i++)
                Assert.IsTrue(ids.Add(defs[i].Id));
            Assert.AreEqual("Equipment_Sword_Legendary", GachaCatalog.IdOf(EquipmentSlot.Sword, Grade.Legendary));
            Assert.AreEqual(balance.REFUND_E, GachaCatalog.RefundOf(balance, Grade.Epic), 1e-9);
        }

        [Test]
        public void ComputeStats_LegendarySword_MultipliesAtk()
        {
            var balance = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            double bare = CombatLoadout.ComputeStats(balance, save).Atk;
            save.ownedEquipment.Add(GachaCatalog.IdOf(EquipmentSlot.Sword, Grade.Legendary));
            save.equippedSword = GachaCatalog.IdOf(EquipmentSlot.Sword, Grade.Legendary);

            double equipped = CombatLoadout.ComputeStats(balance, save).Atk;

            Assert.AreEqual(bare * balance.SWORD_ATK_L, equipped, 1e-9);
        }

        [Test]
        public void ComputeStats_EnhancedSword_ScalesGradeBonus()
        {
            var balance = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            string id = GachaCatalog.IdOf(EquipmentSlot.Sword, Grade.Rare);
            EquipmentLevels.AddOwned(save, id);
            save.equippedSword = id;
            double bare = CombatLoadout.ComputeStats(balance, SaveDataV2.CreateNew()).Atk;

            EquipmentLevels.Set(save, id, 5);
            double enhanced = CombatLoadout.ComputeStats(balance, save).Atk;

            double expectedMult = 1d + (balance.SWORD_ATK_R - 1d) * System.Math.Pow(1d + balance.EQUIP_ENHANCE_GAIN, 5);
            Assert.AreEqual(bare * expectedMult, enhanced, 1e-9);
        }

        [Test]
        public void Levels_OldSaveWithoutLevels_ReadsZero()
        {
            var save = SaveDataV2.CreateNew();
            save.ownedEquipment.Add("Equipment_Helm_Epic");
            save.ownedEquipmentLevels = null;

            Assert.AreEqual(0, EquipmentLevels.Get(save, "Equipment_Helm_Epic"));
            Assert.AreEqual(1, save.ownedEquipmentLevels.Count);
        }

        [Test]
        public void Grant_BossKill_MultipliesExp()
        {
            var balance = new BalanceValues();
            balance.EXP_REQ_BASE = 1e9;
            var data = SaveDataV2.CreateNew();

            Result result = KillExp.Grant(data, balance, 10, isBoss: true);

            Assert.IsTrue(result.Ok);
            Assert.AreEqual(Formulas.EnemyExp(balance, 10, false) * balance.BOSS_EXP_MULT, data.heroExp, 1e-9);
        }
    }
}
