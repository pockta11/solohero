using System;
using System.Text;
using SoloHero.Core.Common;
using SoloHero.Core.Config;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// D-153 (L-02) difficulty tiers, the genre's "same lands, harder" ladder: every TIER_CHAPTERS chapters is one tier -
    /// normal (chapters 1-10), hard (11-20), hell, nightmare, abyss, then abyss II, III and on. Nothing resets: g keeps
    /// climbing and every stat still follows g. A tier only renames a stage ("hard 3-7" is 13-7) and colours it.
    /// </summary>
    public static class DifficultyTier
    {
        /// <summary>Tiers with a name of their own; later tiers repeat the last name with a numeral.</summary>
        public const int NamedTiers = 5;

        public static int ChaptersPerTier(BalanceValues b) => b == null || b.TIER_CHAPTERS < 1 ? 10 : b.TIER_CHAPTERS;

        /// <summary>0 = normal, 1 = hard, ... for a global chapter number.</summary>
        public static int OfChapter(BalanceValues b, int chapter) => chapter < 1 ? 0 : (chapter - 1) / ChaptersPerTier(b);

        public static int Of(BalanceValues b, int g)
        {
            StageIndex.FromGlobal(g, b.STAGES_PER_CHAPTER, out int chapter, out _);
            return OfChapter(b, chapter);
        }

        /// <summary>The chapter number inside its tier (13 -> 3).</summary>
        public static int ChapterInTier(BalanceValues b, int chapter) => chapter < 1 ? 1 : (chapter - 1) % ChaptersPerTier(b) + 1;

        public static int StagesPerTier(BalanceValues b) => ChaptersPerTier(b) * b.STAGES_PER_CHAPTER;

        /// <summary>The 1-1 of <paramref name="tier"/>.</summary>
        public static int FirstStage(BalanceValues b, int tier) => (tier < 0 ? 0 : tier) * StagesPerTier(b) + 1;

        /// <summary>The last boss of a tier: its first clear opens the next tier.</summary>
        public static bool IsLastBoss(BalanceValues b, int g) => g > 0 && g % StagesPerTier(b) == 0;

        /// <summary>The string key of the tier's name; tiers past the named ones share the last name.</summary>
        public static string NameKey(int tier) => "tier.name." + Math.Max(0, Math.Min(tier, NamedTiers - 1));

        /// <summary>The numeral after a repeated name: abyss II is tier 5, abyss III tier 6. 0 for the named tiers.</summary>
        public static int Rank(int tier) => tier < NamedTiers ? 0 : tier - NamedTiers + 2;

        /// <summary>"Hard", "Abyss II" ... from the string table.</summary>
        public static string Name(int tier)
        {
            string name = Strings.Get(NameKey(tier));
            int rank = Rank(tier);
            return rank > 0 ? name + " " + Roman(rank) : name;
        }

        /// <summary>
        /// A stage as the player reads it: "3-7" in the normal tier, "Hard 3-7" past it (tier name from the strings).
        /// </summary>
        public static string StageLabel(BalanceValues b, int g)
        {
            StageIndex.FromGlobal(g, b.STAGES_PER_CHAPTER, out int chapter, out int stage);
            int tier = OfChapter(b, chapter);
            int inTier = ChapterInTier(b, chapter);
            return tier == 0 ? inTier + "-" + stage : Strings.Format("tier.stage", Name(tier), inTier, stage);
        }

        /// <summary>A chapter as the player reads it: "chapter 3" in the normal tier, "Hard chapter 3" past it.</summary>
        public static string ChapterLabel(BalanceValues b, int chapter)
        {
            int tier = OfChapter(b, chapter);
            int inTier = ChapterInTier(b, chapter);
            return tier == 0 ? Strings.Format("stage.chapter", inTier) : Strings.Format("tier.chapter", Name(tier), inTier);
        }

        public static string Roman(int n)
        {
            if (n <= 0) return "";
            int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] digits = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length && n > 0; i++)
            {
                while (n >= values[i])
                {
                    sb.Append(digits[i]);
                    n -= values[i];
                }
            }

            return sb.ToString();
        }
    }
}
