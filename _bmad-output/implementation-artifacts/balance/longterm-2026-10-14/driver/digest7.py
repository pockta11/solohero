import sys, re

# Digest of driver.cs -v output files: day 1/3/7 ranges, and per check id the PASS/FAIL counts with measured values.
d1, d3, d7 = [], [], []
checks = {}
for fn in sys.argv[1:]:
    for line in open(fn, encoding="utf-8", errors="replace"):
        m = re.search(r"d1=(\d+)-(\d+) d3=(\d+)-(\d+) d7=(\d+)-(\d+)", line)
        if m:
            c1, s1, c3, s3, c7, s7 = map(int, m.groups())
            d1.append((c1 - 1) * 10 + s1); d3.append((c3 - 1) * 10 + s3); d7.append((c7 - 1) * 10 + s7)
            continue
        m = re.match(r"\s+(Pass|Fail|Info) (\S+) .* measured (.*)$", line)
        if m:
            status, cid, measured = m.groups()
            checks.setdefault(cid, []).append((status, measured.strip()))

def lab(g):
    return "%d-%d" % ((g - 1) // 10 + 1, (g - 1) % 10 + 1)

if d1:
    print("d1 %s~%s  d3 %s~%s  d7 %s~%s  (n %d)" % (lab(min(d1)), lab(max(d1)), lab(min(d3)), lab(max(d3)), lab(min(d7)), lab(max(d7)), len(d1)))
for cid in ["V-1a", "V-1b", "V-1c", "V-2", "V-4", "V-6a", "V-6b", "V-7", "V-8", "E9-04", "E9-05", "FS-5"]:
    if cid in checks:
        rows = checks[cid]
        fails = sum(1 for s, _ in rows if s == "Fail")
        vals = [m.split(" (")[0] for _, m in rows]
        print("%-6s fail %d/%d  %s" % (cid, fails, len(rows), " ".join(vals[:18])))
