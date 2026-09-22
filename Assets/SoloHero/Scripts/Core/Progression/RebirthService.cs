using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Core.Progression
{
    public sealed class RebirthService
    {
        public enum PermLane
        {
            Gold,
            Atk,
            Offline
        }

        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public RebirthService(BalanceValues balance, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public static int MinStage(BalanceValues balance)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            return StageIndex.ToGlobal(
                balance.MVP_CHAPTERS,
                balance.STAGES_PER_CHAPTER,
                balance.STAGES_PER_CHAPTER);
        }

        // GDD: soul gained at rebirth = floor(g / 5) from global stage index g.
        public static double SoulGain(int g)
        {
            if (g < 1) return 0d;
            return Math.Floor(g / 5d);
        }

        public Result TryRebirth(SaveDataV2 save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            if (save.highestStage < MinStage(_balance))
                return Result.Fail(FailReason.Locked);

            save.soul += SoulGain(save.highestStage);
            save.rebirthCount++;

            save.gold = 0d;
            save.upgradeHp = 0;
            save.upgradeAtk = 0;
            save.upgradeDef = 0;
            save.upgradeSpd = 0;
            save.heroLevel = 1;
            save.heroExp = 0d;
            save.highestStage = 1;
            save.farmingStage = 1;
            save.skillLevel1 = 0;
            save.skillLevel2 = 0;
            save.skillLevel3 = 0;

            _save?.RequestSave();
            return Result.Success;
        }

        public Result TryUpgradePerm(SaveDataV2 save, PermLane lane)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            ValidateLane(lane);

            // GDD has no soul cost / % gain / max level in Number Balancing or BalanceValues.
            return Result.Fail(FailReason.Locked);
        }

        private static void ValidateLane(PermLane lane)
        {
            switch (lane)
            {
                case PermLane.Gold:
                case PermLane.Atk:
                case PermLane.Offline:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }
    }
}
