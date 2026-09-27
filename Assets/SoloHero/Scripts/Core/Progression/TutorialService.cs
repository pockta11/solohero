using System;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    public enum TutorialEvent
    {
        None,

        /// <summary>Reward granted: TUTORIAL_GOLD plus TUTORIAL_FREE_PULLS free pulls (D-056, D-060).</summary>
        Rewarded,

        /// <summary>Point the player at the upgrade lanes.</summary>
        HintUpgrade,

        /// <summary>The second skill just unlocked; point the player at the skill panel.</summary>
        HintSkill,

        Finished
    }

    /// <summary>
    /// First-session tutorial (E7-13) as a step machine on <see cref="SaveDataV2.tutorialStep"/>.
    /// GDD first 30 minutes: reward + free pull after the first clears, then upgrade hint, then skill hint.
    /// Each step fires once; the step is saved so a restart never repeats a reward.
    /// </summary>
    public sealed class TutorialService
    {
        public const int StepReward = 0;
        public const int StepUpgrade = 1;
        public const int StepSkill = 2;
        public const int StepDone = 3;

        private readonly BalanceValues _balance;
        private readonly GachaService _gacha;
        private readonly ISaveRequester _save;

        public TutorialService(BalanceValues balance, GachaService gacha, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _gacha = gacha ?? throw new ArgumentNullException(nameof(gacha));
            _save = save;
        }

        /// <summary>Items from the last reward's free pulls, for the reward popup or toast.</summary>
        public GachaPullItem[] LastRewardItems { get; private set; } = Array.Empty<GachaPullItem>();

        public TutorialEvent Tick(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            switch (data.tutorialStep)
            {
                case StepReward:
                    if (data.highestStage < _balance.TUTORIAL_REWARD_STAGE) return TutorialEvent.None;
                    GrantReward(data);
                    data.tutorialStep = StepUpgrade;
                    _save?.RequestSave();
                    return TutorialEvent.Rewarded;

                case StepUpgrade:
                    if (data.upgradeHp + data.upgradeAtk + data.upgradeDef + data.upgradeSpd > 0)
                    {
                        data.tutorialStep = StepSkill;
                        return TutorialEvent.None;
                    }

                    return TutorialEvent.HintUpgrade;

                case StepSkill:
                    if (!SkillLevelService.IsUnlocked(_balance, SkillSlot.Slot2, data.heroLevel)) return TutorialEvent.None;
                    data.tutorialStep = StepDone;
                    return TutorialEvent.HintSkill;

                default:
                    return TutorialEvent.None;
            }
        }

        private void GrantReward(SaveDataV2 data)
        {
            data.gold += _balance.TUTORIAL_GOLD;
            int count = _balance.TUTORIAL_FREE_PULLS;
            var items = new GachaPullItem[count];
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                // A free pull: fund the exact price, then pull through the normal path (pity, auto-equip, enhance).
                data.gold += _balance.GACHA_COST_SINGLE;
                GachaBatchResult r = _gacha.TryPull(data);
                if (!r.Status.Ok || r.Items.Length == 0) continue;
                items[n++] = r.Items[0];
            }

            if (n < count) Array.Resize(ref items, n);
            LastRewardItems = items;
        }
    }
}
