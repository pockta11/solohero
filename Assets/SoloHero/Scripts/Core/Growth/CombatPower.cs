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

        /// <summary>The save's combat power right now (stats from the loadout, the equipped pet).</summary>
        public static double OfSave(BalanceValues balance, SoloHero.Core.Save.SaveDataV2 save) =>
            Of(balance, SoloHero.Core.Stage.CombatLoadout.ComputeStats(balance, save), SoloHero.Core.Pets.PetService.EquippedRate(balance, save));

        /// <summary>
        /// D-127 recommended combat power for stage <paramref name="g"/>: about what players have when they first clear
        /// it (sim-calibrated), a boss a little more.
        /// </summary>
        public static double Recommended(BalanceValues balance, int g)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            return Recommended(balance, g, SoloHero.Core.Stage.StageIndex.IsBoss(g < 1 ? 1 : g, balance.STAGES_PER_CHAPTER));
        }

        /// <summary>As above for a fight that is (or is not) a boss whatever the stage number (D-130 tower floors).</summary>
        public static double Recommended(BalanceValues balance, int g, bool boss)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (g < 1) g = 1;
            double cp = balance.REC_CP_BASE * Math.Pow(balance.REC_CP_GROWTH, g - 1);
            if (boss) cp *= balance.REC_CP_BOSS_MULT;
            return Math.Floor(cp);
        }
    }
}
