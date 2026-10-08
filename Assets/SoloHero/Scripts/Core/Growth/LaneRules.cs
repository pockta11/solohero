using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    /// <summary>What a lane's buy button can do right now.</summary>
    public enum LaneState
    {
        /// <summary>Opens at a higher hero level (D-142).</summary>
        Locked,

        /// <summary>Can be raised with gold.</summary>
        Open,

        /// <summary>At its limit: the next level needs a limit break (D-143).</summary>
        NeedsBreak,

        /// <summary>Capped lane at its last level.</summary>
        Max
    }

    /// <summary>
    /// Rules of the gold stat lanes: D-142 crit lanes open with hero levels and are capped like attack speed; D-143 every
    /// LIMIT_STEP levels a lane stops until a limit break, the k-th costing LIMIT_STONE_BASE x k breakthrough stones.
    /// Breaks are saved per lane (SaveDataV2.laneBreaks, index = UpgradeLane).
    /// </summary>
    public static class LaneRules
    {
        public static int MaxLevel(BalanceValues balance, UpgradeLane lane)
        {
            switch (lane)
            {
                case UpgradeLane.Spd: return balance.UPG_MAX_LEVEL_SPD;
                case UpgradeLane.Crit: return balance.UPG_MAX_LEVEL_CRIT;
                default: return int.MaxValue;
            }
        }

        public static int UnlockLevel(BalanceValues balance, UpgradeLane lane)
        {
            switch (lane)
            {
                case UpgradeLane.Crit: return balance.UPG_UNLOCK_LV_CRIT;
                case UpgradeLane.CritDmg: return balance.UPG_UNLOCK_LV_CRITDMG;
                default: return 1;
            }
        }

        public static bool IsUnlocked(BalanceValues balance, UpgradeLane lane, int heroLevel) =>
            heroLevel >= UnlockLevel(balance, lane);

        public static int Breaks(SaveDataV2 data, UpgradeLane lane)
        {
            int i = (int)lane;
            return data != null && i >= 0 && i < data.laneBreaks.Count ? Math.Max(0, data.laneBreaks[i]) : 0;
        }

        /// <summary>The highest level the lane can reach with <paramref name="breaks"/> limit breaks.</summary>
        public static int Cap(BalanceValues balance, UpgradeLane lane, int breaks)
        {
            int max = MaxLevel(balance, lane);
            if (balance.LIMIT_STEP <= 0) return max;
            long cap = (long)balance.LIMIT_STEP * (Math.Max(0, breaks) + 1L);
            return cap >= max ? max : (int)cap;
        }

        /// <summary>Stones for the next limit break of a lane that has <paramref name="breaks"/> already.</summary>
        public static int BreakCost(BalanceValues balance, int breaks) =>
            balance.LIMIT_STONE_BASE * (Math.Max(0, breaks) + 1);

        public static LaneState State(BalanceValues balance, SaveDataV2 data, UpgradeLane lane)
        {
            if (!IsUnlocked(balance, lane, data.heroLevel)) return LaneState.Locked;
            int level = LaneLevels.From(data).Get(lane);
            if (level >= MaxLevel(balance, lane)) return LaneState.Max;
            return level >= Cap(balance, lane, Breaks(data, lane)) ? LaneState.NeedsBreak : LaneState.Open;
        }

        /// <summary>
        /// D-143 save repair, idempotent: gives the break list one entry per lane. A lane missing from it (every lane of a
        /// save from before limit breaks) counts as broken up to its current level, so no existing level is lost.
        /// </summary>
        public static bool EnsureBreaks(BalanceValues balance, SaveDataV2 data)
        {
            if (balance == null || data == null) return false;
            bool changed = false;
            LaneLevels levels = LaneLevels.From(data);
            while (data.laneBreaks.Count < UpgradeLanes.Count)
            {
                var lane = (UpgradeLane)data.laneBreaks.Count;
                int level = levels.Get(lane);
                data.laneBreaks.Add(balance.LIMIT_STEP > 0 ? level / balance.LIMIT_STEP : 0);
                changed = true;
            }

            return changed;
        }
    }
}
