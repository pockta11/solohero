using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-111 guide quests and combat power.</summary>
    public sealed class GuideQuestTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int Count;

            public void RequestSave() => Count++;
        }

        [Test]
        public void FirstQuest_UpgradeOnce_ClaimPaysGemsAndAdvances()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var requester = new FakeSaveRequester();
            var quests = new GuideQuestService(b, save, requester);
            GuideQuestDef first = quests.Current;
            Assert.AreEqual(GuideKind.UpgradeTotal, first.Kind);

            Assert.IsFalse(quests.IsComplete);
            Assert.AreEqual(FailReason.Locked, quests.TryClaim().Reason);
            Assert.AreEqual(0, save.guideQuest);

            save.upgradeAtk = 1;
            Assert.IsTrue(quests.IsComplete);
            double gems = save.gem;
            Assert.IsTrue(quests.TryClaim().Ok);

            Assert.AreEqual(gems + first.Gems, save.gem, 1e-9);
            Assert.AreEqual(1, save.guideQuest);
            Assert.AreEqual(1, requester.Count);
        }

        [Test]
        public void GoldQuest_PaysStageClearsOfTheFarmingStage()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var quests = new GuideQuestService(b, save);
            save.guideQuest = 1; // clear 1-3, paid in gold
            GuideQuestDef def = quests.Current;
            Assert.Greater(def.GoldStages, 0d);
            save.highestStage = def.Target;
            save.farmingStage = def.Target;
            double expected = System.Math.Floor(Formulas.StageGold(b, def.Target) * def.GoldStages);

            Assert.IsTrue(quests.TryClaim().Ok);
            Assert.AreEqual(expected, save.gold, 1e-9);
        }

        [Test]
        public void AfterTheChain_QuestsCycleWithGrowingTargets()
        {
            int start = GuideQuestCatalog.CycleStart;
            int len = GuideQuestCatalog.CycleLength;
            for (int k = 0; k < len; k++)
            {
                GuideQuestDef a = GuideQuestCatalog.At(start + k);
                GuideQuestDef b = GuideQuestCatalog.At(start + k + len);
                Assert.AreEqual(a.Kind, b.Kind);
                Assert.Greater(b.Target, a.Target);
                Assert.Greater(a.Gems + a.GoldStages, 0d);
            }
        }

        [Test]
        public void CombatPower_RisesWithEveryUpgradeLane()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            double base0 = CombatPower.Of(b, CombatLoadout.ComputeStats(b, save));
            foreach (UpgradeLane lane in new[] { UpgradeLane.Hp, UpgradeLane.Atk, UpgradeLane.Def, UpgradeLane.Spd })
            {
                double up = CombatPower.Of(b, CombatLoadout.ComputeStatsAfterUpgrade(b, save, lane));
                Assert.Greater(up, base0, lane.ToString());
            }
        }
    }
}
