using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;

namespace SoloHero.Core.Skills
{
    /// <summary>
    /// One skill of the catalog (D-078). Damage is ATK x <see cref="DamageMult"/> per target per wave; a skill fires
    /// <see cref="Waves"/> waves <see cref="WaveInterval"/> seconds apart. Every percent value grows with the skill
    /// level by SKILL_LEVEL_GAIN. Views read <see cref="Vfx"/>, <see cref="Tint"/>, <see cref="Sound"/> and the id
    /// (icon, name and description keys).
    /// </summary>
    public sealed class SkillDef
    {
        public string Id { get; init; } = "";
        public Grade Grade { get; init; }
        public SkillKind Kind { get; init; }

        /// <summary>D-104 job line that may equip it; None = starter skill, usable by everyone.</summary>
        public JobLine Line { get; init; }

        /// <summary>Icon id in the SkillIconSet; defaults to the skill id (job ultimates borrow a catalog icon).</summary>
        public string Icon { get; init; }

        public string IconId => string.IsNullOrEmpty(Icon) ? Id : Icon;
        public float Cooldown { get; init; } = 10f;

        /// <summary>Reach to the right of the hero (units). Strike / Area targets and the auto-cast check use it.</summary>
        public double Range { get; init; } = 1.6;

        public double DamageMult { get; init; }
        public int Waves { get; init; } = 1;
        public float WaveInterval { get; init; } = 0.15f;

        /// <summary>Burn / poison on every enemy hit: ATK x this % per second.</summary>
        public double DotPercent { get; init; }
        public float DotSeconds { get; init; }

        /// <summary>Enemies hit stop attacking for this long (bosses: x SKILL_BOSS_STUN_MULT).</summary>
        public float StunSeconds { get; init; }

        public SkillBuff Buff { get; init; }
        public double BuffAmount { get; init; }
        public float BuffSeconds { get; init; }

        /// <summary>Heal on cast, % of max HP.</summary>
        public double HealPercent { get; init; }

        /// <summary>Shield on cast, % of max HP, absorbed before HP.</summary>
        public double ShieldPercent { get; init; }
        public float ShieldSeconds { get; init; }

        /// <summary>When above 0 the skill auto-casts only at or under this % of max HP (heals).</summary>
        public double HpThreshold { get; init; }

        /// <summary>VfxSet clip name.</summary>
        public string Vfx { get; init; } = "boom";
        public SkillVfxAt VfxAt { get; init; } = SkillVfxAt.Impact;
        public float VfxScale { get; init; } = 1f;

        /// <summary>0xRRGGBB tint for the clip; white keeps the art's own colours.</summary>
        public uint Tint { get; init; } = 0xFFFFFF;

        /// <summary>Sound family: strike, whoosh, cry, fire, thunder, ice, heal, magic.</summary>
        public string Sound { get; init; } = "strike";

        public bool DealsDamage => DamageMult > 0d && (Kind == SkillKind.Strike || Kind == SkillKind.Area);

        public string NameKey => "skill.name." + Id;

        public string DescKey => "skill.desc." + Id;
    }
}
