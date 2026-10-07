using System;
using SoloHero.Core.Gacha;

namespace SoloHero.Core.Pets
{
    public sealed class PetDef
    {
        public string Id { get; init; } = "";
        public GearGrade Grade { get; init; }

        /// <summary>
        /// D-102 companions joined by clearing a stage; D-114 pets come from the pet summon. An older save keeps the
        /// pets it had unlocked (highest cleared stage at least this, see PetService.EnsureOwned). -1: summon only.
        /// </summary>
        public int LegacyUnlockStage { get; init; } = -1;

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

        public string NameKey => "pet.name." + Id;

        /// <summary>Average damage per second at level 1 as hero ATK multiples (hits plus burn), for previews and the simulator.</summary>
        public double DamagePerSecond => (AttackMult + DotPercent / 100d * DotSeconds) / Interval;
    }

    /// <summary>
    /// D-114 pets (the D-102 companions): sixteen creatures on the gear grade ladder, fewer at each higher grade -
    /// three common, uncommon, rare and epic, two legendary, one mythic, one ancient. Levels and enhance levels are
    /// saved index-aligned, so entries are only ever appended; the first four are the D-102 companions. Like
    /// SkillCatalog this table is the content's single source.
    /// </summary>
    public static class PetCatalog
    {
        public static readonly PetDef[] All =
        {
            // D-102 companions (their indices are in old saves).
            new PetDef { Id = "slime", Grade = GearGrade.Common, LegacyUnlockStage = 0, AttackMult = 1.0, Interval = 2f, Vfx = "spark", Tint = 0x9AF09A },
            new PetDef { Id = "wisp", Grade = GearGrade.Rare, LegacyUnlockStage = 10, AttackMult = 2.0, Interval = 2f, StunSeconds = 0.6f, Vfx = "ice", Tint = 0xBFE6FF },
            new PetDef { Id = "owl", Grade = GearGrade.Epic, LegacyUnlockStage = 20, AttackMult = 4.0, Interval = 2.5f, Range = 5.0, Vfx = "bolt", Tint = 0xFFE0A0 },
            new PetDef { Id = "dragon", Grade = GearGrade.Legendary, LegacyUnlockStage = 30, AttackMult = 5.0, Interval = 3f, DotPercent = 80, DotSeconds = 3f, Vfx = "fire" },
            // D-114 summon pets.
            new PetDef { Id = "chick", Grade = GearGrade.Common, AttackMult = 0.8, Interval = 1.6f, Vfx = "spark", Tint = 0xFFE680 },
            new PetDef { Id = "bunny", Grade = GearGrade.Common, AttackMult = 1.2, Interval = 2.4f, Vfx = "spark", Tint = 0xFFD0E0 },
            new PetDef { Id = "frog", Grade = GearGrade.Uncommon, AttackMult = 0.8, Interval = 2f, DotPercent = 20, DotSeconds = 3f, Vfx = "poison", Tint = 0xB0FF90 },
            new PetDef { Id = "bat", Grade = GearGrade.Uncommon, AttackMult = 1.2, Interval = 1.6f, Vfx = "spark", Tint = 0xD0A0FF },
            new PetDef { Id = "piglet", Grade = GearGrade.Uncommon, AttackMult = 1.8, Interval = 2.4f, StunSeconds = 0.4f, Vfx = "spark", Tint = 0xFFB8C8 },
            new PetDef { Id = "fox", Grade = GearGrade.Rare, AttackMult = 1.4, Interval = 2.2f, DotPercent = 30, DotSeconds = 3f, Vfx = "fire", Tint = 0xFFB060 },
            new PetDef { Id = "penguin", Grade = GearGrade.Rare, AttackMult = 2.0, Interval = 2f, StunSeconds = 0.5f, Vfx = "ice", Tint = 0xC8F0FF },
            new PetDef { Id = "cat", Grade = GearGrade.Epic, AttackMult = 3.6, Interval = 2.2f, Range = 4.5, Vfx = "bolt", Tint = 0xE0B0FF },
            new PetDef { Id = "turtle", Grade = GearGrade.Epic, AttackMult = 4.4, Interval = 2.8f, StunSeconds = 0.8f, Vfx = "wave", Tint = 0x90E8FF },
            new PetDef { Id = "phoenix", Grade = GearGrade.Legendary, AttackMult = 5.0, Interval = 2.8f, DotPercent = 70, DotSeconds = 3f, Range = 4.5, Vfx = "phoenix" },
            new PetDef { Id = "gumiho", Grade = GearGrade.Mythic, AttackMult = 8.0, Interval = 2.6f, DotPercent = 100, DotSeconds = 3f, Range = 5.0, Vfx = "fire", Tint = 0x9AD8FF },
            new PetDef { Id = "azure", Grade = GearGrade.Ancient, AttackMult = 16.0, Interval = 2.8f, StunSeconds = 0.8f, Range = 6.0, Vfx = "bolt", Tint = 0x7FFFE8 },
        };

        private static readonly PetDef[][] ByGrade = BuildByGrade();

        public static int Count => All.Length;

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return i;
            return -1;
        }

        public static PetDef Find(string id)
        {
            int i = IndexOf(id);
            return i < 0 ? null : All[i];
        }

        /// <summary>The summon pool of one grade.</summary>
        public static PetDef[] OfGrade(GearGrade grade)
        {
            int g = (int)grade;
            if (g < 0 || g >= ByGrade.Length) throw new ArgumentOutOfRangeException(nameof(grade));
            return ByGrade[g];
        }

        private static PetDef[][] BuildByGrade()
        {
            var result = new PetDef[GearGrades.Count][];
            for (int g = 0; g < result.Length; g++)
            {
                var list = new System.Collections.Generic.List<PetDef>();
                for (int i = 0; i < All.Length; i++)
                    if ((int)All[i].Grade == g) list.Add(All[i]);
                result[g] = list.ToArray();
            }

            return result;
        }
    }
}
