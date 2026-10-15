using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SoloHero.Core.Balance;
using SoloHero.Core.Config;

// Long-term dump: one run (persona, path, seed) -> per-day rows and first-clear stage rows, for offline analysis.
public static class LtDriver
{
    public static int Main(string[] args)
    {
        var inv = CultureInfo.InvariantCulture;
        var b = new BalanceValues();
        string persona = "light", path = "knight", outFile = null;
        int seed = 1, days = 365;
        foreach (string a in args)
        {
            if (a.StartsWith("--persona=")) { persona = a.Substring(10); continue; }
            if (a.StartsWith("--path=")) { path = a.Substring(7); continue; }
            if (a.StartsWith("--seed=")) { seed = int.Parse(a.Substring(7)); continue; }
            if (a.StartsWith("--days=")) { days = int.Parse(a.Substring(7)); continue; }
            if (a.StartsWith("--out=")) { outFile = a.Substring(6); continue; }
            int eq = a.IndexOf('=');
            if (eq < 0) continue;
            FieldInfo f = typeof(BalanceValues).GetField(a.Substring(0, eq));
            if (f == null) { Console.Error.WriteLine("unknown field " + a); return 2; }
            f.SetValue(b, Convert.ChangeType(a.Substring(eq + 1), f.FieldType, inv));
        }

        string job1 = path == "knight" || path == "berserker" ? "warrior" : path == "pyro" || path == "cryo" ? "mage" : "archer";
        SimSettings set = persona == "light" ? SimSettings.NoAds(seed) : SimSettings.WithAds(seed);
        if (persona == "heavy")
        {
            set.Name = "heavy";
            set.DailySessions = new[] { new SimSession(7.5, 30), new SimSession(12.0, 30), new SimSession(18.0, 60), new SimSession(21.5, 60) };
        }
        set.Days = days; set.Job1 = job1; set.Job2 = path;
        var t0 = DateTime.Now;
        SimReport r = BalanceSimulator.Run(b, set);

        var sb = new StringBuilder();
        sb.AppendLine("# persona " + persona + " path " + path + " seed " + seed + " secs " + (DateTime.Now - t0).TotalSeconds.ToString("F0", inv));
        sb.AppendLine("D day g lv tower gearPulls petPulls skillPulls skillsOwned skillLvSum sword helm armor boots upHp upAtk upDef stones breaks gem earned spUpg spGacha spSkill spPet bossAtt bossFail bossDeath normAtt normFail play");
        int gearPulls = 0, petPulls = 0;
        for (int i = 0; i < r.Days.Count; i++)
        {
            SimDayRow d = r.Days[i];
            gearPulls += d.GoldPulls + d.GemPulls + d.TicketPulls;
            petPulls += d.PetPulls;
            SimStageRow st = r.Stages.Where(s => s.Day <= d.Day).LastOrDefault();
            sb.Append("D ").Append(d.Day).Append(' ').Append(d.HighestStage).Append(' ').Append(d.HeroLevel).Append(' ').Append(d.TowerFloor)
              .Append(' ').Append(gearPulls).Append(' ').Append(petPulls)
              .Append(' ').Append(st != null ? st.SkillPulls : 0).Append(' ').Append(st != null ? st.SkillsOwned : 0).Append(' ').Append(st != null ? st.SkillLevelSum : 0)
              .Append(' ').Append(st != null ? st.SwordGrade : 0).Append(' ').Append(st != null ? st.HelmGrade : 0).Append(' ').Append(st != null ? st.ArmorGrade : 0).Append(' ').Append(st != null ? st.BootsGrade : 0)
              .Append(' ').Append(st != null ? st.UpgradeHp : 0).Append(' ').Append(st != null ? st.UpgradeAtk : 0).Append(' ').Append(st != null ? st.UpgradeDef : 0)
              .Append(' ').Append(d.Stones).Append(' ').Append(d.Breaks).Append(' ').Append(d.GemEnd.ToString("F0", inv))
              .Append(' ').Append(d.Earned.ToString("E3", inv)).Append(' ').Append(d.SpentUpgrade.ToString("E3", inv)).Append(' ').Append(d.SpentGacha.ToString("E3", inv))
              .Append(' ').Append(d.SpentSkill.ToString("E3", inv)).Append(' ').Append(d.SpentPet.ToString("E3", inv))
              .Append(' ').Append(d.BossAttempts).Append(' ').Append(d.BossFails).Append(' ').Append(d.BossDeaths).Append(' ').Append(d.NormalAttempts).Append(' ').Append(d.NormalFails)
              .Append(' ').Append(d.PlaySeconds.ToString("F0", inv)).AppendLine();
        }
        sb.AppendLine("S g boss day cp attempts fails clearSec stuckSec lv");
        foreach (SimStageRow s in r.Stages)
        {
            sb.Append("S ").Append(s.G).Append(' ').Append(s.IsBoss ? 1 : 0).Append(' ').Append(s.Day).Append(' ').Append(s.Cp.ToString("E4", inv))
              .Append(' ').Append(s.Attempts).Append(' ').Append(s.Fails).Append(' ').Append(s.ClearSeconds.ToString("F1", inv)).Append(' ').Append(s.StuckPlaySeconds.ToString("F0", inv))
              .Append(' ').Append(s.HeroLevel).AppendLine();
        }
        if (outFile == null) Console.Write(sb.ToString());
        else File.WriteAllText(outFile, sb.ToString());
        return 0;
    }
}
