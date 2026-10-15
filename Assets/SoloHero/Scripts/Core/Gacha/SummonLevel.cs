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
    /// D-159 (L-09): past SUMMON_LV_LATE_FROM a level takes SUMMON_LV_LATE_STEP_MULT times the pulls, the good grades
    /// keep that level's boost and only the top grades (from <see cref="FirstTopGrade"/>: Mythic and Ancient) rise by
    /// SUMMON_LV_TOP_GAIN per level; each of those levels pays tickets (<see cref="RewardTickets"/>).
    /// </summary>
    public static class SummonLevel
    {
        /// <summary>
        /// The first grade a level boosts: Rare on the seven-grade gear ladder, Epic on the four-grade skill one (both
        /// are index 2).
        /// </summary>
        public const int FirstBoostedGrade = 2;

        /// <summary>D-159: the first grade the late levels raise (Mythic on the seven-grade ladder; none on the skill one).</summary>
        public const int FirstTopGrade = 5;

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
            int step = Step(balance, kind);
            int late = balance.SUMMON_LV_LATE_FROM;
            if (late < 1 || level <= late) return step * (level - 1) * level / 2;
            // D-159: every step past the late level costs LATE_STEP_MULT times as much.
            int tail = (level - 1) * level / 2 - (late - 1) * late / 2;
            return step * (late - 1) * late / 2 + balance.SUMMON_LV_LATE_STEP_MULT * step * tail;
        }

        /// <summary>D-159: tickets of its own summon for reaching <paramref name="level"/> (late levels only).</summary>
        public static int RewardTickets(BalanceValues balance, SummonKind kind, int level)
        {
            if (balance.SUMMON_LV_LATE_FROM < 1 || level <= balance.SUMMON_LV_LATE_FROM || level > balance.SUMMON_LV_MAX) return 0;
            switch (kind)
            {
                case SummonKind.Skill: return balance.SUMMON_LV_REWARD_SKILL;
                case SummonKind.Pet: return balance.SUMMON_LV_REWARD_PET;
                default: return balance.SUMMON_LV_REWARD_GEAR;
            }
        }

        /// <summary>
        /// D-159: pays the tickets of every late level crossed between <paramref name="before"/> and
        /// <paramref name="after"/> (the summon level around one pull or batch). Returns the tickets paid.
        /// </summary>
        public static int PayLevelRewards(BalanceValues balance, SaveDataV2 data, SummonKind kind, int before, int after)
        {
            if (balance == null || data == null) return 0;
            int paid = 0;
            for (int level = before + 1; level <= after; level++) paid += RewardTickets(balance, kind, level);
            if (paid <= 0) return 0;
            switch (kind)
            {
                case SummonKind.Skill: data.skillTickets += paid; break;
                case SummonKind.Pet: data.petTickets += paid; break;
                default: data.gearTickets += paid; break;
            }

            return paid;
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
        /// D-159: the good grades' multiplier with the late levels held at the late level's boost
        /// (<paramref name="lateFrom"/> 0 = no late levels).
        /// </summary>
        public static double RateMult(double gain, int level, int lateFrom) =>
            RateMult(gain, lateFrom > 0 && level > lateFrom ? lateFrom : level);

        /// <summary>D-159: the extra multiplier of the top grades at a late level (1 below it).</summary>
        public static double TopMult(double topGain, int level, int lateFrom) =>
            lateFrom > 0 && level > lateFrom ? 1d + topGain * (level - lateFrom) : 1d;

        /// <summary>
        /// Writes the level's rates (percent, lowest grade first) into <paramref name="into"/>: grades from
        /// <see cref="FirstBoostedGrade"/> up are multiplied, the lowest grade takes what is left of 100.
        /// </summary>
        public static void Boost(double[] baseRates, double[] into, double mult) => Boost(baseRates, into, mult, null, 1);

        /// <summary>
        /// D-121: as <see cref="Boost(double[], double[], double)"/>, and a grade whose open level (index-aligned,
        /// 0 or 1 = always) is above <paramref name="level"/> gets 0; the lowest grade takes its share too.
        /// </summary>
        public static void Boost(double[] baseRates, double[] into, double mult, int[] openLevels, int level) =>
            Boost(baseRates, into, mult, openLevels, level, 1d);

        /// <summary>D-159: as above, and the grades from <see cref="FirstTopGrade"/> up take <paramref name="topMult"/> on top.</summary>
        public static void Boost(double[] baseRates, double[] into, double mult, int[] openLevels, int level, double topMult)
        {
            if (baseRates == null) throw new ArgumentNullException(nameof(baseRates));
            if (into == null || into.Length != baseRates.Length) throw new ArgumentException("size mismatch", nameof(into));
            double sum = 0d;
            for (int g = 1; g < baseRates.Length; g++)
            {
                bool open = openLevels == null || openLevels[g] <= level;
                double gradeMult = g >= FirstTopGrade ? mult * topMult : g >= FirstBoostedGrade ? mult : 1d;
                into[g] = !open ? 0d : baseRates[g] * gradeMult;
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
