using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SoloHero.Core.Balance;
using SoloHero.Core.Config;
using SoloHero.Game.Config;
using UnityEditor;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>
    /// Tools > Balance > Run Simulation. Reads the project's BalanceConfig asset (the same data the game boots with),
    /// runs the no-ads and ads player profiles over several seeds, and writes CSV + a markdown summary.
    /// </summary>
    public static class BalanceSimulatorMenu
    {
        private const int Seeds = 3;
        private const string OutputFolder = "_bmad-output/implementation-artifacts/balance";

        [MenuItem("Tools/Balance/Run Simulation")]
        public static void Run() => Execute(interactive: true);

        /// <summary>Batch entry: Unity.exe -batchmode -quit -executeMethod SoloHero.Editor.BalanceSimulatorMenu.RunBatch</summary>
        public static void RunBatch() => Execute(interactive: false);

        private static void Execute(bool interactive)
        {
            BalanceConfig config = FindConfig();
            if (config == null)
            {
                Debug.LogError("[Balance] No BalanceConfig asset found under Assets/SoloHero/Data/Config");
                return;
            }

            BalanceValues balance = config.ToValues();
            var runs = new List<SimReport>();
            var checks = new List<List<SimCheck>>();
            try
            {
                for (int profile = 0; profile < 2; profile++)
                {
                    for (int seed = 1; seed <= Seeds; seed++)
                    {
                        if (interactive)
                            EditorUtility.DisplayProgressBar("Balance simulation", "Running seed " + seed, (profile * Seeds + seed) / (2f * Seeds));
                        SimSettings settings = profile == 0 ? SimSettings.NoAds(seed) : SimSettings.WithAds(seed);
                        SimReport report = BalanceSimulator.Run(balance, settings);
                        runs.Add(report);
                        checks.Add(BalanceChecks.Evaluate(report, balance));
                    }
                }
            }
            finally
            {
                if (interactive) EditorUtility.ClearProgressBar();
            }

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
            Directory.CreateDirectory(folder);
            var utf8 = new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(folder, "sim-summary.md"), SimCsv.Summary(runs, checks), utf8);
            File.WriteAllText(Path.Combine(folder, "sim-stages-no-ads.csv"), SimCsv.Stages(runs[0]), utf8);
            File.WriteAllText(Path.Combine(folder, "sim-days-no-ads.csv"), SimCsv.Days(runs[0]), utf8);
            File.WriteAllText(Path.Combine(folder, "sim-stages-ads.csv"), SimCsv.Stages(runs[Seeds]), utf8);
            File.WriteAllText(Path.Combine(folder, "sim-days-ads.csv"), SimCsv.Days(runs[Seeds]), utf8);

            int failed = BalanceChecks.CountFailed(checks[0]);
            Debug.Log("[Balance] Simulation done: " + failed + " failed checks (no-ads seed 1). Output: " + folder);
            if (interactive) EditorUtility.RevealInFinder(Path.Combine(folder, "sim-summary.md"));
        }

        /// <summary>D-151 (L-04): the six second-job paths the long-term run plays.</summary>
        private static readonly string[][] JobPaths =
        {
            new[] { "warrior", "knight" }, new[] { "warrior", "berserker" },
            new[] { "mage", "pyro" }, new[] { "mage", "cryo" },
            new[] { "archer", "ranger" }, new[] { "archer", "sniper" }
        };

        private const int LongTermSeeds = 2;
        private const int LongTermDays = 365;

        /// <summary>
        /// D-151 (L-04) Tools > Balance > Run Long-term Simulation: three personas (no-ads, ads, heavy) x six job paths x two
        /// seeds over 365 days, checked against the long-term targets. A run takes minutes, so they go in parallel (the
        /// simulator is plain C# without shared state).
        /// </summary>
        [MenuItem("Tools/Balance/Run Long-term Simulation (365 days)")]
        public static void RunLongTerm() => ExecuteLongTerm(interactive: true);

        /// <summary>Batch entry: Unity.exe -batchmode -quit -executeMethod SoloHero.Editor.BalanceSimulatorMenu.RunLongTermBatch</summary>
        public static void RunLongTermBatch() => ExecuteLongTerm(interactive: false);

        private static void ExecuteLongTerm(bool interactive)
        {
            BalanceConfig config = FindConfig();
            if (config == null)
            {
                Debug.LogError("[Balance] No BalanceConfig asset found under Assets/SoloHero/Data/Config");
                return;
            }

            BalanceValues balance = config.ToValues();
            var settings = new List<SimSettings>();
            for (int persona = 0; persona < 3; persona++)
            {
                foreach (string[] path in JobPaths)
                {
                    for (int seed = 1; seed <= LongTermSeeds; seed++)
                    {
                        SimSettings s = persona == 0 ? SimSettings.NoAds(seed) : persona == 1 ? SimSettings.WithAds(seed) : SimSettings.Heavy(seed);
                        s.Days = LongTermDays;
                        s.Job1 = path[0];
                        s.Job2 = path[1];
                        settings.Add(s);
                    }
                }
            }

            var reports = new SimReport[settings.Count];
            int done = 0;
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };
            Task all = Task.Run(() => Parallel.For(0, settings.Count, options, i =>
            {
                reports[i] = BalanceSimulator.Run(balance, settings[i]);
                Interlocked.Increment(ref done);
            }));
            try
            {
                while (!all.Wait(500))
                {
                    if (interactive)
                        EditorUtility.DisplayProgressBar("Long-term simulation", done + " / " + settings.Count + " runs (365 days each)", (float)done / settings.Count);
                }
            }
            finally
            {
                if (interactive) EditorUtility.ClearProgressBar();
            }

            var runs = new List<SimReport>(reports);
            var checks = new List<List<SimCheck>>();
            foreach (SimReport report in runs)
            {
                var list = new List<SimCheck>();
                foreach (SimCheck check in BalanceChecks.Evaluate(report, balance))
                    if (check.Id.StartsWith("V-1", StringComparison.Ordinal)) list.Add(check);
                list.AddRange(LongTermChecks.Evaluate(report));
                checks.Add(list);
            }

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputFolder));
            Directory.CreateDirectory(folder);
            var utf8 = new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(folder, "sim-longterm-summary.md"), "# Long-term simulation (" + LongTermDays + " days)\n\n" + SimCsv.LongTerm(runs, checks), utf8);
            var csv = new StringBuilder("persona,job,seed,day,highest,label,hero_level,tower\n");
            foreach (SimReport r in runs)
            {
                foreach (SimDayRow d in r.Days)
                    csv.Append(r.Name).Append(',').Append(r.Job).Append(',').Append(r.Seed).Append(',').Append(d.Day).Append(',').Append(d.HighestStage)
                        .Append(',').Append(BalanceChecks.Label(d.HighestStage)).Append(',').Append(d.HeroLevel).Append(',').Append(d.TowerFloor).Append('\n');
            }

            File.WriteAllText(Path.Combine(folder, "sim-longterm-days.csv"), csv.ToString(), utf8);
            int failed = 0;
            foreach (List<SimCheck> list in checks) failed += BalanceChecks.CountFailed(list);
            Debug.Log("[Balance] Long-term simulation done: " + failed + " failed checks over " + runs.Count + " runs. Output: " + folder);
            if (interactive) EditorUtility.RevealInFinder(Path.Combine(folder, "sim-longterm-summary.md"));
        }

        private static BalanceConfig FindConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(BalanceConfig), new[] { "Assets/SoloHero/Data/Config" });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<BalanceConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
