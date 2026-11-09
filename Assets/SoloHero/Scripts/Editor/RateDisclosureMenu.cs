using System.IO;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using SoloHero.Game.Config;
using UnityEditor;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>
    /// D-183: writes the public probability page from the game's own balance asset and draw tables
    /// (_bmad-output/implementation-artifacts/store/probability-ko.html). Run it after any rate, pity, summon-level or
    /// pool change, then publish the page again; RateDisclosureTests fails while the committed page is out of date.
    /// </summary>
    public static class RateDisclosureMenu
    {
        private const string StringsPath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const string BalancePath = "Assets/SoloHero/Data/Config/BalanceConfig.asset";

        [MenuItem("Tools/Release/Export Rate Disclosure")]
        public static void Export()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalancePath);
            if (config == null)
            {
                Debug.LogError("[Disclosure] " + BalancePath + " not found");
                return;
            }

            Strings.Load(Strings.ParseTsv(File.ReadAllText(StringsPath)));
            Strings.UseTermSet("");
            string html = RateDisclosure.BuildHtml(config.ToValues());
            File.WriteAllText(RateDisclosure.PagePath, html, new System.Text.UTF8Encoding(false));
            Debug.Log("[Disclosure] wrote " + RateDisclosure.PagePath + " (" + html.Length + " chars) - publish it with the privacy policy");
        }
    }
}
