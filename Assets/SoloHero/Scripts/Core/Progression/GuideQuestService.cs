using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Progression
{
    /// <summary>
    /// D-111 guide quest bar: the current quest of <see cref="GuideQuestCatalog"/>, its progress read from the save,
    /// and the claim (reward, then the next quest). Claiming is a save trigger like any reward.
    /// </summary>
    public sealed class GuideQuestService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly ISaveRequester _save;

        public GuideQuestService(BalanceValues balance, SaveDataV2 data, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save;
        }

        /// <summary>Raised after a claim (the bar redraws on its next poll anyway).</summary>
        public event Action Claimed;

        public int Index => _data.guideQuest < 0 ? 0 : _data.guideQuest;

        public GuideQuestDef Current => GuideQuestCatalog.At(Index);

        public int Progress => ProgressOf(_balance, _data, Current.Kind);

        public bool IsComplete => Progress >= Current.Target;

        /// <summary>Gold the current quest pays right now (its stage-clear count at the farming stage).</summary>
        public double RewardGold => RewardGoldOf(_balance, _data, Current);

        public static double RewardGoldOf(BalanceValues balance, SaveDataV2 data, GuideQuestDef def)
        {
            if (def == null || def.GoldStages <= 0d) return 0d;
            int g = data.farmingStage > 0 ? data.farmingStage : 1;
            return Math.Floor(Formulas.StageGold(balance, g) * def.GoldStages);
        }

        public Result TryClaim()
        {
            GuideQuestDef def = Current;
            if (ProgressOf(_balance, _data, def.Kind) < def.Target) return Result.Fail(FailReason.Locked);
            _data.gold += RewardGoldOf(_balance, _data, def);
            _data.gem += def.Gems;
            _data.guideQuest = Index + 1;
            _save?.RequestSave();
            Claimed?.Invoke();
            return Result.Success;
        }

        public static int ProgressOf(BalanceValues balance, SaveDataV2 data, GuideKind kind)
        {
            switch (kind)
            {
                case GuideKind.UpgradeTotal: return data.upgradeHp + data.upgradeAtk + data.upgradeDef + data.upgradeSpd;
                case GuideKind.ClearStage: return data.highestStage;
                case GuideKind.HeroLevel: return data.heroLevel;
                case GuideKind.GearPulls: return data.totalPullCount;
                case GuideKind.SkillPulls: return data.skillPullCount;
                case GuideKind.EquipSkills:
                {
                    int n = 0;
                    int slots = SkillService.SlotCount(balance);
                    for (int s = 0; s < slots; s++)
                    {
                        if (!string.IsNullOrEmpty(SkillBook.EquippedAt(data, s))) n++;
                    }

                    return n;
                }
                case GuideKind.JobTier: return JobService.TierOf(data);
                case GuideKind.Talents: return TalentService.Spent(data);
                case GuideKind.OwnedGear: return data.ownedEquipment != null ? data.ownedEquipment.Count : 0;
                default: return 0;
            }
        }
    }
}
