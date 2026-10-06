namespace SoloHero.Core.Skills
{
    /// <summary>How a skill picks its targets. Every kind may also carry a buff, heal or shield rider.</summary>
    public enum SkillKind
    {
        /// <summary>The nearest enemy in range, once per wave.</summary>
        Strike = 0,

        /// <summary>Every enemy between the hero and the skill range, once per wave.</summary>
        Area = 1,

        /// <summary>No damage: a buff on the hero.</summary>
        Buff = 2,

        /// <summary>No damage: heals (and may shield) the hero.</summary>
        Heal = 3
    }

    /// <summary>The stat a buff raises while it lasts.</summary>
    public enum SkillBuff
    {
        None = 0,

        /// <summary>ATK x (1 + amount%).</summary>
        Atk = 1,

        /// <summary>Attack speed x (1 + amount%).</summary>
        AtkSpd = 2,

        /// <summary>Crit rate + amount percentage points.</summary>
        Crit = 3,

        /// <summary>Damage taken x (1 - amount%).</summary>
        Guard = 4,

        /// <summary>D-109: every skill cooldown runs amount% faster.</summary>
        Haste = 5
    }

    /// <summary>Where the view plays a skill's effect clip.</summary>
    public enum SkillVfxAt
    {
        /// <summary>Once, on the hero at cast.</summary>
        Hero = 0,

        /// <summary>Once, in the middle of the skill range at cast.</summary>
        Front = 1,

        /// <summary>Every wave, on the wave's first target.</summary>
        Impact = 2,

        /// <summary>Every wave, on each enemy hit.</summary>
        EachTarget = 3
    }
}
