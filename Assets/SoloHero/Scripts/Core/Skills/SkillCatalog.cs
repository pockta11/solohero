using System;
using SoloHero.Core.Gacha;

namespace SoloHero.Core.Skills
{
    /// <summary>
    /// The skill data table (D-078): 24 skills, 6 per grade, modelled on the idle-RPG skill summon (Soul Strike,
    /// Seven Knights Idle, Mushroom Warrior). Like <see cref="GachaCatalog"/> this is the single source for the
    /// content; ids are saved, so never rename one. Grade-wide economy (summon rates, costs, refunds, owned bonus,
    /// level-up cost) lives in BalanceValues. The first three are the starter skills every hero owns.
    /// </summary>
    public static class SkillCatalog
    {
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
                Id = "quick_slash", Grade = Grade.Common, Kind = SkillKind.Strike, Cooldown = 7f, Range = 2.0,
                DamageMult = 1.1, Waves = 3, WaveInterval = 0.15f, Vfx = "cross", VfxScale = 1.2f,
                Tint = 0xFFFFFF, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "iron_skin", Grade = Grade.Common, Kind = SkillKind.Buff, Cooldown = 18f, Range = 2.5,
                Buff = SkillBuff.Guard, BuffAmount = 25, BuffSeconds = 8f, Vfx = "shield", VfxAt = SkillVfxAt.Hero,
                Sound = "magic"
            },
            new SkillDef
            {
                Id = "first_aid", Grade = Grade.Common, Kind = SkillKind.Heal, Cooldown = 15f, HealPercent = 20,
                HpThreshold = 70, Vfx = "heal", VfxAt = SkillVfxAt.Hero, Sound = "heal"
            },

            // Rare
            new SkillDef
            {
                Id = "fireball", Grade = Grade.Rare, Kind = SkillKind.Strike, Cooldown = 9f, Range = 4.5,
                DamageMult = 4.0, DotPercent = 40, DotSeconds = 4f, Vfx = "fire", Sound = "fire"
            },
            new SkillDef
            {
                Id = "frost_nova", Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 14f, Range = 2.8,
                DamageMult = 2.5, StunSeconds = 2f, Vfx = "ice", VfxAt = SkillVfxAt.EachTarget, VfxScale = 1.2f,
                Sound = "ice"
            },
            new SkillDef
            {
                Id = "thunder", Grade = Grade.Rare, Kind = SkillKind.Strike, Cooldown = 10f, Range = 5.0,
                DamageMult = 7.0, Vfx = "bolt", Sound = "thunder"
            },
            new SkillDef
            {
                Id = "berserk", Grade = Grade.Rare, Kind = SkillKind.Buff, Cooldown = 20f, Range = 2.5,
                Buff = SkillBuff.AtkSpd, BuffAmount = 40, BuffSeconds = 8f, Vfx = "aura", VfxAt = SkillVfxAt.Hero,
                VfxScale = 1.3f, Tint = 0xFF4040, Sound = "cry"
            },
            new SkillDef
            {
                Id = "poison_cloud", Grade = Grade.Rare, Kind = SkillKind.Area, Cooldown = 13f, Range = 3.5,
                DamageMult = 0.8, DotPercent = 60, DotSeconds = 5f, Vfx = "poison", VfxAt = SkillVfxAt.Front,
                VfxScale = 1.4f, Sound = "magic"
            },
            new SkillDef
            {
                Id = "eagle_eye", Grade = Grade.Rare, Kind = SkillKind.Buff, Cooldown = 20f, Range = 2.5,
                Buff = SkillBuff.Crit, BuffAmount = 40, BuffSeconds = 10f, Vfx = "aura", VfxAt = SkillVfxAt.Hero,
                VfxScale = 1.3f, Tint = 0xFFE040, Sound = "magic"
            },

            // Epic
            new SkillDef
            {
                Id = "meteor", Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 18f, Range = 5.0,
                DamageMult = 12.0, Vfx = "meteor", VfxAt = SkillVfxAt.Front, VfxScale = 1.5f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "chain_lightning", Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 12f, Range = 5.0,
                DamageMult = 2.5, Waves = 4, WaveInterval = 0.2f, Vfx = "bolt", VfxAt = SkillVfxAt.EachTarget,
                Tint = 0xC0A0FF, Sound = "thunder"
            },
            new SkillDef
            {
                Id = "blade_storm", Grade = Grade.Epic, Kind = SkillKind.Area, Cooldown = 15f, Range = 3.0,
                DamageMult = 1.0, Waves = 8, WaveInterval = 0.12f, Vfx = "tornado", VfxAt = SkillVfxAt.Front,
                VfxScale = 1.4f, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "holy_light", Grade = Grade.Epic, Kind = SkillKind.Heal, Cooldown = 22f, HealPercent = 35,
                ShieldPercent = 20, ShieldSeconds = 6f, HpThreshold = 60, Vfx = "holy", VfxAt = SkillVfxAt.Hero,
                Sound = "heal"
            },
            new SkillDef
            {
                Id = "glacier_spear", Grade = Grade.Epic, Kind = SkillKind.Strike, Cooldown = 16f, Range = 5.0,
                DamageMult = 10.0, StunSeconds = 3f, Vfx = "spear", VfxScale = 1.4f, Sound = "ice"
            },
            new SkillDef
            {
                Id = "war_god", Grade = Grade.Epic, Kind = SkillKind.Buff, Cooldown = 30f, Range = 2.5,
                Buff = SkillBuff.Atk, BuffAmount = 60, BuffSeconds = 10f, ShieldPercent = 15, ShieldSeconds = 10f,
                Vfx = "aura", VfxAt = SkillVfxAt.Hero, VfxScale = 1.6f, Tint = 0xFFD040, Sound = "cry"
            },

            // Legendary
            new SkillDef
            {
                Id = "dragon_breath", Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 20f, Range = 5.0,
                DamageMult = 2.6, Waves = 5, WaveInterval = 0.15f, DotPercent = 150, DotSeconds = 4f,
                Vfx = "breath", VfxAt = SkillVfxAt.Front, VfxScale = 1.2f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "judgement", Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 25f, Range = 6.0,
                DamageMult = 30.0, Vfx = "judge", VfxAt = SkillVfxAt.EachTarget, VfxScale = 0.8f, Sound = "thunder"
            },
            new SkillDef
            {
                Id = "time_stop", Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 30f, Range = 8.0,
                DamageMult = 3.0, StunSeconds = 4f, Buff = SkillBuff.Atk, BuffAmount = 40, BuffSeconds = 4f,
                Vfx = "clock", VfxAt = SkillVfxAt.Front, VfxScale = 1.6f, Sound = "magic"
            },
            new SkillDef
            {
                Id = "sword_rain", Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 20f, Range = 5.0,
                DamageMult = 2.2, Waves = 10, WaveInterval = 0.1f, Vfx = "swords", VfxAt = SkillVfxAt.Front,
                VfxScale = 1.3f, Sound = "whoosh"
            },
            new SkillDef
            {
                Id = "phoenix", Grade = Grade.Legendary, Kind = SkillKind.Heal, Cooldown = 35f, HealPercent = 50,
                Buff = SkillBuff.Atk, BuffAmount = 50, BuffSeconds = 10f, HpThreshold = 50, Vfx = "phoenix",
                VfxAt = SkillVfxAt.Hero, VfxScale = 1.4f, Sound = "fire"
            },
            new SkillDef
            {
                Id = "black_hole", Grade = Grade.Legendary, Kind = SkillKind.Area, Cooldown = 28f, Range = 6.0,
                DamageMult = 3.0, Waves = 6, WaveInterval = 0.2f, StunSeconds = 1.5f, Vfx = "vortex",
                VfxAt = SkillVfxAt.Front, VfxScale = 1.6f, Sound = "magic"
            },
        };

        private static readonly SkillDef[][] ByGrade = BuildByGrade();

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

        private static SkillDef[][] BuildByGrade()
        {
            var result = new SkillDef[GachaCatalog.GradeCount][];
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
