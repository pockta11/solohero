using System;
using System.Linq;
using SoloHero.Core.Balance;
using SoloHero.Core.Config;

public static class AxesDriver
{
    public static int Main(string[] args)
    {
        bool ads = args.Contains("--ads");
        int days = 365;
        string job1 = "warrior", job2 = "knight";
        foreach (var a in args)
        {
            if (a.StartsWith("--days=")) days = int.Parse(a.Substring(7));
            if (a.StartsWith("--job=")) { var p = a.Substring(6).Split('/'); job1 = p[0]; job2 = p[1]; }
        }
        var b = new BalanceValues();
        var set = ads ? SimSettings.WithAds(1) : SimSettings.NoAds(1);
        set.Days = days; set.Job1 = job1; set.Job2 = job2;
        if (args.Contains("--heavy")) set.DailySessions = new[] { new SimSession(7.5, 30), new SimSession(12.0, 30), new SimSession(18.0, 60), new SimSession(21.5, 60) };
        SimReport r = BalanceSimulator.Run(b, set);
        int[] want = { 1, 3, 7, 14, 30, 45, 60, 90, 120, 150, 180, 240, 300, 365 };
        Console.WriteLine("day stage L | upg hp/atk/def/spd/crit/cd | skills own/lvsum | gear grade s/h/a/b | pulls | breaks stones | tower | spend upg/gacha/skill/pet % | play min per new stage (last)");
        foreach (int d in want)
        {
            if (d > days) break;
            var dr = r.Days[d - 1];
            var st = r.Stages.Where(s => s.Day <= d).LastOrDefault();
            if (st == null) continue;
            double tot = Math.Max(1, dr.Spent);
            var recent = r.Stages.Where(s => s.Day <= d && s.Day > Math.Max(0, d - 30)).ToList();
            string pace = recent.Count > 0 ? (30.0 * 30 / recent.Count).ToString("0") + " min" : "-";
            Console.WriteLine(string.Format("d{0} {1} L{2} | {3}/{4}/{5}/{6}/{7}/{8} | {9}/{10} | {11}/{12}/{13}/{14} | {15} | {16} {17} | T{18} | {19:0}/{20:0}/{21:0}/{22:0} | {23} per stage (30d window: {24} new)",
                d, BalanceChecks.Label(dr.HighestStage), dr.HeroLevel,
                st.UpgradeHp, st.UpgradeAtk, st.UpgradeDef, st.UpgradeSpd, st.UpgradeCrit, st.UpgradeCritDmg,
                st.SkillsOwned, st.SkillLevelSum, st.SwordGrade, st.HelmGrade, st.ArmorGrade, st.BootsGrade, st.Pulls,
                dr.Breaks, dr.Stones, dr.TowerFloor,
                100 * dr.SpentUpgrade / tot, 100 * dr.SpentGacha / tot, 100 * dr.SpentSkill / tot, 100 * dr.SpentPet / tot,
                pace, recent.Count));
        }
        return 0;
    }
}
