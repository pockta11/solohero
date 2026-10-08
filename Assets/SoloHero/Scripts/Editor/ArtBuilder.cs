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
    /// sources) and applies the UI font (rounded Jua) to every UI Text in Boot and Game. Safe to run again.
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod SoloHero.Editor.ArtBuilder.BuildBatch
    /// </summary>
    public static class ArtBuilder
    {
        public const string FontPath = "Assets/SoloHero/Art/Fonts/SoloHeroJua.ttf";

        private const string ArtRoot = "Assets/SoloHero/Art";
        private const string DataArt = "Assets/SoloHero/Data/Art";
        private const string DataChapters = "Assets/SoloHero/Data/Chapters";

        /// <summary>D-091 camera height: the ground line (y = 0) shows at about 54% of the screen, mid-floor.</summary>
        private const float CameraY = -0.6f;
        private const string DataAudio = "Assets/SoloHero/Data/Audio";
        private const string AudioRoot = "Assets/SoloHero/Audio";
        private const int SfxSources = 6;
        private const string GameScene = "Assets/SoloHero/Scenes/Game.unity";
        private const string BootScene = "Assets/SoloHero/Scenes/Boot.unity";
        private const string FlashShader = "SoloHero/SpriteFlash";
        private const string FlashMaterialPath = "Assets/SoloHero/Art/Materials/SpriteFlash.mat";

        /// <summary>D-096: character clips have twice the frames of the 10 fps era, so they play at 20 fps.</summary>
        private const float CharacterFps = 20f;

        private const int CoinCount = 24;
        private const int LayerSlots = 8;
        private const float ViewHeightPx = 240f;
        private const float NearestFollow = 0.3f;
        private const float FarthestFollow = 0.97f;

        /// <summary>
        /// Chapter looks (E8-03..05, D-082): background folder, 2-3 enemy looks mixed per stage, boss look and music.
        /// 8 enemy looks (goblin, skeleton, mushroom, flying eye + 4 recolours) and 6 bosses, all modelled in Blender
        /// and pixelised by tools/art/build3d.py.
        /// </summary>
        private static readonly (string folder, string label, string[] enemies, string boss, string bgm)[] Chapters =
        {
            ("Ch1_Meadow", "Meadow", new[] { "goblin", "mushroom" }, "ronin", "bgm_ch1"),
            ("Ch2_Snowpeak", "Snowpeak", new[] { "skeleton", "goblin", "flyeye" }, "necro", "bgm_ch2"),
            ("Ch3_Forest", "Forest", new[] { "mushroom", "flyeye", "goblinr" }, "golem", "bgm_ch3"),
            ("Ch4_Dusk", "Dusk", new[] { "skeletonv", "flyeyer", "mushroomb" }, "shadowmage", "bgm_ch4"),
            ("Ch5_Sunset", "Sunset", new[] { "goblinr", "skeletonv", "flyeyer" }, "firemage", "bgm_ch5"),
            // D-131 chapters 6-10 (new bosses and recolours; music reused).
            ("Ch6_Desert", "Desert", new[] { "goblins", "skeleton", "flyeye" }, "sandking", "bgm_ch5"),
            ("Ch7_Volcano", "Volcano", new[] { "goblinr", "skeletonr", "flyeyer" }, "lavagolem", "bgm_ch4"),
            ("Ch8_Swamp", "Swamp", new[] { "mushroomg", "goblin", "flyeye" }, "swampwitch", "bgm_ch3"),
            ("Ch9_Crystal", "Crystal", new[] { "flyeyec", "skeletonv", "mushroomb" }, "crystalgolem", "bgm_ch2"),
            ("Ch10_Castle", "Castle", new[] { "skeletonr", "goblinr", "flyeyer" }, "demonknight", "bgm_ch4"),
        };

        /// <summary>
        /// D-110 role looks per chapter (index-aligned with <see cref="Chapters"/>): soldier, rusher, tank, shooter.
        /// The same creature always plays the same role - goblins fight up close, flying eyes rush, mushrooms soak,
        /// skeletons throw bones - with the chapter's colour variants.
        /// </summary>
        private static readonly (string melee, string fast, string tank, string ranged)[] RoleLooks =
        {
            ("goblin", "flyeye", "mushroom", "skeleton"),
            ("goblin", "flyeye", "mushroomb", "skeleton"),
            ("goblinr", "flyeye", "mushroom", "skeleton"),
            ("goblinr", "flyeyer", "mushroomb", "skeletonv"),
            ("goblinr", "flyeyer", "mushroom", "skeletonv"),
            ("goblins", "flyeye", "mushroom", "skeleton"),
            ("goblinr", "flyeyer", "mushroomb", "skeletonr"),
            ("goblin", "flyeye", "mushroomg", "skeleton"),
            ("goblinr", "flyeyec", "mushroomb", "skeletonv"),
            ("goblinr", "flyeyer", "mushroomb", "skeletonr"),
        };

        /// <summary>
        /// Look key -> (asset name, integer pixel scale). D-092: every look is drawn at 1x on the same pixel grid as
        /// the hero; bosses get a larger head in the art instead of a 2x scale.
        /// </summary>
        private static readonly Dictionary<string, (string asset, int scale)> LookNames = new Dictionary<string, (string, int)>
        {
            { "goblin", ("Enemy_Goblin", 1) }, { "goblinr", ("Enemy_GoblinRed", 1) },
            { "skeleton", ("Enemy_Skeleton", 1) }, { "skeletonv", ("Enemy_SkeletonViolet", 1) },
            { "mushroom", ("Enemy_Mushroom", 1) }, { "mushroomb", ("Enemy_MushroomBlue", 1) },
            { "flyeye", ("Enemy_FlyingEye", 1) }, { "flyeyer", ("Enemy_FlyingEyeRed", 1) },
            { "ronin", ("Boss_Ronin", 1) }, { "necro", ("Boss_Necromancer", 1) }, { "ranger", ("Boss_Ranger", 1) },
            { "shadowmage", ("Boss_ShadowMage", 1) }, { "firemage", ("Boss_FireMage", 1) }, { "golem", ("Boss_Golem", 1) },
            // D-131 chapters 6-10.
            { "goblins", ("Enemy_GoblinSand", 1) }, { "skeletonr", ("Enemy_SkeletonRed", 1) },
            { "mushroomg", ("Enemy_MushroomGreen", 1) }, { "flyeyec", ("Enemy_FlyingEyeCyan", 1) },
            { "sandking", ("Boss_SandKing", 1) }, { "lavagolem", ("Boss_LavaGolem", 1) }, { "swampwitch", ("Boss_SwampWitch", 1) },
            { "crystalgolem", ("Boss_CrystalGolem", 1) }, { "demonknight", ("Boss_DemonKnight", 1) },
        };

        [MenuItem("Tools/Setup/Build Art")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildBatch();
        }

        /// <summary>Batch: reimport character sheets so changed PNGs replace the cached textures.</summary>
        public static void ReimportCharactersBatch()
        {
            AssetDatabase.Refresh();
            foreach (string folder in new[] { "Hero", "Enemies", "Bosses" })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot + "/" + folder }))
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Art] characters reimported");
        }

        public static void BuildBatch()
        {
            ReimportArt();
            CharacterArt hero = BuildCharacter("Hero", "knight", "Hero_Knight", 1);
            // D-114 pets (D-102 companions), index-aligned with PetCatalog: Art/Pets/pet{id}_* into Data/Art/Pet_{Id}.asset.
            var petArts = new CharacterArt[SoloHero.Core.Pets.PetCatalog.Count];
            for (int p = 0; p < petArts.Length; p++)
            {
                string id = SoloHero.Core.Pets.PetCatalog.All[p].Id;
                petArts[p] = BuildCharacter("Pets", "pet" + id, "Pet_" + char.ToUpperInvariant(id[0]) + id.Substring(1), 1);
            }
            // D-104 job looks, index-aligned with JobCatalog.All (the beginner is the knight above).
            var jobArts = new CharacterArt[SoloHero.Core.Jobs.JobCatalog.Count];
            for (int j = 0; j < jobArts.Length; j++)
            {
                string look = SoloHero.Core.Jobs.JobCatalog.All[j].Look;
                jobArts[j] = look == "knight" ? hero : BuildCharacter("Hero", look, JobAssetName(look), 1);
            }
            var looks = new Dictionary<string, CharacterArt>();
            foreach (KeyValuePair<string, (string asset, int scale)> look in LookNames)
            {
                bool boss = look.Value.asset.StartsWith("Boss_");
                looks[look.Key] = BuildCharacter(boss ? "Bosses" : "Enemies", look.Key, look.Value.asset, look.Value.scale);
            }

            ChapterThemeSet themes = BuildThemes(looks);
            VfxSet vfx = BuildVfx();
            BuildEquipmentIcons();
            BuildSkillIcons();
            SoundBank bank = BuildSoundBank();
            WireGameScene(hero, jobArts, petArts, looks["goblin"], looks["ronin"], themes, vfx);
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

        /// <summary>D-104 job look asset under Data/Art: "knight1" -> "Hero_Knight1", "jobmage" -> "Hero_Jobmage".</summary>
        public static string JobAssetName(string look) => "Hero_" + char.ToUpperInvariant(look[0]) + look.Substring(1);

        public static string JobArtPath(string look) => DataArt + "/" + JobAssetName(look) + ".asset";

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
            art.fps = CharacterFps;
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
                theme.melee = looks[RoleLooks[c].melee];
                theme.fast = looks[RoleLooks[c].fast];
                theme.tank = looks[RoleLooks[c].tank];
                theme.ranged = looks[RoleLooks[c].ranged];
                theme.boss = looks[Chapters[c].boss];
                theme.bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/Bgm/" + Chapters[c].bgm + ".ogg");
                theme.floor = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Tiles/floor_" + (c + 1) + ".png");
                if (theme.floor == null) Debug.LogWarning("[Art] missing floor for chapter " + (c + 1));
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
            var clips = new List<VfxClip>();
            foreach ((string name, bool ground, float fps) in SkillClips)
            {
                Sprite[] frames = Clip(dir, "vfx" + name, "play", true);
                if (frames == null || frames.Length == 0) continue;
                clips.Add(new VfxClip
                {
                    name = name,
                    frames = frames,
                    fps = fps,
                    ground = ground,
                    halfHeight = frames[0].rect.height * 0.5f / frames[0].pixelsPerUnit
                });
            }

            set.clips = clips.ToArray();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>D-078 coloured skill clips: name (SkillDef.Vfx), stands on the ground, frames per second.</summary>
        private static readonly (string, bool, float)[] SkillClips =
        {
            ("fire", false, 16f), ("bolt", true, 14f), ("ice", true, 14f), ("poison", true, 12f),
            ("meteor", true, 16f), ("holy", true, 14f), ("tornado", true, 14f), ("swords", true, 16f),
            ("heal", true, 12f), ("shield", false, 12f), ("aura", true, 14f), ("vortex", false, 14f),
            ("breath", true, 14f), ("phoenix", true, 12f), ("wave", false, 20f),
            // D-098 unique clips: glacier spear, judgement sword, quick slash cross, time stop clock.
            ("spear", false, 18f), ("judge", true, 16f), ("cross", false, 22f), ("clock", false, 14f),
            // D-109 mark skills' target sigil.
            ("mark", false, 16f)
        };

        /// <summary>D-078: Art/Icons/Skills/skill_{id}.png for every catalog skill, plus the lock icon.</summary>
        private static void BuildSkillIcons()
        {
            string path = DataArt + "/SkillIcons.asset";
            var set = AssetDatabase.LoadAssetAtPath<SkillIconSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<SkillIconSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            // Catalog skills, then D-104 job ultimates (their skill id) and job main attacks ("main_" + job id).
            var ids = new List<string>();
            foreach (SoloHero.Core.Skills.SkillDef def in SoloHero.Core.Skills.SkillCatalog.All) ids.Add(def.Id);
            foreach (SoloHero.Core.Jobs.JobDef job in SoloHero.Core.Jobs.JobCatalog.All)
            {
                if (job.Ultimate != null) ids.Add(job.Ultimate.IconId);
                if (job.Main != null) ids.Add(SoloHero.Core.Jobs.JobCatalog.MainIconId(job));
            }

            set.ids = ids.ToArray();
            set.icons = new Sprite[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                // D-108: the smooth icons (tools/art/skillgen3.py) win over the 24 px pixel ones.
                string file = ArtRoot + "/UI/Hd/Skills/skill_" + ids[i] + ".png";
                if (AssetDatabase.LoadAssetAtPath<Sprite>(file) == null) file = ArtRoot + "/Icons/Skills/skill_" + ids[i] + ".png";
                set.icons[i] = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                if (set.icons[i] == null) Debug.LogWarning("[Art] missing icon " + file);
            }

            set.locked = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/Hd/Icons/hdicon_lock.png");
            if (set.locked == null) set.locked = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/Icons/icon_lock.png");
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        /// <summary>E8-07: Art/Icons/Equipment/equip_{slot}_{grade}.png into the icon set (D-109: 8 slots; D-113: x 7 grades).</summary>
        private static void BuildEquipmentIcons()
        {
            string path = DataArt + "/EquipmentIcons.asset";
            var set = AssetDatabase.LoadAssetAtPath<EquipmentIconSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EquipmentIconSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            string[] slots = { "sword", "helm", "armor", "boots", "gloves", "necklace", "ring", "earring" };
            string[] grades = { "common", "uncommon", "rare", "epic", "legendary", "mythic", "ancient" };
            set.icons = new Sprite[slots.Length * grades.Length];
            for (int s = 0; s < slots.Length; s++)
            {
                for (int g = 0; g < grades.Length; g++)
                {
                    // D-108: the smooth icons (tools/art/equipgen3.py) win over the 16 px pixel ones.
                    string file = ArtRoot + "/UI/Hd/Equipment/equip_" + slots[s] + "_" + grades[g] + ".png";
                    if (AssetDatabase.LoadAssetAtPath<Sprite>(file) == null)
                        file = ArtRoot + "/Icons/Equipment/equip_" + slots[s] + "_" + grades[g] + ".png";
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
            FitCamera(Object.FindObjectOfType<Camera>());

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

        private static void WireGameScene(CharacterArt hero, CharacterArt[] jobArts, CharacterArt[] petArts, CharacterArt pig, CharacterArt boss, ChapterThemeSet themes, VfxSet vfxSet)
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
            // D-091 floor plane: the chapter floor replaces it at runtime (ParallaxRig.ApplyFloor).
            ground.sprite = themes != null && themes.themes.Length > 0 && themes.themes[0].floor != null
                ? themes.themes[0].floor
                : AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Tiles/ground_beam.png");
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = new Vector2(ground.sprite.bounds.size.x * 8f, ground.sprite.bounds.size.y);
            ground.sortingOrder = -5;
            groundGo.transform.position = new Vector3(0f, ParallaxRig.FloorTop - ground.sprite.bounds.size.y, 0f);

            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("_session").objectReferenceValue = session;
            rigSo.FindProperty("_themes").objectReferenceValue = themes;
            SerializedProperty layerProp = rigSo.FindProperty("_layers");
            layerProp.arraySize = LayerSlots;
            for (int i = 0; i < LayerSlots; i++) layerProp.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
            rigSo.FindProperty("_ground").objectReferenceValue = ground;
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            Camera camera = Object.FindObjectOfType<Camera>();
            // D-091: the camera sits lower so the floor plane fills the band above the skill bar and the
            // characters stand in the middle of it (about 54% of the screen height).
            if (camera != null) camera.transform.position = new Vector3(camera.transform.position.x, CameraY, camera.transform.position.z);
            FitCamera(camera);
            CameraShake shake = camera != null ? camera.GetComponent<CameraShake>() : null;
            if (camera != null && shake == null) shake = camera.gameObject.AddComponent<CameraShake>();

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_heroArt").objectReferenceValue = hero;
            SerializedProperty jobsProp = viewSo.FindProperty("_jobArts");
            jobsProp.arraySize = jobArts.Length;
            for (int j = 0; j < jobArts.Length; j++) jobsProp.GetArrayElementAtIndex(j).objectReferenceValue = jobArts[j];
            viewSo.FindProperty("_enemyArt").objectReferenceValue = pig;
            viewSo.FindProperty("_bossArt").objectReferenceValue = boss;
            viewSo.FindProperty("_themes").objectReferenceValue = themes;
            viewSo.FindProperty("_shake").objectReferenceValue = shake;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            WireVfx(session, view, vfxSet, shake, themes);
            WireHpBars(view);
            WireShadows(view);
            WireShots(view);
            WireCoins(view, camera);
            WirePet(session, vfxSet, petArts);

            Material flash = FlashMaterial();
            var heroRenderer = (SpriteRenderer)viewSo.FindProperty("_heroRenderer").objectReferenceValue;
            PrepareActor(heroRenderer, hero, 10, flash);
            viewSo.Update();
            SerializedProperty enemies = viewSo.FindProperty("_enemyRenderers");
            EnsureEnemyRenderers(viewSo, enemies);
            for (int i = 0; i < enemies.arraySize; i++)
                PrepareActor((SpriteRenderer)enemies.GetArrayElementAtIndex(i).objectReferenceValue, pig, 5, flash);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// D-107: the scene came with four enemy renderers; clone the last one (flipbook and all) until there is one per
        /// enemy slot, named Enemy{i} next to the others.
        /// </summary>
        private static void EnsureEnemyRenderers(SerializedObject viewSo, SerializedProperty enemies)
        {
            int want = CombatWorldView.EnemySlotVisualCount;
            if (enemies.arraySize >= want || enemies.arraySize == 0) return;
            var last = (SpriteRenderer)enemies.GetArrayElementAtIndex(enemies.arraySize - 1).objectReferenceValue;
            if (last == null) return;
            for (int i = enemies.arraySize; i < want; i++)
            {
                GameObject copy = Object.Instantiate(last.gameObject, last.transform.parent);
                copy.name = "Enemy" + i;
                enemies.arraySize = i + 1;
                enemies.GetArrayElementAtIndex(i).objectReferenceValue = copy.GetComponent<SpriteRenderer>();
            }

            viewSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>World HP bars for the enemy slots (back + fill), drawn above enemies and under VFX.</summary>
        private static void WireHpBars(CombatWorldView view)
        {
            GameObject old = GameObject.Find("EnemyHpBars");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("EnemyHpBars");
            // D-108: 1 x 3 px gradient strips (light top, mid, dark bottom) stretched into the bar.
            Sprite back = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/ui_hpback.png");
            Sprite fill = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/ui_hpfill.png");
            int count = CombatWorldView.EnemySlotVisualCount;
            var backs = new SpriteRenderer[count];
            var fills = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                backs[i] = BarPart(root.transform, "Back" + i, back, Color.white, 300);
                fills[i] = BarPart(root.transform, "Fill" + i, fill, Color.white, 301);
            }

            var so = new SerializedObject(view);
            so.FindProperty("_hpBarWidth").floatValue = 0.78f;
            so.FindProperty("_hpBarHeight").floatValue = 0.1f;
            SerializedProperty b = so.FindProperty("_hpBacks");
            SerializedProperty f = so.FindProperty("_hpFills");
            b.arraySize = count;
            f.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                b.GetArrayElementAtIndex(i).objectReferenceValue = backs[i];
                f.GetArrayElementAtIndex(i).objectReferenceValue = fills[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-082 ground shadows: one under the hero and one per enemy slot, above the ground, under bodies.</summary>
        private static void WireShadows(CombatWorldView view)
        {
            GameObject old = GameObject.Find("Shadows");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Shadows");
            Sprite blob = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/Tiles/shadow_blob.png");
            var shadows = new SpriteRenderer[1 + CombatWorldView.EnemySlotVisualCount];
            for (int i = 0; i < shadows.Length; i++)
                shadows[i] = BarPart(root.transform, "Shadow" + i, blob, Color.white, 1);
            var so = new SerializedObject(view);
            SerializedProperty prop = so.FindProperty("_shadows");
            prop.arraySize = shadows.Length;
            for (int i = 0; i < shadows.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = shadows[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-112 hit shards: one world-space ParticleSystem (manual Emit only) with square white particles that fall with
        /// gravity, shrink and fade; CombatFx bursts it on hits and kills.
        /// </summary>
        private static HitParticles WireParticles()
        {
            GameObject old = GameObject.Find("HitParticles");
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject("HitParticles");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 0.4f;
            main.startSpeed = 0f;
            main.startSize = 0.075f;
            main.gravityModifier = 0.75f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.loop = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f)));
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 260;
            renderer.sharedMaterial = ShardMaterial();
            HitParticles particles = go.AddComponent<HitParticles>();
            var so = new SerializedObject(particles);
            so.FindProperty("_system").objectReferenceValue = ps;
            so.ApplyModifiedPropertiesWithoutUndo();
            return particles;
        }

        /// <summary>D-112: an unlit sprite material over a plain white texture, so shards are crisp squares in their colour.</summary>
        private static Material ShardMaterial()
        {
            const string path = ArtRoot + "/Materials/HitShard.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (material == null)
            {
                material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }

            Texture2D white = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/UI/ui_white.png");
            if (white != null) material.mainTexture = white;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>D-110: one renderer per ranged shot slot (CombatWorld.ShotCapacity) showing the thrown bone.</summary>
        private static void WireShots(CombatWorldView view)
        {
            GameObject old = GameObject.Find("Shots");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Shots");
            Sprite[] frames = Clip(ArtRoot + "/Vfx", "vfxbone", "play", false);
            Sprite bone = frames.Length > 0 ? frames[0] : null;
            if (bone == null) Debug.LogWarning("[Art] missing Vfx/vfxbone_play_1.png");
            var shots = new SpriteRenderer[SoloHero.Core.Combat.CombatWorld.ShotCapacity];
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = BarPart(root.transform, "Shot" + i, bone, Color.white, 150);
                shots[i].enabled = false;
            }

            var so = new SerializedObject(view);
            SerializedProperty prop = so.FindProperty("_shotRenderers");
            prop.arraySize = shots.Length;
            for (int i = 0; i < shots.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = shots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-102 / D-114: the pet's renderer (flash material, flipbook) and its view, next to the hero.</summary>
        private static void WirePet(CombatSession session, VfxSet vfxSet, CharacterArt[] petArts)
        {
            foreach (string name in new[] { "Companion", "Pet" })
            {
                GameObject old = GameObject.Find(name);
                if (old != null) Object.DestroyImmediate(old);
            }

            var go = new GameObject("Pet");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 99;
            Material flash = FlashMaterial();
            if (flash != null) renderer.sharedMaterial = flash;
            go.AddComponent<SpriteFlipbook>();
            renderer.enabled = false;
            PetView view = go.AddComponent<PetView>();
            var so = new SerializedObject(view);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_renderer").objectReferenceValue = renderer;
            so.FindProperty("_vfx").objectReferenceValue = Object.FindObjectOfType<VfxPool>();
            so.FindProperty("_set").objectReferenceValue = vfxSet;
            SerializedProperty arts = so.FindProperty("_arts");
            arts.arraySize = petArts.Length;
            for (int i = 0; i < petArts.Length; i++) arts.GetArrayElementAtIndex(i).objectReferenceValue = petArts[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-096 kill coins: pooled coin sprites above characters and VFX, flown to the gold counter by CoinBurst.</summary>
        private static void WireCoins(CombatWorldView view, Camera camera)
        {
            GameObject old = GameObject.Find("Coins");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Coins");
            Sprite coin = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/UI/Icons/icon_coin.png");
            var coins = new SpriteRenderer[CoinCount];
            for (int i = 0; i < CoinCount; i++)
            {
                coins[i] = BarPart(root.transform, "Coin" + i, coin, Color.white, 420);
                coins[i].transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }

            CoinBurst burst = root.AddComponent<CoinBurst>();
            var so = new SerializedObject(burst);
            so.FindProperty("_view").objectReferenceValue = view;
            so.FindProperty("_camera").objectReferenceValue = camera;
            SerializedProperty prop = so.FindProperty("_coins");
            prop.arraySize = CoinCount;
            for (int i = 0; i < CoinCount; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = coins[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-096: the 2D Pixel Perfect package camera does nothing under URP except force an off-screen copy, so it is
        /// replaced by PixelCameraFit (integer zoom, full-resolution rendering).
        /// </summary>
        private static void FitCamera(Camera camera)
        {
            if (camera == null) return;
            foreach (MonoBehaviour c in camera.GetComponents<MonoBehaviour>())
            {
                if (c != null && c.GetType().Name == "PixelPerfectCamera") Object.DestroyImmediate(c, true);
            }

            if (camera.GetComponent<PixelCameraFit>() == null) camera.gameObject.AddComponent<PixelCameraFit>();
            EditorUtility.SetDirty(camera.gameObject);
        }

        /// <summary>The shared white-flash sprite material for characters (SoloHero/SpriteFlash).</summary>
        private static Material FlashMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (mat != null) return mat;
            Shader shader = Shader.Find(FlashShader);
            if (shader == null)
            {
                Debug.LogError("[Art] shader not found: " + FlashShader);
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(FlashMaterialPath));
            mat = new Material(shader) { name = "SpriteFlash" };
            AssetDatabase.CreateAsset(mat, FlashMaterialPath);
            AssetDatabase.SaveAssets();
            return mat;
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
            fxSo.FindProperty("_particles").objectReferenceValue = WireParticles();
            fxSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PrepareActor(SpriteRenderer renderer, CharacterArt art, int sortingOrder, Material material)
        {
            if (renderer == null) return;
            if (material != null) renderer.sharedMaterial = material;
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
