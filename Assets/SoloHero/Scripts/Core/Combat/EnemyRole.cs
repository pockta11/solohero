namespace SoloHero.Core.Combat
{
    /// <summary>
    /// D-110 enemy behaviour (genre mob mix). Every role walks in toward the hero; the role sets its stats, speed and
    /// where it stops. Views pick the chapter's look for the role (goblin soldier, flying-eye rusher, mushroom tank,
    /// bone-throwing skeleton).
    /// </summary>
    public enum EnemyRole
    {
        /// <summary>The standard soldier: walks in, takes a front spot, attacks up close.</summary>
        Melee = 0,

        /// <summary>Rusher: quick and frail, reaches the hero first.</summary>
        Fast = 1,

        /// <summary>Slow and sturdy, a bigger body.</summary>
        Tank = 2,

        /// <summary>Stops behind the pack and shoots from range; its shots fly to the hero.</summary>
        Ranged = 3
    }
}
