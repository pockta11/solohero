using SoloHero.Core.Gacha;

namespace SoloHero.Core.Companions
{
    public sealed class CompanionDef
    {
        public string Id { get; init; } = "";
        public Grade Grade { get; init; }

        /// <summary>Unlocked once save.highestStage (the highest cleared stage) reaches this.</summary>
        public int UnlockStage { get; init; }

        /// <summary>Damage per attack at level 1: hero ATK x this.</summary>
        public double AttackMult { get; init; }
        public float Interval { get; init; } = 2f;
        public double Range { get; init; } = 3.5;

        /// <summary>Optional status on hit (burn per second as % of ATK, or a stun) - they feed the D-098 combos.</summary>
        public double DotPercent { get; init; }
        public float DotSeconds { get; init; }
        public float StunSeconds { get; init; }

        /// <summary>VfxSet clip played on the target and its tint (0xRRGGBB).</summary>
        public string Vfx { get; init; } = "spark";
        public uint Tint { get; init; } = 0xFFFFFF;

        /// <summary>Sheet entity name under Art/Pets.</summary>
        public string Art => "pet" + Id;

        public string NameKey => "companion.name." + Id;
    }

    /// <summary>
    /// D-102 companions: four creatures, one per grade, unlocked by the first three chapter bosses. The index is
    /// saved (companionLevels), so only append. Like SkillCatalog this table is the content's single source.
    /// </summary>
    public static class CompanionCatalog
    {
        public static readonly CompanionDef[] All =
        {
            new CompanionDef { Id = "slime", Grade = Grade.Common, UnlockStage = 0, AttackMult = 1.0, Interval = 2f, Vfx = "spark", Tint = 0x9AF09A },
            new CompanionDef { Id = "wisp", Grade = Grade.Rare, UnlockStage = 10, AttackMult = 2.0, Interval = 2f, StunSeconds = 0.6f, Vfx = "ice", Tint = 0xBFE6FF },
            new CompanionDef { Id = "owl", Grade = Grade.Epic, UnlockStage = 20, AttackMult = 4.0, Interval = 2.5f, Range = 5.0, Vfx = "bolt", Tint = 0xFFE0A0 },
            new CompanionDef { Id = "dragon", Grade = Grade.Legendary, UnlockStage = 30, AttackMult = 5.0, Interval = 3f, DotPercent = 80, DotSeconds = 3f, Vfx = "fire" },
        };

        public static int Count => All.Length;

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return i;
            return -1;
        }
    }
}
