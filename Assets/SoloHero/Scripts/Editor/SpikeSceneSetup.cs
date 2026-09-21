using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the placeholder Boot scene used by story 1-09 (build spike) and registers it in
/// EditorBuildSettings. EnsureBootScene() only acts when the build settings contain no enabled
/// scene at all - it never replaces an existing scene list, so the real Boot scene from E1-04
/// cannot be silently swapped for the spike scene. Replaced by the real Boot scene in E1-04.
/// </summary>
public static class SpikeSceneSetup
{
    public const string ScenePath = "Assets/SoloHero/Scenes/Boot.unity";

    [MenuItem("Tools/SoloHero/Spike/Create Boot Scene")]
    public static void CreateBootScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.5f; // 15 units tall = 480 px / PPU 32 (GDD PIXEL_REF_RESOLUTION)
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.16f);
        cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        cameraGo.AddComponent<AudioListener>();

        var probeGo = new GameObject("BuildSpikeProbe");
        probeGo.AddComponent<BuildSpikeProbe>();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError($"[Spike] failed to save scene at {ScenePath}");
            return;
        }

        RegisterIfMissing();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Spike] boot scene created at {ScenePath} and registered in EditorBuildSettings");
    }

    /// <summary>
    /// Guarantees at least one enabled scene exists for the build. If the build settings already
    /// contain any enabled scene, nothing is touched.
    /// </summary>
    public static void EnsureBootScene()
    {
        if (EditorBuildSettings.scenes.Any(s => s.enabled))
            return;

        if (File.Exists(ScenePath))
        {
            RegisterIfMissing();
            Debug.LogWarning("[Spike] no enabled scenes in EditorBuildSettings - registered the spike Boot scene");
            return;
        }

        Debug.LogWarning("[Spike] no scenes in EditorBuildSettings and no Boot scene on disk - creating the spike Boot scene");
        CreateBootScene();
    }

    private static void RegisterIfMissing()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        var existing = scenes.FirstOrDefault(s => s.path == ScenePath);
        if (existing != null)
        {
            existing.enabled = true;
        }
        else
        {
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
