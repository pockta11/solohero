#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoloHero.Game.Diagnostics
{
    /// <summary>
    /// Development-build draw-call attribution for the E9-19 budget (120 in battle). Runs only when the device holds
    /// persistentDataPath/dc_probe.txt. Once the battle is up it measures the median "Draw Calls Count" with everything
    /// shown, then hides one group at a time (world renderers forced off - the battle view re-enables its renderers every
    /// frame, so toggling enabled would not hide them - UI graphics culled; no logic is touched) and logs
    /// what the group costs, splitting big groups into their children. It also logs a census of what is visible: world
    /// renderers and UI graphics by material and texture. Afterwards (or only, when the file says "spikes") it watches
    /// for frames above the budget (120, or the number after "spikes") and logs which UI blocks were drawing and how
    /// many world renderers were visible.
    /// Every line starts with "[DcProbe]". Compiled out of release builds.
    /// </summary>
    public sealed class DrawCallProbe : MonoBehaviour
    {
        private const string Trigger = "dc_probe.txt";
        private const float StartDelay = 12f;
        private const int Cycles = 2;
        private const int SettleFrames = 3;
        private const int SampleFrames = 24;
        private const int MaxDepth = 4;
        private const int SplitAbove = 8;
        private const int DefaultSpikeAbove = 120;
        private const float SpikeLogGap = 2f;

        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _setPass;
        private bool _spikesOnly;
        private int _spikeAbove = DefaultSpikeAbove;
        private bool _watching;
        private float _lastSpike = -100f;

        private sealed class Group
        {
            public string Name;
            public int Depth;
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public readonly List<CanvasRenderer> Graphics = new List<CanvasRenderer>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Application.isEditor) return;
            string path = Path.Combine(Application.persistentDataPath, Trigger);
            if (!File.Exists(path)) return;
            var go = new GameObject("DrawCallProbe");
            DontDestroyOnLoad(go);
            string text = File.ReadAllText(path);
            DrawCallProbe probe = go.AddComponent<DrawCallProbe>();
            probe._spikesOnly = text.Contains("spikes");
            foreach (string word in text.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(word, out int above) && above > 1) probe._spikeAbove = above;
        }

        private void OnEnable()
        {
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        }

        private void OnDisable()
        {
            _drawCalls.Dispose();
            _setPass.Dispose();
        }

        private IEnumerator Start()
        {
            while (FindObjectOfType<SoloHero.Game.Combat.CombatSession>() == null) yield return null;
            yield return new WaitForSecondsRealtime(StartDelay);
            if (_spikesOnly)
            {
                Debug.Log("[DcProbe] spike watch above " + _spikeAbove);
                _watching = true;
                yield break;
            }

            Debug.Log("[DcProbe] start");
            Census(1);
            List<Group> groups = BuildGroups();
            Debug.Log("[DcProbe] groups=" + groups.Count);
            foreach (Group group in groups) yield return Measure(group, 1);

            // The two totals again at the end: how much the battle itself moved meanwhile.
            for (int cycle = 2; cycle <= Cycles; cycle++)
            {
                List<Group> again = BuildGroups();
                yield return Measure(again[0], cycle);
                yield return Measure(again[1], cycle);
                Census(cycle);
            }

            Debug.Log("[DcProbe] done");
            _watching = true;
        }

        private void Update()
        {
            if (!_watching || !_drawCalls.Valid) return;
            long drawCalls = _drawCalls.LastValue;
            if (drawCalls < _spikeAbove || Time.realtimeSinceStartup - _lastSpike < SpikeLogGap) return;
            _lastSpike = Time.realtimeSinceStartup;
            int world = 0;
            foreach (GameObject root in Roots())
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
                    if (r.enabled && r.isVisible && r.GetComponentInParent<Canvas>() == null) world++;
            var sb = new StringBuilder("[DcProbe] spike dc=").Append(drawCalls).Append(" world=").Append(world).Append(" ui:");
            foreach (GameObject root in Roots())
                if (root.GetComponent<Canvas>() != null) AppendDrawing(root.transform, 0, sb);
            Debug.Log(sb.ToString());
            // In spike-only mode, what exactly was drawing (by material, texture and order), to tell the effects apart
            // from the UI.
            if (_spikesOnly) Census(0);
        }

        /// <summary>Children that draw UI graphics, with their counts; big ones opened two levels down.</summary>
        private static void AppendDrawing(Transform node, int depth, StringBuilder sb)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                int drawn = Drawn(child);
                if (drawn == 0) continue;
                sb.Append(' ').Append(child.name).Append('=').Append(drawn);
                if (depth >= 2 || drawn <= 20) continue;
                sb.Append(" {");
                AppendDrawing(child, depth + 1, sb);
                sb.Append(" }");
            }
        }

        private static int Drawn(Transform node)
        {
            int n = 0;
            foreach (Graphic g in node.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.enabled || g.canvasRenderer == null || g.canvasRenderer.cull) continue;
                if (g.canvasRenderer.GetInheritedAlpha() <= 0f || g.color.a <= 0f) continue;
                n++;
            }

            return n;
        }

        private IEnumerator Measure(Group group, int cycle)
        {
            var before = new List<long>();
            var hidden = new List<long>();
            var after = new List<long>();
            yield return Sample(before);
            var rendererStates = new bool[group.Renderers.Count];
            for (int i = 0; i < group.Renderers.Count; i++)
            {
                Renderer r = group.Renderers[i];
                if (r == null) continue;
                rendererStates[i] = r.forceRenderingOff;
                r.forceRenderingOff = true;
            }

            var cullStates = new bool[group.Graphics.Count];
            for (int i = 0; i < group.Graphics.Count; i++)
            {
                CanvasRenderer c = group.Graphics[i];
                if (c == null) continue;
                cullStates[i] = c.cull;
                c.cull = true;
            }

            yield return Sample(hidden);
            for (int i = 0; i < group.Renderers.Count; i++)
                if (group.Renderers[i] != null) group.Renderers[i].forceRenderingOff = rendererStates[i];
            for (int i = 0; i < group.Graphics.Count; i++)
                if (group.Graphics[i] != null) group.Graphics[i].cull = cullStates[i];

            yield return Sample(after);
            long shown = (Median(before) + Median(after)) / 2;
            long without = Median(hidden);
            Debug.Log("[DcProbe] c" + cycle + " d" + group.Depth + " " + group.Name + " renderers=" + group.Renderers.Count
                + " graphics=" + group.Graphics.Count + " shown=" + shown + " hidden=" + without + " cost=" + (shown - without));
        }

        private IEnumerator Sample(List<long> into)
        {
            for (int i = 0; i < SettleFrames; i++) yield return null;
            for (int i = 0; i < SampleFrames; i++)
            {
                yield return null;
                if (_drawCalls.Valid) into.Add(_drawCalls.LastValue);
            }
        }

        private static long Median(List<long> values)
        {
            if (values.Count == 0) return -1;
            values.Sort();
            return values[values.Count / 2];
        }

        /// <summary>All world renderers, all UI, then every hierarchy node worth splitting down to MaxDepth.</summary>
        private static List<Group> BuildGroups()
        {
            var groups = new List<Group>();
            var allWorld = new Group { Name = "ALL-WORLD", Depth = 0 };
            var allUi = new Group { Name = "ALL-UI", Depth = 0 };
            foreach (GameObject root in Roots())
            {
                Collect(root.transform, allWorld, allUi);
            }

            groups.Add(allWorld);
            groups.Add(allUi);
            foreach (GameObject root in Roots()) Split(root.transform, root.name, 1, groups);
            return groups;
        }

        private static void Split(Transform node, string path, int depth, List<Group> groups)
        {
            var group = new Group { Name = path, Depth = depth };
            Collect(node, group, group);
            int count = group.Renderers.Count + group.Graphics.Count;
            if (count == 0) return;
            groups.Add(group);
            if (depth >= MaxDepth || count <= SplitAbove) return;
            int childrenWithContent = 0;
            for (int i = 0; i < node.childCount; i++)
            {
                var probe = new Group();
                Collect(node.GetChild(i), probe, probe);
                if (probe.Renderers.Count + probe.Graphics.Count > 0) childrenWithContent++;
            }

            if (childrenWithContent < 2) return;
            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                Split(child, path + "/" + child.name, depth + 1, groups);
            }
        }

        /// <summary>Active renderers outside canvases go to <paramref name="world"/>, active UI graphics to <paramref name="ui"/>.</summary>
        private static void Collect(Transform node, Group world, Group ui)
        {
            if (!node.gameObject.activeInHierarchy) return;
            foreach (Renderer r in node.GetComponentsInChildren<Renderer>(false))
            {
                if (r.GetComponentInParent<Canvas>() == null) world.Renderers.Add(r);
            }

            foreach (Graphic g in node.GetComponentsInChildren<Graphic>(false))
            {
                if (g.enabled && g.canvasRenderer != null) ui.Graphics.Add(g.canvasRenderer);
            }
        }

        private static IEnumerable<GameObject> Roots()
        {
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.activeInHierarchy && root.name != "DrawCallProbe") yield return root;
            }
        }

        /// <summary>What is on screen now: visible world renderers and drawn UI graphics by material and texture.</summary>
        private void Census(int cycle)
        {
            var world = new Dictionary<string, int>();
            int worldVisible = 0;
            foreach (GameObject root in Roots())
            {
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || !r.isVisible || r.GetComponentInParent<Canvas>() != null) continue;
                    worldVisible++;
                    string texture = r is SpriteRenderer sr && sr.sprite != null ? sr.sprite.texture.name : "-";
                    string key = r.GetType().Name + " " + (r.sharedMaterial != null ? r.sharedMaterial.name : "null") + " " + texture
                        + " order " + r.sortingOrder;
                    world.TryGetValue(key, out int n);
                    world[key] = n + 1;
                }
            }

            var ui = new Dictionary<string, int>();
            var uiTextures = new HashSet<string>();
            int uiDrawn = 0;
            foreach (GameObject root in Roots())
            {
                foreach (Graphic g in root.GetComponentsInChildren<Graphic>(false))
                {
                    if (!g.enabled || g.canvasRenderer == null || g.canvasRenderer.cull || g.canvasRenderer.GetInheritedAlpha() <= 0f) continue;
                    if (g.color.a <= 0f) continue;
                    uiDrawn++;
                    Texture t = g.mainTexture;
                    string texture = t != null ? t.name : "-";
                    uiTextures.Add(texture);
                    // The nearest canvas and the graphic's batching depth: distinct (canvas, depth, material, texture)
                    // keys are roughly the UI's batches.
                    Canvas canvas = g.canvas;
                    string key = (canvas != null ? canvas.name : "?") + " d" + g.depth + " " + g.GetType().Name + " "
                        + (g.material != null ? g.material.name : "null") + " " + texture;
                    ui.TryGetValue(key, out int n);
                    ui[key] = n + 1;
                }
            }

            Debug.Log("[DcProbe] c" + cycle + " census drawCalls=" + (_drawCalls.Valid ? _drawCalls.LastValue : -1)
                + " setPass=" + (_setPass.Valid ? _setPass.LastValue : -1) + " worldVisible=" + worldVisible
                + " uiGraphics=" + uiDrawn + " uiTextures=" + uiTextures.Count);
            LogTop("world", world, 40);
            LogTop("ui", ui, 120);
        }

        private static void LogTop(string label, Dictionary<string, int> counts, int max)
        {
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count && i < max; i++)
            {
                sb.Clear();
                sb.Append("[DcProbe] ").Append(label).Append(' ').Append(list[i].Value).Append("x ").Append(list[i].Key);
                Debug.Log(sb.ToString());
            }

            Debug.Log("[DcProbe] " + label + " distinct=" + list.Count);
        }
    }
}
#endif
