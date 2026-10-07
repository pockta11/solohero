using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Growth
{
    /// <summary>
    /// D-111 combat power (the genre CP number): one number that rises with every upgrade, item and level, shown in the HUD
    /// with a "+N" pop. Damage per second (ATK x attack speed x average crit) and staying power (HP, DEF) are weighted
    /// so all four upgrade lanes move it visibly.
    /// </summary>
    public static class CombatPower
    {
        public const double AtkWeight = 8d;
        public const double HpWeight = 0.8d;
        public const double DefWeight = 3d;

        /// <summary>D-114: the equipped pet's damage per second (hero ATK multiples) counts at this share of a hero hit rate.</summary>
        public const double PetWeight = 0.35d;

        /// <param name="petRate">D-114: the equipped pet's damage per second in hero ATK multiples (0 without a pet).</param>
        public static double Of(BalanceValues balance, HeroStats stats, double petRate = 0d)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            double crit = Math.Min(1d, Math.Max(0d, stats.CritRate / 100d));
            double critFactor = 1d + crit * (balance.CRIT_MULT - 1d + stats.CritDamageBonus);
            double power = stats.Atk * AtkWeight * (stats.AtkSpd * critFactor + petRate * PetWeight)
                + stats.Hp * HpWeight + stats.Def * DefWeight;
            return Math.Floor(power);
        }
    }
}
