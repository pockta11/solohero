using System;
using System.Linq;
using SoloHero.Core.Balance;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;

public static class Axes2Driver
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
        int[] want = { 7, 14, 30, 45, 60, 90, 120, 180, 240, 365 };
        Console.WriteLine("day stage L | summon lv gear/skill/pet (pulls) | gem end | gold earned that day | longest wait for a new stage so far (days) | days without a new stage in last 30");
        int prevHigh = 0, lastNewDay = 0, longest = 0;
        var newDays = new bool[days + 1];
        for (int i = 0; i < r.Days.Count; i++)
        {
            var dr = r.Days[i];
            if (dr.HighestStage > prevHigh) { newDays[dr.Day] = true; longest = Math.Max(longest, dr.Day - lastNewDay - 1); lastNewDay = dr.Day; prevHigh = dr.HighestStage; }
            if (!want.Contains(dr.Day)) continue;
            int gearPulls = r.Days.Take(i + 1).Sum(x => x.GoldPulls + x.GemPulls + x.TicketPulls);
            int petPulls = r.Days.Take(i + 1).Sum(x => x.PetPulls);
            var st = r.Stages.Where(s => s.Day <= dr.Day).LastOrDefault();
            int skillPulls = st != null ? st.SkillPulls : 0;
            int idle = 0; for (int d = Math.Max(1, dr.Day - 29); d <= dr.Day; d++) if (!newDays[d]) idle++;
            int curWait = dr.Day - lastNewDay;
            Console.WriteLine(string.Format("d{0} {1} L{2} | {3}/{4}/{5} ({6}/{7}/{8}) | {9:0} | {10:0.##E+0} | {11} (current {12}) | {13}/30",
                dr.Day, BalanceChecks.Label(dr.HighestStage), dr.HeroLevel,
                SummonLevel.LevelOf(b, SummonKind.Gear, gearPulls), SummonLevel.LevelOf(b, SummonKind.Skill, skillPulls), SummonLevel.LevelOf(b, SummonKind.Pet, petPulls),
                gearPulls, skillPulls, petPulls, dr.GemEnd, dr.Earned, Math.Max(longest, curWait), curWait, idle));
        }
        return 0;
    }
}
