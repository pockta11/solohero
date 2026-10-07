using SoloHero.Core.Config;

namespace SoloHero.Core.Gacha
{
    public sealed class GachaTableValues
    {
        public double RateC;
        public double RateR;
        public double RateE;
        public double RateL;
        public int PityCeiling;
        public bool ResetOnLegendary;

        /// <summary>D-115: how much each summon level raises the good grades (SUMMON_LV_RATE_GAIN) and the level cap.</summary>
        public double LevelRateGain;
        public int MaxLevel = 1;

        /// <summary>D-121 / D-123: summon levels from which Rare, Epic and Legendary can drop (0 or 1 = from the start).</summary>
        public int OpenR;
        public int OpenE;
        public int OpenL;

        private GachaTableValues[] _byLevel;

        public double RatesSum => RateC + RateR + RateE + RateL;

        public static GachaTableValues FromBalance(BalanceValues balance)
        {
            return new GachaTableValues
            {
                RateC = balance.GACHA_RATE_C,
                RateR = balance.GACHA_RATE_R,
                RateE = balance.GACHA_RATE_E,
                RateL = balance.GACHA_RATE_L,
                PityCeiling = balance.GACHA_PITY,
                ResetOnLegendary = balance.GACHA_PITY_RESET_ON_LEGENDARY,
                LevelRateGain = balance.SUMMON_LV_RATE_GAIN,
                MaxLevel = balance.SUMMON_LV_MAX,
                OpenR = balance.SKILL_SUMMON_OPEN_R,
                OpenE = balance.SKILL_SUMMON_OPEN_E,
                OpenL = balance.SKILL_SUMMON_OPEN_L
            };
        }

        /// <summary>D-121: open levels index-aligned with <see cref="Rates"/> (Common always open).</summary>
        public int[] OpenLevels => new[] { 0, OpenR, OpenE, OpenL };

        /// <summary>D-121: the summon level from which <paramref name="grade"/> can drop (1 = from the start).</summary>
        public int OpenLevel(Grade grade)
        {
            switch (grade)
            {
                case Grade.Rare: return System.Math.Max(1, OpenR);
                case Grade.Epic: return System.Math.Max(1, OpenE);
                case Grade.Legendary: return System.Math.Max(1, OpenL);
                default: return 1;
            }
        }

        /// <summary>D-123: whether this (level) table can give a Legendary; the pity only counts while it can.</summary>
        public bool PityOpen => RateL > 0d;

        /// <summary>The four rates, Common first.</summary>
        public double[] Rates => new[] { RateC, RateR, RateE, RateL };

        /// <summary>
        /// D-115 / D-121: this table at a summon level, boosted and with the grades not yet open at 0 (the table itself
        /// when it has neither); cached per level. A derived table has no levels of its own.
        /// </summary>
        public GachaTableValues AtLevel(int level)
        {
            if (LevelRateGain <= 0d && OpenR <= 1 && OpenE <= 1 && OpenL <= 1) return this;
            if (level < 1) level = 1;
            if (level > MaxLevel) level = System.Math.Max(1, MaxLevel);
            if (_byLevel == null) _byLevel = new GachaTableValues[System.Math.Max(1, MaxLevel) + 1];
            GachaTableValues table = _byLevel[level];
            if (table != null) return table;
            var rates = new double[4];
            SummonLevel.Boost(Rates, rates, SummonLevel.RateMult(LevelRateGain, level), OpenLevels, level);
            table = new GachaTableValues
            {
                RateC = rates[0], RateR = rates[1], RateE = rates[2], RateL = rates[3],
                PityCeiling = PityCeiling, ResetOnLegendary = ResetOnLegendary, MaxLevel = MaxLevel
            };
            _byLevel[level] = table;
            return table;
        }

        /// <summary>
        /// Maps a unit-interval sample in [0, 1) to a grade via the cumulative percent table.
        /// Boundaries: [0, 0.55) Common, [0.55, 0.88) Rare, [0.88, 0.98) Epic, [0.98, 1) Legendary.
        /// </summary>
        public Grade PickGrade(double unitInterval)
        {
            double threshold = unitInterval * 100d;
            if (threshold < RateC) return Grade.Common;
            if (threshold < RateC + RateR) return Grade.Rare;
            if (threshold < RateC + RateR + RateE) return Grade.Epic;
            return Grade.Legendary;
        }
    }
}
