using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    /// <summary>A system that a first stage clear opens (compared against the save's highest cleared stage).</summary>
    public enum SystemGate
    {
        Dungeon = 0,
        Tower = 1,
        PetSummon = 2,
        MaterialDungeon = 3,
        Raid = 4,
    }

    /// <summary>
    /// D-178: the one list of stage-gated systems, read by the rail locks, the locked hints and the "opened" ribbon. The
    /// opening stage of each gate stays in <see cref="BalanceValues"/> (the services enforce it); this only reads it, so a
    /// hint and the rule it describes cannot drift apart.
    /// </summary>
    public static class SystemUnlocks
    {
        public static readonly SystemGate[] All =
        {
            SystemGate.Dungeon, SystemGate.Tower, SystemGate.PetSummon, SystemGate.MaterialDungeon, SystemGate.Raid,
        };

        /// <summary>The global stage whose first clear opens the gate.</summary>
        public static int StageOf(BalanceValues balance, SystemGate gate)
        {
            switch (gate)
            {
                case SystemGate.Dungeon: return balance.DUNGEON_UNLOCK_STAGE;
                case SystemGate.Tower: return balance.TOWER_UNLOCK_STAGE;
                case SystemGate.PetSummon: return balance.PET_UNLOCK_STAGE;
                case SystemGate.MaterialDungeon: return balance.MATERIAL_UNLOCK_STAGE;
                case SystemGate.Raid: return balance.RAID_UNLOCK_STAGE;
                default: return 0;
            }
        }

        public static bool IsOpen(BalanceValues balance, SaveDataV2 data, SystemGate gate) =>
            balance != null && data != null && data.highestStage >= StageOf(balance, gate);

        /// <summary>The Strings key of the gate's name ("unlock.dungeon" ...).</summary>
        public static string NameKey(SystemGate gate)
        {
            switch (gate)
            {
                case SystemGate.Dungeon: return "unlock.dungeon";
                case SystemGate.Tower: return "unlock.tower";
                case SystemGate.PetSummon: return "unlock.pet";
                case SystemGate.MaterialDungeon: return "unlock.material";
                case SystemGate.Raid: return "unlock.raid";
                default: return "unlock.dungeon";
            }
        }

        /// <summary>
        /// The gates that the first clear of <paramref name="stage"/> opens, written to <paramref name="buffer"/> in
        /// <see cref="All"/> order; returns how many. Zero for a stage that opens nothing.
        /// </summary>
        public static int OpenedBy(BalanceValues balance, int stage, SystemGate[] buffer)
        {
            int count = 0;
            if (balance == null || buffer == null) return 0;
            for (int i = 0; i < All.Length && count < buffer.Length; i++)
            {
                if (StageOf(balance, All[i]) != stage) continue;
                buffer[count++] = All[i];
            }

            return count;
        }
    }
}
