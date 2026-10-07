using System;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;

namespace SoloHero.Core.Skills
{
    /// <summary>
    /// The skill data table (D-078): 24 skills, 6 per grade, modelled on the idle-RPG skill summon (Soul Strike,
    /// Seven Knights Idle, Mushroom Warrior). Like <see cref="GachaCatalog"/> this is the single source for the
    /// content; ids are saved, so never rename one. Grade-wide economy (summon rates, costs, refunds, owned bonus,
    /// level-up cost) lives in BalanceValues. The first three are the starter skills every hero owns.
    /// D-104: the starters are line-free (the beginner's only skills); the other 21 belong to a job line, 7 each.
    /// D-107: 15 more line skills, so every line has 12 (3 per grade); the starters are the beginner's alone.
    /// D-109: 12 more, so every line has 16 (4 per grade), with two new riders: marks (enemies hit take more damage)
    /// and the Haste buff (cooldowns run faster).
    /// New entries go at the end: the index order is the skill book's order and ids are saved.
    /// </summary>
    public static class SkillCatalog
    {
        /// <summary>Skills use the four-tier <see cref="Grade"/> (gear has its own seven, D-113).</summary>
        public const int GradeCount = 4;

        public const string PowerStrike = "power_strike";
        public const string Whirlwind = "whirlwind";
        public const string BattleCry = "battle_cry";

        public static readonly string[] StarterIds = { PowerStrike, Whirlwind, BattleCry };

        public static readonly SkillDef[] All =
        {
            // Common
            new SkillDef
            {
                Id = PowerStrike, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 6f, Range = 2.0,
                DamageMult = 3.0, Vfx = "boom", VfxScale = 1.2f, Tint = 0xFFE0A0, Sound = "strike"
            },
            new SkillDef
            {
                Id = Whirlwind, Grade = Grade.Common, Kind = SkillKind.Area, Cooldown = 12f, Range = 3.0,
                DamageMult = 1.0, Waves = 2, WaveInterval = 0.2f, Vfx = "whirl", VfxAt = SkillVfxAt.Front,
                VfxScale = 2f, Tint = 0xBFE6FF, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = BattleCry, Grade = Grade.Common, Kind = SkillKind.Buff, Cooldown = 20f, Range = 2.5,
                Buff = SkillBuff.Atk, BuffAmount = 30, BuffSeconds = 8f, HealPercent = 15, Vfx = "aura",
                VfxAt = SkillVfxAt.Hero, VfxScale = 1.3f, Tint = 0xFF9050, Sound = "cry"
            },
            new SkillDef
            {
                Id = "quick_slash", Line = JobLine.Warrior, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 7f, Range = 2.0,
                DamageMult = 1.1, Waves = 3, WaveInterval = 0.15f, Vfx = "cross", VfxScale = 1.2f,
                Tint = 0xFFFFFF, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "iron_skin", Line = JobLine.Archer, Grade = Grade.Common, Kind = SkillKind.Buff, Cooldown = 18f, Range = 2.5,
                Buff = SkillBuff.Guard, BuffAmount = 25, BuffSeconds = 8f, Vfx = "shield", VfxAt = SkillVfxAt.Hero,
                Sound = "magic"
            },
            new SkillDef
            {
                Id = "first_aid", Line = JobLine.Mage, Grade = Grade.Common, Kind = SkillKind.Heal, Cooldown = 15f, HealPercent = 20,
                HpThreshold = 70, Vfx = "heal", VfxAt = SkillVfxAt.Hero, Sound = "heal"
            },

            // Rare
            new SkillDef
            {
                Id = "fireball", Line = JobLine.Mage, Grade = Grade.Rare, Kind = SkillKind.Strike, Cooldown = 9f, Range = 4.5,
                DamageMult = 4.0, DotPercent = 40, DotSeconds = 4f, Vfx = "fire", Sound = "fire"
            },
            new SkillDef
            {
                Id = "frost_nova", Line = JobLine.Mage, Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 14f, Range = 2.8,
                DamageMult = 2.5, StunSeconds = 2f, Vfx = "ice", VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.2f,
                Sound = "ice"
            },
            new SkillDef
            {
                Id = "thunder", Line = JobLine.Warrior, Grade = Grade.Rare, Kind = SkillKind.Strike, Cooldown = 10f, Range = 5.0,
                DamageMult = 7.0, Vfx = "bolt", Sound = "thunder"
            },
            new SkillDef
            {
                Id = "berserk", Line = JobLine.Warrior, Grade = Grade.Rare, Kind = SkillKind.Buff, Cooldown = 20f, Range = 2.5,
                Buff = SkillBuff.AtkSpd, BuffAmount = 40, BuffSeconds = 8f, Vfx = "aura", VfxAt = SkillVfxAt.Hero,
                VfxScale = 1.3f, Tint = 0xFF4040, Sound = "cry"
            },
            new SkillDef
            {
                Id = "poison_cloud", Line = JobLine.Archer, Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 13f, Range = 3.5,
                DamageMult = 0.8, DotPercent = 60, DotSeconds = 5f, Vfx = "poison", VfxAt = SkillVfxAt.Front,
                VfxScale = 1.4f, Sound = "magic"
            },
            new SkillDef
            {
                Id = "eagle_eye", Line = JobLine.Archer, Grade = Grade.Rare, Kind = SkillKind.Buff, Cooldown = 20f, Range = 2.5,
                Buff = SkillBuff.Crit, BuffAmount = 40, BuffSeconds = 10f, Vfx = "aura", VfxAt = SkillVfxAt.Hero,
                VfxScale = 1.3f, Tint = 0xFFE040, Sound = "magic"
            },

            // Epic
            new SkillDef
            {
                Id = "meteor", Line = JobLine.Mage, Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 18f, Range = 5.0,
                DamageMult = 12.0, Vfx = "meteor", VfxAt = SkillVfxAt.Front, VfxScale = 1.5f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "chain_lightning", Line = JobLine.Mage, Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 12f, Range = 5.0,
                DamageMult = 2.5, Waves = 4, WaveInterval = 0.2f, Vfx = "bolt", VfxAt = SkillVfxAt.EachTarget,
                Tint = 0xC0A0FF, Sound = "thunder"
            },
            new SkillDef
            {
                Id = "blade_storm", Line = JobLine.Warrior, Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 15f, Range = 3.0,
                DamageMult = 1.0, Waves = 8, WaveInterval = 0.12f, Vfx = "tornado", VfxAt = SkillVfxAt.Front,
                VfxScale = 1.4f, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "holy_light", Line = JobLine.Archer, Grade = Grade.Epic, Kind = SkillKind.Heal, Cooldown = 22f, HealPercent = 35,
                ShieldPercent = 20, ShieldSeconds = 6f, HpThreshold = 60, Vfx = "holy", VfxAt = SkillVfxAt.Hero,
                Sound = "heal"
            },
            new SkillDef
            {
                Id = "glacier_spear", Line = JobLine.Archer, Grade = Grade.Epic, Kind = SkillKind.Strike, Cooldown = 16f, Range = 5.0,
                DamageMult = 10.0, StunSeconds = 3f, Vfx = "spear", VfxScale = 1.4f, Sound = "ice"
            },
            new SkillDef
            {
                Id = "war_god", Line = JobLine.Warrior, Grade = Grade.Epic, Kind = SkillKind.Buff, Cooldown = 30f, Range = 2.5,
                Buff = SkillBuff.Atk, BuffAmount = 60, BuffSeconds = 10f, ShieldPercent = 15, ShieldSeconds = 10f,
                Vfx = "aura", VfxAt = SkillVfxAt.Hero, VfxScale = 1.6f, Tint = 0xFFD040, Sound = "cry"
            },

            // Legendary
            new SkillDef
            {
                Id = "dragon_breath", Line = JobLine.Mage, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 20f, Range = 5.0,
                DamageMult = 2.6, Waves = 5, WaveInterval = 0.15f, DotPercent = 150, DotSeconds = 4f,
                Vfx = "breath", VfxAt = SkillVfxAt.Front, VfxScale = 1.2f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "judgement", Line = JobLine.Warrior, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 25f, Range = 6.0,
                DamageMult = 30.0, Vfx = "judge", VfxAt = SkillVfxAt.EachTarget, VfxScale = 0.8f, Sound = "thunder"
            },
            new SkillDef
            {
                Id = "time_stop", Line = JobLine.Archer, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 30f, Range = 8.0,
                DamageMult = 3.0, StunSeconds = 4f, Buff = SkillBuff.Atk, BuffAmount = 40, BuffSeconds = 4f,
                Vfx = "clock", VfxAt = SkillVfxAt.Front, VfxScale = 1.6f, Sound = "magic"
            },
            new SkillDef
            {
                Id = "sword_rain", Line = JobLine.Warrior, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 20f, Range = 5.0,
                DamageMult = 2.2, Waves = 10, WaveInterval = 0.1f, Vfx = "swords", VfxAt = SkillVfxAt.Front,
                VfxScale = 1.3f, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "phoenix", Line = JobLine.Archer, Grade = Grade.Legendary, Kind = SkillKind.Heal, Cooldown = 35f, HealPercent = 50,
                Buff = SkillBuff.Atk, BuffAmount = 50, BuffSeconds = 10f, HpThreshold = 50, Vfx = "phoenix",
                VfxAt = SkillVfxAt.Hero, VfxScale = 1.4f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "black_hole", Line = JobLine.Mage, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 28f, Range = 6.0,
                DamageMult = 3.0, Waves = 6, WaveInterval = 0.2f, StunSeconds = 1.5f, Vfx = "vortex",
                VfxAt = SkillVfxAt.Front, VfxScale = 1.6f, Sound = "magic"
            },

            // D-107 warrior
            new SkillDef
            {
                Id = "shield_bash", Line = JobLine.Warrior, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 8f, Range = 2.0,
                DamageMult = 3.2, StunSeconds = 1f, Vfx = "boom", VfxScale = 1.1f, Tint = 0xC8D8F0, Sound = "strike"
            },
            new SkillDef
            {
                Id = "ground_slam", Line = JobLine.Warrior, Grade = Grade.Common, Kind = SkillKind.Area, Cooldown = 10f, Range = 2.6,
                DamageMult = 1.8, Vfx = "boom", VfxAt = SkillVfxAt.EachTarget, Tint = 0xD0A070, Sound = "strike"
            },
            new SkillDef
            {
                Id = "cleave", Line = JobLine.Warrior, Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 11f, Range = 3.0,
                DamageMult = 3.5, Vfx = "whirl", VfxAt = SkillVfxAt.Front, VfxScale = 1.8f, Tint = 0xFFFFFF, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "earthquake", Line = JobLine.Warrior, Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 16f, Range = 4.5,
                DamageMult = 4.0, Waves = 3, WaveInterval = 0.25f, StunSeconds = 0.8f, Vfx = "boom",
                VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.3f, Tint = 0xC08040, Sound = "strike"
            },
            new SkillDef
            {
                Id = "dragon_slash", Line = JobLine.Warrior, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 22f, Range = 4.0,
                DamageMult = 7.0, Waves = 3, WaveInterval = 0.15f, Vfx = "cross", VfxAt = SkillVfxAt.Front, VfxScale = 2f,
                Tint = 0xFF6040, Sound = "whoosh"
            },

            // D-107 mage
            new SkillDef
            {
                Id = "magic_missile", Line = JobLine.Mage, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 7f, Range = 4.5,
                DamageMult = 1.2, Waves = 3, WaveInterval = 0.12f, Vfx = "bolt", Tint = 0xC890FF, Sound = "magic"
            },
            new SkillDef
            {
                Id = "ember", Line = JobLine.Mage, Grade = Grade.Common, Kind = SkillKind.Area, Cooldown = 10f, Range = 3.0,
                DamageMult = 1.2, DotPercent = 30, DotSeconds = 3f, Vfx = "fire", VfxAt = SkillVfxAt.EachTarget,
                VfxScale = 0.8f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "ice_lance", Line = JobLine.Mage, Grade = Grade.Rare, Kind = SkillKind.Strike, Cooldown = 10f, Range = 5.0,
                DamageMult = 5.5, StunSeconds = 1f, Vfx = "spear", VfxScale = 1.1f, Tint = 0xA0E0FF, Sound = "ice"
            },
            new SkillDef
            {
                Id = "flame_pillar", Line = JobLine.Mage, Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 15f, Range = 3.5,
                DamageMult = 3.0, Waves = 3, WaveInterval = 0.2f, DotPercent = 60, DotSeconds = 3f, Vfx = "fire",
                VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.4f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "arcane_storm", Line = JobLine.Mage, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 24f, Range = 6.0,
                DamageMult = 2.0, Waves = 8, WaveInterval = 0.12f, Vfx = "bolt", VfxAt = SkillVfxAt.EachTarget,
                Tint = 0x80C0FF, Sound = "thunder"
            },

            // D-107 archer
            new SkillDef
            {
                Id = "arrow_shot", Line = JobLine.Archer, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 6f, Range = 5.0,
                DamageMult = 3.0, Vfx = "spear", VfxScale = 0.8f, Tint = 0xE0D0A0, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "scatter_shot", Line = JobLine.Archer, Grade = Grade.Common, Kind = SkillKind.Area, Cooldown = 10f, Range = 4.0,
                DamageMult = 0.9, Waves = 2, WaveInterval = 0.15f, Vfx = "swords", VfxAt = SkillVfxAt.Front, VfxScale = 0.8f,
                Tint = 0xD8F0B0, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "piercing_arrow", Line = JobLine.Archer, Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 11f, Range = 6.0,
                DamageMult = 3.5, Vfx = "spear", VfxAt = SkillVfxAt.Front, VfxScale = 1.2f, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "storm_arrows", Line = JobLine.Archer, Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 15f, Range = 5.0,
                DamageMult = 1.2, Waves = 6, WaveInterval = 0.12f, Vfx = "swords", VfxAt = SkillVfxAt.Front,
                Tint = 0xC8F0A0, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "starfall_arrow", Line = JobLine.Archer, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 24f, Range = 7.0,
                DamageMult = 4.0, Waves = 4, WaveInterval = 0.2f, Vfx = "judge", VfxAt = SkillVfxAt.EachTarget,
                VfxScale = 0.7f, Tint = 0xB0FFB0, Sound = "thunder"
            },

            // D-109 warrior
            new SkillDef
            {
                Id = "rending_blade", Line = JobLine.Warrior, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 8f, Range = 2.0,
                DamageMult = 2.0, DotPercent = 50, DotSeconds = 4f, Vfx = "slash", VfxScale = 1.2f, Tint = 0xFF7070, Sound = "strike"
            },
            new SkillDef
            {
                Id = "armor_break", Line = JobLine.Warrior, Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 12f, Range = 2.6,
                DamageMult = 2.5, MarkPercent = 25, MarkSeconds = 6f, Vfx = "mark", VfxAt = SkillVfxAt.EachTarget,
                Tint = 0xFFB060, Sound = "strike"
            },
            new SkillDef
            {
                Id = "valor", Line = JobLine.Warrior, Grade = Grade.Epic, Kind = SkillKind.Buff, Cooldown = 28f, Range = 2.5,
                Buff = SkillBuff.Haste, BuffAmount = 50, BuffSeconds = 10f, ShieldPercent = 15, ShieldSeconds = 10f,
                Vfx = "aura", VfxAt = SkillVfxAt.Hero, VfxScale = 1.5f, Tint = 0x60E0FF, Sound = "cry"
            },
            new SkillDef
            {
                Id = "titan_crush", Line = JobLine.Warrior, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 24f, Range = 3.5,
                DamageMult = 20.0, StunSeconds = 1.5f, MarkPercent = 30, MarkSeconds = 6f, Vfx = "wave", VfxAt = SkillVfxAt.Front,
                VfxScale = 2.2f, Tint = 0xFFC060, Sound = "thunder"
            },

            // D-109 mage
            new SkillDef
            {
                Id = "spark", Line = JobLine.Mage, Grade = Grade.Common, Kind = SkillKind.Area, Cooldown = 9f, Range = 4.0,
                DamageMult = 0.8, Waves = 3, WaveInterval = 0.15f, Vfx = "spark", VfxAt = SkillVfxAt.EachTarget,
                Tint = 0xFFF080, Sound = "thunder"
            },
            new SkillDef
            {
                Id = "hex", Line = JobLine.Mage, Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 13f, Range = 5.0,
                DamageMult = 1.5, DotPercent = 30, DotSeconds = 6f, MarkPercent = 30, MarkSeconds = 6f, Vfx = "mark",
                VfxAt = SkillVfxAt.EachTarget, Tint = 0xC070FF, Sound = "magic"
            },
            new SkillDef
            {
                Id = "mana_surge", Line = JobLine.Mage, Grade = Grade.Epic, Kind = SkillKind.Buff, Cooldown = 26f, Range = 2.5,
                Buff = SkillBuff.Haste, BuffAmount = 60, BuffSeconds = 8f, Vfx = "aura", VfxAt = SkillVfxAt.Hero, VfxScale = 1.5f,
                Tint = 0x70A0FF, Sound = "magic"
            },
            new SkillDef
            {
                Id = "absolute_zero", Line = JobLine.Mage, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 26f, Range = 6.0,
                DamageMult = 1.8, Waves = 10, WaveInterval = 0.15f, StunSeconds = 0.4f, Vfx = "ice", VfxAt = SkillVfxAt.EachTarget,
                VfxScale = 0.9f, Sound = "ice"
            },

            // D-109 archer
            new SkillDef
            {
                Id = "rapid_shot", Line = JobLine.Archer, Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 6f, Range = 5.0,
                DamageMult = 1.6, Waves = 2, WaveInterval = 0.12f, Vfx = "spear", VfxScale = 0.8f, Tint = 0xFFF0C0, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "hunters_mark", Line = JobLine.Archer, Grade = Grade.Rare, Kind = SkillKind.Strike, Cooldown = 12f, Range = 6.0,
                DamageMult = 3.0, MarkPercent = 30, MarkSeconds = 6f, Vfx = "mark", VfxScale = 1.2f, Tint = 0xFF5050, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "deadeye", Line = JobLine.Archer, Grade = Grade.Epic, Kind = SkillKind.Strike, Cooldown = 18f, Range = 8.0,
                DamageMult = 16.0, Vfx = "spear", VfxScale = 1.6f, Sound = "thunder"
            },
            new SkillDef
            {
                Id = "phantom_volley", Line = JobLine.Archer, Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 24f, Range = 7.0,
                DamageMult = 2.0, Waves = 9, WaveInterval = 0.1f, MarkPercent = 20, MarkSeconds = 5f, Vfx = "swords",
                VfxAt = SkillVfxAt.Front, VfxScale = 1.4f, Tint = 0xC890FF, Sound = "whoosh"
            },
        };

        private static readonly SkillDef[][] ByGrade = BuildByGrade();
        private static readonly SkillDef[][][] ByLine = BuildByLine();

        public static int Count => All.Length;

        public static SkillDef Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }

            return null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return i;
            }

            return -1;
        }

        public static SkillDef[] OfGrade(Grade grade)
        {
            int g = (int)grade;
            if (g < 0 || g >= ByGrade.Length) throw new ArgumentOutOfRangeException(nameof(grade));
            return ByGrade[g];
        }

        /// <summary>D-107: the skills of one job line and grade (the skill summon pool).</summary>
        public static SkillDef[] OfLine(JobLine line, Grade grade)
        {
            int l = (int)line;
            int g = (int)grade;
            if (l < 0 || l >= ByLine.Length) throw new ArgumentOutOfRangeException(nameof(line));
            if (g < 0 || g >= GradeCount) throw new ArgumentOutOfRangeException(nameof(grade));
            return ByLine[l][g];
        }

        private static SkillDef[][][] BuildByLine()
        {
            var result = new SkillDef[4][][];
            for (int l = 0; l < result.Length; l++)
            {
                result[l] = new SkillDef[GradeCount][];
                for (int g = 0; g < GradeCount; g++)
                {
                    var list = new System.Collections.Generic.List<SkillDef>();
                    for (int i = 0; i < All.Length; i++)
                        if ((int)All[i].Line == l && (int)All[i].Grade == g) list.Add(All[i]);
                    result[l][g] = list.ToArray();
                }
            }

            return result;
        }

        private static SkillDef[][] BuildByGrade()
        {
            var result = new SkillDef[GradeCount][];
            for (int g = 0; g < result.Length; g++)
            {
                int n = 0;
                for (int i = 0; i < All.Length; i++)
                {
                    if ((int)All[i].Grade == g) n++;
                }

                result[g] = new SkillDef[n];
                n = 0;
                for (int i = 0; i < All.Length; i++)
                {
                    if ((int)All[i].Grade == g) result[g][n++] = All[i];
                }
            }

            return result;
        }
    }
}
