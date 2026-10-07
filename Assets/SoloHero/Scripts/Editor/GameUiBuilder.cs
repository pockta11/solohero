using SoloHero.Game.Boot;
using SoloHero.Game.Combat;
using SoloHero.Game.Config;
using SoloHero.Game.Infrastructure;
using SoloHero.Game.Pooling;
using SoloHero.Game.UI;
using SoloHero.Core.Talents;
using SoloHero.Game.UI.Common;
using SoloHero.Game.UI.Panels;
using SoloHero.Game.View;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SoloHero.Editor
{
    /// <summary>
    /// Tools > Setup > Build Growth UI. Rebuilds the "GrowthUI" branch under the BattleHud canvas in Game.unity:
    /// bottom tab bar, Character / Equipment / Gacha / Skill / Talent panels, HUD, rails, popups and toast. Safe to
    /// run again (it replaces the branch). Fixed labels carry LocalizedText keys from the Strings table (E7-17).
    /// D-108: the smooth casual skin (UiSkin / Art/UI/Hd): cream panels and windows with brown lettering, candy
    /// buttons, a navy HUD and tab bar with outlined white lettering, grade slots, popup ribbons and close buttons.
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod SoloHero.Editor.GameUiBuilder.BuildBatch
    /// </summary>
    public static class GameUiBuilder
    {
        private const string ScenePath = "Assets/SoloHero/Scenes/Game.unity";
        private const string RootName = "GrowthUI";
        private const string DamageLayerName = "DamageTextLayer";
        private const string RevealName = "GachaReveal";
        private const string SettingsButtonName = "SettingsButton";
        private const string SettingsPopupName = "SettingsPopup";
        private const string DailyName = "DailyPopup";
        private const string DungeonName = "DungeonPopup";
        private const string CompanionName = "CompanionPopup";
        private const string JobName = "JobPopup";
        private static readonly string[] CompanionArtPaths =
        {
            "Assets/SoloHero/Data/Art/Pet_Slime.asset", "Assets/SoloHero/Data/Art/Pet_Wisp.asset",
            "Assets/SoloHero/Data/Art/Pet_Owl.asset", "Assets/SoloHero/Data/Art/Pet_Dragon.asset",
        };
        private const string SettingsWindowName = "SettingsWindow";
        private const string QuitPopupName = "QuitConfirm";
        private const string SafeAreaName = "SafeArea";
        private const string StageSelectName = "StageSelect";
        private const string BossIntroName = "BossIntro";
        private const string FlashName = "ScreenFlash";
        private const string SkillBarName = "SkillBar";
        private const string SkillAutoName = "SkillAuto";
        private const string BasicSkillName = "BasicSkill";
        private const string UltimateSkillName = "UltimateSkill";
        private const string SkillIconsPath = "Assets/SoloHero/Data/Art/SkillIcons.asset";
        private const string GradeFramesPath = "Assets/SoloHero/Data/Art/GradeFrames.asset";
        private const string HeroArtPath = "Assets/SoloHero/Data/Art/Hero_Knight.asset";
        private const string EquipmentIconsPath = "Assets/SoloHero/Data/Art/EquipmentIcons.asset";
        private const int BurstParticles = 24;

        // Layout in canvas units (1080 wide reference; the height is 1920 on 16:9 and up to 2400 on 20:9).
        // The growth panel spans PanelTop - TabTop of the height: 634 units on 16:9, so fixed bands must fit that.
        private const float TabTop = 0.07f;
        private const float PanelTop = 0.40f;
        /// <summary>Content inset inside the cream panel (its rim is 10 units).</summary>
        private const float PanelPad = 26f;
        /// <summary>D-106 / D-108 top bar height in canvas units.</summary>
        private const float TopBarHeight = 132f;
        private const float RailTop = TopBarHeight + 18f;
        private const float RailInset = 14f;
        private const float RailItem = 108f;
        private const float RailStep = 168f;
        private const float RailMenuButton = 104f;
        private const float RailMenuStep = 120f;
        private const float SkillSlot = 100f;
        private const float SkillSlotStep = 112f;
        private const float SkillGridCell = 132f;
        private const float SkillGridIcon = 104f;
        private const int SkillGridColumns = 5;
        private const float SkillDetailHeight = 172f;
        private const float TalentCell = 64f;
        private const float TalentDetailHeight = 134f;

        private static readonly string[] TabIcons = { "crown", "helm", "portal", "book", "burst" };
        private static readonly string[] TabKeys = { "tab.hero", "tab.gear", "tab.summon", "tab.skill", "tab.talent" };
        private static readonly string[] LaneIcons = { "heart", "atk", "def", "spd" };
        private static readonly string[] LaneKeys = { "stat.hp", "stat.atk", "stat.def", "stat.atkspd" };
        private static readonly Tone[] LaneTones = { Tone.Red, Tone.Orange, Tone.Blue, Tone.Gold };
        private static readonly string[] StatKeys = { "stat.hp", "stat.atk", "stat.def", "stat.atkspd", "stat.crit", "stat.critdmg" };
        private static readonly string[] StatIcons = { "heart", "atk", "def", "spd", "crit", "burst" };
        private static readonly string[] SettingKeys = { "settings.bgm", "settings.sfx", "settings.low_effect", "settings.fps30" };
        private const string StringsPath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const string CreditsPath = "Assets/SoloHero/Data/Strings/credits_ko.txt";

        private static readonly Color DimColor = new Color(0.06f, 0.04f, 0.12f, 0.74f);

        private static Font _font;
        private static GradeFrameSet _gradeFrames;

        [MenuItem("Tools/Setup/Build Growth UI")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UiSkin.Reload();
            BuildInScene();
        }

        public static void BuildBatch()
        {
            UiSkin.Reload();
            BuildInScene();
            BuildBootScene();
        }

        private const string BootScenePath = "Assets/SoloHero/Scenes/Boot.unity";
        private const string BuildConfigPath = "Assets/SoloHero/Data/Config/BuildConfig.asset";

        [MenuItem("Tools/Setup/Build Boot Ads")]
        public static void BuildBootMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UiSkin.Reload();
            BuildBootScene();
        }

        /// <summary>
        /// Boot scene: BuildConfig asset, AdService + MainThreadDispatcher on the Boot object, and the offline reward
        /// popup (E6-09): a dimmed full-screen "Panel" holding a window box with the title ribbon, coin and amount,
        /// the time against the cap, and claim / ad x2 buttons. The popup finds its gold text as the first non-button
        /// text under it, so "Amount" stays the first text in the box. Safe to run again.
        /// </summary>
        private static void BuildBootScene()
        {
            BuildConfig config = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildConfig>();
                AssetDatabase.CreateAsset(config, BuildConfigPath);
                AssetDatabase.SaveAssets();
            }

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            _font = LoadFont();
            foreach (CanvasScaler scaler in Object.FindObjectsOfType<CanvasScaler>(true)) MatchWidth(scaler.gameObject);
            BootSequence boot = Object.FindObjectOfType<BootSequence>();
            if (boot == null)
            {
                Debug.LogError("[UI] BootSequence not found in " + BootScenePath);
                return;
            }

            // The E1-09 build spike probe initialises Firebase and AdMob on its own; running it next to the real
            // boot makes auth fail ("CheckDependencies is running") and forces local mode. Remove it.
            BuildSpikeProbe probe = Object.FindObjectOfType<BuildSpikeProbe>(true);
            if (probe != null) Object.DestroyImmediate(probe.gameObject);

            if (boot.GetComponent<MainThreadDispatcher>() == null) boot.gameObject.AddComponent<MainThreadDispatcher>();
            AdService ads = boot.GetComponent<AdService>();
            if (ads == null) ads = boot.gameObject.AddComponent<AdService>();
            var adsSo = new SerializedObject(ads);
            adsSo.FindProperty("_build").objectReferenceValue = config;
            adsSo.ApplyModifiedPropertiesWithoutUndo();

            var bootSo = new SerializedObject(boot);
            bootSo.FindProperty("_strings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(StringsPath);
            bootSo.ApplyModifiedPropertiesWithoutUndo();

            LoadFailBanner banner = Object.FindObjectOfType<LoadFailBanner>(true);
            if (banner != null)
            {
                Text bannerText = banner.GetComponentInChildren<Text>(true);
                if (bannerText != null) bannerText.text = "";
            }

            OfflineRewardPopup popup = Object.FindObjectOfType<OfflineRewardPopup>(true);
            Transform panel = popup != null ? popup.transform.Find("Panel") : null;
            if (panel != null) BuildOfflinePopup(popup, (RectTransform)panel);
            else Debug.LogWarning("[UI] OfflineRewardPopup/Panel not found; offline popup skipped");
            BuildLoadingScreen(boot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UI] Boot scene set up in " + BootScenePath);
        }

        private const string LoadingName = "LoadingScreen";
        private const string LoadingBackdropPath = "Assets/SoloHero/Art/UI/ui_loading_bg.png";
        private const string PetSlimePath = "Assets/SoloHero/Data/Art/Pet_Slime.asset";

        /// <summary>
        /// D-108 loading screen (Boot scene, its own overlay canvas at order 60: over the battle HUD at 40, under the offline
        /// popup at 80 and the load-fail banner at 100): the meadow backdrop bottom-aligned (taller screens show more
        /// sky), the title logo, the knight and the slime idling on the grass, and a bar with a status line and a tip.
        /// </summary>
        private static void BuildLoadingScreen(BootSequence boot)
        {
            GameObject old = GameObject.Find(LoadingName);
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject(LoadingName, typeof(RectTransform));
            go.layer = 5;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            go.AddComponent<CanvasScaler>();
            MatchWidth(go);
            go.AddComponent<GraphicRaycaster>();
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            LoadingScreen screen = go.AddComponent<LoadingScreen>();
            var root = (RectTransform)go.transform;

            RectTransform backdrop = Rect("Backdrop", root, 0f, 0f, 1f, 1f);
            backdrop.pivot = new Vector2(0.5f, 0f);
            Image back = backdrop.gameObject.AddComponent<Image>();
            back.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LoadingBackdropPath);
            back.color = Color.white;
            AspectRatioFitter fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 270f / 600f;

            // The grass starts 170 / 600 of the backdrop height above the bottom: 680 units on the 1080-wide layout.
            CharacterArt heroArt = AssetDatabase.LoadAssetAtPath<CharacterArt>(HeroArtPath);
            RectTransform hero = Box("Hero", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(60f, 640f), new Vector2(96f, 96f));
            Plain(hero, heroArt != null && heroArt.idle.Length > 0 ? heroArt.idle[0] : null).preserveAspect = false;
            UiFlipbook heroBook = hero.gameObject.AddComponent<UiFlipbook>();
            var heroSo = new SerializedObject(heroBook);
            heroSo.FindProperty("_art").objectReferenceValue = heroArt;
            heroSo.FindProperty("_scale").floatValue = 6f;
            heroSo.ApplyModifiedPropertiesWithoutUndo();
            PixelOutline(hero.GetComponent<Image>(), 6f);
            CharacterArt slimeArt = AssetDatabase.LoadAssetAtPath<CharacterArt>(PetSlimePath);
            RectTransform pet = Box("Pet", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 650f), new Vector2(48f, 48f));
            Plain(pet, slimeArt != null && slimeArt.idle.Length > 0 ? slimeArt.idle[0] : null).preserveAspect = false;
            UiFlipbook petBook = pet.gameObject.AddComponent<UiFlipbook>();
            var petSo = new SerializedObject(petBook);
            petSo.FindProperty("_art").objectReferenceValue = slimeArt;
            petSo.FindProperty("_scale").floatValue = 5f;
            petSo.ApplyModifiedPropertiesWithoutUndo();
            PixelOutline(pet.GetComponent<Image>(), 5f);

            RectTransform logo = Box("Logo", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -430f), new Vector2(805f, 268f));
            Plain(logo, UiSkin.Hd("hd_logo"));
            UiPulse bob = logo.gameObject.AddComponent<UiPulse>();
            var bobSo = new SerializedObject(bob);
            bobSo.FindProperty("_scale").floatValue = 1.025f;
            bobSo.FindProperty("_speed").floatValue = 2.2f;
            bobSo.ApplyModifiedPropertiesWithoutUndo();

            Text status = AddText(Box("Status", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 290f), new Vector2(940f, 56f)), 36, TextAnchor.MiddleCenter);
            RectTransform well = Box("Bar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 228f), new Vector2(780f, 40f));
            RectTransform fill = Gauge(well, UiSkin.FillGold);
            fill.anchorMax = new Vector2(0.04f, 1f);
            Text tip = AddText(Box("Tip", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 48f)), 28, TextAnchor.MiddleCenter);
            tip.color = UiPalette.HudMuted;
            tip.horizontalOverflow = HorizontalWrapMode.Overflow;

            var so = new SerializedObject(screen);
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_fill").objectReferenceValue = fill;
            so.FindProperty("_status").objectReferenceValue = status;
            so.FindProperty("_tip").objectReferenceValue = tip;
            SetArray(so, "_tipKeys", new[] { "loading.tip.0", "loading.tip.1", "loading.tip.2", "loading.tip.3", "loading.tip.4", "loading.tip.5" });
            so.ApplyModifiedPropertiesWithoutUndo();

            var bootSo = new SerializedObject(boot);
            bootSo.FindProperty("_loading").objectReferenceValue = screen;
            bootSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildOfflinePopup(OfflineRewardPopup popup, RectTransform panel)
        {
            // The panel becomes the full-screen dim; the window is a fixed-size box in its middle.
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image dim = panel.GetComponent<Image>();
            if (dim == null) dim = panel.gameObject.AddComponent<Image>();
            dim.sprite = null;
            dim.type = Image.Type.Simple;
            dim.color = DimColor;

            Transform box = panel.Find("Box");
            if (box == null)
            {
                RectTransform made = Box("Box", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(920f, 660f));
                made.SetAsFirstSibling();
                box = made;
            }

            var boxRect = (RectTransform)box;
            Place(boxRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(920f, 660f));
            box.SetAsFirstSibling();
            Image window = box.GetComponent<Image>();
            if (window == null) window = box.gameObject.AddComponent<Image>();
            UiSkin.Sliced(window, UiSkin.Window);
            if (box.GetComponent<CanvasGroup>() == null) box.gameObject.AddComponent<CanvasGroup>();
            if (box.GetComponent<PopupIntro>() == null) box.gameObject.AddComponent<PopupIntro>();

            foreach (string child in new[] { "Amount", "Claim", "ClaimDouble", "OfflineTime", "OfflineCap", "Coin", "Ribbon", "Icon" })
            {
                Transform t = panel.Find(child);
                if (t == null) continue;
                if (child == "Amount" || child == "Claim") t.SetParent(box, false);
                else Object.DestroyImmediate(t.gameObject);
            }

            foreach (string stale in new[] { "ClaimDouble", "OfflineTime", "OfflineCap", "Coin", "Ribbon" })
            {
                Transform t = box.Find(stale);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var amount = (RectTransform)box.Find("Amount");
            Text amountText = amount != null ? amount.GetComponent<Text>() : null;
            if (amount != null)
            {
                amount.SetAsFirstSibling();
                amount.anchorMin = new Vector2(0f, 1f);
                amount.anchorMax = new Vector2(1f, 1f);
                amount.pivot = new Vector2(0.5f, 1f);
                amount.offsetMin = new Vector2(150f, -260f);
                amount.offsetMax = new Vector2(-60f, -120f);
            }

            if (amountText != null)
            {
                amountText.font = _font;
                amountText.fontSize = 84;
                amountText.alignment = TextAnchor.MiddleCenter;
                amountText.horizontalOverflow = HorizontalWrapMode.Overflow;
                amountText.verticalOverflow = VerticalWrapMode.Overflow;
                UiSkin.Ink(amountText, UiPalette.InkGold);
            }

            Image coin = FixedIcon(box, "coin", new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(84f, -190f), 96f);
            coin.name = "Coin";
            Text timeText = AddText(TopBand("OfflineTime", box, 270f, 44f, 60f, 60f), 32, TextAnchor.MiddleCenter);
            UiSkin.Ink(timeText, UiPalette.Ink);
            RectTransform cap = TopBand("OfflineCap", box, 322f, 34f, 110f, 110f);
            RectTransform capFill = Gauge(cap, UiSkin.FillGold, UiSkin.GaugeLight);

            var claim = (RectTransform)box.Find("Claim");
            Button claimButton = claim != null ? claim.GetComponent<Button>() : null;
            if (claim != null)
            {
                claim.anchorMin = new Vector2(0f, 0f);
                claim.anchorMax = new Vector2(0.5f, 0f);
                claim.pivot = new Vector2(0.5f, 0f);
                claim.offsetMin = new Vector2(52f, 54f);
                claim.offsetMax = new Vector2(-12f, 166f);
            }

            if (claimButton != null)
            {
                UiSkin.Button(claimButton, Tone.Green);
                Text claimLabel = claimButton.GetComponentInChildren<Text>(true);
                if (claimLabel != null)
                {
                    claimLabel.font = _font;
                    claimLabel.fontSize = 44;
                    UiSkin.ButtonText(claimLabel, Tone.Green);
                    Localize(claimLabel, "offline.claim");
                }
            }

            Button doubleButton = MakeButton("ClaimDouble", box, 0.5f, 0f, 1f, 0f, "", 34, out Text doubleLabel, Tone.Gold);
            var doubleRect = (RectTransform)doubleButton.transform;
            doubleRect.pivot = new Vector2(0.5f, 0f);
            doubleRect.offsetMin = new Vector2(12f, 54f);
            doubleRect.offsetMax = new Vector2(-52f, 166f);
            IconButton(doubleButton, doubleLabel, "tv", 48f);
            Ribbon(box, "offline.title", 520f);

            UnityEventTools.AddPersistentListener(doubleButton.onClick, popup.ClaimDoubled);
            var popupSo = new SerializedObject(popup);
            popupSo.FindProperty("_doubleButton").objectReferenceValue = doubleButton;
            popupSo.FindProperty("_doubleLabel").objectReferenceValue = doubleLabel;
            popupSo.FindProperty("_timeText").objectReferenceValue = timeText;
            popupSo.FindProperty("_capFill").objectReferenceValue = capFill;
            popupSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildInScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hud = GameObject.Find("BattleHud");
            if (hud == null)
            {
                Debug.LogError("[UI] BattleHud canvas not found in " + ScenePath);
                return;
            }

            _font = LoadFont();
            _gradeFrames = BuildGradeFrames();
            MatchWidth(hud);
            UnwrapSafeArea(hud.transform);
            Transform old = hud.transform.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            foreach (string name in new[] { DamageLayerName, RevealName, SettingsButtonName, SettingsPopupName, SettingsWindowName, QuitPopupName, StageSelectName, DailyName, DungeonName, CompanionName, JobName, BossIntroName, FlashName })
            {
                Transform stale = hud.transform.Find(name);
                if (stale != null) Object.DestroyImmediate(stale.gameObject);
            }

            CombatSession session = Object.FindObjectOfType<CombatSession>();
            RectTransform root = Rect(RootName, hud.transform, 0f, 0f, 1f, 1f);
            root.SetAsFirstSibling();
            SubCanvas(root.gameObject, true);
            MoveChallengeButton(hud.transform);

            ToastQueue toast = BuildToast(root);
            // D-108: a panel is always open; a navy band under it fills the strip its rounded bottom leaves.
            RectTransform backdrop = Rect("PanelBackdrop", root, 0f, TabTop, 1f, TabTop);
            backdrop.offsetMax = new Vector2(0f, 60f);
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.sprite = UiSkin.White;
            backdropImage.color = new Color32(0x1C, 0x1E, 0x42, 0xFF);
            backdropImage.raycastTarget = false;
            RectTransform panelArea = Rect("Panels", root, 0f, TabTop, 1f, PanelTop);

            JobPresenter jobPopup = BuildJobPopup(hud.transform, session, toast);
            GameObject character = BuildCharacter(panelArea, session, toast, jobPopup);
            GameObject equipment = BuildEquipment(panelArea, session, toast);
            GameObject gacha = BuildGacha(panelArea, session, toast);
            GameObject skill = BuildSkill(panelArea, session, toast);
            GameObject talent = BuildTalentPanel(panelArea, session, toast);
            SettingsPresenter settings = BuildSettings(hud.transform);

            BuildTabs(root, new[] { character, equipment, gacha, skill, talent });
            BattleHud hudView = Object.FindObjectOfType<BattleHud>();
            if (hudView != null)
            {
                var hudSo = new SerializedObject(hudView);
                hudSo.FindProperty("_panels").objectReferenceValue = root.GetComponent<PanelHost>();
                hudSo.ApplyModifiedPropertiesWithoutUndo();
            }
            BuildTutorial(root, session, toast, character, gacha.GetComponent<GachaPanelPresenter>());
            WireGuideQuest(hud.transform, root.GetComponent<PanelHost>(), gacha.GetComponent<GachaPanelPresenter>(), toast);
            BuildDamageText(hud.transform, session);
            GachaRevealView reveal = BuildGachaReveal(hud.transform, gacha.GetComponent<GachaPanelPresenter>());
            StageSelectPresenter stageSelect = BuildStageSelect(hud.transform, session);
            DailyPresenter daily = BuildDaily(hud.transform, session, toast);
            DungeonPresenter dungeon = BuildDungeon(hud.transform, session, toast);
            CompanionPresenter companion = BuildCompanion(hud.transform, session, toast);
            BuildRails(root, toast, settings, stageSelect, daily, dungeon, companion);
            // Built early (the character panel and the rails need them); lift them over the damage numbers like the
            // other popups. The quit confirm (BuildBackKey) stays the very last layer.
            jobPopup.transform.SetAsLastSibling();
            settings.transform.SetAsLastSibling();
            Transform creditsLayer = hud.transform.Find(SettingsPopupName);
            if (creditsLayer != null) creditsLayer.SetAsLastSibling();
            BuildBackKey(hud.transform, reveal, settings, stageSelect, root.GetComponent<PanelHost>(), daily, dungeon, companion, jobPopup);
            BuildBossIntro(hud.transform, session);
            ScreenFlash flash = BuildFlash(hud.transform);
            WrapSafeArea(hud.transform);
            // The boss banner sits right above the HUD controls and under numbers and popups.
            Transform bossIntro = hud.transform.Find(BossIntroName);
            if (bossIntro != null) bossIntro.SetSiblingIndex(1);
            // D-078 skill flash: over the battle and the HUD, under the damage numbers and popups.
            Transform damageLayer = hud.transform.Find(DamageLayerName);
            if (damageLayer != null) flash.transform.SetSiblingIndex(damageLayer.GetSiblingIndex());
            WireJuice(hud, toast, flash);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UI] Growth UI rebuilt in " + ScenePath);
        }

        /// <summary>
        /// D-108 tab bar: a navy bar; each tab is a transparent hit area with a gold plate behind the active tab, an icon
        /// that grows and lifts when active, and a label. A red dot sits at the icon's corner.
        /// </summary>
        private static void BuildTabs(RectTransform root, GameObject[] panels)
        {
            RectTransform bar = Rect("TabBar", root, 0f, 0f, 1f, TabTop);
            Image barImage = bar.gameObject.AddComponent<Image>();
            UiSkin.Sliced(barImage, UiSkin.TabBar);
            PanelHost host = root.gameObject.AddComponent<PanelHost>();
            var plates = new Image[TabKeys.Length];
            var icons = new RectTransform[TabKeys.Length];
            var labels = new Text[TabKeys.Length];
            var badges = new GameObject[TabKeys.Length];
            float width = 1f / TabKeys.Length;
            for (int i = 0; i < TabKeys.Length; i++)
            {
                RectTransform cell = Rect("Tab" + i, bar, i * width, 0f, (i + 1) * width, 1f);
                Image hit = cell.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                Button button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;

                RectTransform plate = Inset("Plate", cell, 10f, 10f, 10f, 14f);
                plates[i] = plate.gameObject.AddComponent<Image>();
                UiSkin.Sliced(plates[i], UiSkin.TabActive);
                plates[i].raycastTarget = false;
                plates[i].enabled = false;

                RectTransform icon = Box("Icon", cell, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
                Plain(icon, UiSkin.Icon(TabIcons[i]));
                icon.gameObject.AddComponent<UiPunch>();
                icons[i] = icon;

                labels[i] = AddText(BottomBand("Label", cell, 12f, 40f, 0f, 0f), 30, TextAnchor.MiddleCenter);
                labels[i].verticalOverflow = VerticalWrapMode.Overflow;
                Localize(labels[i], TabKeys[i]);
                UnityEventTools.AddIntPersistentListener(button.onClick, host.Toggle, i);
                badges[i] = Badge(cell, new Vector2(0.5f, 0.62f), new Vector2(40f, 36f));
            }

            // D-103: red dots on the character tab (job advancement ready) and the talent tab (points to spend).
            TabBadges tabBadges = bar.gameObject.AddComponent<TabBadges>();
            var badgeSo = new SerializedObject(tabBadges);
            badgeSo.FindProperty("_heroBadge").objectReferenceValue = badges[0];
            badgeSo.FindProperty("_talentBadge").objectReferenceValue = badges[TabKeys.Length - 1];
            badgeSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(host);
            SetArray(so, "_panels", panels);
            SetArray(so, "_tabBackgrounds", plates);
            SetArray(so, "_tabIcons", icons);
            SetArray(so, "_tabLabels", labels);
            so.FindProperty("_tabIdleSprite").objectReferenceValue = null;
            so.FindProperty("_tabActiveSprite").objectReferenceValue = UiSkin.TabActive;
            so.FindProperty("_alwaysOpen").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-106 / D-108 character panel: a blue name plate (job and level) with the job button, six stats in a cream well,
        /// then the four upgrades as a 2x2 grid of cards (coloured icon tile, name, level badge, current -> next value,
        /// green cost button that repeats while held).
        /// </summary>
        private static GameObject BuildCharacter(RectTransform area, CombatSession session, ToastQueue toast, JobPresenter jobPopup)
        {
            RectTransform panel = Panel("CharacterPanel", area);
            var presenter = panel.gameObject.AddComponent<CharacterPanelPresenter>();

            RectTransform header = TopBand("Header", panel, 22f, 72f, PanelPad, PanelPad);
            RectTransform plate = Inset("Plate", header, 0f, 0f, 300f, 0f);
            Image plateImage = plate.gameObject.AddComponent<Image>();
            UiSkin.Sliced(plateImage, UiSkin.PlateSprite(Tone.Blue));
            plateImage.raycastTarget = false;
            FixedIcon(plate, "crown", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 2f), 48f);
            Text heroName = AddText(Inset("Name", plate, 76f, 4f, 180f, 0f), 38, TextAnchor.MiddleLeft);
            heroName.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiSkin.ButtonText(heroName, Tone.Blue);
            Text heroLevel = AddText(Inset("Level", plate, 0f, 4f, 22f, 0f), 34, TextAnchor.MiddleRight);
            UiSkin.ButtonText(heroLevel, Tone.Blue);
            heroLevel.color = UiPalette.HudGold;
            UiPunch levelPunch = plate.gameObject.AddComponent<UiPunch>();
            Button jobButton = MakeButton("Job", header, 1f, 0f, 1f, 1f, "", 32, out Text jobLabel, Tone.Orange);
            RectTransform jobRect = (RectTransform)jobButton.transform;
            jobRect.offsetMin = new Vector2(-286f, -2f);
            jobRect.offsetMax = new Vector2(0f, 4f);
            TapGuardButton jobGuard = jobButton.gameObject.AddComponent<TapGuardButton>();
            UnityEventTools.AddPersistentListener(jobGuard.OnTap, presenter.OpenJobs);

            // Six stats, three columns by two rows, in a cream well.
            RectTransform statArea = TopBand("Stats", panel, 106f, 104f, PanelPad, PanelPad);
            UiSkin.Sliced(statArea.gameObject.AddComponent<Image>(), UiSkin.Inset);
            statArea.GetComponent<Image>().raycastTarget = false;
            var stats = new Text[StatKeys.Length];
            for (int i = 0; i < StatKeys.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                RectTransform chip = Rect("Stat" + i, statArea, col / 3f, 1f - (row + 1) * 0.5f, (col + 1) / 3f, 1f - row * 0.5f);
                chip.offsetMin = new Vector2(col == 0 ? 14f : 8f, row == 1 ? 6f : 0f);
                chip.offsetMax = new Vector2(col == 2 ? -16f : -8f, row == 0 ? -6f : 0f);
                FixedIcon(chip, StatIcons[i], new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, 40f);
                Text label = InkText(Inset("Label", chip, 48f, 0f, 90f, 0f), 26, TextAnchor.MiddleLeft, UiPalette.InkMuted);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                Localize(label, StatKeys[i]);
                stats[i] = InkText(Inset("Value", chip, 120f, 0f, 0f, 0f), 30, TextAnchor.MiddleRight, UiPalette.InkTitle);
                stats[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            // Upgrades: 2x2 cards.
            RectTransform grid = Inset("Upgrades", panel, PanelPad, 22f, PanelPad, 222f);
            var levels = new Text[4];
            var values = new Text[4];
            var costs = new Text[4];
            var buttons = new TapGuardButton[4];
            var punches = new UiPunch[4];
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;
                RectTransform card = Rect("Lane" + i, grid, col * 0.5f, 1f - (row + 1) * 0.5f, (col + 1) * 0.5f, 1f - row * 0.5f);
                card.offsetMin = new Vector2(col == 0 ? 0f : 7f, row == 1 ? 0f : 7f);
                card.offsetMax = new Vector2(col == 0 ? -7f : 0f, row == 0 ? 0f : -7f);
                UiSkin.Sliced(card.gameObject.AddComponent<Image>(), UiSkin.Card);
                punches[i] = card.gameObject.AddComponent<UiPunch>();

                RectTransform tile = Box("IconTile", card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(68f, 68f));
                UiSkin.Sliced(Plain(tile, null), UiSkin.PlateSprite(LaneTones[i]));
                FixedIcon(tile, LaneIcons[i], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), 48f);
                Text name = InkText(TopBand("Name", card, 12f, 44f, 94f, 120f), 34, TextAnchor.MiddleLeft, UiPalette.InkTitle);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                Localize(name, LaneKeys[i]);
                RectTransform badge = Box("Level", card, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(112f, 40f));
                UiSkin.Sliced(Plain(badge, null), UiSkin.LevelBadge);
                levels[i] = AddText(Inset("Label", badge, 4f, 2f, 4f, 0f), 26, TextAnchor.MiddleCenter);
                UiSkin.ButtonText(levels[i], Tone.Blue);

                // Current -> next value between the title row and the button (char.preview colours the next value).
                values[i] = InkText(Inset("Value", card, 94f, 96f, 14f, 56f), 34, TextAnchor.MiddleLeft, UiPalette.Ink);
                values[i].supportRichText = true;
                values[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                values[i].verticalOverflow = VerticalWrapMode.Overflow;

                Button buy = MakeButton("Buy", card, 0f, 0f, 1f, 0f, "", 38, out costs[i], Tone.Green);
                RectTransform buyRect = (RectTransform)buy.transform;
                buyRect.pivot = new Vector2(0.5f, 0f);
                buyRect.offsetMin = new Vector2(12f, 14f);
                buyRect.offsetMax = new Vector2(-12f, 92f);
                IconButton(buy, costs[i], "coin", 40f);
                buttons[i] = buy.gameObject.AddComponent<TapGuardButton>();
                buy.gameObject.AddComponent<HoldRepeat>();
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Upgrade, i);
            }

            var so = new SerializedObject(presenter);
            SetArray(so, "_punches", punches);
            SetArray(so, "_levelTexts", levels);
            SetArray(so, "_valueTexts", values);
            SetArray(so, "_costTexts", costs);
            SetArray(so, "_buttons", buttons);
            SetArray(so, "_statTexts", stats);
            so.FindProperty("_heroLevelText").objectReferenceValue = heroLevel;
            so.FindProperty("_heroNameText").objectReferenceValue = heroName;
            so.FindProperty("_jobButton").objectReferenceValue = jobGuard;
            so.FindProperty("_jobLabel").objectReferenceValue = jobLabel;
            so.FindProperty("_jobPopup").objectReferenceValue = jobPopup;
            so.FindProperty("_levelPunch").objectReferenceValue = levelPunch;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        /// <summary>
        /// D-078 skill book: 6 loadout slots + auto-equip, the owned bonus line, a 5-column collection in a cream well
        /// (the rest scrolls), and a detail card: icon, grade chip, name, info and description, with level-up and
        /// equip stacked on the right.
        /// </summary>
        private static GameObject BuildSkill(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("SkillPanel", area);
            var presenter = panel.gameObject.AddComponent<SkillPanelPresenter>();
            var icons = AssetDatabase.LoadAssetAtPath<SkillIconSet>(SkillIconsPath);
            int slotCount = new SoloHero.Core.Config.BalanceValues().SKILL_SLOT_COUNT;

            RectTransform strip = TopBand("Slots", panel, 20f, SkillSlot + 8f, PanelPad, PanelPad);
            var slotCells = new SkillCell[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                RectTransform slot = Box("Slot" + i, strip, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * SkillSlotStep, 0f), new Vector2(SkillSlot, SkillSlot));
                slotCells[i] = BuildSkillCell(slot, true, 80f);
                Button tap = slot.gameObject.AddComponent<Button>();
                tap.transition = Selectable.Transition.None;
                UnityEventTools.AddIntPersistentListener(tap.onClick, presenter.TapSlot, i);
            }

            Button auto = MakeButton("AutoEquip", strip, 0f, 0f, 1f, 1f, "", 32, out Text autoLabel, Tone.Purple);
            var autoRect = (RectTransform)auto.transform;
            autoRect.offsetMin = new Vector2(slotCount * SkillSlotStep + 4f, 10f);
            autoRect.offsetMax = new Vector2(0f, -8f);
            IconButton(auto, autoLabel, "star", 40f);
            Localize(autoLabel, "skill.auto_equip");
            UnityEventTools.AddPersistentListener(auto.onClick, presenter.AutoEquip);

            RectTransform ownedRow = TopBand("Owned", panel, 20f + SkillSlot + 14f, 40f, PanelPad + 4f, PanelPad + 4f);
            FixedIcon(ownedRow, "book", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, 40f);
            Text bonus = InkText(Inset("Bonus", ownedRow, 50f, 0f, 260f, 0f), 28, TextAnchor.MiddleLeft, UiPalette.InkGold);
            bonus.horizontalOverflow = HorizontalWrapMode.Overflow;
            bonus.verticalOverflow = VerticalWrapMode.Overflow;
            Text ownedCount = InkText(Inset("Count", ownedRow, 600f, 0f, 0f, 0f), 28, TextAnchor.MiddleRight, UiPalette.InkMuted);
            ownedCount.verticalOverflow = VerticalWrapMode.Overflow;

            float gridTop = 20f + SkillSlot + 14f + 40f + 10f;
            RectTransform well = Inset("GridWell", panel, PanelPad, 20f + SkillDetailHeight + 10f, PanelPad, gridTop);
            UiSkin.Sliced(well.gameObject.AddComponent<Image>(), UiSkin.Inset);
            well.GetComponent<Image>().raycastTarget = false;
            RectTransform viewport = Inset("Grid", well, 6f, 6f, 6f, 6f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            RectTransform content = Rect("Content", viewport, 0f, 1f, 1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(SkillGridCell, SkillGridCell);
            grid.spacing = new Vector2(48f, 14f);
            grid.padding = new RectOffset(10, 10, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = SkillGridColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            int count = SoloHero.Core.Skills.SkillCatalog.Count;
            var cells = new SkillCell[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform cell = Rect("Cell" + i, content, 0f, 0f, 1f, 1f);
                cells[i] = BuildSkillCell(cell, false, SkillGridIcon);
                Button tap = cell.gameObject.AddComponent<Button>();
                tap.transition = Selectable.Transition.None;
                UnityEventTools.AddIntPersistentListener(tap.onClick, presenter.SelectCell, i);
            }

            RectTransform selection = Rect("Selection", content, 0f, 0f, 1f, 1f);
            selection.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiSkin.Sliced(selection.gameObject.AddComponent<Image>(), UiSkin.Selection);
            selection.GetComponent<Image>().raycastTarget = false;
            selection.gameObject.AddComponent<UiPulse>();
            SubCanvas(selection.gameObject, false);

            RectTransform detail = BottomBand("Detail", panel, 20f, SkillDetailHeight, PanelPad, PanelPad);
            UiSkin.Sliced(detail.gameObject.AddComponent<Image>(), UiSkin.Card);
            UiPunch punch = detail.gameObject.AddComponent<UiPunch>();
            RectTransform detailFrameRect = Box("IconFrame", detail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 2f), new Vector2(120f, 120f));
            Image detailFrame = Plain(detailFrameRect, UiSkin.SlotEmpty);
            detailFrame.type = Image.Type.Sliced;
            Image detailIcon = Plain(Box("Icon", detailFrameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(98f, 98f)), null);
            RectTransform chip = Box("Grade", detail, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -14f), new Vector2(92f, 40f));
            Image gradeChip = Plain(chip, UiSkin.PlateSprite(Tone.Gray));
            gradeChip.type = Image.Type.Sliced;
            Text gradeText = AddText(Inset("Label", chip, 0f, 2f, 0f, 0f), 24, TextAnchor.MiddleCenter);
            Text name = InkText(TopBand("Name", detail, 12f, 44f, 252f, 300f), 36, TextAnchor.MiddleLeft, UiPalette.InkTitle);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Overflow;
            Text info = InkText(TopBand("Info", detail, 58f, 30f, 150f, 300f), 24, TextAnchor.MiddleLeft, UiPalette.InkMuted);
            info.horizontalOverflow = HorizontalWrapMode.Overflow;
            info.verticalOverflow = VerticalWrapMode.Overflow;
            info.supportRichText = true;
            Text desc = InkText(Inset("Desc", detail, 150f, 14f, 300f, 92f), 25, TextAnchor.UpperLeft, UiPalette.Ink);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            desc.lineSpacing = 0.95f;

            RectTransform actions = Rect("Actions", detail, 1f, 0f, 1f, 1f);
            actions.offsetMin = new Vector2(-288f, 14f);
            actions.offsetMax = new Vector2(-14f, -14f);
            Button level = MakeButton("LevelUp", actions, 0f, 0.5f, 1f, 1f, "", 32, out Text levelCost, Tone.Gold);
            var levelRect = (RectTransform)level.transform;
            levelRect.offsetMin = new Vector2(0f, 4f);
            IconButton(level, levelCost, "coin", 40f);
            TapGuardButton levelGuard = level.gameObject.AddComponent<TapGuardButton>();
            level.gameObject.AddComponent<HoldRepeat>();
            UnityEventTools.AddPersistentListener(levelGuard.OnTap, presenter.LevelUp);
            Button equip = MakeButton("Equip", actions, 0f, 0f, 1f, 0.5f, "", 32, out Text equipLabel, Tone.Blue);
            var equipRect = (RectTransform)equip.transform;
            equipRect.offsetMax = new Vector2(0f, -4f);
            TapGuardButton equipGuard = equip.gameObject.AddComponent<TapGuardButton>();
            UnityEventTools.AddPersistentListener(equipGuard.OnTap, presenter.ToggleEquip);

            var so = new SerializedObject(presenter);
            SetArray(so, "_slotFrames", System.Array.ConvertAll(slotCells, c => c.Frame));
            SetArray(so, "_slotIcons", System.Array.ConvertAll(slotCells, c => c.Icon));
            SetArray(so, "_slotLevels", System.Array.ConvertAll(slotCells, c => c.Level));
            SetArray(so, "_slotBadges", System.Array.ConvertAll(slotCells, c => c.Badge));
            SetArray(so, "_slotLocks", System.Array.ConvertAll(slotCells, c => c.Lock));
            SetArray(so, "_slotLockTexts", System.Array.ConvertAll(slotCells, c => c.LockText));
            SetArray(so, "_cellFrames", System.Array.ConvertAll(cells, c => c.Frame));
            SetArray(so, "_cellIcons", System.Array.ConvertAll(cells, c => c.Icon));
            SetArray(so, "_cellLevels", System.Array.ConvertAll(cells, c => c.Level));
            SetArray(so, "_cellBadges", System.Array.ConvertAll(cells, c => c.Badge));
            SetArray(so, "_cellMarks", System.Array.ConvertAll(cells, c => c.Tag));
            SetArray(so, "_cellMarkTexts", System.Array.ConvertAll(cells, c => c.TagText));
            SetArray(so, "_cellLocks", System.Array.ConvertAll(cells, c => c.Lock));
            so.FindProperty("_selection").objectReferenceValue = selection;
            so.FindProperty("_detailIcon").objectReferenceValue = detailIcon;
            so.FindProperty("_detailFrame").objectReferenceValue = detailFrame;
            so.FindProperty("_detailGradeChip").objectReferenceValue = gradeChip;
            so.FindProperty("_detailGrade").objectReferenceValue = gradeText;
            so.FindProperty("_detailName").objectReferenceValue = name;
            so.FindProperty("_detailInfo").objectReferenceValue = info;
            so.FindProperty("_detailDesc").objectReferenceValue = desc;
            so.FindProperty("_ownedBonus").objectReferenceValue = bonus;
            so.FindProperty("_ownedCount").objectReferenceValue = ownedCount;
            so.FindProperty("_levelButton").objectReferenceValue = levelGuard;
            so.FindProperty("_levelCost").objectReferenceValue = levelCost;
            so.FindProperty("_equipButton").objectReferenceValue = equipGuard;
            so.FindProperty("_equipLabel").objectReferenceValue = equipLabel;
            so.FindProperty("_detailPunch").objectReferenceValue = punch;
            so.FindProperty("_icons").objectReferenceValue = icons;
            so.FindProperty("_frames").objectReferenceValue = _gradeFrames;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        /// <summary>D-087 / D-089: the talent tree as its own bottom tab.</summary>
        private static GameObject BuildTalentPanel(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("TalentPanel", area);
            BuildTalentPage(panel, session, toast);
            return panel.gameObject;
        }

        /// <summary>
        /// Talent page: free points and a gem reset on top, three branch columns in cream wells (a spine behind four
        /// tiers of nodes; two nodes per tier, one for the lower tiers), and a detail card with the learn button.
        /// </summary>
        private static void BuildTalentPage(RectTransform page, CombatSession session, ToastQueue toast)
        {
            var presenter = page.gameObject.AddComponent<TalentPanelPresenter>();

            RectTransform header = TopBand("Header", page, 18f, 60f, PanelPad, PanelPad);
            FixedIcon(header, "star", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), 48f);
            Text points = InkText(Inset("Points", header, 60f, 0f, 300f, 0f), 36, TextAnchor.MiddleLeft, UiPalette.InkTitle);
            points.horizontalOverflow = HorizontalWrapMode.Overflow;
            Button reset = MakeButton("Reset", header, 1f, 0f, 1f, 1f, "", 30, out Text resetLabel, Tone.Purple);
            Place((RectTransform)reset.transform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(290f, 64f));
            IconButton(reset, resetLabel, "gem", 40f);
            resetLabel.verticalOverflow = VerticalWrapMode.Overflow;
            TapGuardButton resetGuard = reset.gameObject.AddComponent<TapGuardButton>();
            UnityEventTools.AddPersistentListener(resetGuard.OnTap, presenter.ResetTree);

            float treeTop = 18f + 60f + 8f;
            float treeBottom = 18f + TalentDetailHeight + 8f;
            RectTransform tree = Inset("Tree", page, PanelPad, treeBottom, PanelPad, treeTop);
            var frames = new Image[TalentCatalog.All.Length];
            var icons = new Image[TalentCatalog.All.Length];
            var ranks = new Text[TalentCatalog.All.Length];
            var sprites = new Sprite[TalentCatalog.All.Length];
            var branchTexts = new Text[TalentCatalog.BranchCount];
            for (int b = 0; b < TalentCatalog.BranchCount; b++)
            {
                RectTransform column = Rect("Branch" + b, tree, b / 3f, 0f, (b + 1) / 3f, 1f);
                column.offsetMin = new Vector2(b == 0 ? 0f : 6f, 0f);
                column.offsetMax = new Vector2(b == 2 ? 0f : -6f, 0f);
                UiSkin.Sliced(column.gameObject.AddComponent<Image>(), UiSkin.Inset);
                column.GetComponent<Image>().raycastTarget = false;
                branchTexts[b] = InkText(TopBand("Title", column, 4f, 34f, 4f, 4f), 28, TextAnchor.MiddleCenter, UiPalette.InkTitle);

                RectTransform tiers = Inset("Tiers", column, 0f, 10f, 0f, 38f);
                RectTransform spine = Rect("Spine", tiers, 0.5f, 0.1f, 0.5f, 0.9f);
                spine.offsetMin = new Vector2(-4f, 0f);
                spine.offsetMax = new Vector2(4f, 0f);
                Image spineImage = spine.gameObject.AddComponent<Image>();
                spineImage.sprite = UiSkin.White;
                spineImage.color = new Color32(0xD4, 0xBC, 0x92, 0xFF);
                spineImage.raycastTarget = false;

                for (int i = 0; i < TalentCatalog.All.Length; i++)
                {
                    TalentDef def = TalentCatalog.All[i];
                    if ((int)def.Branch != b) continue;
                    bool pair = HasPair(def);
                    float x = pair ? (def.Column == 0 ? 0.27f : 0.73f) : 0.5f;
                    float y = 1f - (def.Tier + 0.5f) / TalentCatalog.TierCount;
                    RectTransform cell = Box("Node" + i, tiers, new Vector2(x, y), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(TalentCell, TalentCell));
                    frames[i] = cell.gameObject.AddComponent<Image>();
                    UiSkin.Sliced(frames[i], UiSkin.GradeFrame(SoloHero.Core.Gacha.Grade.Common));
                    // D-106: 16 px talent icons at 3 units per pixel; D-108: the rank on a chip over the node's bottom edge.
                    icons[i] = Plain(Box("Icon", cell, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(44f, 44f)), null);
                    RectTransform rankChip = Box("RankChip", cell, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), new Vector2(60f, 28f));
                    UiSkin.Sliced(Plain(rankChip, null), UiSkin.LevelBadge);
                    ranks[i] = AddText(Inset("Rank", rankChip, 0f, 2f, 0f, 0f), 22, TextAnchor.MiddleCenter);
                    ranks[i].verticalOverflow = VerticalWrapMode.Overflow;
                    ranks[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                    UiSkin.ButtonText(ranks[i], Tone.Blue);
                    sprites[i] = UiSkin.Icon(TalentIcon(def.Id));
                    Button tap = cell.gameObject.AddComponent<Button>();
                    tap.transition = Selectable.Transition.None;
                    UnityEventTools.AddIntPersistentListener(tap.onClick, presenter.SelectCell, i);
                }
            }

            RectTransform selection = Rect("Selection", tree, 0f, 0f, 1f, 1f);
            UiSkin.Sliced(selection.gameObject.AddComponent<Image>(), UiSkin.Selection);
            selection.GetComponent<Image>().raycastTarget = false;
            selection.gameObject.AddComponent<UiPulse>();
            SubCanvas(selection.gameObject, false);

            RectTransform detail = BottomBand("Detail", page, 18f, TalentDetailHeight, PanelPad, PanelPad);
            UiSkin.Sliced(detail.gameObject.AddComponent<Image>(), UiSkin.Card);
            UiPunch punch = detail.gameObject.AddComponent<UiPunch>();
            RectTransform frameRect = Box("IconFrame", detail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 2f), new Vector2(100f, 100f));
            Image detailFrame = Plain(frameRect, UiSkin.GradeFrame(SoloHero.Core.Gacha.Grade.Common));
            detailFrame.type = Image.Type.Sliced;
            Image detailIcon = Plain(Box("Icon", frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f)), null);
            Text name = InkText(TopBand("Name", detail, 10f, 44f, 134f, 290f), 36, TextAnchor.MiddleLeft, UiPalette.InkTitle);
            name.verticalOverflow = VerticalWrapMode.Overflow;
            Text rank = InkText(TopBand("Rank", detail, 10f, 44f, 134f, 290f), 28, TextAnchor.MiddleRight, UiPalette.InkGold);
            Text desc = InkText(Inset("Desc", detail, 134f, 8f, 290f, 56f), 24, TextAnchor.UpperLeft, UiPalette.Ink);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            Button learn = MakeButton("Learn", detail, 1f, 0.5f, 1f, 0.5f, "", 36, out Text learnLabel, Tone.Green);
            Place((RectTransform)learn.transform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(252f, 92f));
            TapGuardButton learnGuard = learn.gameObject.AddComponent<TapGuardButton>();
            learn.gameObject.AddComponent<HoldRepeat>();
            UnityEventTools.AddPersistentListener(learnGuard.OnTap, presenter.Learn);

            var so = new SerializedObject(presenter);
            SetArray(so, "_cellFrames", frames);
            SetArray(so, "_cellIcons", icons);
            SetArray(so, "_cellRanks", ranks);
            SetArray(so, "_icons", sprites);
            SetArray(so, "_branchTexts", branchTexts);
            so.FindProperty("_selection").objectReferenceValue = selection;
            so.FindProperty("_pointsText").objectReferenceValue = points;
            so.FindProperty("_resetButton").objectReferenceValue = resetGuard;
            so.FindProperty("_resetLabel").objectReferenceValue = resetLabel;
            so.FindProperty("_detailIcon").objectReferenceValue = detailIcon;
            so.FindProperty("_detailFrame").objectReferenceValue = detailFrame;
            so.FindProperty("_detailName").objectReferenceValue = name;
            so.FindProperty("_detailRank").objectReferenceValue = rank;
            so.FindProperty("_detailDesc").objectReferenceValue = desc;
            so.FindProperty("_learnButton").objectReferenceValue = learnGuard;
            so.FindProperty("_learnLabel").objectReferenceValue = learnLabel;
            so.FindProperty("_detailPunch").objectReferenceValue = punch;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>True when another node shares this node's branch and tier (the pair sits side by side).</summary>
        private static bool HasPair(TalentDef def)
        {
            foreach (TalentDef other in TalentCatalog.All)
            {
                if (other != def && other.Branch == def.Branch && other.Tier == def.Tier) return true;
            }

            return false;
        }

        private static string TalentIcon(string id)
        {
            switch (id)
            {
                case "sharpness": return "atk";
                case "precision": return "crit";
                case "ferocity": return "burst";
                case "swiftness": return "spd";
                case "giant_slayer": return "skull";
                case "execute": return "strike";
                case "vitality": return "heart";
                case "iron_hide": return "def";
                case "endurance": return "helm";
                case "mending": return "cry";
                case "fortitude": return "heart";
                case "last_stand": return "crown";
                case "focus": return "book";
                case "haste": return "clock";
                case "persistence": return "star";
                case "venom": return "whirl";
                case "mastery": return "burst";
                default: return "gem";
            }
        }

        /// <summary>Parts of one skill square, for the presenter arrays.</summary>
        private struct SkillCell
        {
            public Image Frame;
            public Image Icon;
            public GameObject Badge;
            public Text Level;
            public GameObject Tag;
            public Text TagText;
            public GameObject Lock;
            public Text LockText;
        }

        /// <summary>
        /// Skill square on <paramref name="rect"/>: grade frame, icon, level badge (bottom right), slot tag (top left).
        /// Slots get a centred lock with the opening level; catalog cells a small lock in the corner.
        /// </summary>
        private static SkillCell BuildSkillCell(RectTransform rect, bool slotLock, float iconSize)
        {
            var cell = new SkillCell();
            cell.Frame = rect.gameObject.AddComponent<Image>();
            UiSkin.Sliced(cell.Frame, UiSkin.SlotEmpty);
            cell.Icon = Plain(Box("Icon", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize)), null);

            RectTransform badge = Box("Badge", rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(10f, -8f), new Vector2(68f, 34f));
            UiSkin.Sliced(Plain(badge, null), UiSkin.LevelBadge);
            cell.Level = AddText(Inset("Label", badge, 2f, 2f, 2f, 0f), 22, TextAnchor.MiddleCenter);
            UiSkin.ButtonText(cell.Level, Tone.Blue);
            cell.Badge = badge.gameObject;

            RectTransform tag = Box("Tag", rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-8f, 8f), new Vector2(36f, 36f));
            UiSkin.Sliced(Plain(tag, null), UiSkin.PlateSprite(Tone.Green));
            cell.TagText = AddText(Inset("Label", tag, 0f, 2f, 0f, 0f), 24, TextAnchor.MiddleCenter);
            UiSkin.ButtonText(cell.TagText, Tone.Green);
            cell.Tag = tag.gameObject;
            cell.Tag.SetActive(false);

            if (slotLock)
            {
                RectTransform lockRoot = Inset("Lock", rect, 0f, 0f, 0f, 0f);
                FixedIcon(lockRoot, "lock", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), 40f);
                cell.LockText = InkText(BottomBand("Label", lockRoot, 8f, 28f, 0f, 0f), 24, TextAnchor.MiddleCenter, UiPalette.InkMuted);
                cell.Lock = lockRoot.gameObject;
            }
            else
            {
                cell.Lock = FixedIcon(rect, "lock", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f), 32f).gameObject;
            }

            return cell;
        }

        /// <summary>
        /// D-106 / D-108 equipment panel: the hero stands on a stone stage in a cream well with the sword and helm slots on the
        /// left and the armor and boots slots on the right. A slot shows its grade frame, icon and grade +level, and the
        /// item's effect under it; tapping it swaps to the next owned grade. Under the stage: ATK / HP / DEF and
        /// "equip best".
        /// </summary>
        private static GameObject BuildEquipment(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("EquipmentPanel", area);
            var presenter = panel.gameObject.AddComponent<EquipmentPanelPresenter>();

            RectTransform stage = Inset("Stage", panel, PanelPad, 22f + 88f + 12f, PanelPad, 22f);
            UiSkin.Sliced(stage.gameObject.AddComponent<Image>(), UiSkin.Inset);
            stage.GetComponent<Image>().raycastTarget = true;
            stage.gameObject.AddComponent<RectMask2D>();
            Image glow = Plain(Box("Glow", stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(460f, 460f)), UiSkin.Hd("hd_glow"));
            glow.color = new Color(1f, 0.93f, 0.7f, 0.9f);
            glow.gameObject.AddComponent<UiPulse>();
            SubCanvas(glow.gameObject, false);
            Plain(Box("Pedestal", stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(320f, 96f)), UiSkin.Hd("hd_pedestal"));
            CharacterArt heroArt = AssetDatabase.LoadAssetAtPath<CharacterArt>(HeroArtPath);
            RectTransform heroRect = Box("Hero", stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(96f, 96f));
            Plain(heroRect, heroArt != null && heroArt.idle.Length > 0 ? heroArt.idle[0] : null).preserveAspect = false;
            UiFlipbook flipbook = heroRect.gameObject.AddComponent<UiFlipbook>();
            var flipSo = new SerializedObject(flipbook);
            flipSo.FindProperty("_art").objectReferenceValue = heroArt;
            SetArray(flipSo, "_jobs", JobArts());
            flipSo.FindProperty("_scale").floatValue = 5f;
            flipSo.ApplyModifiedPropertiesWithoutUndo();
            PixelOutline(heroRect.GetComponent<Image>(), 5f);
            Button poke = stage.gameObject.AddComponent<Button>();
            poke.transition = Selectable.Transition.None;
            poke.targetGraphic = stage.GetComponent<Image>();
            UnityEventTools.AddPersistentListener(poke.onClick, flipbook.Poke);

            // D-109: eight slots, 2 x 2 on each side of the hero: gear (sword, helm / armor, boots) on the left,
            // accessories (gloves, necklace / ring, earring) on the right; the owned bonus sits over the hero.
            int slotCount = SoloHero.Core.Gacha.GachaCatalog.SlotCount;
            var slots = new Text[slotCount];
            var owned = new Text[slotCount];
            var buttons = new TapGuardButton[slotCount];
            var icons = new Image[slotCount];
            var frames = new Image[slotCount];
            const float slotSize = 136f;
            const float colPitch = 160f;
            const float rowPitch = 196f;
            const float edge = 18f;
            for (int i = 0; i < slotCount; i++)
            {
                bool left = i < 4;
                int k = i % 4;
                int row = k / 2;
                int col = k % 2;
                // Reading order on each side: the left side's first column is the outer one, the right side's the inner one.
                float x = left ? edge + col * colPitch : -(edge + (1 - col) * colPitch);
                Vector2 anchor = new Vector2(left ? 0f : 1f, 1f);
                Vector2 pos = new Vector2(x, -20f - row * rowPitch);
                RectTransform frameRect = Box("Slot" + i, stage, anchor, new Vector2(left ? 0f : 1f, 1f), pos, new Vector2(slotSize, slotSize));
                frames[i] = Plain(frameRect, UiSkin.SlotEmpty);
                frames[i].type = Image.Type.Sliced;
                frames[i].raycastTarget = true;
                icons[i] = Plain(Box("Icon", frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(88f, 88f)), null);
                slots[i] = InkText(Box("Grade", frameRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(slotSize, 32f)), 24, TextAnchor.MiddleCenter, UiPalette.InkMuted);
                slots[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                owned[i] = InkText(Box("Effect", frameRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(colPitch, 36f)), 22, TextAnchor.UpperCenter, UiPalette.Ink);
                owned[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                owned[i].verticalOverflow = VerticalWrapMode.Overflow;
                Button button = frameRect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = frames[i];
                frameRect.gameObject.AddComponent<PressScale>();
                buttons[i] = frameRect.gameObject.AddComponent<TapGuardButton>();
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Swap, i);
            }

            RectTransform ownedPill = Box("OwnedBonus", stage, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(360f, 52f));
            UiSkin.Sliced(ownedPill.gameObject.AddComponent<Image>(), UiSkin.Pill);
            ownedPill.GetComponent<Image>().raycastTarget = false;
            Text ownedBonus = AddText(Inset("Text", ownedPill, 12f, 0f, 12f, 0f), 24, TextAnchor.MiddleCenter);
            ownedBonus.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Under the stage: ATK / HP / DEF chips and the "equip best" button.
            RectTransform footer = BottomBand("Footer", panel, 22f, 88f, PanelPad, PanelPad);
            string[] iconNames = { "atk", "heart", "def" };
            var statTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform chip = Rect("Stat" + i, footer, i * 0.215f, 0f, (i + 1) * 0.215f, 1f);
                chip.offsetMin = new Vector2(i == 0 ? 0f : 6f, 6f);
                chip.offsetMax = new Vector2(-6f, -6f);
                UiSkin.Sliced(chip.gameObject.AddComponent<Image>(), UiSkin.Inset);
                chip.GetComponent<Image>().raycastTarget = false;
                FixedIcon(chip, iconNames[i], new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), 40f);
                statTexts[i] = InkText(Inset("Value", chip, 54f, 0f, 14f, 0f), 30, TextAnchor.MiddleRight, UiPalette.InkTitle);
                statTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            Button best = MakeButton("EquipBest", footer, 0.655f, 0f, 1f, 1f, "", 32, out Text bestLabel, Tone.Blue);
            Localize(bestLabel, "equip.best");
            UnityEventTools.AddPersistentListener(best.onClick, presenter.EquipBest);

            var so = new SerializedObject(presenter);
            SetArray(so, "_slotTexts", slots);
            SetArray(so, "_ownedTexts", owned);
            SetArray(so, "_swapButtons", buttons);
            SetArray(so, "_slotIcons", icons);
            SetArray(so, "_slotFrames", frames);
            SetArray(so, "_statTexts", statTexts);
            so.FindProperty("_ownedBonusText").objectReferenceValue = ownedBonus;
            so.FindProperty("_frames").objectReferenceValue = _gradeFrames;
            so.FindProperty("_icons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EquipmentIconSet>(EquipmentIconsPath);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        /// <summary>
        /// D-078 / D-108 summon panel: equipment / skill segmented tabs, the pity line and gauge, the rate disclosure, a slowly
        /// turning summon circle with the last result over it, the gem gold pack, and single / 10 / gem 10 buttons.
        /// </summary>
        private static GameObject BuildGacha(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("GachaPanel", area);
            var presenter = panel.gameObject.AddComponent<GachaPanelPresenter>();

            var tabImages = new Image[2];
            string[] modeKeys = { "gacha.mode_gear", "gacha.mode_skill" };
            string[] modeIcons = { "helm", "book" };
            RectTransform modes = TopBand("Modes", panel, 20f, 76f, PanelPad, PanelPad);
            for (int i = 0; i < 2; i++)
            {
                Button tab = MakeButton("Mode" + i, modes, i * 0.5f, 0f, (i + 1) * 0.5f, 1f, "", 34, out Text tabLabel, Tone.Gold);
                var tabRect = (RectTransform)tab.transform;
                tabRect.offsetMin = new Vector2(i == 0 ? 0f : 8f, 0f);
                tabRect.offsetMax = new Vector2(i == 0 ? -8f : 0f, 0f);
                tab.transition = Selectable.Transition.None;
                tabImages[i] = tab.GetComponent<Image>();
                UiSkin.Sliced(tabImages[i], UiSkin.PlateSprite(Tone.Gold));
                tabLabel.rectTransform.offsetMin = new Vector2(8f, 4f);
                IconButton(tab, tabLabel, modeIcons[i], 48f);
                Localize(tabLabel, modeKeys[i]);
                UnityEventTools.AddIntPersistentListener(tab.onClick, presenter.SetMode, i);
            }

            Text pity = InkText(TopBand("Pity", panel, 106f, 40f, PanelPad, PanelPad), 30, TextAnchor.MiddleCenter, UiPalette.InkTitle);
            RectTransform gauge = TopBand("PityGauge", panel, 150f, 30f, 70f, 70f);
            RectTransform fillRect = Gauge(gauge, UiSkin.FillGold, UiSkin.GaugeLight);
            Image fill = fillRect.GetComponent<Image>();

            Text rates = InkText(TopBand("Rates", panel, 186f, 64f, PanelPad, PanelPad), 24, TextAnchor.MiddleCenter, UiPalette.InkMuted);
            rates.verticalOverflow = VerticalWrapMode.Overflow;

            // D-103 / D-108: a turning summon circle fills the middle; the last result is written over it.
            RectTransform middle = Inset("Middle", panel, PanelPad, 22f + 104f + 10f + 70f, PanelPad, 254f);
            RectTransform circleRect = Rect("SummonCircle", middle, 0.5f, 0f, 0.5f, 1f);
            Image circle = Plain(circleRect, UiSkin.Hd("hd_summon_circle"));
            circle.color = new Color(0.62f, 0.45f, 1f, 0.55f);
            AspectRatioFitter fit = circleRect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fit.aspectRatio = 1f;
            circleRect.gameObject.AddComponent<UiSpin>();
            SubCanvas(circleRect.gameObject, false);
            Text result = InkText(Inset("Result", middle, 0f, 0f, 0f, 0f), 32, TextAnchor.MiddleCenter, UiPalette.InkTitle);
            result.supportRichText = true;
            result.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform pulls = BottomBand("Pulls", panel, 22f, 104f, PanelPad, PanelPad);
            TapGuardButton single = PullButton(pulls, "PullOne", 0f, 0.32f, out Text singleCost, Tone.Gold, "coin");
            TapGuardButton ten = PullButton(pulls, "PullTen", 0.34f, 0.66f, out Text tenCost, Tone.Gold, "coin");
            TapGuardButton gem = PullButton(pulls, "PullGemTen", 0.68f, 1f, out Text gemCost, Tone.Purple, "gem");
            UnityEventTools.AddPersistentListener(single.OnTap, presenter.PullSingle);
            UnityEventTools.AddPersistentListener(ten.OnTap, presenter.PullTen);
            UnityEventTools.AddPersistentListener(gem.OnTap, presenter.PullTenWithGem);
            Button pack = MakeButton("GoldPack", panel, 0.5f, 0f, 1f, 0f, "", 26, out Text packLabel, Tone.Purple);
            var packRect = (RectTransform)pack.transform;
            packRect.pivot = new Vector2(0.5f, 0f);
            packRect.offsetMin = new Vector2(0f, 22f + 104f + 10f);
            packRect.offsetMax = new Vector2(-PanelPad, 22f + 104f + 10f + 66f);
            IconButton(pack, packLabel, "gem", 40f);
            TapGuardButton packGuard = pack.gameObject.AddComponent<TapGuardButton>();
            UnityEventTools.AddPersistentListener(packGuard.OnTap, presenter.BuyGoldPack);

            var so = new SerializedObject(presenter);
            so.FindProperty("_pityText").objectReferenceValue = pity;
            so.FindProperty("_pityFill").objectReferenceValue = fill;
            so.FindProperty("_rateText").objectReferenceValue = rates;
            so.FindProperty("_resultText").objectReferenceValue = result;
            so.FindProperty("_singleCostText").objectReferenceValue = singleCost;
            so.FindProperty("_tenCostText").objectReferenceValue = tenCost;
            so.FindProperty("_gemCostText").objectReferenceValue = gemCost;
            so.FindProperty("_singleButton").objectReferenceValue = single;
            so.FindProperty("_tenButton").objectReferenceValue = ten;
            so.FindProperty("_gemButton").objectReferenceValue = gem;
            so.FindProperty("_goldPackButton").objectReferenceValue = packGuard;
            so.FindProperty("_goldPackText").objectReferenceValue = packLabel;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            SetArray(so, "_tabImages", tabImages);
            so.FindProperty("_tabIdle").objectReferenceValue = UiSkin.PlateSprite(Tone.Gray);
            so.FindProperty("_tabActive").objectReferenceValue = UiSkin.PlateSprite(Tone.Gold);
            so.FindProperty("_equipmentIcons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EquipmentIconSet>(EquipmentIconsPath);
            so.FindProperty("_skillIcons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SkillIconSet>(SkillIconsPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        /// <summary>Grade frame sprites for the presenters (Data/Art/GradeFrames.asset), rewritten on every build.</summary>
        private static GradeFrameSet BuildGradeFrames()
        {
            var set = AssetDatabase.LoadAssetAtPath<GradeFrameSet>(GradeFramesPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<GradeFrameSet>();
                AssetDatabase.CreateAsset(set, GradeFramesPath);
            }

            set.frames = new Sprite[4];
            for (int g = 0; g < set.frames.Length; g++) set.frames[g] = UiSkin.GradeFrame((SoloHero.Core.Gacha.Grade)g);
            set.empty = UiSkin.SlotEmpty;
            set.emptyDark = UiSkin.SlotDark;
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        /// <summary>D-078: full-screen colour flash for Epic / Legendary skills (raycasts off).</summary>
        private static ScreenFlash BuildFlash(Transform hud)
        {
            RectTransform rect = Rect(FlashName, hud, 0f, 0f, 1f, 1f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSkin.White;
            image.raycastTarget = false;
            image.enabled = false;
            return rect.gameObject.AddComponent<ScreenFlash>();
        }

        /// <summary>E8-09 gold punch on the HUD gold label; toast, skill flash and skill-name labels for CombatFx.</summary>
        private static void WireJuice(GameObject hud, ToastQueue toast, ScreenFlash flash)
        {
            BattleHud battleHud = Object.FindObjectOfType<BattleHud>();
            if (battleHud != null)
            {
                var so = new SerializedObject(battleHud);
                var gold = so.FindProperty("_goldText").objectReferenceValue as Text;
                if (gold != null)
                {
                    UiPunch punch = gold.GetComponent<UiPunch>();
                    if (punch == null) punch = gold.gameObject.AddComponent<UiPunch>();
                    so.FindProperty("_goldPunch").objectReferenceValue = punch;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            CombatFx fx = Object.FindObjectOfType<CombatFx>();
            if (fx != null)
            {
                var so = new SerializedObject(fx);
                so.FindProperty("_toast").objectReferenceValue = toast;
                so.FindProperty("_flash").objectReferenceValue = flash;
                so.FindProperty("_labels").objectReferenceValue = hud.GetComponentInChildren<DamageTextPool>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// E5-11 / E8-10 overlay, last sibling of the HUD so it covers everything: dim tap area, summon circle,
        /// 10 cards (the view lays them out), UI particles, skip button and close hint.
        /// </summary>
        private static GachaRevealView BuildGachaReveal(Transform hud, GachaPanelPresenter presenter)
        {
            RectTransform holder = Rect(RevealName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            GachaRevealView view = holder.gameObject.AddComponent<GachaRevealView>();

            RectTransform root = Rect("Root", holder, 0f, 0f, 1f, 1f);
            Image dim = root.gameObject.AddComponent<Image>();
            // Linear colour space: a 0.94 black still shows the battle at about a quarter brightness, so go near opaque.
            dim.color = new Color(0.03f, 0.015f, 0.08f, 0.985f);
            Button tap = root.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(tap.onClick, view.Tap);

            Image rays = Plain(Box("Glow", root, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 1100f)), UiSkin.Hd("hd_glow"));
            rays.color = new Color(0.55f, 0.35f, 1f, 0.35f);

            RectTransform circle = Rect("Circle", root, 0.5f, 0.55f, 0.5f, 0.55f);
            circle.sizeDelta = new Vector2(720f, 720f);
            Image circleImage = circle.gameObject.AddComponent<Image>();
            circleImage.sprite = UiSkin.Hd("hd_summon_circle");
            circleImage.color = new Color(0.8f, 0.68f, 1f, 1f);
            circleImage.raycastTarget = false;

            RectTransform area = Rect("Cards", root, 0.02f, 0.3f, 0.98f, 0.8f);
            Sprite back = UiSkin.Hd("hd9_cardback");
            Sprite face = UiSkin.Hd("hd9_cardface");
            var cards = new GachaCard[10];
            for (int i = 0; i < cards.Length; i++) cards[i] = BuildCard(area, i, back, face);

            RectTransform burstRect = Rect("Burst", root, 0f, 0f, 1f, 1f);
            UiBurst burst = burstRect.gameObject.AddComponent<UiBurst>();
            var particles = new Image[BurstParticles];
            Sprite dot = UiSkin.Hd("hd_glow");
            for (int i = 0; i < BurstParticles; i++)
            {
                RectTransform p = Rect("P" + i, burstRect, 0.5f, 0.5f, 0.5f, 0.5f);
                p.sizeDelta = i % 3 == 0 ? new Vector2(34f, 34f) : new Vector2(22f, 22f);
                particles[i] = p.gameObject.AddComponent<Image>();
                particles[i].sprite = dot;
                particles[i].raycastTarget = false;
            }

            var burstSo = new SerializedObject(burst);
            SetArray(burstSo, "_particles", particles);
            burstSo.ApplyModifiedPropertiesWithoutUndo();

            Button skip = MakeButton("Skip", root, 0.28f, 0.17f, 0.72f, 0.24f, "", 34, out Text skipLabel, Tone.Gray);
            Localize(skipLabel, "gacha.skip");
            UnityEventTools.AddPersistentListener(skip.onClick, view.SkipAll);
            Text hint = MakeText("CloseHint", root, 0.1f, 0.18f, 0.9f, 0.24f, "", 36, TextAnchor.MiddleCenter);
            hint.gameObject.AddComponent<UiPulse>();
            Localize(hint, "gacha.tap_close");

            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root.gameObject;
            so.FindProperty("_circle").objectReferenceValue = circle;
            so.FindProperty("_circleImage").objectReferenceValue = circleImage;
            so.FindProperty("_cardArea").objectReferenceValue = area;
            SetArray(so, "_cards", cards);
            so.FindProperty("_burst").objectReferenceValue = burst;
            so.FindProperty("_skipButton").objectReferenceValue = skip.gameObject;
            so.FindProperty("_closeHint").objectReferenceValue = hint.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);

            if (presenter != null)
            {
                var presenterSo = new SerializedObject(presenter);
                presenterSo.FindProperty("_reveal").objectReferenceValue = view;
                presenterSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return view;
        }

        private static GachaCard BuildCard(RectTransform area, int index, Sprite back, Sprite face)
        {
            RectTransform rect = Rect("Card" + index, area, 0f, 0f, 0.2f, 0.4f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = back;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            RectTransform emblem = Box("Emblem", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
            Plain(emblem, UiSkin.Hd("hd_card_emblem"));
            Text grade = MakeText("Grade", rect, 0.04f, 0.76f, 0.96f, 0.95f, "", 30, TextAnchor.MiddleCenter);
            RectTransform iconRect = Rect("Icon", rect, 0.18f, 0.4f, 0.82f, 0.76f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            Text slot = MakeText("Slot", rect, 0.04f, 0.22f, 0.96f, 0.4f, "", 28, TextAnchor.MiddleCenter);
            Text note = MakeText("Note", rect, 0.04f, 0.04f, 0.96f, 0.22f, "", 22, TextAnchor.MiddleCenter);
            foreach (Text t in new[] { grade, slot, note })
            {
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Overflow;
            }

            rect.gameObject.AddComponent<UiPunch>();
            GachaCard card = rect.gameObject.AddComponent<GachaCard>();
            var so = new SerializedObject(card);
            so.FindProperty("_image").objectReferenceValue = image;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_grade").objectReferenceValue = grade;
            so.FindProperty("_slot").objectReferenceValue = slot;
            so.FindProperty("_note").objectReferenceValue = note;
            so.FindProperty("_back").objectReferenceValue = back;
            so.FindProperty("_face").objectReferenceValue = face;
            so.FindProperty("_emblem").objectReferenceValue = emblem.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            rect.gameObject.SetActive(false);
            return card;
        }

        /// <summary>
        /// D-089 settings window over everything (dim + framed box), opened from the right menu rail. The presenter
        /// lives on the always-active holder; the window and the credits overlay are its children.
        /// </summary>
        private static SettingsPresenter BuildSettings(Transform hud)
        {
            RectTransform holder = Rect(SettingsWindowName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            SettingsPresenter presenter = holder.gameObject.AddComponent<SettingsPresenter>();
            RectTransform box = PopupWindow(holder, new Vector2(960f, 760f), "settings.title", presenter.CloseWindow, out RectTransform window);

            var states = new Text[SettingKeys.Length];
            var images = new Image[SettingKeys.Length];
            for (int i = 0; i < SettingKeys.Length; i++)
            {
                RectTransform row = TopBand("Row" + i, box, 96f + i * 116f, 104f, 44f, 44f);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                Text rowLabel = InkText(Inset("Label", row, 28f, 4f, 300f, 0f), 36, TextAnchor.MiddleLeft, UiPalette.InkTitle);
                rowLabel.verticalOverflow = VerticalWrapMode.Overflow;
                Localize(rowLabel, SettingKeys[i]);
                Button toggle = MakeButton("Toggle", row, 1f, 0f, 1f, 1f, "", 32, out states[i], Tone.Green);
                var toggleRect = (RectTransform)toggle.transform;
                toggleRect.offsetMin = new Vector2(-232f, 14f);
                toggleRect.offsetMax = new Vector2(-16f, -12f);
                images[i] = toggle.GetComponent<Image>();
                UnityEventTools.AddIntPersistentListener(toggle.onClick, presenter.Toggle, i);
            }

            Button credits = MakeButton("Credits", box, 0.5f, 0f, 0.5f, 0f, "", 34, out Text creditsLabel, Tone.Gray);
            Place((RectTransform)credits.transform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(360f, 92f));
            Localize(creditsLabel, "settings.credits");
            UnityEventTools.AddPersistentListener(credits.onClick, presenter.OpenCredits);

            // Credits: a scrollable text window over everything.
            RectTransform creditsHolder = Rect(SettingsPopupName, hud, 0f, 0f, 1f, 1f);
            creditsHolder.SetAsLastSibling();
            RectTransform creditsBox = PopupWindow(creditsHolder, new Vector2(980f, 1560f), "settings.credits", presenter.CloseCredits, out RectTransform creditsPopup);
            // The credits popup is the holder itself (the presenter toggles it); its dim lives on the inner popup.
            RectTransform viewport = Inset("Viewport", creditsBox, 48f, 48f, 48f, 96f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = new Color(0f, 0f, 0f, 0f);
            RectTransform content = Rect("Content", viewport, 0f, 1f, 1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            Text creditsText = content.gameObject.AddComponent<Text>();
            creditsText.font = _font;
            creditsText.fontSize = 26;
            creditsText.color = UiPalette.Ink;
            creditsText.alignment = TextAnchor.UpperLeft;
            creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.raycastTarget = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            creditsPopup.gameObject.SetActive(true);
            creditsHolder.gameObject.SetActive(false);

            var so = new SerializedObject(presenter);
            so.FindProperty("_window").objectReferenceValue = window.gameObject;
            SetArray(so, "_stateTexts", states);
            SetArray(so, "_stateImages", images);
            so.FindProperty("_creditsPopup").objectReferenceValue = creditsHolder.gameObject;
            so.FindProperty("_creditsText").objectReferenceValue = creditsText;
            so.FindProperty("_credits").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(CreditsPath);
            so.FindProperty("_onSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Green);
            so.FindProperty("_offSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Gray);
            so.ApplyModifiedPropertiesWithoutUndo();
            window.gameObject.SetActive(false);
            return presenter;
        }

        /// <summary>
        /// E7-01: portrait layout scales by width (1080 reference) so tall 19.5:9 / 20:9 screens never push UI past
        /// the sides; anchors are ratios, so every region keeps its share of the height.
        /// </summary>
        private static void MatchWidth(GameObject canvas)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
            EditorUtility.SetDirty(scaler);
        }

        /// <summary>
        /// E7-02: every HUD control goes into a SafeArea container fitted to Screen.safeArea; full-screen layers
        /// (damage numbers, gacha reveal, settings, quit confirm) stay outside so their dims cover the whole screen.
        /// </summary>
        private static void WrapSafeArea(Transform hud)
        {
            var outside = new[] { DamageLayerName, RevealName, SettingsPopupName, SettingsWindowName, QuitPopupName, StageSelectName, DailyName, DungeonName, CompanionName, JobName, BossIntroName, FlashName };
            RectTransform safe = Rect(SafeAreaName, hud, 0f, 0f, 1f, 1f);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var move = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in hud)
            {
                if (child == safe || System.Array.IndexOf(outside, child.name) >= 0) continue;
                move.Add(child);
            }

            foreach (Transform child in move) child.SetParent(safe, false);
            safe.SetAsFirstSibling();
        }

        /// <summary>Undo <see cref="WrapSafeArea"/> so the rebuild finds its children by name directly under the HUD.</summary>
        private static void UnwrapSafeArea(Transform hud)
        {
            Transform safe = hud.Find(SafeAreaName);
            if (safe == null) return;
            int index = safe.GetSiblingIndex();
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in safe) children.Add(child);
            foreach (Transform child in children)
            {
                child.SetParent(hud, false);
                child.SetSiblingIndex(index++);
            }

            Object.DestroyImmediate(safe.gameObject);
        }

        /// <summary>GDD boss rule 12 / E7-11: dim + banner with the chapter's boss name during BossIntro.</summary>
        private static void BuildBossIntro(Transform hud, CombatSession session)
        {
            RectTransform holder = Rect(BossIntroName, hud, 0f, 0f, 1f, 1f);
            CanvasGroup group = holder.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Image dim = Rect("Dim", holder, 0f, 0f, 1f, 1f).gameObject.AddComponent<Image>();
            dim.color = new Color(0.05f, 0f, 0.05f, 0.62f);
            dim.raycastTarget = false;
            RectTransform strip = Rect("Banner", holder, 0f, 0.62f, 1f, 0.62f);
            strip.offsetMin = new Vector2(-30f, -10f);
            strip.offsetMax = new Vector2(30f, 200f);
            Image stripImage = strip.gameObject.AddComponent<Image>();
            UiSkin.Sliced(stripImage, UiSkin.Hd("hd9_bossband"));
            stripImage.raycastTarget = false;
            Image skull = FixedIcon(strip, "skull", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), 80f);
            skull.name = "Skull";
            Text chapter = MakeText("Chapter", strip, 0f, 0.55f, 1f, 0.9f, "", 36, TextAnchor.MiddleCenter);
            UiSkin.ButtonText(chapter, Tone.Red);
            chapter.color = new Color(1f, 0.88f, 0.84f, 1f);
            Text name = MakeText("Name", strip, 0f, 0.08f, 1f, 0.58f, "", 64, TextAnchor.MiddleCenter);
            UiSkin.ButtonText(name, Tone.Red);
            name.color = UiPalette.HudGold;

            BossIntroBanner banner = holder.gameObject.AddComponent<BossIntroBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_chapterText").objectReferenceValue = chapter;
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>E3-09: the stage plate opens a one-chapter sheet of 10 stage buttons (D-072: bosses are not farmable).</summary>
        private static StageSelectPresenter BuildStageSelect(Transform hud, CombatSession session)
        {
            RectTransform holder = Rect(StageSelectName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            StageSelectPresenter presenter = holder.gameObject.AddComponent<StageSelectPresenter>();
            RectTransform box = PopupWindow(holder, new Vector2(980f, 620f), "stage.title", presenter.Close, out RectTransform popup);

            RectTransform nav = TopBand("Nav", box, 100f, 96f, 60f, 60f);
            Button prev = MakeButton("Prev", nav, 0f, 0f, 0f, 1f, "", 40, out _, Tone.Blue);
            Place((RectTransform)prev.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(120f, 92f));
            Image prevIcon = FixedIcon(prev.transform, "next", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), 56f);
            prevIcon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            Text chapter = InkText(Inset("Chapter", nav, 130f, 0f, 130f, 0f), 46, TextAnchor.MiddleCenter, UiPalette.InkTitle);
            Button next = MakeButton("Next", nav, 1f, 0f, 1f, 1f, "", 40, out _, Tone.Blue);
            Place((RectTransform)next.transform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(120f, 92f));
            FixedIcon(next.transform, "next", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), 56f);
            UnityEventTools.AddPersistentListener(prev.onClick, presenter.PrevChapter);
            UnityEventTools.AddPersistentListener(next.onClick, presenter.NextChapter);

            RectTransform grid = Inset("Stages", box, 50f, 60f, 50f, 214f);
            var buttons = new Button[10];
            var labels = new Text[10];
            for (int i = 0; i < 10; i++)
            {
                int col = i % 5;
                int row = i / 5;
                buttons[i] = MakeButton("Stage" + i, grid, col / 5f, 1f - (row + 1) * 0.5f, (col + 1) / 5f, 1f - row * 0.5f, "", 34, out labels[i], Tone.Blue);
                var rect = (RectTransform)buttons[i].transform;
                rect.offsetMin = new Vector2(8f, 10f);
                rect.offsetMax = new Vector2(-8f, -10f);
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, presenter.Pick, i);
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_chapterText").objectReferenceValue = chapter;
            SetArray(so, "_stageButtons", buttons);
            SetArray(so, "_stageLabels", labels);
            so.FindProperty("_prev").objectReferenceValue = prev;
            so.FindProperty("_next").objectReferenceValue = next;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_normalSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Blue);
            so.FindProperty("_currentSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Gold);
            so.FindProperty("_bossSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Red);
            so.FindProperty("_lockedSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Gray);
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);

            // The stage plate in the HUD opens the sheet (GDD: tap the stage bar).
            BattleHud battleHud = Object.FindObjectOfType<BattleHud>();
            var stageText = battleHud != null ? new SerializedObject(battleHud).FindProperty("_stageText").objectReferenceValue as Text : null;
            if (stageText != null)
            {
                Button oldButton = stageText.GetComponent<Button>();
                if (oldButton != null) Object.DestroyImmediate(oldButton);
                stageText.raycastTarget = true;
                Button open = stageText.gameObject.AddComponent<Button>();
                open.transition = Selectable.Transition.None;
                open.targetGraphic = stageText;
                UnityEventTools.AddPersistentListener(open.onClick, presenter.Open);
            }

            return presenter;
        }

        /// <summary>E7-14: back key router and the quit confirm popup (last sibling, above every other layer).</summary>
        private static void BuildBackKey(Transform hud, GachaRevealView reveal, SettingsPresenter settings, StageSelectPresenter stageSelect, PanelHost panels, DailyPresenter daily, DungeonPresenter dungeon, CompanionPresenter companion, JobPresenter job)
        {
            RectTransform holder = Rect(QuitPopupName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            BackKeyRouter router = holder.gameObject.AddComponent<BackKeyRouter>();
            RectTransform box = PopupWindow(holder, new Vector2(860f, 420f), null, router.CancelQuit, out RectTransform popup);
            Text title = InkText(TopBand("Title", box, 70f, 120f, 40f, 40f), 44, TextAnchor.MiddleCenter, UiPalette.InkTitle);
            Localize(title, "quit.title");
            Button cancel = MakeButton("Cancel", box, 0f, 0f, 0.5f, 0f, "", 38, out Text cancelLabel, Tone.Gray);
            var cancelRect = (RectTransform)cancel.transform;
            cancelRect.pivot = new Vector2(0.5f, 0f);
            cancelRect.offsetMin = new Vector2(52f, 50f);
            cancelRect.offsetMax = new Vector2(-12f, 154f);
            Localize(cancelLabel, "quit.cancel");
            Button quit = MakeButton("Quit", box, 0.5f, 0f, 1f, 0f, "", 38, out Text quitLabel, Tone.Red);
            var quitRect = (RectTransform)quit.transform;
            quitRect.pivot = new Vector2(0.5f, 0f);
            quitRect.offsetMin = new Vector2(12f, 50f);
            quitRect.offsetMax = new Vector2(-52f, 154f);
            Localize(quitLabel, "quit.confirm");
            UnityEventTools.AddPersistentListener(cancel.onClick, router.CancelQuit);
            UnityEventTools.AddPersistentListener(quit.onClick, router.ConfirmQuit);

            var so = new SerializedObject(router);
            so.FindProperty("_reveal").objectReferenceValue = reveal;
            so.FindProperty("_settings").objectReferenceValue = settings;
            so.FindProperty("_stageSelect").objectReferenceValue = stageSelect;
            so.FindProperty("_daily").objectReferenceValue = daily;
            so.FindProperty("_dungeon").objectReferenceValue = dungeon;
            so.FindProperty("_companion").objectReferenceValue = companion;
            so.FindProperty("_job").objectReferenceValue = job;
            so.FindProperty("_panels").objectReferenceValue = panels;
            so.FindProperty("_quitConfirm").objectReferenceValue = popup.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
        }

        /// <summary>A pull button filling its share of the pull row (icon on the left, cost label).</summary>
        private static TapGuardButton PullButton(RectTransform row, string name, float xMin, float xMax, out Text label, Tone tone, string icon)
        {
            Button button = MakeButton(name, row, xMin, 0f, xMax, 1f, "", 32, out label, tone);
            IconButton(button, label, icon, 44f);
            return button.gameObject.AddComponent<TapGuardButton>();
        }

        /// <summary>
        /// D-089 / D-108 battle-screen rails: rewards on the left (A-3 gold booster, A-2 gems: round candy buttons with
        /// an ad badge and a caption chip), a folding menu on the right (navy tile; the column lists daily rewards,
        /// dungeons, companions, settings, stage and credits). Both sit under the top bar and above the battle lane.
        /// </summary>
        private static void BuildRails(RectTransform root, ToastQueue toast, SettingsPresenter settings, StageSelectPresenter stageSelect, DailyPresenter daily, DungeonPresenter dungeon, CompanionPresenter companion)
        {
            RectTransform left = Box("LeftRail", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RailInset, -RailTop), new Vector2(RailItem + 40f, RailStep * 2f));
            TapGuardButton booster = RailButton(left, "Booster", 0, "coin", Tone.Gold, out Text boosterLabel);
            TapGuardButton gem = RailButton(left, "GemAd", 1, "gem", Tone.Purple, out Text gemLabel);
            AdSlotsPresenter ads = left.gameObject.AddComponent<AdSlotsPresenter>();
            UnityEventTools.AddPersistentListener(booster.OnTap, ads.WatchBooster);
            UnityEventTools.AddPersistentListener(gem.OnTap, ads.WatchGem);
            var so = new SerializedObject(ads);
            so.FindProperty("_gemButton").objectReferenceValue = gem;
            so.FindProperty("_gemLabel").objectReferenceValue = gemLabel;
            so.FindProperty("_boosterButton").objectReferenceValue = booster;
            so.FindProperty("_boosterLabel").objectReferenceValue = boosterLabel;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();

            string[] icons = { "gift", "gate", "paw", "cog", "flag", "scroll" };
            string[] keys = { "rail.daily", "rail.dungeon", "rail.companion", "rail.settings", "rail.stage", "rail.credits" };
            RectTransform right = Box("RightRail", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RailInset, -RailTop), new Vector2(RailMenuButton + 40f, RailMenuButton + 24f + icons.Length * RailMenuStep));
            RailMenu menu = right.gameObject.AddComponent<RailMenu>();
            Button toggle = MakeButton("Menu", right, 0.5f, 1f, 0.5f, 1f, "", 22, out Text unusedLabel, Tone.Gray);
            Object.DestroyImmediate(unusedLabel.gameObject);
            Place((RectTransform)toggle.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(RailMenuButton, RailMenuButton));
            UiSkin.Sliced(toggle.GetComponent<Image>(), UiSkin.RailTile);
            toggle.transition = Selectable.Transition.None;
            FixedIcon(toggle.transform, "menu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), 64f);
            UnityEventTools.AddPersistentListener(toggle.onClick, menu.Toggle);
            var badges = new System.Collections.Generic.List<GameObject> { Badge(toggle.transform, new Vector2(1f, 1f), new Vector2(-8f, -8f)) };

            RectTransform column = Rect("Items", right, 0f, 0f, 1f, 1f);
            column.offsetMax = new Vector2(0f, -(RailMenuButton + 10f));
            UiSkin.Sliced(column.gameObject.AddComponent<Image>(), UiSkin.RailTile);
            column.gameObject.AddComponent<CanvasGroup>();
            column.gameObject.AddComponent<PopupIntro>();
            UnityAction[] actions = { daily.Open, dungeon.Open, companion.Open, settings.Open, stageSelect.Open, settings.OpenCredits };
            for (int i = 0; i < icons.Length; i++)
            {
                RectTransform item = Box("Item" + i, column, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f - i * RailMenuStep), new Vector2(RailItem + 20f, RailMenuStep - 6f));
                Button button = item.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                Image hit = item.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                button.targetGraphic = hit;
                item.gameObject.AddComponent<PressScale>();
                FixedIcon(item, icons[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), 64f);
                Text caption = AddText(BottomBand("Caption", item, 4f, 30f, -16f, -16f), 24, TextAnchor.MiddleCenter);
                caption.verticalOverflow = VerticalWrapMode.Overflow;
                Localize(caption, keys[i]);
                UnityEventTools.AddPersistentListener(button.onClick, actions[i]);
                UnityEventTools.AddPersistentListener(button.onClick, menu.Fold);
                if (i == 0) badges.Add(Badge(item, new Vector2(0.5f, 1f), new Vector2(34f, -8f)));
            }

            var dailySo = new SerializedObject(daily);
            SetArray(dailySo, "_badges", badges.ToArray());
            dailySo.ApplyModifiedPropertiesWithoutUndo();

            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("_content").objectReferenceValue = column.gameObject;
            menuSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-099: a pulsing red dot, centred at <paramref name="anchor"/> + <paramref name="offset"/>; hidden by default.</summary>
        private static GameObject Badge(Transform parent, Vector2 anchor, Vector2 offset)
        {
            RectTransform dot = Box("Badge", parent, anchor, new Vector2(0.5f, 0.5f), offset, new Vector2(36f, 36f));
            Plain(dot, UiSkin.Dot);
            dot.gameObject.AddComponent<UiPulse>();
            dot.gameObject.SetActive(false);
            return dot.gameObject;
        }

        /// <summary>
        /// D-099 attendance + daily missions popup: a 7-day strip with the claim button, then six mission rows
        /// (name and progress, gem reward, claim). The holder stays active so the presenter can track progress.
        /// </summary>
        private static DailyPresenter BuildDaily(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(DailyName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            DailyPresenter presenter = holder.gameObject.AddComponent<DailyPresenter>();
            RectTransform box = PopupWindow(holder, new Vector2(1000f, 1240f), "daily.title", presenter.Close, out RectTransform popup);

            Text attendTitle = InkText(TopBand("AttendTitle", box, 92f, 48f, 50f, 50f), 34, TextAnchor.MiddleLeft, UiPalette.InkTitle);
            var cells = new Image[7];
            var icons = new Image[7];
            var amounts = new Text[7];
            var checks = new GameObject[7];
            RectTransform strip = TopBand("Days", box, 146f, 168f, 40f, 40f);
            Sprite check = UiSkin.Icon("star");
            for (int d = 0; d < 7; d++)
            {
                RectTransform cell = Rect("Day" + d, strip, d / 7f, 0f, (d + 1) / 7f, 1f);
                cell.offsetMin = new Vector2(5f, 0f);
                cell.offsetMax = new Vector2(-5f, 0f);
                cells[d] = cell.gameObject.AddComponent<Image>();
                UiSkin.Sliced(cells[d], UiSkin.Card);
                Text dayLabel = InkText(TopBand("Label", cell, 8f, 32f, 0f, 0f), 26, TextAnchor.MiddleCenter, UiPalette.InkMuted);
                dayLabel.text = (d + 1).ToString();
                icons[d] = Plain(Box("Icon", cell, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(56f, 56f)), null);
                amounts[d] = InkText(BottomBand("Amount", cell, 12f, 32f, 0f, 0f), 26, TextAnchor.MiddleCenter, UiPalette.InkTitle);
                RectTransform mark = Box("Done", cell, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(4f, 4f), new Vector2(44f, 44f));
                Plain(mark, check);
                mark.gameObject.SetActive(false);
                checks[d] = mark.gameObject;
            }

            Button attend = MakeButton("Attend", box, 0.5f, 1f, 0.5f, 1f, "", 36, out Text attendLabel, Tone.Green);
            Place((RectTransform)attend.transform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(520f, 96f));
            UnityEventTools.AddPersistentListener(attend.onClick, presenter.ClaimAttendance);

            Localize(InkText(TopBand("MissionsTitle", box, 444f, 48f, 50f, 50f), 34, TextAnchor.MiddleLeft, UiPalette.InkTitle), "daily.missions");
            var names = new Text[6];
            var rewards = new Text[6];
            var buttons = new Button[6];
            var labels = new Text[6];
            for (int i = 0; i < 6; i++)
            {
                RectTransform row = TopBand("Mission" + i, box, 500f + i * 112f, 102f, 40f, 40f);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                names[i] = InkText(Inset("Name", row, 26f, 4f, 380f, 0f), 30, TextAnchor.MiddleLeft, UiPalette.InkTitle);
                names[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                FixedIcon(row, "gem", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-322f, 2f), 48f);
                rewards[i] = InkText(Inset("Reward", row, 600f, 4f, 230f, 0f), 30, TextAnchor.MiddleLeft, UiPalette.InkBlue);
                rewards[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                buttons[i] = MakeButton("Claim", row, 1f, 0f, 1f, 1f, "", 30, out labels[i], Tone.Gold);
                var claimRect = (RectTransform)buttons[i].transform;
                claimRect.offsetMin = new Vector2(-214f, 12f);
                claimRect.offsetMax = new Vector2(-14f, -10f);
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, presenter.ClaimMission, i);
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.FindProperty("_attendTitle").objectReferenceValue = attendTitle;
            SetArray(so, "_dayCells", cells);
            SetArray(so, "_dayIcons", icons);
            SetArray(so, "_dayAmounts", amounts);
            SetArray(so, "_dayChecks", checks);
            so.FindProperty("_attendButton").objectReferenceValue = attend;
            so.FindProperty("_attendLabel").objectReferenceValue = attendLabel;
            SetArray(so, "_missionNames", names);
            SetArray(so, "_missionRewards", rewards);
            SetArray(so, "_missionButtons", buttons);
            SetArray(so, "_missionLabels", labels);
            so.FindProperty("_gemIcon").objectReferenceValue = UiSkin.Icon("gem");
            so.FindProperty("_goldIcon").objectReferenceValue = UiSkin.Icon("coin");
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
            return presenter;
        }

        /// <summary>D-104 job looks index-aligned with JobCatalog.All, as ArtBuilder writes them.</summary>
        private static CharacterArt[] JobArts()
        {
            var arts = new CharacterArt[SoloHero.Core.Jobs.JobCatalog.Count];
            for (int j = 0; j < arts.Length; j++)
            {
                string look = SoloHero.Core.Jobs.JobCatalog.All[j].Look;
                arts[j] = AssetDatabase.LoadAssetAtPath<CharacterArt>(look == "knight" ? HeroArtPath : ArtBuilder.JobArtPath(look));
            }

            return arts;
        }

        /// <summary>
        /// D-104 job advancement popup: the current job and the next requirement on top, then up to three job cards
        /// (portrait on a stage, name, main attack and mastery, description, pick / confirm button).
        /// </summary>
        private static JobPresenter BuildJobPopup(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(JobName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            JobPresenter presenter = holder.gameObject.AddComponent<JobPresenter>();
            RectTransform box = PopupWindow(holder, new Vector2(1000f, 1406f), "job.title", presenter.Close, out RectTransform popup);
            Text current = InkText(TopBand("Current", box, 92f, 48f, 44f, 44f), 28, TextAnchor.MiddleCenter, UiPalette.InkGold);
            current.horizontalOverflow = HorizontalWrapMode.Overflow;

            var cards = new GameObject[3];
            var portraits = new Image[3];
            var names = new Text[3];
            var mains = new Text[3];
            var descs = new Text[3];
            var picks = new TapGuardButton[3];
            var pickLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform row = TopBand("Card" + i, box, 150f + i * 412f, 396f, 36f, 36f);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                cards[i] = row.gameObject;
                RectTransform well = Box("Stage", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 2f), new Vector2(250f, 350f));
                UiSkin.Sliced(Plain(well, null), UiSkin.Inset);
                Image glow = Plain(Box("Glow", well, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(300f, 300f)), UiSkin.Hd("hd_glow"));
                glow.color = new Color(1f, 0.92f, 0.65f, 0.8f);
                Plain(Box("Pedestal", well, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(220f, 66f)), UiSkin.Hd("hd_pedestal"));
                RectTransform face = Box("Portrait", well, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(384f, 288f));
                portraits[i] = Plain(face, null);
                portraits[i].preserveAspect = true;
                names[i] = InkText(TopBand("Name", row, 22f, 54f, 290f, 30f), 42, TextAnchor.MiddleLeft, UiPalette.InkTitle);
                mains[i] = InkText(TopBand("Main", row, 84f, 110f, 290f, 26f), 26, TextAnchor.UpperLeft, UiPalette.InkBlue);
                mains[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                mains[i].verticalOverflow = VerticalWrapMode.Overflow;
                descs[i] = InkText(Inset("Desc", row, 290f, 122f, 26f, 200f), 24, TextAnchor.UpperLeft, UiPalette.Ink);
                descs[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                descs[i].verticalOverflow = VerticalWrapMode.Overflow;
                Button pick = MakeButton("Pick", row, 1f, 0f, 1f, 0f, "", 34, out pickLabels[i], Tone.Orange);
                var pickRect = (RectTransform)pick.transform;
                pickRect.pivot = new Vector2(1f, 0f);
                pickRect.offsetMin = new Vector2(-330f, 20f);
                pickRect.offsetMax = new Vector2(-24f, 112f);
                picks[i] = pick.gameObject.AddComponent<TapGuardButton>();
                UnityEventTools.AddIntPersistentListener(picks[i].OnTap, presenter.Pick, i);
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.FindProperty("_current").objectReferenceValue = current;
            so.FindProperty("_window").objectReferenceValue = box;
            so.FindProperty("_windowBase").floatValue = 170f;
            so.FindProperty("_cardStep").floatValue = 412f;
            SetArray(so, "_arts", JobArts());
            SetArray(so, "_cards", cards);
            SetArray(so, "_portraits", portraits);
            SetArray(so, "_names", names);
            SetArray(so, "_mains", mains);
            SetArray(so, "_descs", descs);
            SetArray(so, "_picks", picks);
            SetArray(so, "_pickLabels", pickLabels);
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
            return presenter;
        }

        /// <summary>D-102 companions popup: four rows (portrait, name and level, attack or unlock stage, equip, level up).</summary>
        private static CompanionPresenter BuildCompanion(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(CompanionName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            CompanionPresenter presenter = holder.gameObject.AddComponent<CompanionPresenter>();
            RectTransform box = PopupWindow(holder, new Vector2(1000f, 1160f), "companion.title", presenter.Close, out RectTransform popup);

            var portraits = new Image[4];
            var names = new Text[4];
            var infos = new Text[4];
            var equips = new Button[4];
            var equipLabels = new Text[4];
            var levels = new Button[4];
            var levelLabels = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                RectTransform row = TopBand("Row" + i, box, 100f + i * 250f, 234f, 36f, 36f);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                RectTransform well = Box("Well", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 2f), new Vector2(190f, 190f));
                UiSkin.Sliced(Plain(well, null), UiSkin.Inset);
                Image glow = Plain(Box("Glow", well, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f)), UiSkin.Hd("hd_glow"));
                glow.color = new Color(1f, 0.95f, 0.75f, 0.8f);
                RectTransform face = Box("Portrait", well, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(192f, 192f));
                portraits[i] = Plain(face, null);
                portraits[i].preserveAspect = true;
                names[i] = InkText(TopBand("Name", row, 30f, 56f, 230f, 300f), 38, TextAnchor.MiddleLeft, UiPalette.InkTitle);
                names[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                infos[i] = InkText(TopBand("Info", row, 96f, 100f, 230f, 300f), 26, TextAnchor.UpperLeft, UiPalette.Ink);
                infos[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                infos[i].verticalOverflow = VerticalWrapMode.Overflow;
                equips[i] = MakeButton("Equip", row, 1f, 0.5f, 1f, 1f, "", 30, out equipLabels[i], Tone.Blue);
                var equipRect = (RectTransform)equips[i].transform;
                equipRect.offsetMin = new Vector2(-284f, 4f);
                equipRect.offsetMax = new Vector2(-20f, -20f);
                UnityEventTools.AddIntPersistentListener(equips[i].onClick, presenter.Equip, i);
                levels[i] = MakeButton("Level", row, 1f, 0f, 1f, 0.5f, "", 30, out levelLabels[i], Tone.Green);
                var levelRect = (RectTransform)levels[i].transform;
                levelRect.offsetMin = new Vector2(-284f, 24f);
                levelRect.offsetMax = new Vector2(-20f, -4f);
                IconButton(levels[i], levelLabels[i], "coin", 40f);
                levels[i].gameObject.AddComponent<HoldRepeat>();
                UnityEventTools.AddIntPersistentListener(levels[i].onClick, presenter.LevelUp, i);
            }

            var arts = new CharacterArt[CompanionArtPaths.Length];
            for (int i = 0; i < arts.Length; i++) arts[i] = AssetDatabase.LoadAssetAtPath<CharacterArt>(CompanionArtPaths[i]);

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            SetArray(so, "_arts", arts);
            SetArray(so, "_portraits", portraits);
            SetArray(so, "_names", names);
            SetArray(so, "_infos", infos);
            SetArray(so, "_equipButtons", equips);
            SetArray(so, "_equipLabels", equipLabels);
            SetArray(so, "_levelButtons", levels);
            SetArray(so, "_levelLabels", levelLabels);
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
            return presenter;
        }

        /// <summary>
        /// D-100 daily dungeons: an entry popup with a gold and an EXP card (icon tile, title, description, entries left,
        /// reward per kill, enter), and the in-run banner under the top bar (name, seconds left, amount earned).
        /// </summary>
        private static DungeonPresenter BuildDungeon(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(DungeonName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            DungeonPresenter presenter = holder.gameObject.AddComponent<DungeonPresenter>();

            RectTransform banner = Box("Banner", holder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 128f)), new Vector2(700f, 128f));
            UiSkin.Sliced(banner.gameObject.AddComponent<Image>(), UiSkin.Pill);
            banner.GetComponent<Image>().raycastTarget = false;
            Text bannerTitle = MakeText("Title", banner, 0.07f, 0.5f, 0.62f, 0.94f, "", 34, TextAnchor.MiddleLeft);
            Text bannerTime = MakeText("Time", banner, 0.6f, 0.5f, 0.93f, 0.94f, "", 34, TextAnchor.MiddleRight);
            bannerTime.color = UiPalette.HudGold;
            Text bannerEarned = MakeText("Earned", banner, 0.05f, 0.06f, 0.95f, 0.52f, "", 34, TextAnchor.MiddleCenter);
            bannerEarned.color = UiPalette.HudGood;
            banner.gameObject.SetActive(false);

            RectTransform box = PopupWindow(holder, new Vector2(980f, 980f), "dungeon.title", presenter.Close, out RectTransform popup);
            string[] icons = { "coin", "star" };
            Tone[] tones = { Tone.Gold, Tone.Blue };
            string[] names = { "dungeon.gold", "dungeon.exp" };
            string[] descs = { "dungeon.gold_desc", "dungeon.exp_desc" };
            var tickets = new Text[2];
            var rewards = new Text[2];
            var enters = new Button[2];
            for (int i = 0; i < 2; i++)
            {
                RectTransform card = TopBand("Card" + i, box, 100f + i * 420f, 400f, 36f, 36f);
                UiSkin.Sliced(card.gameObject.AddComponent<Image>(), UiSkin.Card);
                RectTransform tile = Box("Tile", card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(170f, 170f));
                UiSkin.Sliced(Plain(tile, null), UiSkin.PlateSprite(tones[i]));
                FixedIcon(tile, icons[i], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), 112f);
                Localize(InkText(TopBand("Name", card, 26f, 56f, 220f, 30f), 42, TextAnchor.MiddleLeft, UiPalette.InkTitle), names[i]);
                Text desc = InkText(TopBand("Desc", card, 90f, 100f, 220f, 30f), 28, TextAnchor.UpperLeft, UiPalette.Ink);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                desc.verticalOverflow = VerticalWrapMode.Overflow;
                Localize(desc, descs[i]);
                RectTransform ticketChip = Box("TicketChip", card, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(109f, -206f), new Vector2(150f, 48f));
                UiSkin.Sliced(Plain(ticketChip, null), UiSkin.LevelBadge);
                tickets[i] = AddText(Inset("Tickets", ticketChip, 0f, 2f, 0f, 0f), 28, TextAnchor.MiddleCenter);
                UiSkin.ButtonText(tickets[i], Tone.Blue);
                rewards[i] = InkText(BottomBand("Reward", card, 36f, 80f, 40f, 400f), 36, TextAnchor.MiddleLeft, UiPalette.InkGold);
                rewards[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                enters[i] = MakeButton("Enter", card, 1f, 0f, 1f, 0f, "", 40, out Text enterLabel, Tone.Green);
                var enterRect = (RectTransform)enters[i].transform;
                enterRect.pivot = new Vector2(1f, 0f);
                enterRect.offsetMin = new Vector2(-340f, 28f);
                enterRect.offsetMax = new Vector2(-28f, 132f);
                Localize(enterLabel, "dungeon.enter");
                UnityEventTools.AddIntPersistentListener(enters[i].onClick, presenter.Enter, i);
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            SetArray(so, "_ticketTexts", tickets);
            SetArray(so, "_rewardTexts", rewards);
            SetArray(so, "_enterButtons", enters);
            so.FindProperty("_banner").objectReferenceValue = banner.gameObject;
            so.FindProperty("_bannerTitle").objectReferenceValue = bannerTitle;
            so.FindProperty("_bannerTime").objectReferenceValue = bannerTime;
            so.FindProperty("_bannerEarned").objectReferenceValue = bannerEarned;
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);
            return presenter;
        }

        /// <summary>One left-rail reward: a round candy button with a 4x icon, an ad badge, and a caption chip under it.</summary>
        private static TapGuardButton RailButton(RectTransform rail, string name, int index, string icon, Tone tone, out Text caption)
        {
            RectTransform slot = Box(name, rail, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -index * RailStep), new Vector2(RailItem + 40f, RailStep));
            Button button = MakeButton("Button", slot, 0.5f, 1f, 0.5f, 1f, "", 22, out Text unused, tone);
            Object.DestroyImmediate(unused.gameObject);
            Place((RectTransform)button.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(RailItem, RailItem));
            Image face = button.GetComponent<Image>();
            face.sprite = UiSkin.RoundSprite(tone);
            face.type = Image.Type.Simple;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white, highlightedColor = Color.white, pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f),
                selectedColor = Color.white, disabledColor = new Color(0.6f, 0.6f, 0.65f, 1f), colorMultiplier = 1f, fadeDuration = 0.08f
            };
            FixedIcon(button.transform, icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), 64f);
            RectTransform badge = Box("Ad", button.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(44f, 44f));
            Plain(badge, UiSkin.Icon("tv"));

            RectTransform chip = BottomBand("CaptionChip", slot, 14f, 38f, 0f, 0f);
            UiSkin.Sliced(Plain(chip, null), UiSkin.Caption);
            caption = AddText(Inset("Caption", chip, 4f, 0f, 4f, 0f), 24, TextAnchor.MiddleCenter);
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            return button.gameObject.AddComponent<TapGuardButton>();
        }

        private static void BuildDamageText(Transform hud, CombatSession session)
        {
            // Last sibling: numbers draw over the battle HUD. Raycasts off so they never block taps.
            RectTransform layer = Rect(DamageLayerName, hud, 0f, 0f, 1f, 1f);
            layer.SetAsLastSibling();
            SubCanvas(layer.gameObject, false);
            RectTransform templateRect = Rect("DamageTextTemplate", layer, 0.5f, 0.5f, 0.5f, 0.5f);
            templateRect.sizeDelta = new Vector2(360f, 70f);
            // Plate first so it draws under the text; only skill-name labels switch it on.
            RectTransform plate = Box("Plate", templateRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 64f));
            Image plateImage = plate.gameObject.AddComponent<Image>();
            UiSkin.Sliced(plateImage, UiSkin.Pill);
            plateImage.color = new Color(1f, 1f, 1f, 0.95f);
            plateImage.raycastTarget = false;
            plate.gameObject.SetActive(false);
            RectTransform labelRect = Rect("Label", templateRect, 0f, 0f, 1f, 1f);
            Text label = labelRect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = 38;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            Outline outline = labelRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.06f, 0.16f, 1f);
            outline.effectDistance = new Vector2(3.5f, -3.5f);
            Shadow shadow = labelRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.02f, 0.1f, 0.6f);
            shadow.effectDistance = new Vector2(0f, -5f);
            DamageText template = templateRect.gameObject.AddComponent<DamageText>();
            var templateSo = new SerializedObject(template);
            templateSo.FindProperty("_label").objectReferenceValue = label;
            templateSo.FindProperty("_plate").objectReferenceValue = plate;
            templateSo.ApplyModifiedPropertiesWithoutUndo();
            templateRect.gameObject.SetActive(false);

            DamageTextPool pool = layer.gameObject.AddComponent<DamageTextPool>();
            var so = new SerializedObject(pool);
            so.FindProperty("_template").objectReferenceValue = template;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_layer").objectReferenceValue = layer;
            so.FindProperty("_normalSize").intValue = 38;
            so.FindProperty("_critSize").intValue = 60;
            so.FindProperty("_skillSize").intValue = 48;
            so.FindProperty("_dotSize").intValue = 34;
            so.FindProperty("_comboSize").intValue = 52;
            so.FindProperty("_companionSize").intValue = 40;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-111 combat power pill left of the stage plate; a green "+N" floats up from it on every rise.</summary>
        private static void BuildCombatPower(Transform hud)
        {
            RectTransform pill = Box("CombatPower", hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-268f, -(TopBarHeight + 16f)), new Vector2(204f, 92f));
            UiSkin.Sliced(Plain(pill, null), UiSkin.Pill);
            pill.GetComponent<Image>().raycastTarget = false;
            FixedIcon(pill, "atk", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), 46f);
            Text label = AddText(Box("Label", pill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -6f), new Vector2(134f, 32f)), 22, TextAnchor.MiddleLeft);
            label.color = UiPalette.HudGold;
            Localize(label, "cp.label");
            Text value = AddText(Box("Value", pill, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(64f, 8f), new Vector2(134f, 46f)), 34, TextAnchor.MiddleLeft);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform gainRect = Box("Gain", pill, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(180f, 44f));
            Text gain = AddText(gainRect, 32, TextAnchor.MiddleLeft);
            gain.color = UiPalette.HudGood;
            gain.horizontalOverflow = HorizontalWrapMode.Overflow;
            CanvasGroup gainGroup = gainRect.gameObject.AddComponent<CanvasGroup>();
            gainGroup.alpha = 0f;
            gainGroup.blocksRaycasts = false;

            CombatPowerPresenter presenter = pill.gameObject.AddComponent<CombatPowerPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("_value").objectReferenceValue = value;
            so.FindProperty("_gain").objectReferenceValue = gain;
            so.FindProperty("_gainGroup").objectReferenceValue = gainGroup;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-111 guide quest bar under the left rail: scroll icon, quest and progress, reward on the right. Gold and
        /// pulsing once done; tap to claim or to jump to where the quest is made. Panels and toast are wired later.
        /// </summary>
        private static void BuildGuideQuest(Transform hud)
        {
            RectTransform bar = Box("GuideQuest", hud, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RailInset, -(RailTop + 2f * RailStep + 8f)), new Vector2(476f, 100f));
            Image background = Plain(bar, null);
            UiSkin.Sliced(background, UiSkin.Pill);
            background.raycastTarget = true;
            FixedIcon(bar, "quest", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), 70f);
            Text title = AddText(Box("Title", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -10f), new Vector2(286f, 42f)), 27, TextAnchor.MiddleLeft);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text progress = AddText(Box("Progress", bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(90f, 10f), new Vector2(286f, 38f)), 25, TextAnchor.MiddleLeft);
            progress.color = UiPalette.HudGold;
            Image rewardIcon = Plain(Box("RewardIcon", bar, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-48f, 12f), new Vector2(46f, 46f)), UiSkin.Icon("gem"));
            rewardIcon.preserveAspect = true;
            Text reward = AddText(Box("Reward", bar, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-48f, -24f), new Vector2(96f, 30f)), 24, TextAnchor.MiddleCenter);
            reward.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiPulse pulse = bar.gameObject.AddComponent<UiPulse>();
            pulse.enabled = false;
            Button button = bar.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            bar.gameObject.AddComponent<PressScale>();

            GuideQuestPresenter presenter = bar.gameObject.AddComponent<GuideQuestPresenter>();
            UnityEventTools.AddPersistentListener(button.onClick, presenter.OnTap);
            var so = new SerializedObject(presenter);
            so.FindProperty("_title").objectReferenceValue = title;
            so.FindProperty("_progress").objectReferenceValue = progress;
            so.FindProperty("_reward").objectReferenceValue = reward;
            so.FindProperty("_rewardIcon").objectReferenceValue = rewardIcon;
            so.FindProperty("_gemSprite").objectReferenceValue = UiSkin.Icon("gem");
            so.FindProperty("_goldSprite").objectReferenceValue = UiSkin.Icon("coin");
            so.FindProperty("_background").objectReferenceValue = background;
            so.FindProperty("_idleSprite").objectReferenceValue = UiSkin.Pill;
            so.FindProperty("_readySprite").objectReferenceValue = UiSkin.PlateSprite(Tone.Gold);
            so.FindProperty("_pulse").objectReferenceValue = pulse;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-111: hands the guide quest bar the tab host, the summon panel and the toast once they exist.</summary>
        private static void WireGuideQuest(Transform hud, PanelHost panels, GachaPanelPresenter gacha, ToastQueue toast)
        {
            Transform bar = hud.Find("GuideQuest");
            GuideQuestPresenter presenter = bar != null ? bar.GetComponent<GuideQuestPresenter>() : null;
            if (presenter == null) return;
            var so = new SerializedObject(presenter);
            so.FindProperty("_panels").objectReferenceValue = panels;
            so.FindProperty("_gacha").objectReferenceValue = gacha;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-112: the battle's dark sticker outline for a UI character (uGUI Outline copies the sprite one texel each
        /// way in the outline colour); <paramref name="texel"/> is the flipbook's scale, one sprite pixel in canvas units.
        /// </summary>
        private static void PixelOutline(Image image, float texel)
        {
            if (image == null) return;
            Outline outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.08f, 0.19f, 1f);
            outline.effectDistance = new Vector2(texel, -texel);
            outline.useGraphicAlpha = true;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static void BuildTutorial(RectTransform root, CombatSession session, ToastQueue toast, GameObject characterPanel, GachaPanelPresenter gacha)
        {
            RectTransform banner = Box("TutorialBanner", root, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 84f));
            UiSkin.Sliced(banner.gameObject.AddComponent<Image>(), UiSkin.PlateSprite(Tone.Orange));
            banner.GetComponent<Image>().raycastTarget = false;
            Text label = MakeText("Label", banner, 0f, 0f, 1f, 1f, "", 32, TextAnchor.MiddleCenter);
            label.rectTransform.offsetMin = new Vector2(20f, 4f);
            label.rectTransform.offsetMax = new Vector2(-20f, 0f);
            UiSkin.ButtonText(label, Tone.Orange);
            banner.gameObject.AddComponent<UiPulse>();
            banner.gameObject.SetActive(false);

            // D-111: a bobbing hand over the ATK upgrade button while the upgrade hint is up.
            RectTransform pointer = Box("TutorialPointer", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
            Image hand = Plain(pointer, UiSkin.Icon("hand"));
            hand.raycastTarget = false;
            pointer.gameObject.SetActive(false);
            Transform lane = FindDeep(characterPanel != null ? characterPanel.transform : null, "Lane1");
            Transform buy = lane != null ? lane.Find("Buy") : null;
            if (buy == null) Debug.LogWarning("[UI] ATK upgrade button not found for the tutorial pointer");

            TutorialHints hints = root.gameObject.AddComponent<TutorialHints>();
            var so = new SerializedObject(hints);
            so.FindProperty("_banner").objectReferenceValue = banner.gameObject;
            so.FindProperty("_bannerText").objectReferenceValue = label;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_gacha").objectReferenceValue = gacha;
            so.FindProperty("_pointer").objectReferenceValue = pointer;
            so.FindProperty("_pointerTarget").objectReferenceValue = buy as RectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ToastQueue BuildToast(RectTransform root)
        {
            RectTransform box = Box("Toast", root, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 88f));
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Pill);
            box.GetComponent<Image>().raycastTarget = false;
            box.gameObject.AddComponent<CanvasGroup>();
            box.gameObject.AddComponent<PopupIntro>();
            Text label = MakeText("Label", box, 0f, 0f, 1f, 1f, "", 34, TextAnchor.MiddleCenter);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            label.rectTransform.offsetMax = new Vector2(-24f, 0f);
            ToastQueue toast = root.gameObject.AddComponent<ToastQueue>();
            var so = new SerializedObject(toast);
            so.FindProperty("_root").objectReferenceValue = box.gameObject;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toast;
        }

        /// <summary>
        /// Portrait layout (E7-01): characters stand on the ground line at 50% of the screen height, so nothing
        /// interactive may sit in 45-62%. The challenge button rides next to the stage plate; skills sit above the panel.
        /// </summary>
        private static void MoveChallengeButton(Transform hud)
        {
            Localize(FindLabel(hud, "FailPanel/Retry"), "hud.retry");
            Localize(FindLabel(hud, "FailPanel/Retreat"), "hud.retreat");
            Localize(FindLabel(hud, "Challenge"), "hud.challenge");

            Transform oldBar = hud.Find("TopBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);
            foreach (string stale in new[] { "StagePlate", "BossBar", "GemCount", "CombatPower", "GuideQuest" })
            {
                Transform t = hud.Find(stale);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            // D-106 / D-108: a navy strip TopBarHeight units tall across the top.
            RectTransform bar = Rect("TopBar", hud, 0f, 1f, 1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -TopBarHeight);
            bar.offsetMax = Vector2.zero;
            Image barImage = bar.gameObject.AddComponent<Image>();
            UiSkin.Sliced(barImage, UiSkin.HudBar);
            barImage.raycastTarget = false;
            bar.SetSiblingIndex(1);
            SkinHud(hud, bar);
        }

        /// <summary>
        /// D-106 / D-108 HUD: top bar (round portrait with level badge, job name, HP and EXP gauges, gold and gem pills), the
        /// stage plate under it (stage, kill gauge; tap for stage select) with the challenge button beside it, the boss
        /// bar and timer, the fail window and the skill bar. The scene's own texts (Gold, Stage, Kills, BossTimer) and
        /// buttons are placed and restyled here; everything else is rebuilt.
        /// </summary>
        private static void SkinHud(Transform hud, RectTransform bar)
        {
            BattleHud battleHud = Object.FindObjectOfType<BattleHud>();
            var so = battleHud != null ? new SerializedObject(battleHud) : null;

            // Round portrait: sky disc (also the mask), the job look's idle loop cropped to the head, gold ring, level.
            RectTransform avatar = Box("Avatar", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(112f, 112f));
            Image disc = Plain(avatar, UiSkin.AvatarDisc);
            disc.preserveAspect = false;
            Mask mask = avatar.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            CharacterArt heroArt = AssetDatabase.LoadAssetAtPath<CharacterArt>(HeroArtPath);
            // The chibi face sits about 21 px above and 7 px left of the foot pivot; at 4x that centres it in the disc.
            RectTransform face = Box("Hero", avatar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(28f, -28f), new Vector2(96f, 96f));
            Plain(face, heroArt != null && heroArt.idle.Length > 0 ? heroArt.idle[0] : null).preserveAspect = false;
            UiFlipbook faceBook = face.gameObject.AddComponent<UiFlipbook>();
            var faceSo = new SerializedObject(faceBook);
            faceSo.FindProperty("_art").objectReferenceValue = heroArt;
            SetArray(faceSo, "_jobs", JobArts());
            faceSo.FindProperty("_scale").floatValue = 4f;
            faceSo.ApplyModifiedPropertiesWithoutUndo();
            PixelOutline(face.GetComponent<Image>(), 4f);
            Plain(Box("Ring", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(112f, 112f)), UiSkin.AvatarRing);
            RectTransform lvBadge = Box("LevelBadge", bar, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(68f, -116f), new Vector2(100f, 40f));
            UiSkin.Sliced(Plain(lvBadge, null), UiSkin.LevelBadge);
            Text heroLevel = AddText(Inset("Label", lvBadge, 2f, 2f, 2f, 0f), 24, TextAnchor.MiddleCenter);
            UiSkin.ButtonText(heroLevel, Tone.Blue);

            const float barsX = 140f;
            const float barsW = 320f;
            Text heroName = AddText(Box("HeroName", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(barsX + 4f, -8f), new Vector2(barsW, 40f)), 30, TextAnchor.MiddleLeft);
            heroName.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform hpWell = Box("HeroHp", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(barsX, -52f), new Vector2(barsW, 34f));
            RectTransform hpFill = Gauge(hpWell, UiSkin.FillRed);
            Text hpText = AddText(Inset("Label", hpWell, 8f, 0f, 14f, 0f), 24, TextAnchor.MiddleRight);
            RectTransform expWell = Box("HeroExp", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(barsX, -92f), new Vector2(barsW, 28f));
            RectTransform expFill = Gauge(expWell, UiSkin.FillBlue);
            Text expTag = AddText(Inset("Tag", expWell, 12f, 0f, 0f, 0f), 20, TextAnchor.MiddleLeft);
            expTag.color = UiPalette.HudGem;
            Localize(expTag, "char.exp");
            Text expText = AddText(Inset("Label", expWell, 0f, 0f, 14f, 0f), 20, TextAnchor.MiddleRight);
            foreach (Text t in new[] { hpText, expTag, expText }) t.verticalOverflow = VerticalWrapMode.Overflow;

            // Currencies: gold and gem pills on the right, the icon overlapping each pill's left end.
            TopPill(bar, "GoldPill", 626f, 226f, "coin");
            TopText(hud, "Gold", new Vector2(0f, 1f), new Vector2(684f, -30f), new Vector2(156f, 56f), 34, TextAnchor.MiddleRight, UiPalette.HudGold);
            TopPill(bar, "GemPill", 870f, 196f, "gem");
            Text gem = AddText(Box("GemCount", hud, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(926f, -30f), new Vector2(126f, 56f)), 34, TextAnchor.MiddleRight);
            gem.text = "0";
            gem.color = UiPalette.HudGem;
            gem.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Stage plate under the bar: "2-7" over a kill gauge with "4 / 12"; tapping the number opens stage select.
            RectTransform plate = Box("StagePlate", hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 8f)), new Vector2(300f, 112f));
            UiSkin.Sliced(Plain(plate, null), UiSkin.Pill);
            plate.GetComponent<Image>().raycastTarget = false;
            plate.SetSiblingIndex(2);
            RectTransform killWell = Box("KillGauge", plate, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(232f, 30f));
            RectTransform killFill = Gauge(killWell, UiSkin.FillGold);
            // The stage number's rect covers the whole plate so a tap anywhere on it opens stage select.
            TopText(hud, "Stage", new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 14f)), new Vector2(300f, 104f), 50, TextAnchor.UpperCenter, Color.white);
            TopText(hud, "Kills", new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 8f + 112f - 47f)), new Vector2(232f, 30f), 22, TextAnchor.MiddleCenter, Color.white);

            BuildCombatPower(hud);
            BuildGuideQuest(hud);

            // Boss HP bar: name on the left inside a red gauge under the stage plate; the timer sits under it.
            RectTransform bossBar = Box("BossBar", hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 132f)), new Vector2(720f, 62f));
            RectTransform bossFill = Gauge(bossBar, UiSkin.FillRed);
            Text bossName = MakeText("Name", bossBar, 0.03f, 0f, 0.97f, 1f, "", 32, TextAnchor.MiddleLeft);
            FixedIcon(bossBar, "skull", new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 0f), 64f);
            foreach (Graphic g in bossBar.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            bossBar.gameObject.SetActive(false);

            Text timer = hud.Find("BossTimer") != null ? hud.Find("BossTimer").GetComponent<Text>() : null;
            if (timer != null)
            {
                TopText(hud, "BossTimer", new Vector2(0.5f, 1f), new Vector2(0f, -(TopBarHeight + 200f)), new Vector2(300f, 80f), 64, TextAnchor.MiddleCenter, new Color(1f, 0.62f, 0.5f, 1f));
            }

            if (so != null)
            {
                so.FindProperty("_bossBar").objectReferenceValue = bossBar.gameObject;
                so.FindProperty("_bossFill").objectReferenceValue = bossFill;
                so.FindProperty("_bossName").objectReferenceValue = bossName;
                so.FindProperty("_gemText").objectReferenceValue = gem;
                so.FindProperty("_heroNameText").objectReferenceValue = heroName;
                so.FindProperty("_heroLevelText").objectReferenceValue = heroLevel;
                so.FindProperty("_heroHpFill").objectReferenceValue = hpFill;
                so.FindProperty("_heroHpText").objectReferenceValue = hpText;
                so.FindProperty("_heroExpFill").objectReferenceValue = expFill;
                so.FindProperty("_heroExpText").objectReferenceValue = expText;
                so.FindProperty("_killFill").objectReferenceValue = killFill;
            }

            // Challenge: an orange pill with a skull right of the stage plate, gently pulsing while it is offered.
            Transform challenge = hud.Find("Challenge");
            if (challenge != null)
            {
                Place((RectTransform)challenge, new Vector2(0.5f, 1f), new Vector2(262f, -(TopBarHeight + 64f)), new Vector2(196f, 92f));
                ((RectTransform)challenge).pivot = new Vector2(0.5f, 0.5f);
                StyleExisting(hud, "Challenge", Tone.Orange, 36);
                Button challengeButton = challenge.GetComponent<Button>();
                Text challengeLabel = challenge.GetComponentInChildren<Text>(true);
                if (challengeButton != null && challengeLabel != null) IconButton(challengeButton, challengeLabel, "skull", 44f);
                UiPulse stalePulse = challenge.GetComponent<UiPulse>();
                if (stalePulse != null) Object.DestroyImmediate(stalePulse);
                // The button itself has PressScale; only its skull beats so the two never fight over the scale.
                Transform skull = challenge.Find("Icon");
                if (skull != null) skull.gameObject.AddComponent<UiPulse>();
            }

            Text prompt = hud.Find("RetreatPrompt") != null ? hud.Find("RetreatPrompt").GetComponent<Text>() : null;
            if (prompt != null)
            {
                SetAnchors(hud, "RetreatPrompt", 0.1f, 0.64f, 0.9f, 0.69f);
                prompt.font = _font;
                prompt.fontSize = 38;
                UiSkin.TextShadow(prompt);
            }

            // Fail window: a cream window over the battle with retry (green) and retreat (red).
            Transform fail = hud.Find("FailPanel");
            if (fail != null)
            {
                Place((RectTransform)fail, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(880f, 250f));
                Image failImage = fail.GetComponent<Image>();
                if (failImage != null) UiSkin.Sliced(failImage, UiSkin.Window);
                SetAnchors(fail, "Retry", 0.06f, 0.2f, 0.48f, 0.8f);
                SetAnchors(fail, "Retreat", 0.52f, 0.2f, 0.94f, 0.8f);
            }

            StyleExisting(hud, "FailPanel/Retry", Tone.Green, 40);
            StyleExisting(hud, "FailPanel/Retreat", Tone.Red, 40);

            BuildSkillBar(hud, battleHud, so);
            if (so != null) so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-078 skill bar above the panel: one square per slot with the skill icon in a grade frame, a radial
        /// cooldown with seconds, and a lock with the opening hero level. Tapping casts the slot now. Above the slots:
        /// the main attack (left), the 2nd job's ultimate next to it, and the AUTO toggle (right).
        /// </summary>
        private static void BuildSkillBar(Transform hud, BattleHud battleHud, SerializedObject so)
        {
            foreach (string stale in new[] { "Skill1", "Skill2", "Skill3", SkillBarName, SkillAutoName, BasicSkillName, UltimateSkillName })
            {
                Transform t = hud.Find(stale);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            int count = new SoloHero.Core.Config.BalanceValues().SKILL_SLOT_COUNT;
            const float slotSize = 124f;
            const float slotStep = 168f;
            RectTransform bar = Rect(SkillBarName, hud, 0f, PanelTop, 1f, PanelTop);
            bar.offsetMin = new Vector2(0f, 18f);
            bar.offsetMax = new Vector2(0f, 18f + slotSize);
            SubCanvas(bar.gameObject, true);
            // D-104: one more entry after the equip slots for the second job's ultimate, next to the basic skill.
            int total = count + 1;
            var icons = new Image[total];
            var frames = new Image[total];
            var cooldowns = new Image[total];
            var times = new Text[total];
            var locks = new Text[total];
            var punches = new UiPunch[total];
            var readyMarks = new GameObject[total];
            Sprite lockSprite = UiSkin.Icon("lock");
            float firstX = (1080f - (count - 1) * slotStep) / 2f;
            for (int i = 0; i < total; i++)
            {
                bool ultimate = i == count;
                RectTransform slot;
                Vector2 inner;
                if (ultimate)
                {
                    slot = Rect(UltimateSkillName, hud, 0f, PanelTop, 0f, PanelTop);
                    slot.pivot = new Vector2(0f, 0f);
                    slot.anchoredPosition = new Vector2(150f, 18f + slotSize + 26f);
                    slot.sizeDelta = new Vector2(104f, 104f);
                    inner = new Vector2(80f, 80f);
                }
                else
                {
                    slot = Box("Slot" + i, bar, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(firstX + i * slotStep, 0f), new Vector2(slotSize, slotSize));
                    inner = new Vector2(100f, 100f);
                }

                frames[i] = slot.gameObject.AddComponent<Image>();
                UiSkin.Sliced(frames[i], UiSkin.SlotDark);
                Button button = slot.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                if (battleHud != null) UnityEventTools.AddIntPersistentListener(button.onClick, battleHud.CastSlot, i);
                punches[i] = slot.gameObject.AddComponent<UiPunch>();

                icons[i] = Plain(Box("Icon", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, inner), null);

                RectTransform overlay = Box("Cooldown", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, inner + new Vector2(8f, 8f));
                cooldowns[i] = overlay.gameObject.AddComponent<Image>();
                cooldowns[i].sprite = UiSkin.Veil;
                cooldowns[i].color = new Color(0.05f, 0.03f, 0.12f, 0.72f);
                cooldowns[i].type = Image.Type.Filled;
                cooldowns[i].fillMethod = Image.FillMethod.Radial360;
                cooldowns[i].fillOrigin = (int)Image.Origin360.Top;
                cooldowns[i].fillClockwise = false;
                cooldowns[i].fillAmount = 0f;
                cooldowns[i].raycastTarget = false;

                // D-097: the seconds sit in the lower-right corner so the radial sweep and the icon stay readable.
                times[i] = MakeText("Time", slot, 0.25f, 0.02f, 0.96f, 0.5f, "", 36, TextAnchor.LowerRight);

                // D-085 manual mode: a pulsing ring on slots that can be tapped now.
                RectTransform ready = Box("Ready", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slotSize + 30f, slotSize + 30f));
                UiSkin.Sliced(Plain(ready, null), UiSkin.Selection);
                ready.gameObject.AddComponent<UiPulse>();
                ready.gameObject.SetActive(false);
                readyMarks[i] = ready.gameObject;

                locks[i] = MakeText("Lock", slot, 0f, 0.04f, 1f, 0.42f, "", 26, TextAnchor.MiddleCenter);
                RectTransform lockIcon = Rect("LockIcon", locks[i].transform, 0.3f, 1.05f, 0.7f, 2.1f);
                Image lockImage = lockIcon.gameObject.AddComponent<Image>();
                lockImage.sprite = lockSprite;
                lockImage.preserveAspect = true;
                lockImage.raycastTarget = false;
                if (ultimate)
                {
                    RectTransform tagChip = BottomBand("TagChip", slot, -22f, 34f, -20f, -20f);
                    UiSkin.Sliced(Plain(tagChip, null), UiSkin.PlateSprite(Tone.Orange));
                    Text ultTag = AddText(Inset("Tag", tagChip, 0f, 2f, 0f, 0f), 22, TextAnchor.MiddleCenter);
                    UiSkin.ButtonText(ultTag, Tone.Orange);
                    ultTag.verticalOverflow = VerticalWrapMode.Overflow;
                    Localize(ultTag, "hud.ultimate");
                    slot.gameObject.SetActive(false);
                }
            }

            // D-085: AUTO toggle above the last slots; green = auto, gray = manual.
            Button auto = MakeButton(SkillAutoName, hud, 1f, PanelTop, 1f, PanelTop, "", 34, out Text autoLabel, Tone.Green);
            var autoRect = (RectTransform)auto.transform;
            autoRect.pivot = new Vector2(1f, 0f);
            autoRect.anchoredPosition = new Vector2(-24f, 18f + slotSize + 30f);
            autoRect.sizeDelta = new Vector2(196f, 84f);
            IconButton(auto, autoLabel, "auto", 44f);

            // D-093 basic skill: always-on main attack, shown above the first slot with its swing timer.
            RectTransform basic = Rect(BasicSkillName, hud, 0f, PanelTop, 0f, PanelTop);
            SubCanvas(basic.gameObject, false);
            basic.pivot = new Vector2(0f, 0f);
            basic.anchoredPosition = new Vector2(24f, 18f + slotSize + 26f);
            basic.sizeDelta = new Vector2(104f, 104f);
            UiSkin.Sliced(Plain(basic, null), UiSkin.GradeFrame(SoloHero.Core.Gacha.Grade.Rare));
            Image basicIcon = FixedIcon(basic, "basic", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 64f);
            Image basicCooldown = Box("Cooldown", basic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f)).gameObject.AddComponent<Image>();
            basicCooldown.sprite = UiSkin.Veil;
            basicCooldown.color = new Color(0.05f, 0.03f, 0.12f, 0.6f);
            basicCooldown.type = Image.Type.Filled;
            basicCooldown.fillMethod = Image.FillMethod.Radial360;
            basicCooldown.fillOrigin = (int)Image.Origin360.Top;
            basicCooldown.fillClockwise = false;
            basicCooldown.raycastTarget = false;
            RectTransform basicChip = BottomBand("TagChip", basic, -22f, 34f, -26f, -26f);
            UiSkin.Sliced(Plain(basicChip, null), UiSkin.PlateSprite(Tone.Blue));
            Text basicTag = AddText(Inset("Tag", basicChip, 0f, 2f, 0f, 0f), 22, TextAnchor.MiddleCenter);
            UiSkin.ButtonText(basicTag, Tone.Blue);
            basicTag.verticalOverflow = VerticalWrapMode.Overflow;
            basicTag.horizontalOverflow = HorizontalWrapMode.Overflow;
            // D-104: BattleHud writes the job's main attack name here.
            basicTag.text = "";
            Localize(autoLabel, "hud.skill_auto");
            if (battleHud != null) UnityEventTools.AddPersistentListener(auto.onClick, battleHud.ToggleSkillAuto);

            if (so == null) return;
            so.FindProperty("_basicCooldown").objectReferenceValue = basicCooldown;
            so.FindProperty("_basicTag").objectReferenceValue = basicTag;
            so.FindProperty("_basicIcon").objectReferenceValue = basicIcon;
            so.FindProperty("_autoImage").objectReferenceValue = auto.GetComponent<Image>();
            so.FindProperty("_autoLabel").objectReferenceValue = autoLabel;
            so.FindProperty("_autoOnSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Green);
            so.FindProperty("_autoOffSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Gray);
            SetArray(so, "_skillReadyMarks", readyMarks);
            so.FindProperty("_emptySlotIcon").objectReferenceValue = UiSkin.Icon("plus");
            so.FindProperty("_skillIconSet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SkillIconSet>(SkillIconsPath);
            so.FindProperty("_gradeFrames").objectReferenceValue = _gradeFrames;
            SetArray(so, "_skillIcons", icons);
            SetArray(so, "_skillFrames", frames);
            SetArray(so, "_skillCooldowns", cooldowns);
            SetArray(so, "_skillTimes", times);
            SetArray(so, "_skillLocks", locks);
            SetArray(so, "_skillPunches", punches);
        }

        /// <summary>D-106: a currency pill in the top bar, x from the left edge, with its icon overlapping the left end.</summary>
        private static void TopPill(RectTransform bar, string name, float x, float width, string icon)
        {
            RectTransform pill = Box(name, bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -30f), new Vector2(width, 58f));
            Image image = pill.gameObject.AddComponent<Image>();
            UiSkin.Sliced(image, UiSkin.Pill);
            image.raycastTarget = false;
            if (icon != null) FixedIcon(pill, icon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), 64f);
        }

        /// <summary>D-106: places a scene HUD text by anchor, position and size, and gives it the outlined HUD style.</summary>
        private static void TopText(Transform hud, string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            Transform t = hud.Find(name);
            if (t == null) return;
            var rect = (RectTransform)t;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = t.GetComponent<Text>();
            if (text == null) return;
            text.font = _font;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            UiSkin.TextShadow(text);
        }

        private static void StyleExisting(Transform hud, string path, Tone tone, int fontSize)
        {
            Transform t = hud.Find(path);
            Button button = t != null ? t.GetComponent<Button>() : null;
            if (button == null) return;
            UiSkin.Button(button, tone);
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            label.font = _font;
            label.fontSize = fontSize;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.rectTransform.offsetMin = new Vector2(8f, 12f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            UiSkin.ButtonText(label, tone);
        }

        private static Text FindLabel(Transform parent, string path)
        {
            Transform t = parent.Find(path);
            return t != null ? t.GetComponentInChildren<Text>(true) : null;
        }

        /// <summary>Stores only the key in the scene; LocalizedText fills the Korean value at runtime (E7-17).</summary>
        private static void Localize(Text label, string key)
        {
            if (label == null) return;
            LocalizedText localized = label.GetComponent<LocalizedText>();
            if (localized == null) localized = label.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
            label.text = key;
        }

        private static void SetAnchors(Transform parent, string child, float xMin, float yMin, float xMax, float yMax)
        {
            Transform t = parent.Find(child);
            if (t == null) return;
            var rect = (RectTransform)t;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// D-108: a nested canvas, so the elements that change every frame (cooldown sweeps, pulses, the turning summon
        /// circle, damage numbers) re-batch only their own small canvas instead of the whole HUD. Interactive ones need
        /// their own raycaster.
        /// </summary>
        private static void SubCanvas(GameObject go, bool raycast)
        {
            if (go.GetComponent<Canvas>() == null) go.AddComponent<Canvas>();
            if (raycast && go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        }

        /// <summary>A growth panel: the content rect plus a cream back that tucks under the tab bar.</summary>
        private static RectTransform Panel(string name, RectTransform area)
        {
            RectTransform panel = Rect(name, area, 0f, 0f, 1f, 1f);
            RectTransform back = Rect("Back", panel, 0f, 0f, 1f, 1f);
            back.offsetMin = new Vector2(0f, -40f);
            back.offsetMax = new Vector2(0f, 6f);
            Image image = back.gameObject.AddComponent<Image>();
            UiSkin.Sliced(image, UiSkin.Panel);
            panel.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>
        /// D-108 popup: <paramref name="popup"/> is a full-screen dim (tapping it calls <paramref name="close"/>) holding a
        /// fixed-size wood-framed window that pops in, an orange title ribbon over its top edge (none when
        /// <paramref name="titleKey"/> is null) and a red close button on its top-right corner. Returns the window.
        /// </summary>
        private static RectTransform PopupWindow(RectTransform holder, Vector2 size, string titleKey, UnityAction close, out RectTransform popup)
        {
            popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            Image dim = popup.gameObject.AddComponent<Image>();
            dim.color = DimColor;
            if (close != null)
            {
                Button outside = popup.gameObject.AddComponent<Button>();
                outside.transition = Selectable.Transition.None;
                UnityEventTools.AddPersistentListener(outside.onClick, close);
            }

            RectTransform box = Box("Box", popup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), size);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Window);
            box.gameObject.AddComponent<CanvasGroup>();
            box.gameObject.AddComponent<PopupIntro>();
            if (titleKey != null) Ribbon(box, titleKey, Mathf.Min(560f, size.x - 220f));
            if (close != null)
            {
                RectTransform x = Box("Close", box, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, -30f), new Vector2(92f, 92f));
                Image xImage = x.gameObject.AddComponent<Image>();
                xImage.sprite = UiSkin.Hd("hd_close");
                Button xButton = x.gameObject.AddComponent<Button>();
                xButton.targetGraphic = xImage;
                xButton.transition = Selectable.Transition.ColorTint;
                x.gameObject.AddComponent<PressScale>();
                UnityEventTools.AddPersistentListener(xButton.onClick, close);
            }

            return box;
        }

        /// <summary>Orange title ribbon centred on the top edge of a window, with the localized title.</summary>
        private static Text Ribbon(Transform box, string titleKey, float width)
        {
            RectTransform ribbon = Box("Ribbon", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(width, 100f));
            UiSkin.Sliced(Plain(ribbon, null), UiSkin.Ribbon);
            Text title = AddText(Inset("Title", ribbon, 40f, 26f, 40f, 4f), 42, TextAnchor.MiddleCenter);
            title.verticalOverflow = VerticalWrapMode.Overflow;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiSkin.ButtonText(title, Tone.Orange);
            Localize(title, titleKey);
            return title;
        }

        private static Button MakeButton(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
            string text, int size, out Text label, Tone tone = Tone.Green)
        {
            RectTransform rect = Rect(name, parent, xMin, yMin, xMax, yMax);
            rect.gameObject.AddComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            UiSkin.Button(button, tone);
            label = MakeText("Label", rect, 0f, 0f, 1f, 1f, text, size, TextAnchor.MiddleCenter);
            // Keep the label on the face of the button, above its 8 unit lip.
            label.rectTransform.offsetMin = new Vector2(8f, 12f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            // The font's line height is taller than a short button face; truncation would hide the label entirely.
            label.verticalOverflow = VerticalWrapMode.Overflow;
            UiSkin.ButtonText(label, tone);
            return button;
        }

        /// <summary>Icon at a fixed size on the left of the button face; the label centres in the rest.</summary>
        private static void IconButton(Button button, Text label, string icon, float size)
        {
            Transform stale = button.transform.Find("Icon");
            if (stale != null) Object.DestroyImmediate(stale.gameObject);
            FixedIcon(button.transform, icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 4f), size);
            label.rectTransform.offsetMin = new Vector2(16f + size, label.rectTransform.offsetMin.y);
        }

        /// <summary>A UI icon (16 px art) at a fixed size, so it scales by a whole number (32 = 2x, 48 = 3x, 64 = 4x).</summary>
        private static Image FixedIcon(Transform parent, string icon, Vector2 anchor, Vector2 pivot, Vector2 position, float size) =>
            Plain(Box("Icon", parent, anchor, pivot, position, new Vector2(size, size)), UiSkin.Icon(icon));

        /// <summary>A plain, non-interactive image on <paramref name="rect"/>.</summary>
        private static Image Plain(RectTransform rect, Sprite sprite)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Gauge well with an inset fill; returns the fill, whose anchorMax.x is the ratio.</summary>
        private static RectTransform Gauge(RectTransform well, Sprite fillSprite, Sprite wellSprite = null)
        {
            UiSkin.Sliced(well.gameObject.AddComponent<Image>(), wellSprite != null ? wellSprite : UiSkin.Gauge);
            well.GetComponent<Image>().raycastTarget = false;
            RectTransform area = Inset("FillArea", well, 4f, 4f, 4f, 4f);
            RectTransform fill = Rect("Fill", area, 0f, 0f, 0.5f, 1f);
            UiSkin.Sliced(fill.gameObject.AddComponent<Image>(), fillSprite);
            fill.GetComponent<Image>().raycastTarget = false;
            return fill;
        }

        /// <summary>Band pinned to the parent's top edge: <paramref name="top"/> units down, fixed height, side insets.</summary>
        private static RectTransform TopBand(string name, Transform parent, float top, float height, float left, float right)
        {
            RectTransform rect = Rect(name, parent, 0f, 1f, 1f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Band pinned to the parent's bottom edge.</summary>
        private static RectTransform BottomBand(string name, Transform parent, float bottom, float height, float left, float right)
        {
            RectTransform rect = Rect(name, parent, 0f, 0f, 1f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
            return rect;
        }

        /// <summary>Stretches over the parent minus fixed insets.</summary>
        private static RectTransform Inset(string name, Transform parent, float left, float bottom, float right, float top)
        {
            RectTransform rect = Rect(name, parent, 0f, 0f, 1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Fixed-size box whose pivot sits at <paramref name="anchor"/> of the parent, moved by <paramref name="position"/>.</summary>
        private static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(name, parent, anchor.x, anchor.y, anchor.x, anchor.y);
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>Turns an existing rect into a fixed-size box anchored and pivoted at <paramref name="anchor"/>.</summary>
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Text MakeText(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
            string text, int size, TextAnchor anchor)
        {
            Text label = AddText(Rect(name, parent, xMin, yMin, xMax, yMax), size, anchor);
            label.text = text;
            return label;
        }

        /// <summary>A white label with the dark outline and drop shadow (HUD, battle field, buttons) filling <paramref name="rect"/>.</summary>
        private static Text AddText(RectTransform rect, int size, TextAnchor anchor)
        {
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            label.text = "";
            label.raycastTarget = false;
            UiSkin.TextShadow(label);
            return label;
        }

        /// <summary>D-108 lettering on the cream surfaces: coloured, no outline.</summary>
        private static Text InkText(RectTransform rect, int size, TextAnchor anchor, Color color)
        {
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color;
            label.text = "";
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void SetArray(SerializedObject so, string field, string[] values)
        {
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetArray<T>(SerializedObject so, string field, T[] values) where T : Object
        {
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Font LoadFont()
        {
            var ui = AssetDatabase.LoadAssetAtPath<Font>(ArtBuilder.FontPath);
            if (ui != null) return ui;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
