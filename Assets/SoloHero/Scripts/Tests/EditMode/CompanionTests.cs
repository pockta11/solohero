using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Companions;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    public sealed class CompanionTests
    {
        private sealed class FixedRandom : IRandom
        {
            public double NextDouble() => 0.99d;

            public int Next(int maxExclusive) => 0;
        }

        [Test]
        public void Companions_UnlockByClearedStage_EquipAndLevelWithGold()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var companions = new CompanionService(save, b);
            int wisp = CompanionCatalog.IndexOf("wisp");

            Assert.IsTrue(companions.IsUnlocked(CompanionCatalog.IndexOf("slime")));
            Assert.AreEqual(FailReason.Locked, companions.TryEquip(wisp).Reason);
            save.highestStage = CompanionCatalog.All[wisp].UnlockStage;
            Assert.IsTrue(companions.TryEquip(wisp).Ok);
            Assert.AreEqual(wisp, companions.EquippedIndex);

            double cost = companions.LevelCost(wisp);
            Assert.AreEqual(Formulas.CompanionLevelCost(b, CompanionCatalog.All[wisp].Grade, 1), cost);
            Assert.AreEqual(FailReason.NotEnoughGold, companions.TryLevelUp(wisp).Reason);
            save.gold = cost;
            Assert.IsTrue(companions.TryLevelUp(wisp).Ok);
            Assert.AreEqual(2, companions.Level(wisp));
            Assert.AreEqual(0d, save.gold, 1e-9);
        }

        [Test]
        public void EquippedCompanion_AttacksInCombat_WithItsHitKind()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.companionEquipped = "slime";
            var runner = new StageRunner(b, new FixedRandom(), new HeroStats(1e6, 1d, 1e4, b.ATKSPD_BASE, 0d), save);
            CombatLoadout.Apply(runner, b, save);
            int companionHits = 0;
            runner.World.HitLanded += (target, amount, kind) => { if (kind == HitKind.Companion) companionHits++; };
            runner.Begin(1);
            for (int i = 0; i < 200; i++) runner.Tick(0.05f);
            Assert.Greater(companionHits, 2, "the slime attacks every 2 s once enemies are in range");
        }
    }
}
