namespace SoloHero.Core.Stage
{
    /// <summary>D-100 daily dungeons: 30 seconds of endless waves at the farming stage, paid per kill.</summary>
    public enum DungeonKind
    {
        None = 0,

        /// <summary>Each kill pays the stage's clear gold x DUNGEON_GOLD_PER_KILL.</summary>
        Gold = 1,

        /// <summary>Each kill pays the enemy's EXP x DUNGEON_EXP_MULT.</summary>
        Exp = 2,

        /// <summary>D-130: the infinite tower - one boss per floor, climbing while the hero wins.</summary>
        Tower = 3
    }
}
