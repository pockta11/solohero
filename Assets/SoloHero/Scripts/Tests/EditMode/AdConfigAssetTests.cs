using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// E6-14 guard: the release rewarded unit id is well formed and belongs to the same AdMob publisher as the App ID
    /// that GMA injects into the manifest. Reads the assets as text (this assembly has no engine references).
    /// </summary>
    public sealed class AdConfigAssetTests
    {
        private const string BuildConfigPath = "Assets/SoloHero/Data/Config/BuildConfig.asset";
        private const string GmaSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        [Test]
        public void ReleaseRewardedUnit_IsWellFormedAndMatchesAppPublisher()
        {
            string config = File.ReadAllText(BuildConfigPath);
            string gma = File.ReadAllText(GmaSettingsPath);

            Match unit = Regex.Match(config, @"rewardedAdUnitIdRelease: (ca-app-pub-(\d{16})/\d{10})\s");
            Match app = Regex.Match(gma, @"adMobAndroidAppId: ca-app-pub-(\d{16})~\d{10}\s");

            Assert.IsTrue(unit.Success, "release rewarded unit id missing or malformed in " + BuildConfigPath);
            Assert.IsTrue(app.Success, "Android App ID missing or malformed in " + GmaSettingsPath);
            Assert.AreEqual(app.Groups[1].Value, unit.Groups[2].Value, "ad unit and App ID belong to different publishers");
            StringAssert.Contains("useTestAdIds: 0", config, "release builds would still serve test ads");
        }
    }
}
