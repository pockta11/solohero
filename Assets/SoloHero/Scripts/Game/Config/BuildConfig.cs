using UnityEngine;

namespace SoloHero.Game.Config
{
    /// <summary>
    /// Platform and build settings (architecture D-config). Ad unit ids live here, never in code paths.
    /// Development builds and the editor always use the test id; E6-14 fills the release id before launch.
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Config/Build")]
    public sealed class BuildConfig : ScriptableObject
    {
        public string rewardedAdUnitIdTest = "ca-app-pub-3940256099942544/5224354917";
        public string rewardedAdUnitIdRelease = "";
        public bool useTestAdIds = true;

        public string RewardedAdUnitId
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return rewardedAdUnitIdTest;
#else
                return useTestAdIds || string.IsNullOrEmpty(rewardedAdUnitIdRelease)
                    ? rewardedAdUnitIdTest
                    : rewardedAdUnitIdRelease;
#endif
            }
        }
    }
}
