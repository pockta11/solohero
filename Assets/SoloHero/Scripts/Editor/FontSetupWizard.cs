using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Setup > Setup Korean Font
///
/// Registers a Korean TMP font asset as the TMP default and as a fallback of LiberationSans.
/// It does NOT create the font asset - build it first:
///    Window > TextMeshPro > Font Asset Creator
///    1. Font Source: C:\Windows\Fonts\malgun.ttf
///    2. Sampling Point Size: 60
///    3. Atlas Resolution: 4096 x 4096
///    4. Character Set: Custom Range
///    5. Custom Range: 32-126,44032-55203,12593-12643
///    6. Generate Font Atlas > Save > Assets/Fonts/KoreanSDF
/// Then run this menu item.
///
/// NOTE (E8-12): the final game uses a pixel font and assets move to Assets/SoloHero/Art/Fonts/.
/// Assets/Fonts/ is git-ignored, so this tool only works on a machine that has those fonts locally.
/// </summary>
public static class FontSetupWizard
{
    private const string FontDir = "Assets/Fonts";
    private const string KoreanSDF = "Assets/Fonts/KoreanSDF.asset";
    private const string LiberationSDF =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Setup/Setup Korean Font")]
    public static void SetupKoreanFont()
    {
        // Prefer the conventional path, otherwise take the first TMP font asset under Assets/Fonts.
        var koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanSDF);

        if (koreanFont == null && Directory.Exists(FontDir))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (koreanFont != null) break;
            }
        }

        if (koreanFont == null)
        {
            EditorUtility.DisplayDialog(
                "No Korean font asset",
                "No TMP Font Asset found under Assets/Fonts.\n\n" +
                "Create one first: Window > TextMeshPro > Font Asset Creator, convert malgun.ttf " +
                "and save it to Assets/Fonts.\n\n" +
                "Character Range: 32-126,44032-55203,12593-12643\n" +
                "Atlas: 4096x4096, Sampling: 60",
                "OK");
            return;
        }

        // Add the Korean font to LiberationSans' fallback list so Latin text keeps its metrics.
        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSDF);
        if (liberation != null)
        {
            var fallbacks = liberation.fallbackFontAssetTable
                            ?? new System.Collections.Generic.List<TMP_FontAsset>();
            if (!fallbacks.Contains(koreanFont))
            {
                fallbacks.Add(koreanFont);
                liberation.fallbackFontAssetTable = fallbacks;
                EditorUtility.SetDirty(liberation);
                AssetDatabase.SaveAssets();
            }
        }

        // Point TMP Settings at the font that now carries the Korean fallback.
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = liberation ?? koreanFont;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        EditorUtility.DisplayDialog(
            "Done",
            $"Registered font: {koreanFont.name}\n\n" +
            "Existing TMP components keep their own font reference - reassign them if needed.",
            "OK");
    }
}
