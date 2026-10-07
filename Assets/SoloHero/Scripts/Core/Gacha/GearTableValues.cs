using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Gacha
{
    /// <summary>
    /// D-113 gear rate table: a disclosed percent per <see cref="GearGrade"/> and a pity ceiling that guarantees
    /// <see cref="PityGrade"/>. Skill summons keep the four-grade <see cref="GachaTableValues"/>.
    /// </summary>
    public sealed class GearTableValues
    {
        public const GearGrade PityGrade = GearGrade.Legendary;

        /// <summary>Percent per grade, index = <see cref="GearGrade"/>.</summary>
        public readonly double[] Rates = new double[GearGrades.Count];

        public int PityCeiling;

        /// <summary>A natural pull of <see cref="PityGrade"/> or better resets the pity counter.</summary>
        public bool ResetOnPityGrade;

        public double RatesSum
        {
            get
            {
                double sum = 0d;
                for (int g = 0; g < Rates.Length; g++) sum += Rates[g];
                return sum;
            }
        }

        public double Rate(GearGrade grade) => Rates[(int)grade];

        public static GearTableValues FromBalance(BalanceValues balance)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            var table = new GearTableValues
            {
                PityCeiling = balance.GEAR_PITY,
                ResetOnPityGrade = balance.GACHA_PITY_RESET_ON_LEGENDARY
            };
            table.Rates[(int)GearGrade.Common] = balance.GEAR_RATE_C;
            table.Rates[(int)GearGrade.Uncommon] = balance.GEAR_RATE_U;
            table.Rates[(int)GearGrade.Rare] = balance.GEAR_RATE_R;
            table.Rates[(int)GearGrade.Epic] = balance.GEAR_RATE_E;
            table.Rates[(int)GearGrade.Legendary] = balance.GEAR_RATE_L;
            table.Rates[(int)GearGrade.Mythic] = balance.GEAR_RATE_M;
            table.Rates[(int)GearGrade.Ancient] = balance.GEAR_RATE_A;
            return table;
        }

        /// <summary>
        /// Maps a unit-interval sample in [0, 1) to a grade through the cumulative percent table, lowest grade first;
        /// whatever is left above the second-highest boundary is the highest grade.
        /// </summary>
        public GearGrade PickGrade(double unitInterval)
        {
            double threshold = unitInterval * 100d;
            double cumulative = 0d;
            for (int g = 0; g < Rates.Length - 1; g++)
            {
                cumulative += Rates[g];
                if (threshold < cumulative) return (GearGrade)g;
            }

            return (GearGrade)(Rates.Length - 1);
        }
    }
}
