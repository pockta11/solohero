using System.Collections.Generic;
using System.IO;
using System.Text;
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

        private static BalanceConfig FindConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(BalanceConfig), new[] { "Assets/SoloHero/Data/Config" });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<BalanceConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
