using System;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;

namespace SoloHero.Core
{
    public static class Formulas
    {
        public static double EnemyHp(BalanceValues c, int g) =>
            c.ENEMY_HP_BASE * Math.Pow(c.ENEMY_HP_GROWTH, g - 1);

        public static double EnemyAtk(BalanceValues c, int g) =>
            c.ENEMY_ATK_BASE * Math.Pow(c.ENEMY_ATK_GROWTH, g - 1);

        public static double StageGold(BalanceValues c, int g) =>
            c.STAGE_GOLD_BASE * Math.Pow(c.STAGE_GOLD_GROWTH, g - 1);

        public static double StageClearGold(BalanceValues c, int g, bool isBoss) =>
            StageGold(c, g) * (isBoss ? c.BOSS_GOLD_MULT : 1d);

        public static double EnemyExp(BalanceValues c, int g, bool isBoss) =>
            c.ENEMY_EXP_BASE * Math.Pow(c.ENEMY_EXP_GROWTH, g - 1) * (isBoss ? c.BOSS_EXP_MULT : 1d);

        /// <summary>
        /// Equipment enhancement (D-062): each level multiplies the grade's base effect by (1 + EQUIP_ENHANCE_GAIN),
        /// so gear keeps pace with the compounding upgrade lanes instead of fading late.
        /// </summary>
        public static double EnhancedEffect(BalanceValues c, double baseEffect, int level) =>
            baseEffect * Math.Pow(1d + c.EQUIP_ENHANCE_GAIN, level < 0 ? 0 : level);

        public static double UpgradeCost(BalanceValues c, UpgradeLane lane, int level)
        {
            double baseCost;
            switch (lane)
            {
                case UpgradeLane.Hp: baseCost = c.UPG_BASE_HP; break;
                case UpgradeLane.Atk: baseCost = c.UPG_BASE_ATK; break;
                case UpgradeLane.Def: baseCost = c.UPG_BASE_DEF; break;
                case UpgradeLane.Spd: baseCost = c.UPG_BASE_SPD; break;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }

            return baseCost * Math.Pow(c.UPG_COST_GROWTH, level);
        }

        /// <summary>Skill level scaling (D-078): every percent value of a skill x (1 + SKILL_LEVEL_GAIN% x (level - 1)).</summary>
        public static double SkillLevelScale(BalanceValues c, int level) =>
            1d + c.SKILL_LEVEL_GAIN / 100d * ((level < 1 ? 1 : level) - 1);

        /// <summary>Gold to raise a skill of <paramref name="grade"/> from <paramref name="level"/> to the next.</summary>
        public static double SkillUpgradeCost(BalanceValues c, Grade grade, int level)
        {
            double baseCost;
            switch (grade)
            {
                case Grade.Common: baseCost = c.SKILL_UPG_BASE_C; break;
                case Grade.Rare: baseCost = c.SKILL_UPG_BASE_R; break;
                case Grade.Epic: baseCost = c.SKILL_UPG_BASE_E; break;
                case Grade.Legendary: baseCost = c.SKILL_UPG_BASE_L; break;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }

            return baseCost * Math.Pow(c.SKILL_UPG_COST_GROWTH, (level < 1 ? 1 : level) - 1);
        }

        /// <summary>Owned effect of one skill (genre "collection bonus"): ATK +grade% x level scale, as a fraction.</summary>
        public static double SkillOwnedAtk(BalanceValues c, Grade grade, int level)
        {
            double percent;
            switch (grade)
            {
                case Grade.Common: percent = c.SKILL_OWNED_ATK_C; break;
                case Grade.Rare: percent = c.SKILL_OWNED_ATK_R; break;
                case Grade.Epic: percent = c.SKILL_OWNED_ATK_E; break;
                case Grade.Legendary: percent = c.SKILL_OWNED_ATK_L; break;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }

            return percent / 100d * SkillLevelScale(c, level);
        }

        public static double SkillRefund(BalanceValues c, Grade grade)
        {
            switch (grade)
            {
                case Grade.Common: return c.SKILL_REFUND_C;
                case Grade.Rare: return c.SKILL_REFUND_R;
                case Grade.Epic: return c.SKILL_REFUND_E;
                case Grade.Legendary: return c.SKILL_REFUND_L;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }
        }

        public static double HitDamage(BalanceValues c, double enemyAtk, double def)
        {
            double defRef = c.DEF_REF_MULT * enemyAtk;
            double denom = defRef + def;
            if (denom == 0d) return 1d;
            return Math.Max(1d, enemyAtk * defRef / denom);
        }
    }
}
