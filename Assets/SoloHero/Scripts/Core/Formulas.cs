using System;
using SoloHero.Core.Config;
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

        public static double HitDamage(BalanceValues c, double enemyAtk, double def)
        {
            double defRef = c.DEF_REF_MULT * enemyAtk;
            double denom = defRef + def;
            if (denom == 0d) return 1d;
            return Math.Max(1d, enemyAtk * defRef / denom);
        }
    }
}
