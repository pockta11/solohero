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
            double mult = skillMult * (isCrit ? balance.CRIT_MULT + stats.CritDamageBonus : 1d);
            return Math.Max(1d, stats.Atk * mult);
        }

        public static bool RollCrit(HeroStats stats, IRandom random, double bonusPoints = 0d)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return random.NextDouble() < (stats.CritRate + bonusPoints) * 0.01d;
        }

        /// <summary>Damage taken after a Guard buff (fraction) and before the shield.</summary>
        public static double Guarded(double damage, double guardFraction)
        {
            if (guardFraction <= 0d) return damage;
            if (guardFraction > 0.9d) guardFraction = 0.9d;
            return damage * (1d - guardFraction);
        }

        /// <summary>Burn / poison damage per second: ATK x percent x level scale.</summary>
        public static double SkillDot(HeroStats stats, double percent, double levelScale) =>
            Math.Max(1d, stats.Atk * percent / 100d * levelScale);

        public static double EnemyHit(BalanceValues balance, double enemyAtk, double heroDef) =>
            Formulas.HitDamage(balance, enemyAtk, heroDef);

        public static double SkillHit(HeroStats stats, double skillMult) =>
            Math.Max(1d, stats.Atk * skillMult);

        /// <summary>
        /// D-098 skill combo: a skill hit on a stunned / frozen enemy shatters (x SKILL_SHATTER_MULT), else on a
        /// burning / poisoned one ignites (x SKILL_IGNITE_MULT); otherwise 1. The bigger one wins, they never stack.
        /// </summary>
        public static double SkillCombo(BalanceValues balance, bool stunned, bool burning)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (stunned) return balance.SKILL_SHATTER_MULT;
            return burning ? balance.SKILL_IGNITE_MULT : 1d;
        }
    }
}
