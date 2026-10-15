using System;
using SoloHero.Core.Common;

namespace SoloHero.Core.Gacha
{
    /// <summary>
    /// D-173 (L-19) a pickup banner: for a while, one pet (or, for skills, one skill of each job line) takes
    /// PICKUP_SHARE % of its grade's draws. Grade rates do not move; only the split inside the grade does.
    /// </summary>
    public sealed class PickupBanner
    {
        public string Id { get; init; } = "";
        public SummonKind Kind { get; init; }

        /// <summary>The boosted ids; only those in the hero's pool count (skills: one per line).</summary>
        public string[] Targets { get; init; } = new string[0];

        public string NameKey => "pickup.name." + Id;

        /// <summary>Index in <paramref name="pool"/> of a target of this banner, or -1.</summary>
        public int IndexIn<T>(T[] pool, Func<T, string> idOf)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                string id = idOf(pool[i]);
                for (int t = 0; t < Targets.Length; t++)
                    if (Targets[t] == id) return i;
            }

            return -1;
        }
    }

    /// <summary>D-173: the running pickup of a summon (the event schedule implements it).</summary>
    public interface IPickupSource
    {
        /// <summary>The banner running now for <paramref name="kind"/>, or null.</summary>
        PickupBanner ActivePickup(SummonKind kind);

        /// <summary>Seconds until that banner ends (0 when none runs).</summary>
        long PickupSecondsLeft(SummonKind kind);
    }

    /// <summary>D-173: the pick inside a drawn grade, with or without a pickup.</summary>
    public static class Pickup
    {
        /// <summary>
        /// A uniform pick in <paramref name="pool"/>; while <paramref name="banner"/> has a target in it, the target
        /// takes <paramref name="sharePercent"/> % and the rest split the remainder evenly.
        /// </summary>
        public static int PickIndex<T>(T[] pool, Func<T, string> idOf, PickupBanner banner, double sharePercent, IRandom rng)
        {
            int target = banner != null ? banner.IndexIn(pool, idOf) : -1;
            if (target < 0 || pool.Length < 2) return rng.Next(pool.Length);
            if (rng.NextDouble() * 100d < sharePercent) return target;
            int r = rng.Next(pool.Length - 1);
            return r >= target ? r + 1 : r;
        }

        /// <summary>Disclosed rate (%) of the target, given its grade's rate (%).</summary>
        public static double TargetRate(double gradeRate, double sharePercent) => gradeRate * sharePercent / 100d;

        /// <summary>Disclosed rate (%) of each other item of that grade.</summary>
        public static double OtherRate(double gradeRate, double sharePercent, int poolSize) =>
            poolSize < 2 ? 0d : gradeRate * (100d - sharePercent) / 100d / (poolSize - 1);
    }
}
