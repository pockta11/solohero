namespace SoloHero.Core.Stage
{
    public enum StageState
    {
        Running,
        Clearing,
        BossIntro,
        BossTimer,
        Failed,
        Retreat,

        /// <summary>D-100: a daily dungeon run (timer, endless waves, reward per kill).</summary>
        Dungeon,

        /// <summary>D-100: the dungeon ended; the result shows briefly before the runner returns to its stage.</summary>
        DungeonResult
    }
}
