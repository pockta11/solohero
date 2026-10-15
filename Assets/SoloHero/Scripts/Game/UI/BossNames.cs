using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Stage;

namespace SoloHero.Game.UI
{
    /// <summary>
    /// Chapter boss names (D-131: ten chapter looks). Chapters past the tenth reuse the names cyclically like the
    /// themes (StageIndex.ThemeIndex); D-153 puts the difficulty tier's prefix in front ("Fallen Crystal Golem").
    /// </summary>
    public static class BossNames
    {
        public const int Count = 10;

        public static string KeyFor(int chapter)
        {
            int index = ((chapter - 1) % Count + Count) % Count;
            return "boss.name." + (index + 1);
        }

        /// <summary>The boss name of a global chapter with its tier prefix (none in the normal tier).</summary>
        public static string NameFor(BalanceValues balance, int chapter)
        {
            string name = Strings.Get(KeyFor(chapter));
            int tier = balance != null ? DifficultyTier.OfChapter(balance, chapter) : 0;
            if (tier <= 0) return name;
            string prefix = Strings.Get("tier.boss_prefix." + System.Math.Min(tier, DifficultyTier.NamedTiers - 1));
            return Strings.Format("tier.boss", prefix, name);
        }
    }
}
