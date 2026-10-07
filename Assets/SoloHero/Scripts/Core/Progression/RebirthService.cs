using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Core.Progression
{
    /// <summary>
    /// GDD prestige, shipped in D-117: once the run's highest stage reaches the chapter 5 boss the hero can start over
    /// at 1-1 for floor(g / 5) Soul. Gold, the four upgrade lanes, hero level and EXP, stage progress and talents
    /// reset; gear, skills (a summon collection, D-078), pets, jobs, gems and the summon counters stay. Soul buys three
    /// permanent boosts that survive every rebirth: stage gold x(1 + REBIRTH_GOLD_GAIN)^level, hero ATK
    /// x(1 + REBIRTH_ATK_GAIN)^level and offline gold +REBIRTH_OFFLINE_GAIN x level; level n -> n + 1 costs
    /// REBIRTH_COST_BASE + n Soul.
    /// </summary>
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

        /// <summary>A rebirth or a permanent boost bought.</summary>
        public event Action Changed;

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

        /// <summary>D-117: the best stage of any run; unlocks (the pet summon) and achievements read it.</summary>
        public static int BestStage(SaveDataV2 save) => save == null ? 0 : Math.Max(save.highestStage, save.bestStageEver);

        public bool CanRebirth(SaveDataV2 save) => save != null && save.highestStage >= MinStage(_balance);

        public Result TryRebirth(SaveDataV2 save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            if (save.highestStage < MinStage(_balance))
                return Result.Fail(FailReason.Locked);

            save.bestStageEver = BestStage(save);
            save.bestHeroLevel = Math.Max(save.bestHeroLevel, save.heroLevel);
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
            save.retreatMode = false;
            // Talent points come with hero levels, so the tree starts over too (D-117).
            save.talentIds.Clear();
            save.talentRanks.Clear();
            // Skills are a summon collection like equipment (D-078) and survive rebirth.

            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        public static int Level(SaveDataV2 save, PermLane lane)
        {
            switch (lane)
            {
                case PermLane.Gold: return save.permGoldLevel;
                case PermLane.Atk: return save.permAtkLevel;
                case PermLane.Offline: return save.permOfflineLevel;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }

        /// <summary>Soul for raising a permanent boost from <paramref name="level"/>.</summary>
        public static double Cost(BalanceValues balance, int level) => balance.REBIRTH_COST_BASE + Math.Max(0, level);

        public Result TryUpgradePerm(SaveDataV2 save, PermLane lane)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            ValidateLane(lane);
            double cost = Cost(_balance, Level(save, lane));
            if (save.soul < cost) return Result.Fail(FailReason.NotEnoughSoul);
            save.soul -= cost;
            switch (lane)
            {
                case PermLane.Gold: save.permGoldLevel++; break;
                case PermLane.Atk: save.permAtkLevel++; break;
                default: save.permOfflineLevel++; break;
            }

            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>Stage (and dungeon) gold multiplier of the permanent gold boost.</summary>
        public static double GoldMult(BalanceValues balance, SaveDataV2 save) =>
            save == null ? 1d : Math.Pow(1d + balance.REBIRTH_GOLD_GAIN, save.permGoldLevel);

        /// <summary>Hero ATK multiplier of the permanent ATK boost.</summary>
        public static double AtkMult(BalanceValues balance, SaveDataV2 save) =>
            save == null ? 1d : Math.Pow(1d + balance.REBIRTH_ATK_GAIN, save.permAtkLevel);

        /// <summary>Offline gold multiplier: the gold boost times the offline boost.</summary>
        public static double OfflineMult(BalanceValues balance, SaveDataV2 save) =>
            save == null ? 1d : GoldMult(balance, save) * (1d + balance.REBIRTH_OFFLINE_GAIN * save.permOfflineLevel);

        /// <summary>The effect of a lane at a level as a percent bonus, for the panel (+21% at gold level 2).</summary>
        public static double EffectPercent(BalanceValues balance, PermLane lane, int level)
        {
            switch (lane)
            {
                case PermLane.Gold: return (Math.Pow(1d + balance.REBIRTH_GOLD_GAIN, level) - 1d) * 100d;
                case PermLane.Atk: return (Math.Pow(1d + balance.REBIRTH_ATK_GAIN, level) - 1d) * 100d;
                default: return balance.REBIRTH_OFFLINE_GAIN * level * 100d;
            }
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
