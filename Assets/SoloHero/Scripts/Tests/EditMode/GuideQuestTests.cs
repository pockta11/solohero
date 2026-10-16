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
        public void Chain_D180_Dense_EveryKindGrows_RewardsSmall_TotalsKept()
        {
            GuideQuestDef[] chain = GuideQuestCatalog.Chain;
            Assert.GreaterOrEqual(chain.Length, 55, "the dense chain");
            var last = new System.Collections.Generic.Dictionary<GuideKind, int>();
            double gems = 0d, gold = 0d;
            int firstBoss = -1;
            for (int i = 0; i < chain.Length; i++)
            {
                GuideQuestDef q = chain[i];
                Assert.Greater(q.Target, last.TryGetValue(q.Kind, out int prev) ? prev : 0, "step " + i + " " + q.Kind + " must grow");
                last[q.Kind] = q.Target;
                Assert.Greater(q.Gems + q.GoldStages, 0d, "step " + i + " pays nothing");
                Assert.LessOrEqual(q.Gems, 80d, "step " + i + " pays too many gems at once");
                gems += q.Gems;
                gold += q.GoldStages;
                if (q.Kind == GuideKind.ClearStage && q.Target == 10 && firstBoss < 0) firstBoss = i;
            }

            // The totals of the old 30-step chain (about 1,000 gems, about 110 stage clears of gold) and the pacing:
            // the first boss within the first 15 steps, both job advancements in the chain.
            Assert.That(gems, Is.InRange(900d, 1100d));
            Assert.That(gold, Is.InRange(90d, 130d));
            Assert.That(firstBoss, Is.InRange(10, 16));
            Assert.AreEqual(2, last[GuideKind.JobTier]);
            Assert.AreEqual(40, last[GuideKind.ClearStage]);
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
