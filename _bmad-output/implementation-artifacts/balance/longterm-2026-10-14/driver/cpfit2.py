import sys, glob, os, math, statistics

# Continuous two-segment fit of the median first-clear CP:
#   log rec(g) = a + b * (min(g, S) - 1) + c * max(g - S, 0)
# python cpfit2.py <start S> <min runs> <dir> [<dir> ...]
S = int(sys.argv[1])
MINRUNS = int(sys.argv[2])
GMIN = int(os.environ.get("GMIN", "9"))
normal, boss = {}, {}
for folder in sys.argv[3:]:
    for fn in glob.glob(os.path.join(folder, "*.txt")):
        for line in open(fn, encoding="utf-8"):
            p = line.split()
            if len(p) > 4 and p[0] == "S" and p[1] != "g":
                g, isboss, cp = int(p[1]), int(p[2]), float(p[4])
                if cp > 0:
                    (boss if isboss else normal).setdefault(g, []).append(cp)

pts = [(g, math.log(statistics.median(v))) for g, v in sorted(normal.items()) if g >= GMIN and len(v) >= MINRUNS]

def solve(rows):
    # normal equations for 3 unknowns
    import itertools
    m = [[0.0] * 3 for _ in range(3)]
    r = [0.0] * 3
    for x, y in rows:
        for i in range(3):
            r[i] += x[i] * y
            for j in range(3):
                m[i][j] += x[i] * x[j]
    # gaussian elimination
    for i in range(3):
        piv = m[i][i]
        for j in range(i + 1, 3):
            f = m[j][i] / piv
            for k in range(3):
                m[j][k] -= f * m[i][k]
            r[j] -= f * r[i]
    sol = [0.0] * 3
    for i in reversed(range(3)):
        sol[i] = (r[i] - sum(m[i][k] * sol[k] for k in range(i + 1, 3))) / m[i][i]
    return sol

rows = [([1.0, min(g, S) - 1.0, max(g - S, 0.0)], y) for g, y in pts]
a, b, c = solve(rows)
base, growth, late = math.exp(a), math.exp(b), math.exp(c)
print("fit S=%d: base %.1f growth %.4f late %.4f (points %d)" % (S, base, growth, late, len(pts)))

def rec(g):
    return math.exp(a + b * (min(g, S) - 1) + c * max(g - S, 0))

worst = 0
for g, y in pts:
    ratio = math.exp(y) / rec(g)
    worst = max(worst, abs(math.log(ratio)))
for g in [9, 13, 21, 29, 41, 49, 57, 65, 69, 73, 81, 89, 97, 111, 131, 151, 181, 211, 241, 271, 301, 331]:
    if g in normal and len(normal[g]) >= MINRUNS:
        m = statistics.median(normal[g])
        print("  g%-4d median %.3g fit %.3g ratio %.2f n %d" % (g, m, rec(g), m / rec(g), len(normal[g])))
print("worst |log ratio| %.2f (x%.2f)" % (worst, math.exp(worst)))
ratios = []
for g, v in boss.items():
    if len(v) >= MINRUNS and g >= 20:
        ratios.append(statistics.median(v) / rec(g))
print("boss median first clear / normal fit, per chapter median: %.2f (p25 %.2f p75 %.2f, n %d)" % (
    statistics.median(ratios), sorted(ratios)[len(ratios) // 4], sorted(ratios)[3 * len(ratios) // 4], len(ratios)))
