namespace SoloHero.Core.Growth
{
    /// <summary>
    /// Gold stat lanes of the character panel. Values are saved in analytics and index the UI rows, so only ever append.
    /// D-142: crit rate and crit damage open with hero levels.
    /// </summary>
    public enum UpgradeLane
    {
        Hp,
        Atk,
        Def,
        Spd,
        Crit,
        CritDmg
    }

    public static class UpgradeLanes
    {
        public const int Count = 6;
    }
}
