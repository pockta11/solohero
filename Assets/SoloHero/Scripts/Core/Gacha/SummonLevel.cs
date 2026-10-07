using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Gacha
{
    public enum SummonKind
    {
        Gear = 0,
        Skill = 1,
        Pet = 2
    }

    /// <summary>
    /// D-115 summon level (genre: the summon levels up as you pull): each summon counts its own cumulative pulls, level
    /// L is reached at STEP x (L - 1) x L / 2 pulls, and the level multiplies the good grades' rates by
    /// 1 + SUMMON_LV_RATE_GAIN x (L - 1) while the lowest grade gives up the difference. D-121: a grade drops only from
    /// its open level (SUMMON_OPEN_*). Derived from the saved pull counters, so it needs no save field. The pity rule
    /// is untouched.
    /// </summary>
    public static class SummonLevel
    {
        /// <summary>
        /// The first grade a level boosts: Rare on the seven-grade gear ladder, Epic on the four-grade skill one (both
        /// are index 2).
        /// </summary>
        public const int FirstBoostedGrade = 2;

        public static int Step(BalanceValues balance, SummonKind kind)
        {
            switch (kind)
            {
                case SummonKind.Skill: return balance.SUMMON_LV_STEP_SKILL;
                case SummonKind.Pet: return balance.SUMMON_LV_STEP_PET;
                default: return balance.SUMMON_LV_STEP_GEAR;
            }
        }

        public static int Pulls(SaveDataV2 data, SummonKind kind)
        {
            if (data == null) return 0;
            switch (kind)
            {
                case SummonKind.Skill: return data.skillPullCount;
                case SummonKind.Pet: return data.petPullCount;
                default: return data.totalPullCount;
            }
        }

        /// <summary>Cumulative pulls at which <paramref name="level"/> is reached (0 for level 1).</summary>
        public static int PullsFor(BalanceValues balance, SummonKind kind, int level)
        {
            if (level <= 1) return 0;
            return Step(balance, kind) * (level - 1) * level / 2;
        }

        public static int LevelOf(BalanceValues balance, SummonKind kind, int pulls)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            int level = 1;
            while (level < balance.SUMMON_LV_MAX && pulls >= PullsFor(balance, kind, level + 1)) level++;
            return level;
        }

        public static int Of(BalanceValues balance, SaveDataV2 data, SummonKind kind) => LevelOf(balance, kind, Pulls(data, kind));

        /// <summary>Multiplier of the good grades' rates at <paramref name="level"/>.</summary>
        public static double RateMult(double gain, int level) => 1d + gain * (level < 1 ? 0 : level - 1);

        /// <summary>
        /// Writes the level's rates (percent, lowest grade first) into <paramref name="into"/>: grades from
        /// <see cref="FirstBoostedGrade"/> up are multiplied, the lowest grade takes what is left of 100.
        /// </summary>
        public static void Boost(double[] baseRates, double[] into, double mult) => Boost(baseRates, into, mult, null, 1);

        /// <summary>
        /// D-121: as <see cref="Boost(double[], double[], double)"/>, and a grade whose open level (index-aligned,
        /// 0 or 1 = always) is above <paramref name="level"/> gets 0; the lowest grade takes its share too.
        /// </summary>
        public static void Boost(double[] baseRates, double[] into, double mult, int[] openLevels, int level)
        {
            if (baseRates == null) throw new ArgumentNullException(nameof(baseRates));
            if (into == null || into.Length != baseRates.Length) throw new ArgumentException("size mismatch", nameof(into));
            double sum = 0d;
            for (int g = 1; g < baseRates.Length; g++)
            {
                bool open = openLevels == null || openLevels[g] <= level;
                into[g] = !open ? 0d : g >= FirstBoostedGrade ? baseRates[g] * mult : baseRates[g];
                sum += into[g];
            }

            into[0] = Math.Max(0d, 100d - sum);
        }

        /// <summary>
        /// D-121: the grade that opens exactly at <paramref name="level"/> (-1 when none does), from an open-level
        /// array index-aligned with the grades.
        /// </summary>
        public static int GradeOpeningAt(int[] openLevels, int level)
        {
            if (openLevels == null || level <= 1) return -1;
            for (int g = 0; g < openLevels.Length; g++)
            {
                if (openLevels[g] == level) return g;
            }

            return -1;
        }
    }
}
