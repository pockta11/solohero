using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    /// <summary>D-141 AP stats: the job line's main stat (STR / INT / DEX, raises ATK) and vitality (raises HP).</summary>
    public enum ApStat
    {
        Main,
        Vit
    }

    /// <summary>AP put into each stat (value copy of the save fields).</summary>
    public readonly struct ApPoints
    {
        public readonly int Main;
        public readonly int Vit;

        public ApPoints(int main, int vit)
        {
            Main = main;
            Vit = vit;
        }

        public static ApPoints From(SaveDataV2 data) =>
            data == null ? default : new ApPoints(data.apMain, data.apVit);

        /// <summary>The auto split of every point a hero of <paramref name="heroLevel"/> has (sim, previews, tests).</summary>
        public static ApPoints Auto(BalanceValues balance, int heroLevel)
        {
            int main = 0;
            int vit = 0;
            HeroAp.Split(balance, HeroAp.Total(balance, heroLevel), ref main, ref vit);
            return new ApPoints(main, vit);
        }

        public ApPoints Plus(ApStat stat, int count) =>
            stat == ApStat.Main ? new ApPoints(Main + count, Vit) : new ApPoints(Main, Vit + count);
    }

    /// <summary>
    /// D-141 level-up AP rules (MapleStory style). Every level past 1 grants AP_PER_LEVEL points. In auto mode (the
    /// default, apManual false) <see cref="Settle"/> spends new points at once, AP_AUTO_MAIN : AP_AUTO_VIT, which gives
    /// exactly the old flat gains per level; in manual mode they wait for the player. Saves from before AP load with
    /// nothing allocated, so the first settle hands out every past level in the auto split.
    /// </summary>
    public static class HeroAp
    {
        public static int Total(BalanceValues balance, int heroLevel)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            return heroLevel <= 1 ? 0 : balance.AP_PER_LEVEL * (heroLevel - 1);
        }

        public static int Spent(SaveDataV2 data) => data == null ? 0 : data.apMain + data.apVit;

        public static int Unspent(BalanceValues balance, SaveDataV2 data)
        {
            if (data == null) return 0;
            int left = Total(balance, data.heroLevel) - Spent(data);
            return left < 0 ? 0 : left;
        }

        /// <summary>
        /// Repairs impossible allocations (more spent than earned, negatives: everything back to unspent) and, in auto
        /// mode, spends every unspent point. Call after level-ups, at boot and when auto is switched on. True when the
        /// allocation changed.
        /// </summary>
        public static bool Settle(BalanceValues balance, SaveDataV2 data)
        {
            if (balance == null || data == null) return false;
            bool changed = false;
            if (data.apMain < 0 || data.apVit < 0 || Spent(data) > Total(balance, data.heroLevel))
            {
                data.apMain = 0;
                data.apVit = 0;
                changed = true;
            }

            if (data.apManual) return changed;
            int left = Unspent(balance, data);
            if (left <= 0) return changed;
            int main = data.apMain;
            int vit = data.apVit;
            Split(balance, left, ref main, ref vit);
            data.apMain = main;
            data.apVit = vit;
            return true;
        }

        /// <summary>
        /// Adds <paramref name="points"/> one by one to whichever stat is furthest below the auto ratio, so a fresh
        /// hero holds exactly AP_AUTO_MAIN : AP_AUTO_VIT after every level.
        /// </summary>
        internal static void Split(BalanceValues balance, int points, ref int main, ref int vit)
        {
            int wMain = Math.Max(0, balance.AP_AUTO_MAIN);
            int wVit = Math.Max(0, balance.AP_AUTO_VIT);
            if (wMain + wVit == 0) wMain = 1;
            for (int i = 0; i < points; i++)
            {
                if ((long)main * wVit <= (long)vit * wMain && wMain > 0) main++;
                else vit++;
            }
        }

        /// <summary>Base ATK a point of the main stat adds, as a share of ATK_BASE (the stat window shows it as %).</summary>
        public static double MainAtkShare(BalanceValues balance, int points) =>
            balance.ATK_BASE > 0d ? balance.AP_MAIN_ATK * points / balance.ATK_BASE : 0d;

        /// <summary>Base HP the vitality points add, as a share of HP_BASE.</summary>
        public static double VitHpShare(BalanceValues balance, int points) =>
            balance.HP_BASE > 0d ? balance.AP_VIT_HP * points / balance.HP_BASE : 0d;
    }
}
