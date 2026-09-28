using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SoloHero.Game.Audio;
using SoloHero.Game.Boot;
using SoloHero.Game.Combat;
using SoloHero.Game.Pooling;
using SoloHero.Game.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SoloHero.Editor
{
    /// <summary>
    /// Tools > Setup > Build Art (E8-02..05, E8-08, E8-12, E8-13). Imports the sheets under Art/ and the clips under
    /// Audio/ with their presets, builds CharacterArt / ChapterTheme / VfxSet / SoundBank data assets, wires them into
    /// Game.unity (flipbooks, parallax layers, ground, VFX pool, combat FX, camera shake) and Boot.unity (audio
    /// sources) and applies the Galmuri pixel font to every UI Text in Boot and Game. Safe to run again.
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod SoloHero.Editor.ArtBuilder.BuildBatch
    /// </summary>
    public static class ArtBuilder
    {
        public const string FontPath = "Assets/SoloHero/Art/Fonts/Galmuri11.ttf";

        private const string ArtRoot = "Assets/SoloHero/Art";
        private const string DataArt = "Assets/SoloHero/Data/Art";
        private const string DataChapters = "Assets/SoloHero/Data/Chapters";
        private const string DataAudio = "Assets/SoloHero/Data/Audio";
        private const string AudioRoot = "Assets/SoloHero/Audio";
        private const int SfxSources = 6;
        private const string GameScene = "Assets/SoloHero/Scenes/Game.unity";
        private const string BootScene = "Assets/SoloHero/Scenes/Boot.unity";
        private const int LayerSlots = 8;
        private const float ViewHeightPx = 240f;
        private const float NearestFollow = 0.3f;
        private const float FarthestFollow = 0.97f;

        /// <summary>
        /// Chapter looks (E8-03..05, D-068): background folder, 2-3 enemy looks mixed per stage, boss look and music.
        /// 8 enemy looks = 5 Kings and Pigs pigs + 3 recolours; 3 bosses = King Pig in 3 colours, 2 with a dark variant.
        /// </summary>
        private static readonly (string folder, string label, string[] enemies, string boss, string bgm)[] Chapters =
        {
            ("Ch1_Meadow", "Meadow", new[] { "pig", "matchpig" }, "kingpig", "bgm_ch1"),
            ("Ch2_Snowpeak", "Snowpeak", new[] { "pig", "boxpig", "hidepig" }, "kingpigb", "bgm_ch2"),
            ("Ch3_Forest", "Forest", new[] { "boxpig", "bombpig", "matchpig" }, "kingpigd", "bgm_ch3"),
            ("Ch4_Dusk", "Dusk", new[] { "pigr", "hidepig", "boxpigv" }, "kingpign", "bgm_ch4"),
            ("Ch5_Sunset", "Sunset", new[] { "bombpigr", "pigr", "boxpigv" }, "kingpigr", "bgm_ch5"),
        };

        private static readonly Dictionary<string, string> LookNames = new Dictionary<string, string>
        {
            { "pig", "Enemy_Pig" }, { "pigr", "Enemy_PigRed" }, { "boxpig", "Enemy_BoxPig" }, { "boxpigv", "Enemy_BoxPigViolet" },
            { "bombpig", "Enemy_BombPig" }, { "bombpigr", "Enemy_BombPigRed" }, { "hidepig", "Enemy_HidePig" }, { "matchpig", "Enemy_MatchPig" },
            { "kingpig", "Boss_KingPig" }, { "kingpigb", "Boss_KingPigBlue" }, { "kingpigd", "Boss_KingPigDark" },
            { "kingpign", "Boss_KingPigNavy" }, { "kingpigr", "Boss_KingPigRed" },
        };

        [MenuItem("Tools/Setup/Build Art")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildBatch();
        }

        public static void BuildBatch()
        {
            ReimportArt();
            CharacterArt hero = BuildCharacter("Hero", "king", "Hero_King", 2);
            var looks = new Dictionary<string, CharacterArt>();
            foreach (KeyValuePair<string, string> look in LookNames)
            {
                bool boss = look.Value.StartsWith("Boss_");
                looks[look.Key] = BuildCharacter(boss ? "Bosses" : "Enemies", look.Key, look.Value, boss ? 3 : 2);
            }

            ChapterThemeSet themes = BuildThemes(looks);
            VfxSet vfx = BuildVfx();
            BuildEquipmentIcons();
            SoundBank bank = BuildSoundBank();
            WireGameScene(hero, looks["pig"], looks["kingpig"], themes, vfx);
            WireBootAudio(bank);

            GameUiBuilder.BuildBatch();
            AppIconSetup.Apply();
            ApplyFont(GameScene);
            ApplyFont(BootScene);
            Debug.Log("[Art] art built and wired");
        }

        private static void ReimportArt()
        {
            AssetDatabase.Refresh();
            // New files may have been imported before the preset scripts compiled; force the presets on everything.
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }

        private static CharacterArt BuildCharacter(string folder, string entity, string assetName, int pixelScale)
        {
            Directory.CreateDirectory(DataArt);
            string path = DataArt + "/" + assetName + ".asset";
            var art = AssetDatabase.LoadAssetAtPath<CharacterArt>(path);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<CharacterArt>();
                AssetDatabase.CreateAsset(art, path);
            }

            string dir = ArtRoot + "/" + folder;
            art.idle = Clip(dir, entity, "idle", true);
            art.attack = Clip(dir, entity, "attack", true);
            // Some looks ship without these clips: run falls back to idle, hit only flashes, dead vanishes in a puff.
            art.run = Clip(dir, entity, "run", false);
            if (art.run.Length == 0) art.run = art.idle;
            art.hit = Clip(dir, entity, "hit", false);
            art.dead = Clip(dir, entity, "dead", false);
            art.pixelScale = pixelScale;
            art.headHeight = HeadHeight(art.idle.Length > 0 ? art.idle[0] : null);
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art;
        }

        /// <summary>Highest opaque row of a frame above its pivot, in sprite units (read from the PNG file).</summary>
        private static float HeadHeight(Sprite frame)
        {
            if (frame == null) return 0f;
            string file = AssetDatabase.GetAssetPath(frame.texture);
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(file))) return 0f;
                Rect r = frame.rect;
                for (int y = (int)r.yMax - 1; y >= (int)r.yMin; y--)
                {
                    for (int x = (int)r.xMin; x < (int)r.xMax; x++)
                    {
                        if (texture.GetPixel(x, y).a > 0.1f) return (y + 1 - r.yMin - frame.pivot.y) / frame.pixelsPerUnit;
                    }
                }

                return 0f;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static Sprite[] Clip(string dir, string entity, string clip, bool required)
        {
            string file = Directory.GetFiles(dir, entity + "_" + clip + "_*.png").Select(p => p.Replace('\\', '/')).FirstOrDefault();
            if (file == null && !required) return new Sprite[0];
            if (file == null) throw new InvalidOperationException("missing sheet " + dir + "/" + entity + "_" + clip);
            return AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
        }

        private static ChapterThemeSet BuildThemes(Dictionary<string, CharacterArt> looks)
        {
            Directory.CreateDirectory(DataChapters);
            var list = new List<ChapterTheme>();
            for (int c = 0; c < Chapters.Length; c++)
            {
                string dir = ArtRoot + "/Backgrounds/" + Chapters[c].folder;
                string[] files = Directory.GetFiles(dir, "layer_*.png").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
                Sprite[] layers = files.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();

                string path = DataChapters + "/Chapter_" + (c + 1) + "_" + Chapters[c].label + ".asset";
                var theme = AssetDatabase.LoadAssetAtPath<ChapterTheme>(path);
                if (theme == null)
                {
                    theme = ScriptableObject.CreateInstance<ChapterTheme>();
                    AssetDatabase.CreateAsset(theme, path);
                }

                theme.layers = layers;
                theme.follow = new float[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    float t = layers.Length > 1 ? (float)i / (layers.Length - 1) : 0f;
                    theme.follow[i] = Mathf.Lerp(FarthestFollow, NearestFollow, t);
                }

                float height = layers.Length > 0 ? layers[0].rect.height : ViewHeightPx;
                theme.pixelScale = Mathf.Max(1, Mathf.CeilToInt(ViewHeightPx / height));
                theme.sky = TopLeftColor(files.Length > 0 ? files[0] : null);
                theme.enemies = Chapters[c].enemies.Select(e => looks[e]).ToArray();
                theme.boss = looks[Chapters[c].boss];
                theme.bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/Bgm/" + Chapters[c].bgm + ".ogg");
                if (theme.bgm == null) Debug.LogWarning("[Art] missing music " + Chapters[c].bgm);
                EditorUtility.SetDirty(theme);
                list.Add(theme);
            }

            string setPath = DataChapters + "/ChapterThemeSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<ChapterThemeSet>(setPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ChapterThemeSet>();
                AssetDatabase.CreateAsset(set, setPath);
            }

            set.themes = list.ToArray();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        private static VfxSet BuildVfx()
        {
            string path = DataArt + "/VfxSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<VfxSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<VfxSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            string dir = ArtRoot + "/Vfx";
            set.slash = Clip(dir, "vfxslash", "play", true);
            set.whirl = Clip(dir, "vfxwhirl", "play", true);
            set.ring = Clip(dir, "vfxring", "play", true);
            set.boom = Clip(dir, "vfxboom", "play", true);
            set.spark = Clip(dir, "vfxspark", "play", true);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>E8-07: Art/Icons/Equipment/equip_{slot}_{grade}.png into the 16-slot icon set.</summary>
        private static void BuildEquipmentIcons()
        {
            string path = DataArt + "/EquipmentIcons.asset";
            var set = AssetDatabase.LoadAssetAtPath<EquipmentIconSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EquipmentIconSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            string[] slots = { "sword", "helm", "armor", "boots" };
            string[] grades = { "common", "rare", "epic", "legendary" };
            set.icons = new Sprite[slots.Length * grades.Length];
            for (int s = 0; s < slots.Length; s++)
            {
                for (int g = 0; g < grades.Length; g++)
                {
                    string file = ArtRoot + "/Icons/Equipment/equip_" + slots[s] + "_" + grades[g] + ".png";
                    set.icons[s * grades.Length + g] = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                    if (set.icons[s * grades.Length + g] == null) Debug.LogWarning("[Art] missing icon " + file);
                }
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        /// <summary>SfxId.HeroHurt -> Audio/Sfx/sfx_hero_hurt.wav, SfxId.Skill1 -> sfx_skill1.wav.</summary>
        private static SoundBank BuildSoundBank()
        {
            Directory.CreateDirectory(DataAudio);
            string path = DataAudio + "/SoundBank.asset";
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, path);
            }

            Array ids = Enum.GetValues(typeof(SfxId));
            bank.sfx = new AudioClip[ids.Length];
            foreach (SfxId id in ids)
            {
                string file = AudioRoot + "/Sfx/sfx_" + Regex.Replace(id.ToString(), "(?<=[a-z])(?=[A-Z])", "_").ToLowerInvariant() + ".wav";
                bank.sfx[(int)id] = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                if (bank.sfx[(int)id] == null) Debug.LogWarning("[Art] missing sound " + file);
            }

            bank.bossBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/Bgm/bgm_boss.ogg");
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        /// <summary>Boot object: AudioService with one BGM source and a fixed SFX source pool (no runtime AddComponent).</summary>
        private static void WireBootAudio(SoundBank bank)
        {
            var scene = EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
            BootSequence boot = Object.FindObjectOfType<BootSequence>();
            if (boot == null)
            {
                Debug.LogError("[Art] BootSequence not found in " + BootScene);
                return;
            }

            Transform old = boot.transform.Find("Audio");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Audio");
            root.transform.SetParent(boot.transform, false);

            AudioSource bgm = NewSource(root.transform, "Bgm");
            var sfx = new AudioSource[SfxSources];
            for (int i = 0; i < SfxSources; i++) sfx[i] = NewSource(root.transform, "Sfx" + i);

            AudioService audio = boot.GetComponent<AudioService>();
            if (audio == null) audio = boot.gameObject.AddComponent<AudioService>();
            var so = new SerializedObject(audio);
            so.FindProperty("_bank").objectReferenceValue = bank;
            so.FindProperty("_bgm").objectReferenceValue = bgm;
            SerializedProperty list = so.FindProperty("_sfx");
            list.arraySize = SfxSources;
            for (int i = 0; i < SfxSources; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sfx[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static AudioSource NewSource(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private static Color TopLeftColor(string pngPath)
        {
            if (pngPath == null) return Color.black;
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(pngPath));
            Color c = tex.GetPixel(0, tex.height - 1);
            Object.DestroyImmediate(tex);
            c.a = 1f;
            return c;
        }

        private static void WireGameScene(CharacterArt hero, CharacterArt pig, CharacterArt boss, ChapterThemeSet themes, VfxSet vfxSet)
        {
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            var view = Object.FindObjectOfType<CombatWorldView>();
            var rig = Object.FindObjectOfType<ParallaxRig>();
            var session = Object.FindObjectOfType<CombatSession>();
            if (view == null || rig == null)
            {
                Debug.LogError("[Art] CombatWorldView / ParallaxRig not found in " + GameScene);
                return;
            }

            foreach (string old in new[] { "FarLayer", "MidLayer", "NearLayer", "GroundLayer", "Background" })
            {
                GameObject go = GameObject.Find(old);
                if (go != null) Object.DestroyImmediate(go);
            }

            var root = new GameObject("Background");
            var layers = new SpriteRenderer[LayerSlots];
            for (int i = 0; i < LayerSlots; i++)
            {
                var go = new GameObject("Layer" + i);
                go.transform.SetParent(root.transform, false);
                layers[i] = go.AddComponent<SpriteRenderer>();
                layers[i].drawMode = SpriteDrawMode.Tiled;
                layers[i].enabled = false;
            }

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(root.transform, false);
            var ground = groundGo.AddComponent<SpriteRenderer>();
            ground.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Tiles/ground_beam.png");
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = new Vector2(32f, 1f);
            ground.sortingOrder = -5;
            groundGo.transform.position = new Vector3(0f, -1f, 0f);

            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("_session").objectReferenceValue = session;
            rigSo.FindProperty("_themes").objectReferenceValue = themes;
            SerializedProperty layerProp = rigSo.FindProperty("_layers");
            layerProp.arraySize = LayerSlots;
            for (int i = 0; i < LayerSlots; i++) layerProp.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            rigSo.FindProperty("_ground").objectReferenceValue = ground;
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            Camera camera = Object.FindObjectOfType<Camera>();
            CameraShake shake = camera != null ? camera.GetComponent<CameraShake>() : null;
            if (camera != null && shake == null) shake = camera.gameObject.AddComponent<CameraShake>();

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_heroArt").objectReferenceValue = hero;
            viewSo.FindProperty("_enemyArt").objectReferenceValue = pig;
            viewSo.FindProperty("_bossArt").objectReferenceValue = boss;
            viewSo.FindProperty("_themes").objectReferenceValue = themes;
            viewSo.FindProperty("_shake").objectReferenceValue = shake;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            WireVfx(session, view, vfxSet, shake, themes);
            WireHpBars(view);

            var heroRenderer = (SpriteRenderer)viewSo.FindProperty("_heroRenderer").objectReferenceValue;
            PrepareActor(heroRenderer, hero, 10);
            SerializedProperty enemies = viewSo.FindProperty("_enemyRenderers");
            for (int i = 0; i < enemies.arraySize; i++)
                PrepareActor((SpriteRenderer)enemies.GetArrayElementAtIndex(i).objectReferenceValue, pig, 5);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>World HP bars for the 4 enemy slots (back + fill), drawn above enemies and under VFX.</summary>
        private static void WireHpBars(CombatWorldView view)
        {
            GameObject old = GameObject.Find("EnemyHpBars");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("EnemyHpBars");
            Sprite white = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/ui_white.png");
            var backs = new SpriteRenderer[4];
            var fills = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                backs[i] = BarPart(root.transform, "Back" + i, white, new Color(0.08f, 0.06f, 0.12f, 0.9f), 15);
                fills[i] = BarPart(root.transform, "Fill" + i, white, new Color(0.9f, 0.27f, 0.25f, 1f), 16);
            }

            var so = new SerializedObject(view);
            SerializedProperty b = so.FindProperty("_hpBacks");
            SerializedProperty f = so.FindProperty("_hpFills");
            b.arraySize = 4;
            f.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                b.GetArrayElementAtIndex(i).objectReferenceValue = backs[i];
                f.GetArrayElementAtIndex(i).objectReferenceValue = fills[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteRenderer BarPart(Transform parent, string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            r.enabled = false;
            return r;
        }

        /// <summary>"Vfx" object: the world VFX pool (template + pre-warm at runtime) and CombatFx.</summary>
        private static void WireVfx(CombatSession session, CombatWorldView view, VfxSet set, CameraShake shake, ChapterThemeSet themes)
        {
            GameObject old = GameObject.Find("Vfx");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Vfx");

            var templateGo = new GameObject("VfxTemplate");
            templateGo.transform.SetParent(root.transform, false);
            templateGo.AddComponent<SpriteRenderer>();
            templateGo.AddComponent<SpriteFlipbook>();
            VfxItem template = templateGo.AddComponent<VfxItem>();
            templateGo.SetActive(false);

            VfxPool pool = root.AddComponent<VfxPool>();
            var poolSo = new SerializedObject(pool);
            poolSo.FindProperty("_template").objectReferenceValue = template;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            CombatFx fx = root.AddComponent<CombatFx>();
            var fxSo = new SerializedObject(fx);
            fxSo.FindProperty("_session").objectReferenceValue = session;
            fxSo.FindProperty("_view").objectReferenceValue = view;
            fxSo.FindProperty("_vfx").objectReferenceValue = pool;
            fxSo.FindProperty("_set").objectReferenceValue = set;
            fxSo.FindProperty("_shake").objectReferenceValue = shake;
            fxSo.FindProperty("_themes").objectReferenceValue = themes;
            fxSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PrepareActor(SpriteRenderer renderer, CharacterArt art, int sortingOrder)
        {
            if (renderer == null) return;
            if (renderer.GetComponent<SpriteFlipbook>() == null) renderer.gameObject.AddComponent<SpriteFlipbook>();
            renderer.sprite = art.idle.Length > 0 ? art.idle[0] : null;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localScale = new Vector3(art.pixelScale, art.pixelScale, 1f);
        }

        private static void ApplyFont(string scenePath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) return;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (Text text in Object.FindObjectsOfType<Text>(true))
            {
                text.font = font;
                EditorUtility.SetDirty(text);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
