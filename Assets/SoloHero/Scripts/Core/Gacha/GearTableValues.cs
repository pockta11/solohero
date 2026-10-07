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

        /// <summary>D-115: how much each summon level raises the good grades (SUMMON_LV_RATE_GAIN) and the level cap.</summary>
        public double LevelRateGain;
        public int MaxLevel = 1;

        /// <summary>D-121: summon level from which each grade can drop (0 or 1 = from the start), index = grade.</summary>
        public readonly int[] OpenLevels = new int[GearGrades.Count];

        private GearTableValues[] _byLevel;

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
                ResetOnPityGrade = balance.GACHA_PITY_RESET_ON_LEGENDARY,
                LevelRateGain = balance.SUMMON_LV_RATE_GAIN,
                MaxLevel = balance.SUMMON_LV_MAX
            };
            table.OpenLevels[(int)GearGrade.Uncommon] = balance.SUMMON_OPEN_U;
            table.OpenLevels[(int)GearGrade.Rare] = balance.SUMMON_OPEN_R;
            table.OpenLevels[(int)GearGrade.Epic] = balance.SUMMON_OPEN_E;
            table.OpenLevels[(int)GearGrade.Legendary] = balance.SUMMON_OPEN_L;
            table.OpenLevels[(int)GearGrade.Mythic] = balance.SUMMON_OPEN_M;
            table.OpenLevels[(int)GearGrade.Ancient] = balance.SUMMON_OPEN_A;
            table.Rates[(int)GearGrade.Common] = balance.GEAR_RATE_C;
            table.Rates[(int)GearGrade.Uncommon] = balance.GEAR_RATE_U;
            table.Rates[(int)GearGrade.Rare] = balance.GEAR_RATE_R;
            table.Rates[(int)GearGrade.Epic] = balance.GEAR_RATE_E;
            table.Rates[(int)GearGrade.Legendary] = balance.GEAR_RATE_L;
            table.Rates[(int)GearGrade.Mythic] = balance.GEAR_RATE_M;
            table.Rates[(int)GearGrade.Ancient] = balance.GEAR_RATE_A;
            return table;
        }

        /// <summary>D-121: the summon level from which <paramref name="grade"/> can drop (1 = from the start).</summary>
        public int OpenLevel(GearGrade grade) => Math.Max(1, OpenLevels[(int)grade]);

        /// <summary>D-123: whether this (level) table can give the pity grade; the pity only counts while it can.</summary>
        public bool PityOpen => Rates[(int)PityGrade] > 0d;

        /// <summary>
        /// D-115 / D-121: this table at a summon level, boosted and with the grades not yet open at 0 (the table itself
        /// when it has neither); cached per level. A derived table has no levels of its own.
        /// </summary>
        public GearTableValues AtLevel(int level)
        {
            if (!HasLevels) return this;
            if (level < 1) level = 1;
            if (level > MaxLevel) level = Math.Max(1, MaxLevel);
            if (_byLevel == null) _byLevel = new GearTableValues[Math.Max(1, MaxLevel) + 1];
            GearTableValues table = _byLevel[level];
            if (table != null) return table;
            table = new GearTableValues { PityCeiling = PityCeiling, ResetOnPityGrade = ResetOnPityGrade, MaxLevel = MaxLevel };
            SummonLevel.Boost(Rates, table.Rates, SummonLevel.RateMult(LevelRateGain, level), OpenLevels, level);
            _byLevel[level] = table;
            return table;
        }

        private bool HasLevels
        {
            get
            {
                if (LevelRateGain > 0d) return true;
                for (int g = 0; g < OpenLevels.Length; g++)
                {
                    if (OpenLevels[g] > 1) return true;
                }

                return false;
            }
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
