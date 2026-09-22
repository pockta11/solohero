using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Core.Combat
{
    public static class DamageCalc
    {
        public static double HeroHit(HeroStats stats, bool isCrit, BalanceValues balance, double skillMult = 1d)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            double mult = skillMult * (isCrit ? balance.CRIT_MULT : 1d);
            return Math.Max(1d, stats.Atk * mult);
        }

        public static bool RollCrit(HeroStats stats, IRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return random.NextDouble() < stats.CritRate * 0.01d;
        }

        public static double EnemyHit(BalanceValues balance, double enemyAtk, double heroDef) =>
            Formulas.HitDamage(balance, enemyAtk, heroDef);

        public static double SkillHit(HeroStats stats, double skillMult) =>
            Math.Max(1d, stats.Atk * skillMult);
    }
}
