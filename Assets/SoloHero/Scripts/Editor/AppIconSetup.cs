using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace SoloHero.Editor
{
    /// <summary>
    /// E9-14: Android launcher icons from Art/Icons (adaptive background + foreground, legacy and round share the
    /// full-bleed icon). Tools > Setup > Apply App Icon; also run by Build Art. The 512 px store icon is the same art.
    /// </summary>
    public static class AppIconSetup
    {
        private const string Dir = "Assets/SoloHero/Art/Icons/";

        [MenuItem("Tools/Setup/Apply App Icon")]
        public static void Apply()
        {
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "app_icon_bg.png");
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "app_icon_fg.png");
            var legacy = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "app_icon_legacy.png");
            if (background == null || foreground == null || legacy == null)
            {
                Debug.LogError("[Icon] missing textures under " + Dir);
                return;
            }

            PlatformIcon[] adaptive = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, AndroidPlatformIconKind.Adaptive);
            foreach (PlatformIcon icon in adaptive) icon.SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, AndroidPlatformIconKind.Adaptive, adaptive);

            foreach (PlatformIconKind kind in new[] { AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Legacy })
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
                foreach (PlatformIcon icon in icons) icon.SetTexture(legacy);
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, icons);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Icon] Android icons applied");
        }
    }
}
