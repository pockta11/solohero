import sys, glob, os, statistics

# Summarise lt.exe dumps: python analyze.py <dir> [<dir> ...]
CHECK = [7, 30, 60, 90, 120, 180, 240, 365]

def label(g):
    if g <= 0:
        return "0"
    return "%d-%d" % ((g - 1) // 10 + 1, (g - 1) % 10 + 1)

def load(fn):
    days, stages = [], []
    head = ""
    for line in open(fn, encoding="utf-8"):
        p = line.split()
        if not p:
            continue
        if p[0] == "#":
            head = line.strip()
        elif p[0] == "D" and p[1] != "day":
            days.append([float(x) if "E" in x or "." in x else int(x) for x in p[1:]])
        elif p[0] == "S" and p[1] != "g":
            stages.append([float(x) if "E" in x or "." in x else int(x) for x in p[1:]])
    return head, days, stages

def stall_stats(days):
    prev, last_new, longest = 0, 0, 0
    new = {}
    for row in days:
        d, g = row[0], row[1]
        if g > prev:
            new[d] = True
            longest = max(longest, d - last_new - 1)
            last_new = d
            prev = g
    cur = days[-1][0] - last_new
    longest = max(longest, cur)
    trailing = {}
    for row in days:
        d = row[0]
        trailing[d] = sum(1 for k in range(max(1, d - 29), d + 1) if new.get(k))
    min_from90 = min(trailing[d] for d in trailing if d >= 90) if any(d >= 90 for d in trailing) else None
    return longest, trailing, min_from90

def main():
    for folder in sys.argv[1:]:
        files = sorted(glob.glob(os.path.join(folder, "*.txt")))
        if not files:
            continue
        print("=====", folder, len(files), "runs")
        per_day = {d: [] for d in CHECK}
        lv_day = {d: [] for d in CHECK}
        tw_day = {d: [] for d in CHECK}
        longs, mins = [], []
        for fn in files:
            head, days, stages = load(fn)
            byday = {r[0]: r for r in days}
            longest, trailing, min90 = stall_stats(days)
            longs.append(longest)
            mins.append(min90 if min90 is not None else -1)
            parts = []
            for d in CHECK:
                if d in byday:
                    r = byday[d]
                    per_day[d].append(r[1]); lv_day[d].append(r[2]); tw_day[d].append(r[3])
                    parts.append("d%d %s L%d t%d" % (d, label(r[1]), r[2], r[3]))
            name = os.path.basename(fn).replace(".txt", "")
            print("%-22s %s | stall %d | new30 min(d90+) %s | new30@365 %s" % (name, " | ".join(parts), longest, min90, trailing.get(365)))
        print("-- median over runs")
        for d in CHECK:
            if per_day[d]:
                gs = sorted(per_day[d])
                med = statistics.median(gs)
                print("d%-3d %s ~ %s (median g%.0f = %s)  lv %d-%d  tower %d-%d  gap(g-t) med %.0f" % (
                    d, label(gs[0]), label(gs[-1]), med, label(int(round(med))), min(lv_day[d]), max(lv_day[d]),
                    min(tw_day[d]), max(tw_day[d]), statistics.median([a - b for a, b in zip(per_day[d], tw_day[d])])))
        print("longest stall: median %s max %s | new30 min from d90: median %s min %s" % (
            statistics.median(longs), max(longs), statistics.median(mins), min(mins)))

main()
