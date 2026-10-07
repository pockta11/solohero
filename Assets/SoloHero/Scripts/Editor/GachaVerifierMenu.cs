using System.IO;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Game.Config;
using UnityEditor;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>Tools > Balance > Verify Gacha Rates (E5-14): 100,000 real pulls against the disclosed table.</summary>
    public static class GachaVerifierMenu
    {
        private const string ConfigPath = "Assets/SoloHero/Data/Config/BalanceConfig.asset";
        private const string ReportPath = "_bmad-output/implementation-artifacts/balance/gacha-verifier.md";

        [MenuItem("Tools/Balance/Verify Gacha Rates")]
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath);
            BalanceValues balance = config != null ? config.ToValues() : new BalanceValues();
            // D-123: the pity counts once the summon level opens Legendary; verify at that level.
            int level = GearTableValues.FromBalance(balance).OpenLevel(GearTableValues.PityGrade);
            GachaVerifier.Report report = GachaVerifier.Run(balance, 100000, 20260928, level);
            string markdown = "# Gear gacha rate verification (E5-14, D-113, summon Lv " + level + ")\n\n" + report.ToMarkdown(balance.GEAR_PITY);
            File.WriteAllText(ReportPath, markdown);
            Debug.Log("[Gacha] " + (report.Passed(balance.GEAR_PITY) ? "PASS" : "FAIL") + " - report " + ReportPath);
        }
    }
}
