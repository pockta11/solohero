using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the placeholder Boot scene used by story 1-09 (build spike) and registers it in
/// EditorBuildSettings. Idempotent: BuildAutomator calls EnsureBootScene() so CI can build even
/// on a fresh checkout. The scene is meant to be committed after the first run.
/// Replaced by the real Boot scene in E1-04.
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

        RegisterAsOnlyScene();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Spike] boot scene created at {ScenePath} and registered in EditorBuildSettings");
    }

    public static void EnsureBootScene()
    {
        bool sceneExists = File.Exists(ScenePath);
        bool registered = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath);
        if (sceneExists && registered)
            return;

        if (sceneExists)
        {
            RegisterAsOnlyScene();
            return;
        }

        CreateBootScene();
    }

    private static void RegisterAsOnlyScene()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
