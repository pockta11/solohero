namespace SoloHero.Core.Combat
{
    /// <summary>What kind of hero hit landed; views pick the number style and the effect from it.</summary>
    public enum HitKind
    {
        Normal = 0,
        Crit = 1,
        Skill = 2,

        /// <summary>A burn / poison tick.</summary>
        Dot = 3,

        /// <summary>A skill hit that landed a combo (D-098: shatter on a stunned enemy, ignite on a burning one).</summary>
        Combo = 4
    }
}
