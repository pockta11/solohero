using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoloHero.Game.Combat;
using SoloHero.Game.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SoloHero.Editor
{
    /// <summary>
    /// Tools > Setup > Build Art (E8-02..05, E8-12). Imports the sheets under Art/ with SpriteImportPreset, builds
    /// CharacterArt / ChapterTheme data assets, wires them into Game.unity (flipbooks, parallax layers, ground) and
    /// applies the Galmuri pixel font to every UI Text in Boot and Game. Safe to run again.
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod SoloHero.Editor.ArtBuilder.BuildBatch
    /// </summary>
    public static class ArtBuilder
    {
        public const string FontPath = "Assets/SoloHero/Art/Fonts/Galmuri11.ttf";

        private const string ArtRoot = "Assets/SoloHero/Art";
        private const string DataArt = "Assets/SoloHero/Data/Art";
        private const string DataChapters = "Assets/SoloHero/Data/Chapters";
        private const string GameScene = "Assets/SoloHero/Scenes/Game.unity";
        private const string BootScene = "Assets/SoloHero/Scenes/Boot.unity";
        private const int LayerSlots = 8;
        private const float ViewHeightPx = 240f;
        private const float NearestFollow = 0.3f;
        private const float FarthestFollow = 0.97f;

        private static readonly (string folder, string label)[] Chapters =
        {
            ("Ch1_Meadow", "Meadow"),
            ("Ch2_Snowpeak", "Snowpeak"),
            ("Ch3_Forest", "Forest"),
            ("Ch4_Dusk", "Dusk"),
            ("Ch5_Sunset", "Sunset"),
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
            CharacterArt pig = BuildCharacter("Enemies", "pig", "Enemy_Pig", 2);
            CharacterArt boss = BuildCharacter("Bosses", "kingpig", "Boss_KingPig", 3);
            ChapterThemeSet themes = BuildThemes();
            WireGameScene(hero, pig, boss, themes);

            GameUiBuilder.BuildBatch();
            ApplyFont(GameScene);
            ApplyFont(BootScene);
            Debug.Log("[Art] art built and wired");
        }

        private static void ReimportArt()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
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
            art.idle = Clip(dir, entity, "idle");
            art.run = Clip(dir, entity, "run");
            art.attack = Clip(dir, entity, "attack");
            art.hit = Clip(dir, entity, "hit");
            art.dead = Clip(dir, entity, "dead");
            art.pixelScale = pixelScale;
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return art;
        }

        private static Sprite[] Clip(string dir, string entity, string clip)
        {
            string file = Directory.GetFiles(dir, entity + "_" + clip + "_*.png").Select(p => p.Replace('\\', '/')).FirstOrDefault();
            if (file == null) throw new InvalidOperationException("missing sheet " + dir + "/" + entity + "_" + clip);
            return AssetDatabase.LoadAllAssetsAtPath(file).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
        }

        private static ChapterThemeSet BuildThemes()
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

        private static void WireGameScene(CharacterArt hero, CharacterArt pig, CharacterArt boss, ChapterThemeSet themes)
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

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_heroArt").objectReferenceValue = hero;
            viewSo.FindProperty("_enemyArt").objectReferenceValue = pig;
            viewSo.FindProperty("_bossArt").objectReferenceValue = boss;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var heroRenderer = (SpriteRenderer)viewSo.FindProperty("_heroRenderer").objectReferenceValue;
            PrepareActor(heroRenderer, hero, 10);
            SerializedProperty enemies = viewSo.FindProperty("_enemyRenderers");
            for (int i = 0; i < enemies.arraySize; i++)
                PrepareActor((SpriteRenderer)enemies.GetArrayElementAtIndex(i).objectReferenceValue, pig, 5);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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
