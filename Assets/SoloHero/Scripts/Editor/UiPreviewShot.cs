using System.IO;
using SoloHero.Game.Boot;
using SoloHero.Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoloHero.Editor
{
    /// <summary>
    /// Batch/editor capture of the growth panels at the 1080x1920 play-mode resolution.
    /// Unity.exe -projectPath . -executeMethod SoloHero.Editor.UiPreviewShot.Start -logFile Builds/ui-shot.log
    /// Writes Builds/ui-editor-char.png and Builds/ui-editor-skill.png, then quits.
    /// </summary>
    public static class UiPreviewShot
    {
        private const string Flag = "SoloHero.UiPreviewShot";
        private const int Width = 1080;
        private const int Height = 1920;
        private const float TimeoutSeconds = 90f;

        private static float _started;
        private static int _step;
        private static float _waitUntil;

        public static void Start()
        {
            PlayModeWindow.SetCustomRenderingResolution(Width, Height, "SoloHero shot");
            SessionState.SetBool(Flag, true);
            EditorSceneManager.OpenScene("Assets/SoloHero/Scenes/Boot.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            if (!SessionState.GetBool(Flag, false)) return;
            // Domain reload into play mode runs this before isPlaying flips, so attach on the next editor tick too.
            EditorApplication.delayCall += Attach;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) Attach();
        }

        private static void Attach()
        {
            if (!SessionState.GetBool(Flag, false) || !EditorApplication.isPlaying) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            if (_started <= 0f) _started = Time.realtimeSinceStartup;
            Debug.Log("[UI shot] capture loop attached, scene=" + SceneManager.GetActiveScene().name);
        }

        private static void Tick()
        {
            if (Time.realtimeSinceStartup - _started > TimeoutSeconds)
            {
                Debug.LogError("[UI shot] timed out before both panels were captured");
                Finish(1);
                return;
            }

            if (SceneManager.GetActiveScene().name != "Game") return;
            if (Time.realtimeSinceStartup < _waitUntil) return;

            var host = Object.FindObjectOfType<PanelHost>();
            if (host == null || host.OpenIndex < 0) return;

            OfflineRewardPopup popup = Object.FindObjectOfType<OfflineRewardPopup>();
            if (popup != null && popup.IsOpen)
            {
                popup.Claim();
                _waitUntil = Time.realtimeSinceStartup + 0.4f;
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (_step == 0)
            {
                Grab("Builds/ui-editor-char.png");
                host.Toggle(3);
                _step = 1;
                _waitUntil = Time.realtimeSinceStartup + 0.6f;
                return;
            }

            Grab("Builds/ui-editor-skill.png");
            Debug.Log("[UI shot] wrote character and skill panels at " + Screen.width + "x" + Screen.height);
            Finish(0);
        }

        private static void Grab(string relative)
        {
            string path = Path.GetFullPath(relative);
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("[UI shot] " + path + " " + tex.width + "x" + tex.height);
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Flag, false);
            EditorApplication.Exit(code);
        }
    }
}
