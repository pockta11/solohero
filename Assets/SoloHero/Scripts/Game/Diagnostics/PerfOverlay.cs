#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;

namespace SoloHero.Game.Diagnostics
{
    /// <summary>
    /// Development-build frame meter (D-096, performance target 60 FPS with 1% low >= 50): average FPS and the worst
    /// frame time over the last half second, drawn in the top-right corner. Compiled out of release builds.
    /// </summary>
    public sealed class PerfOverlay : MonoBehaviour
    {
        private const float Window = 0.5f;

        private float _elapsed;
        private int _frames;
        private float _worst;
        private string _text = "";
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Application.isEditor) return;
            var go = new GameObject("PerfOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfOverlay>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            _frames++;
            if (dt > _worst) _worst = dt;
            if (_elapsed < Window) return;
            _text = (_frames / _elapsed).ToString("0") + " fps  max " + (_worst * 1000f).ToString("0") + " ms";
            _elapsed = 0f;
            _frames = 0;
            _worst = 0f;
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = Screen.height / 70, alignment = TextAnchor.UpperRight };
                _style.normal.textColor = Color.yellow;
            }

            GUI.Label(new Rect(0f, Screen.height * 0.085f, Screen.width - 16f, 60f), _text, _style);
        }
    }
}
#endif
