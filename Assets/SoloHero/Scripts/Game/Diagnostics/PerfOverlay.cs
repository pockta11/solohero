#if DEVELOPMENT_BUILD || UNITY_EDITOR
using Unity.Profiling;
using UnityEngine;

namespace SoloHero.Game.Diagnostics
{
    /// <summary>
    /// Development-build frame meter (D-096, performance target 60 FPS with 1% low >= 50): average FPS and the worst
    /// frame time over the last half second, drawn in the top-right corner, plus the render and memory counters of the
    /// E9-19 targets (<= 120 draw calls in battle, <= 400 MB after 2 h). Every 10 s one "[Perf]" line goes to the log so
    /// QA (tools/qa/Qa.ps1) can read the peaks from logcat. Draw calls do not depend on the GPU's speed, so an emulator
    /// reading counts. Compiled out of release builds.
    /// </summary>
    public sealed class PerfOverlay : MonoBehaviour
    {
        private const float Window = 0.5f;
        private const float LogEvery = 10f;
        private const double Megabyte = 1024d * 1024d;

        private float _elapsed;
        private int _frames;
        private float _worst;
        private float _sinceLog;
        private long _peakDrawCalls;
        private long _peakBatches;
        private string _text = "";
        private GUIStyle _style;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _memory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Application.isEditor) return;
            var go = new GameObject("PerfOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfOverlay>();
        }

        private void OnEnable()
        {
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _batches.Dispose();
            _setPass.Dispose();
            _memory.Dispose();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            _sinceLog += dt;
            _frames++;
            if (dt > _worst) _worst = dt;
            long drawCalls = _drawCalls.Valid ? _drawCalls.LastValue : -1;
            long batches = _batches.Valid ? _batches.LastValue : -1;
            if (drawCalls > _peakDrawCalls) _peakDrawCalls = drawCalls;
            if (batches > _peakBatches) _peakBatches = batches;
            if (_elapsed < Window) return;

            string memory = _memory.Valid ? (_memory.LastValue / Megabyte).ToString("0") : "?";
            string fps = (_frames / _elapsed).ToString("0");
            string worst = (_worst * 1000f).ToString("0");
            _text = fps + " fps  max " + worst + " ms\n" + drawCalls + " dc  " + batches + " batch  "
                + (_setPass.Valid ? _setPass.LastValue : -1) + " pass  " + memory + " MB";
            if (_sinceLog >= LogEvery)
            {
                Debug.Log("[Perf] fps=" + fps + " worstMs=" + worst + " drawCalls=" + drawCalls + " peakDrawCalls=" + _peakDrawCalls
                    + " batches=" + batches + " peakBatches=" + _peakBatches + " memMB=" + memory);
                _sinceLog = 0f;
            }

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

            GUI.Label(new Rect(0f, Screen.height * 0.085f, Screen.width - 16f, 120f), _text, _style);
        }
    }
}
#endif
