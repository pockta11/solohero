using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Single build entry point shared by the editor menu and CI (.github/scripts/unity-build.sh).
/// Invoked with: -executeMethod BuildAutomator.Build
/// Output is always Builds/game.aab.
///
/// Environment variables (all optional):
///   SOLOHERO_DEV_BUILD=1          -> BuildOptions.Development (defines DEVELOPMENT_BUILD)
///   SOLOHERO_KEYSTORE_PATH        -> custom keystore; without it the build is debug-signed
///   SOLOHERO_KEYSTORE_PASS, SOLOHERO_KEYALIAS_NAME, SOLOHERO_KEYALIAS_PASS
///   SOLOHERO_JDK_PATH             -> override Unity's embedded JDK (decision B of story 1-09)
///   SOLOHERO_GRADLE_PATH          -> override Unity's embedded Gradle (decision B of story 1-09)
/// </summary>
public static class BuildAutomator
{
    private const string OutputPath = "Builds/game.aab";

    [MenuItem("Tools/Build/Android AAB")]
    public static void Build()
    {
        SpikeSceneSetup.EnsureBootScene();

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        if (scenes.Length == 0)
        {
            Fail("no enabled scenes in EditorBuildSettings");
            return;
        }

        string outputPath = OutputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        ApplyToolchainOverrides();
        ApplyKeystoreFromEnvironment();

        EditorUserBuildSettings.buildAppBundle = true;

        var options = BuildOptions.None;
        if (Environment.GetEnvironmentVariable("SOLOHERO_DEV_BUILD") == "1")
            options |= BuildOptions.Development;

        var playerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = options,
        };

        Debug.Log($"[Build] start unity={Application.unityVersion} scenes={scenes.Length} out={outputPath} dev={(options & BuildOptions.Development) != 0} " +
                  $"targetSdk={PlayerSettings.Android.targetSdkVersion} minSdk={PlayerSettings.Android.minSdkVersion} " +
                  $"arch={PlayerSettings.Android.targetArchitectures} backend={PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");

        BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
        BuildSummary summary = report.summary;
        Debug.Log($"[Build] result={summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} time={summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
            Fail($"build failed: {summary.result} ({summary.totalErrors} errors)");
    }

    private static void ApplyToolchainOverrides()
    {
        string jdk = Environment.GetEnvironmentVariable("SOLOHERO_JDK_PATH");
        if (!string.IsNullOrEmpty(jdk))
        {
            EditorPrefs.SetBool("JdkUseEmbedded", false);
            EditorPrefs.SetString("JdkPath", jdk);
            Debug.Log($"[Build] jdk override={jdk}");
        }

        string gradle = Environment.GetEnvironmentVariable("SOLOHERO_GRADLE_PATH");
        if (!string.IsNullOrEmpty(gradle))
        {
            EditorPrefs.SetBool("GradleUseEmbedded", false);
            EditorPrefs.SetString("GradlePath", gradle);
            Debug.Log($"[Build] gradle override={gradle}");
        }
    }

    private static void ApplyKeystoreFromEnvironment()
    {
        string keystore = Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PATH");
        if (string.IsNullOrEmpty(keystore))
        {
            Debug.Log("[Build] no keystore env, debug signing");
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PASS") ?? string.Empty;
        PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("SOLOHERO_KEYALIAS_NAME") ?? string.Empty;
        PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("SOLOHERO_KEYALIAS_PASS") ?? string.Empty;
        Debug.Log($"[Build] keystore={keystore} alias={PlayerSettings.Android.keyaliasName}");
    }

    private static void Fail(string message)
    {
        Debug.LogError("[Build] " + message);
        if (Application.isBatchMode)
            EditorApplication.Exit(1);
        else
            throw new BuildFailedException(message);
    }
}
