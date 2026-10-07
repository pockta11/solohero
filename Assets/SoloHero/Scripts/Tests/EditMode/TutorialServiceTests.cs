using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class TutorialServiceTests
    {
        private sealed class CountingRequester : ISaveRequester
        {
            public int Count;

            public void RequestSave() => Count++;
        }

        private static TutorialService Create(BalanceValues b, ISaveRequester requester = null)
        {
            var gacha = new GachaService(b, GearTableValues.FromBalance(b), new SystemRandom(new System.Random(1)), GachaCatalog.Standard(b));
            return new TutorialService(b, gacha, requester);
        }

        [Test]
        public void Tick_BeforeRewardStage_DoesNothing()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = b.TUTORIAL_REWARD_STAGE - 1;

            Assert.AreEqual(TutorialEvent.None, Create(b).Tick(save));
            Assert.AreEqual(0d, save.gold, 1e-9);
            Assert.AreEqual(TutorialService.StepReward, save.tutorialStep);
        }

        [Test]
        public void Tick_AtRewardStage_GrantsGoldAndFreePullOnceAndSaves()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = b.TUTORIAL_REWARD_STAGE;
            var requester = new CountingRequester();
            TutorialService tutorial = Create(b, requester);

            TutorialEvent first = tutorial.Tick(save);
            double goldAfter = save.gold;
            TutorialEvent second = tutorial.Tick(save);

            Assert.AreEqual(TutorialEvent.Rewarded, first);
            Assert.AreEqual(b.TUTORIAL_GOLD, goldAfter, 1e-9);
            Assert.AreEqual(b.TUTORIAL_FREE_PULLS, save.totalPullCount);
            Assert.AreEqual(b.TUTORIAL_FREE_PULLS, tutorial.LastRewardItems.Length);
            // D-113: the welcome gift is a free ten-pull; duplicates enhance instead of adding a copy.
            int fresh = 0;
            foreach (GachaPullItem item in tutorial.LastRewardItems) if (!item.WasDuplicate) fresh++;
            Assert.AreEqual(fresh, save.ownedEquipment.Count);
            Assert.GreaterOrEqual(fresh, 1);
            Assert.AreEqual(1, requester.Count);
            Assert.AreEqual(TutorialEvent.HintUpgrade, second);
            Assert.AreEqual(b.TUTORIAL_GOLD, save.gold, 1e-9);
        }

        [Test]
        public void Tick_AfterFirstUpgrade_MovesToSkillHintAtUnlockLevel()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.tutorialStep = TutorialService.StepUpgrade;
            save.upgradeAtk = 1;
            TutorialService tutorial = Create(b);

            Assert.AreEqual(TutorialEvent.None, tutorial.Tick(save));
            Assert.AreEqual(TutorialService.StepSkill, save.tutorialStep);
            Assert.AreEqual(TutorialEvent.None, tutorial.Tick(save));

            save.heroLevel = b.SKILL_UNLOCK_LV_2;
            Assert.AreEqual(TutorialEvent.HintSkill, tutorial.Tick(save));
            Assert.AreEqual(TutorialService.StepDone, save.tutorialStep);
            Assert.AreEqual(TutorialEvent.None, tutorial.Tick(save));
        }
    }
}
