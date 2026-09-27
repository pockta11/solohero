using System;
using System.IO;
using System.IO.Compression;
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
///   SOLOHERO_NO_ADS=1             -> QA build without the ad SDK (BuildConfig.adsEnabled = false for this build only)
///   SOLOHERO_X86_64=1             -> also build x86_64 for this build only (native emulator runs; not for release).
///                                    EDM4U then rewrites mainTemplate.gradle / AndroidResolverDependencies.xml - revert both.
///   SOLOHERO_KEYSTORE_PATH        -> custom keystore; without it the build is debug-signed
///   SOLOHERO_KEYSTORE_PASS, SOLOHERO_KEYALIAS_NAME, SOLOHERO_KEYALIAS_PASS
///   SOLOHERO_JDK_PATH             -> override Unity's embedded JDK (decision B of story 1-09)
///   SOLOHERO_GRADLE_PATH          -> override Unity's embedded Gradle (decision B of story 1-09)
/// </summary>
public static class BuildAutomator
{
    private const string OutputPath = "Builds/game.aab";
    private const string BuildConfigPath = "Assets/SoloHero/Data/Config/BuildConfig.asset";

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
        RepairLocalRepoPoms();

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

        SoloHero.Game.Config.BuildConfig config = AssetDatabase.LoadAssetAtPath<SoloHero.Game.Config.BuildConfig>(BuildConfigPath);
        bool noAds = Environment.GetEnvironmentVariable("SOLOHERO_NO_ADS") == "1" && config != null;
        if (noAds)
        {
            config.adsEnabled = false;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Build] QA build: ads disabled");
        }

        AndroidArchitecture architectures = PlayerSettings.Android.targetArchitectures;
        bool x64 = Environment.GetEnvironmentVariable("SOLOHERO_X86_64") == "1";
        if (x64)
        {
            PlayerSettings.Android.targetArchitectures = architectures | AndroidArchitecture.X86_64;
            Debug.Log("[Build] experiment: x86_64 added");
        }

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(playerOptions);
        }
        finally
        {
            if (x64) PlayerSettings.Android.targetArchitectures = architectures;
            if (noAds)
            {
                // The build unloads assets, so the reference taken before it is gone: load the config again.
                var restored = AssetDatabase.LoadAssetAtPath<SoloHero.Game.Config.BuildConfig>(BuildConfigPath);
                if (restored != null)
                {
                    restored.adsEnabled = true;
                    EditorUtility.SetDirty(restored);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        BuildSummary summary = report.summary;
        Debug.Log($"[Build] result={summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} time={summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
        {
            Fail($"build failed: {summary.result} ({summary.totalErrors} errors)");
            return;
        }

        VerifyNativeLibraries(outputPath);
    }

    private const string LocalRepo = "Assets/GeneratedLocalRepo";

    /// <summary>
    /// EDM4U sometimes rewrites a local-repo pom to packaging "srcaar" while the file next to it is ".aar". Gradle then
    /// resolves nothing for that artifact and the Firebase native library silently drops out of the build (the app
    /// boots in local mode with DllNotFoundException FirebaseCppApp). Point such poms back at the file that exists.
    /// </summary>
    private static void RepairLocalRepoPoms()
    {
        if (!Directory.Exists(LocalRepo)) return;
        foreach (string pom in Directory.GetFiles(LocalRepo, "*.pom", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(pom);
            string aar = Path.ChangeExtension(pom, ".aar");
            if (!text.Contains("<packaging>srcaar</packaging>") || !File.Exists(aar)) continue;
            File.WriteAllText(pom, text.Replace("<packaging>srcaar</packaging>", "<packaging>aar</packaging>"));
            Debug.LogWarning("[Build] repaired pom packaging srcaar -> aar: " + pom);
        }
    }

    /// <summary>Fails the build when a native library the app needs at boot is missing from the bundle.</summary>
    private static void VerifyNativeLibraries(string aabPath)
    {
        string[] required = { "base/lib/arm64-v8a/libFirebaseCppApp-" };
        try
        {
            using (ZipArchive zip = ZipFile.OpenRead(aabPath))
            {
                foreach (string prefix in required)
                {
                    if (zip.Entries.Any(e => e.FullName.StartsWith(prefix, StringComparison.Ordinal))) continue;
                    Fail("native library missing from " + aabPath + ": " + prefix + "*.so");
                    return;
                }
            }

            Debug.Log("[Build] native libraries verified");
        }
        catch (Exception e)
        {
            Fail("could not inspect " + aabPath + ": " + e.Message);
        }
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
