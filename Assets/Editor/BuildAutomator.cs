using UnityEditor;

public class BuildAutomator
{
    [MenuItem("Tools/Build/Android AAB")]
    public static void Build()
    {
        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/GameScene.unity" };
        opt.locationPathName = "Builds/game.aab";
        opt.target = BuildTarget.Android;
        opt.options = BuildOptions.None;

        // AAB 모드 활성화
        EditorUserBuildSettings.buildAppBundle = true;

        BuildPipeline.BuildPlayer(opt);
    }
}
