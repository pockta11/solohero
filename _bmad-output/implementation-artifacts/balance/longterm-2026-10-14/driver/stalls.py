import sys, os, glob, statistics

# Longest stall per period and fail mix per period: python stalls.py <dir> [<dir> ...]
PERIODS = [(1, 30), (31, 90), (91, 180), (181, 365)]

def load(fn):
    rows = []
    for line in open(fn, encoding="utf-8"):
        p = line.split()
        if p and p[0] == "D" and p[1] != "day":
            rows.append([int(x) if x.lstrip("-").isdigit() else x for x in p[1:]])
    return rows

for folder in sys.argv[1:]:
    per_period = {p: [] for p in PERIODS}
    fails = {p: [0, 0, 0, 0] for p in PERIODS}
    finals = []
    for fn in sorted(glob.glob(os.path.join(folder, "*.txt"))):
        rows = load(fn)
        prev, last_new = 0, 0
        stalls = []  # (start_day, end_day, length)
        for r in rows:
            d, g = r[0], r[1]
            if g > prev:
                stalls.append((last_new + 1, d - 1, d - last_new - 1))
                last_new, prev = d, g
            for p in PERIODS:
                if p[0] <= d <= p[1]:
                    f = fails[p]; f[0] += r[28]; f[1] += r[27]; f[2] += r[25]; f[3] += r[26]
        stalls.append((last_new + 1, rows[-1][0], rows[-1][0] - last_new))
        for p in PERIODS:
            # stall days that fall inside the period (clipped)
            longest = 0
            for a, b, n in stalls:
                lo, hi = max(a, p[0]), min(b, p[1])
                if hi >= lo:
                    longest = max(longest, hi - lo + 1)
            per_period[p].append(longest)
        finals.append(rows[-1][1])
    print("== %s (%d runs)" % (folder, len(finals)))
    for p in PERIODS:
        v = per_period[p]; f = fails[p]
        print("  d%d-%d longest stall median %s max %s | normal fail %.1f%% | boss fails %d deaths %d%%" % (
            p[0], p[1], statistics.median(v), max(v), 100.0 * f[0] / max(1, f[1]), f[2], 100 * f[3] // max(1, f[2])))
