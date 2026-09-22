using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Tools > Setup > Setup Korean Font
///
/// ※ 올바른 한글 TMP 폰트 생성 방법:
///    Window → TextMeshPro → Font Asset Creator
///    1. Font Source: C:\Windows\Fonts\malgun.ttf
///    2. Sampling Point Size: 60
///    3. Atlas Resolution: 4096 × 4096
///    4. Character Set: Custom Range
///    5. Custom Range: 32-126,44032-55203,12593-12643
///    6. Generate Font Atlas → Save → Assets/Fonts/KoreanSDF
///    7. Tools > Setup > Fix TMP Fonts 실행
///
/// 이 메뉴는 위 과정을 거친 후 폰트 에셋을 TMP 기본값으로 등록합니다.
/// </summary>
public static class FontSetupWizard
{
    private const string FontDir      = "Assets/Fonts";
    private const string KoreanSDF    = "Assets/Fonts/KoreanSDF.asset";
    private const string LiberationSDF =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Setup/Setup Korean Font")]
    public static void SetupKoreanFont()
    {
        // Font Asset Creator로 만든 에셋 탐색
        var koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanSDF);

        if (koreanFont == null)
        {
            // Assets/Fonts 안의 다른 이름도 탐색
            if (Directory.Exists(FontDir))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontDir }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (koreanFont != null) break;
                }
            }
        }

        if (koreanFont == null)
        {
            EditorUtility.DisplayDialog("한글 폰트 없음",
                "Assets/Fonts 에 TMP Font Asset 이 없습니다.\n\n" +
                "Window → TextMeshPro → Font Asset Creator 로\n" +
                "malgun.ttf 를 먼저 변환해 Assets/Fonts 에 저장하세요.\n\n" +
                "Character Range: 32-126,44032-55203,12593-12643\n" +
                "Atlas: 4096×4096, Sampling: 60", "확인");
            return;
        }

        // LiberationSans 의 fallback 에 한글 폰트 추가
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

        // TMP Settings 기본 폰트 등록
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = liberation ?? koreanFont;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        EditorUtility.DisplayDialog("완료",
            $"폰트 등록 완료: {koreanFont.name}\n\n" +
            "Tools > Setup > Fix TMP Fonts 를 실행하세요.", "확인");
    }
}
