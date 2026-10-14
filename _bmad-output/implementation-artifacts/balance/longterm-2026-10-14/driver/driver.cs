using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SoloHero.Core.Balance;
using SoloHero.Core.Config;

public static class Driver
{
    static readonly string[][] Paths =
    {
        new[] { "warrior", "knight" }, new[] { "warrior", "berserker" },
        new[] { "mage", "pyro" }, new[] { "mage", "cryo" },
        new[] { "archer", "ranger" }, new[] { "archer", "sniper" },
    };

    public static int Main(string[] args)
    {
        var b = new BalanceValues();
        bool ads = false; bool norebirth = false; bool notower = false; bool nodungeon = false; bool verbose = false; bool showDays = false; bool summary = false; bool lanes = false; bool cpfit = false; var cpN = new List<double[]>(); var cpB = new List<double[]>(); double dpsw = -1;
        int seeds = 1; int simDays = 7;
        string only = null;
        foreach (string a in args)
        {
            if (a == "--ads") { ads = true; continue; }
            if (a == "--norebirth") { norebirth = true; continue; }
            if (a == "--notower") { notower = true; continue; }
            if (a == "--nodungeon") { nodungeon = true; continue; }
            if (a == "-v") { verbose = true; continue; }
            if (a == "--days") { showDays = true; continue; }
            if (a == "-s") { summary = true; continue; }
            if (a == "--lanes") { lanes = true; continue; }
            if (a == "--cpfit") { cpfit = true; continue; }
            if (a.StartsWith("--dpsw=")) { dpsw = double.Parse(a.Substring(7), System.Globalization.CultureInfo.InvariantCulture); continue; }
            if (a.StartsWith("--seeds=")) { seeds = int.Parse(a.Substring(8)); continue; }
            if (a.StartsWith("--days=")) { simDays = int.Parse(a.Substring(7)); continue; }
            if (a.StartsWith("--path=")) { only = a.Substring(7); continue; }
            int eq = a.IndexOf('=');
            if (eq < 0) continue;
            string name = a.Substring(0, eq), val = a.Substring(eq + 1);
            FieldInfo f = typeof(BalanceValues).GetField(name);
            if (f == null) { Console.WriteLine("unknown field " + name); return 2; }
            f.SetValue(b, Convert.ChangeType(val, f.FieldType, System.Globalization.CultureInfo.InvariantCulture));
        }

        var failCounts = new Dictionary<string, int>();
        var dl = new List<int>(); var rb = new List<string>(); var d1 = new List<int>(); var d3 = new List<int>(); var d7 = new List<int>(); var meds = new List<double>(); var pshare = new List<double>(); var v7 = new List<string>(); var v2 = new List<string>(); var gshare = new List<double>(); var lvl = new List<int>();
        foreach (var p in Paths)
        {
            if (only != null && p[1] != only) continue;
            for (int s = 1; s <= seeds; s++)
            {
                var set = ads ? SimSettings.WithAds(s) : SimSettings.NoAds(s);
                set.Job1 = p[0]; set.Job2 = p[1]; set.Days = simDays; if (notower) set.UseTower = false; if (nodungeon) set.UseDungeons = false; if (dpsw > 0) set.DpsWeight = dpsw;
                var t0 = DateTime.Now;
                SimReport r = BalanceSimulator.Run(b, set);
                var checks = BalanceChecks.Evaluate(r, b);
                string days = string.Join(" ", r.Days.Where(d => d.Day == 1 || d.Day == 3 || d.Day == 7)
                    .Select(d => "d" + d.Day + "=" + BalanceChecks.Label(d.HighestStage)));
                double gacha = r.Days.Sum(d => d.SpentGacha), skill = r.Days.Sum(d => d.SpentSkill), upg = r.Days.Sum(d => d.SpentUpgrade), pet = r.Days.Sum(d => d.SpentPet);
                double tot = gacha + skill + upg + pet;
                var med = r.ClearStats.Values.Where(c => c.Count > 0).Select(c => c.Average).OrderBy(x => x).ToList();
                double median = med.Count > 0 ? med[med.Count / 2] : 0;
                var last = r.Stages.LastOrDefault();
                d1.Add(r.Days[0].HighestStage); dl.Add(r.Days.Last().HighestStage); d3.Add(r.Days[2].HighestStage); d7.Add(r.Days[6].HighestStage); meds.Add(median);
                gshare.Add(gacha / tot); pshare.Add(pet / tot); lvl.Add(r.Days.Last().HeroLevel);
                if (showDays)
                {
                    Console.WriteLine(p[1] + " s" + s + ": " + string.Join(" ", r.Days.Select(d => "d" + d.Day + "=" + BalanceChecks.Label(d.HighestStage) + "/L" + d.HeroLevel + "/p" + (d.GoldPulls + d.GemPulls) + "/bf" + d.BossFails + "/t" + d.TowerFloor)));
                    var bossRows = r.Stages.Where(x => x.IsBoss).Select(x => BalanceChecks.Label(x.G) + "@d" + x.Day + " att" + x.Attempts + " stuck" + (x.StuckPlaySeconds / 60).ToString("F0") + "m g" + x.SwordGrade + x.HelmGrade + x.ArmorGrade + x.BootsGrade);
                    Console.WriteLine("    bosses: " + string.Join(" | ", bossRows));
                }
                if (cpfit)
                {
                    foreach (var row in r.Stages)
                    {
                        if (row.Cp <= 0) continue;
                        (row.IsBoss ? cpB : cpN).Add(new double[] { row.G, Math.Log(row.Cp) });
                    }
                }
                if (lanes)
                {
                    var parts = new List<string>();
                    foreach (var d in r.Days)
                    {
                        var row = r.Stages.Where(x => x.Day == d.Day).LastOrDefault();
                        if (row == null) continue;
                        parts.Add("d" + d.Day + " L" + row.HeroLevel + " h" + row.UpgradeHp + " a" + row.UpgradeAtk + " d" + row.UpgradeDef + " s" + row.UpgradeSpd + " c" + row.UpgradeCrit + "/" + row.UpgradeCritDmg + " t" + d.TowerFloor + " st" + d.Stones + " br" + d.Breaks);
                    }
                    Console.WriteLine("  " + p[1] + " s" + s + " | " + string.Join(" | ", parts));
                }
                foreach (var c in checks) { if (c.Id == "V-7") v7.Add(c.Measured.Split(' ')[0]); if (c.Id == "V-2") v2.Add(c.Measured.Replace("median ", "").Replace(" s, inside ", "/")); }
                if (summary) continue;
                Console.WriteLine(string.Format("{0,-18} s{1} {2}  med {3:F1}s  spend upg {4:P0} gacha {5:P0} skill {6:P0} pet {10:P0}  lv {7} pulls {8} petpulls {11} ({9:F1}s)",
                    p[0] + "/" + p[1], s, days, median, upg / tot, gacha / tot, skill / tot,
                    r.Days.Last().HeroLevel, r.Days.Sum(d => d.GoldPulls + d.GemPulls), (DateTime.Now - t0).TotalSeconds, pet / tot, r.Days.Sum(d => d.PetPulls)));
                foreach (var c in checks)
                {
                    if (c.Status != SimCheckStatus.Fail && !verbose) continue;
                    string key = c.Id;
                    failCounts[key] = failCounts.TryGetValue(key, out int n) ? n + 1 : 1;
                    Console.WriteLine("    " + c.Status + " " + c.Id + " " + c.Title + " target " + c.Target + " measured " + c.Measured);
                }
            }
        }
        if (cpfit && cpN.Count > 2)
        {
            double n = cpN.Count, sx = cpN.Sum(v => v[0]), sy = cpN.Sum(v => v[1]), sxx = cpN.Sum(v => v[0] * v[0]), sxy = cpN.Sum(v => v[0] * v[1]);
            double b1 = (n * sxy - sx * sy) / (n * sxx - sx * sx), a0 = (sy - b1 * sx) / n;
            Console.WriteLine("cp fit normal: base(g=1) " + Math.Exp(a0 + b1).ToString("F1") + " growth " + Math.Exp(b1).ToString("F4") + " n " + n);
            var resid = cpB.Select(v => v[1] - (a0 + b1 * v[0])).OrderBy(x => x).ToList();
            if (resid.Count > 0) Console.WriteLine("cp boss / normal-fit median " + Math.Exp(resid[resid.Count / 2]).ToString("F3") + " n " + resid.Count);
            var rn = cpN.Select(v => v[1] - (a0 + b1 * v[0])).OrderBy(x => x).ToList();
            Console.WriteLine("normal resid p10 " + Math.Exp(rn[(int)(rn.Count * 0.1)]).ToString("F3") + " p50 " + Math.Exp(rn[rn.Count / 2]).ToString("F3") + " p90 " + Math.Exp(rn[(int)(rn.Count * 0.9)]).ToString("F3"));
            foreach (int g in new[] { 1, 5, 10, 15, 20, 25, 30, 35, 40 })
            {
                var near = cpN.Where(v => Math.Abs(v[0] - g) <= 1).Select(v => Math.Exp(v[1])).OrderBy(x => x).ToList();
                if (near.Count > 0) Console.WriteLine("  g" + g + " median first-clear cp " + near[near.Count / 2].ToString("F0") + "  fit " + Math.Exp(a0 + b1 * g).ToString("F0"));
            }
        }
        if (summary)
        {
            Func<List<int>, string> lab = l => string.Join(",", l.Select(BalanceChecks.Label));
            Console.WriteLine("d1 " + lab(d1) + " | d3 " + lab(d3) + " | d7 " + lab(d7) + " | lv " + string.Join(",", lvl));
            if (simDays > 7) Console.WriteLine("   last day " + lab(dl));
            Console.WriteLine("   V-2 " + string.Join(" ", v2) + " | V-7 " + string.Join(" ", v7) + " | gacha " + string.Join(" ", gshare.Select(x => x.ToString("P0"))) + " | pet " + string.Join(" ", pshare.Select(x => x.ToString("P0"))));
        }
        return 0;
    }
}
