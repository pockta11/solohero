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
///   SOLOHERO_VERSION_CODE=<int>   -> Android versionCode for this build (CI passes github.run_number; every Play
///                                    upload needs a higher code). Without it the ProjectSettings value is used.
///   SOLOHERO_VERSION_NAME=<x.y.z> -> versionName for this build (the Release workflow input).
///   SOLOHERO_RELEASE=1            -> a Play upload: fails unless it is signed with an upload key, carries
///                                    Assets/google-services.json and has ads, no development flag and no x86_64.
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

        CheckReleaseRequirements();
        EditorUserBuildSettings.buildAppBundle = true;
        int versionCodeBefore = PlayerSettings.Android.bundleVersionCode;
        string versionNameBefore = PlayerSettings.bundleVersion;
        bool customKeystoreBefore = PlayerSettings.Android.useCustomKeystore;
        string keystoreBefore = PlayerSettings.Android.keystoreName;
        string keyaliasBefore = PlayerSettings.Android.keyaliasName;
        ApplyVersionCodeFromEnvironment();
        ApplyVersionNameFromEnvironment();
        bool keystoreApplied = ApplyKeystoreFromEnvironment();
        RepairLocalRepoPoms();
        // Last before the build: everything after it is inside the try that puts the machine-wide prefs back.
        ToolchainPrefs toolchain = ApplyToolchainOverrides();

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
            toolchain.Restore();
            if (x64) PlayerSettings.Android.targetArchitectures = architectures;
            // The env version and signing are for this build only; ProjectSettings keeps its own values (and never a
            // keystore path or password).
            if (PlayerSettings.Android.bundleVersionCode != versionCodeBefore) PlayerSettings.Android.bundleVersionCode = versionCodeBefore;
            if (PlayerSettings.bundleVersion != versionNameBefore) PlayerSettings.bundleVersion = versionNameBefore;
            if (keystoreApplied)
            {
                PlayerSettings.Android.useCustomKeystore = customKeystoreBefore;
                PlayerSettings.Android.keystoreName = keystoreBefore;
                PlayerSettings.Android.keyaliasName = keyaliasBefore;
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
            }
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

    /// <summary>
    /// Launch plan P0-2: a Play upload needs the upload key (Play refuses debug signing) and the Firebase config (without
    /// it the app boots in local mode: no cloud save, no analytics), and must be a plain release with ads.
    /// </summary>
    private static void CheckReleaseRequirements()
    {
        if (Environment.GetEnvironmentVariable("SOLOHERO_RELEASE") != "1") return;
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PATH")))
            Fail("release build needs SOLOHERO_KEYSTORE_PATH (the upload key)");
        else if (!File.Exists(Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PATH")))
            Fail("release keystore not found: " + Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PATH"));
        else if (!File.Exists("Assets/google-services.json"))
            Fail("release build needs Assets/google-services.json (Firebase)");
        else if (Environment.GetEnvironmentVariable("SOLOHERO_DEV_BUILD") == "1"
                 || Environment.GetEnvironmentVariable("SOLOHERO_NO_ADS") == "1"
                 || Environment.GetEnvironmentVariable("SOLOHERO_X86_64") == "1")
            Fail("release build cannot be a development, no-ads or x86_64 build");
        else
            Debug.Log("[Build] release requirements met");
    }

    private static void ApplyVersionNameFromEnvironment()
    {
        string name = Environment.GetEnvironmentVariable("SOLOHERO_VERSION_NAME");
        if (string.IsNullOrEmpty(name)) return;
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d+\.\d+\.\d+$"))
        {
            Fail("SOLOHERO_VERSION_NAME must look like 1.0.0: " + name);
            return;
        }

        PlayerSettings.bundleVersion = name;
        Debug.Log("[Build] versionName " + name);
    }

    private static void ApplyVersionCodeFromEnvironment()
    {
        string raw = Environment.GetEnvironmentVariable("SOLOHERO_VERSION_CODE");
        if (string.IsNullOrEmpty(raw)) return;
        if (!int.TryParse(raw, out int code) || code < 1)
        {
            Fail("SOLOHERO_VERSION_CODE must be a positive integer: " + raw);
            return;
        }

        PlayerSettings.Android.bundleVersionCode = code;
        Debug.Log("[Build] versionCode " + code + " (" + PlayerSettings.bundleVersion + ")");
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

    /// <summary>
    /// Points the build at SOLOHERO_JDK_PATH / SOLOHERO_GRADLE_PATH. EditorPrefs are machine-wide, so the returned
    /// snapshot puts the previous values back after the build (the editor keeps using its own toolchain).
    /// </summary>
    private static ToolchainPrefs ApplyToolchainOverrides()
    {
        var previous = ToolchainPrefs.Capture();
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

        return previous;
    }

    /// <summary>The four toolchain EditorPrefs a build may override.</summary>
    private readonly struct ToolchainPrefs
    {
        private readonly bool _hadJdk;
        private readonly bool _jdkEmbedded;
        private readonly string _jdkPath;
        private readonly bool _hadGradle;
        private readonly bool _gradleEmbedded;
        private readonly string _gradlePath;

        private ToolchainPrefs(bool hadJdk, bool jdkEmbedded, string jdkPath, bool hadGradle, bool gradleEmbedded, string gradlePath)
        {
            _hadJdk = hadJdk;
            _jdkEmbedded = jdkEmbedded;
            _jdkPath = jdkPath;
            _hadGradle = hadGradle;
            _gradleEmbedded = gradleEmbedded;
            _gradlePath = gradlePath;
        }

        public static ToolchainPrefs Capture() => new ToolchainPrefs(
            EditorPrefs.HasKey("JdkUseEmbedded"), EditorPrefs.GetBool("JdkUseEmbedded", true), EditorPrefs.GetString("JdkPath", ""),
            EditorPrefs.HasKey("GradleUseEmbedded"), EditorPrefs.GetBool("GradleUseEmbedded", true), EditorPrefs.GetString("GradlePath", ""));

        public void Restore()
        {
            Put("JdkUseEmbedded", _hadJdk, _jdkEmbedded, "JdkPath", _jdkPath);
            Put("GradleUseEmbedded", _hadGradle, _gradleEmbedded, "GradlePath", _gradlePath);
        }

        private static void Put(string embeddedKey, bool had, bool embedded, string pathKey, string path)
        {
            if (!had)
            {
                EditorPrefs.DeleteKey(embeddedKey);
                if (string.IsNullOrEmpty(path)) EditorPrefs.DeleteKey(pathKey);
                else EditorPrefs.SetString(pathKey, path);
                return;
            }

            EditorPrefs.SetBool(embeddedKey, embedded);
            EditorPrefs.SetString(pathKey, path);
        }
    }

    /// <summary>Signs this build with SOLOHERO_KEYSTORE_*; false (debug signing, nothing touched) without them.</summary>
    private static bool ApplyKeystoreFromEnvironment()
    {
        string keystore = Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PATH");
        if (string.IsNullOrEmpty(keystore))
        {
            Debug.Log("[Build] no keystore env, debug signing");
            return false;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("SOLOHERO_KEYSTORE_PASS") ?? string.Empty;
        PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("SOLOHERO_KEYALIAS_NAME") ?? string.Empty;
        PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("SOLOHERO_KEYALIAS_PASS") ?? string.Empty;
        Debug.Log($"[Build] keystore={keystore} alias={PlayerSettings.Android.keyaliasName}");
        return true;
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
