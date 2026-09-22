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
                ResetOnLegendary = balance.GACHA_PITY_RESET_ON_LEGENDARY
            };
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
