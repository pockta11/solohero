using System.Collections.Generic;

namespace SoloHero.Core.Events
{
    /// <summary>D-156: one scheduled event - which template runs, from when to when (UTC seconds, end exclusive).</summary>
    public sealed class ScheduleEntry
    {
        public string Id = "";
        public string Template = "";
        public long StartUtc;
        public long EndUtc;

        public bool RunsAt(long utc) => utc >= StartUtc && utc < EndUtc;
    }

    /// <summary>
    /// D-156 event schedule: the server's list (RTDB config/events, read-only for players) or, without a network, the
    /// last list the device saw. One event runs at a time: the running entry with the latest start whose template the
    /// app knows. An unknown template (newer than the app) is skipped.
    /// </summary>
    public static class EventSchedule
    {
        public static ScheduleEntry ActiveAt(IReadOnlyList<ScheduleEntry> entries, long utc)
        {
            if (entries == null) return null;
            ScheduleEntry best = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ScheduleEntry e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.Id) || !e.RunsAt(utc) || EventCatalog.Find(e.Template) == null) continue;
                if (best == null || e.StartUtc > best.StartUtc) best = e;
            }

            return best;
        }

        /// <summary>
        /// D-173: the pickup banner of <paramref name="kind"/> running at <paramref name="utc"/> (latest start wins). Pickups
        /// run beside the event: their templates are banner ids, which <see cref="ActiveAt"/> skips as unknown.
        /// </summary>
        public static ScheduleEntry PickupAt(IReadOnlyList<ScheduleEntry> entries, long utc, SoloHero.Core.Gacha.SummonKind kind)
        {
            if (entries == null) return null;
            ScheduleEntry best = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ScheduleEntry e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.Id) || !e.RunsAt(utc)) continue;
                SoloHero.Core.Gacha.PickupBanner banner = PickupCatalog.Find(e.Template);
                if (banner == null || banner.Kind != kind) continue;
                if (best == null || e.StartUtc > best.StartUtc) best = e;
            }

            return best;
        }
    }
}
