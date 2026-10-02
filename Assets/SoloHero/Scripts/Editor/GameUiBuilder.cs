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
    /// bottom tab bar, Character / Equipment / Gacha / Skill panels, toast. Safe to run again (it replaces the branch).
    /// Layout is placeholder uGUI until the E8 art pass; fixed labels carry LocalizedText keys from the Strings table (E7-17).
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
        private const string ArtRootUi = "Assets/SoloHero/Art/UI/";
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

        // Panel layout in canvas units (1080 wide reference; UI art is 4 units per pixel, so steps of 4 stay on grid).
        // The growth panel is the bottom 33% (PanelTop 0.40 minus tabs 0.07) is about 634 units; keep bands inside that.
        private const float PanelPad = 16f;
        private const float SkillCellUnits = 104f;
        private const float SkillIconUnits = 72f;
        private const float SkillGridCell = 136f;
        private const float SkillGridIcon = 96f;
        private const float SlotStep = 112f;
        private const float SkillDetailBottom = 16f;
        private const float SkillDetailHeight = 148f;
        private const int SkillGridColumns = 5;
        // D-089 rails: squares in canvas units (4 per art pixel), under the top bar at 90.5% of the height.
        /// <summary>D-106: the side rails start right under the slim top bar.</summary>
        private const float RailBelowBar = TopBarHeight + 14f;
        private const float RailInset = 16f;
        private const float RailItem = 112f;
        private const float RailStep = 160f;
        private const float RailMenuButton = 96f;
        private const float RailMenuStep = 112f;
        private const float TalentCell = 76f;
        private const float TalentDetailHeight = 124f;
        private const float SkillGridTop = 152f;
        private const string CardBackPath = "Assets/SoloHero/Art/UI/ui_card_back.png";
        private const string CardFacePath = "Assets/SoloHero/Art/UI/ui_card_face.png";
        private const string CirclePath = "Assets/SoloHero/Art/UI/ui_summon_circle.png";
        private const string EquipmentIconsPath = "Assets/SoloHero/Data/Art/EquipmentIcons.asset";
        private const int BurstParticles = 24;
        private static readonly string[] TabIcons = { "crown", "helm", "star", "book", "burst" };
        private static readonly string[] LaneIcons = { "heart", "atk", "def", "spd" };
        private static readonly string[] LaneKeys = { "stat.hp", "stat.atk", "stat.def", "stat.atkspd" };
        private static readonly string[] StatKeys = { "stat.hp", "stat.atk", "stat.def", "stat.atkspd", "stat.crit", "stat.critdmg" };
        private static readonly string[] StatIcons = { "heart", "atk", "def", "spd", "crit", "burst" };
        private static readonly string[] SettingKeys = { "settings.bgm", "settings.sfx", "settings.low_effect", "settings.fps30" };
        private const float TabTop = 0.07f;
        /// <summary>D-106 slim top bar height in canvas units (1080 wide).</summary>
        private const float TopBarHeight = 112f;
        private const float PanelTop = 0.40f;

        private static readonly string[] TabKeys = { "tab.hero", "tab.gear", "tab.summon", "tab.skill", "tab.talent" };
        private const string StringsPath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const string CreditsPath = "Assets/SoloHero/Data/Strings/credits_ko.txt";

        private static Font _font;
        private static GradeFrameSet _gradeFrames;

        [MenuItem("Tools/Setup/Build Growth UI")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildInScene();
        }

        public static void BuildBatch()
        {
            BuildInScene();
            BuildBootScene();
        }

        private const string BootScenePath = "Assets/SoloHero/Scenes/Boot.unity";
        private const string BuildConfigPath = "Assets/SoloHero/Data/Config/BuildConfig.asset";

        [MenuItem("Tools/Setup/Build Boot Ads")]
        public static void BuildBootMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildBootScene();
        }

        /// <summary>
        /// Boot scene: BuildConfig asset, AdService + MainThreadDispatcher on the Boot object, and the
        /// "Ad x2" button next to Claim in the offline popup (E6-09). Safe to run again.
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
            if (panel != null)
            {
                Transform oldDouble = panel.Find("ClaimDouble");
                if (oldDouble != null) Object.DestroyImmediate(oldDouble.gameObject);
                var claim = (RectTransform)panel.Find("Claim");
                if (claim != null)
                {
                    claim.anchorMin = new Vector2(0.06f, 0.1f);
                    claim.anchorMax = new Vector2(0.48f, 0.38f);
                }

                Button doubleButton = MakeButton("ClaimDouble", panel, 0.52f, 0.1f, 0.94f, 0.38f, "", 30, out Text doubleLabel, Tone.Gold);
                IconButton(doubleButton, doubleLabel, "tv");
                UiSkin.Sliced(panel.GetComponent<Image>(), UiSkin.Frame);
                Button claimButton = claim != null ? claim.GetComponent<Button>() : null;
                if (claimButton != null)
                {
                    UiSkin.Button(claimButton, Tone.Green);
                    Text claimLabel = claimButton.GetComponentInChildren<Text>(true);
                    if (claimLabel != null)
                    {
                        claimLabel.fontSize = 44;
                        UiSkin.TextShadow(claimLabel);
                    }
                }
                Text amountText = panel.Find("Amount") != null ? panel.Find("Amount").GetComponent<Text>() : null;
                if (amountText != null)
                {
                    amountText.fontSize = 66;
                    amountText.color = new Color(1f, 0.85f, 0.35f, 1f);
                    UiSkin.TextShadow(amountText);
                }

                // Cap gauge (GDD): "3 h 12 min / 6 h" and a bar, between the amount and the buttons.
                foreach (string stale in new[] { "OfflineTime", "OfflineCap" })
                {
                    Transform t = panel.Find(stale);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }

                var amount = (RectTransform)panel.Find("Amount");
                if (amount != null)
                {
                    amount.anchorMin = new Vector2(0.08f, 0.6f);
                    amount.anchorMax = new Vector2(0.92f, 0.92f);
                }

                Text timeText = MakeText("OfflineTime", panel, 0.06f, 0.48f, 0.94f, 0.6f, "", 30, TextAnchor.MiddleCenter);
                RectTransform cap = Rect("OfflineCap", panel, 0.1f, 0.415f, 0.9f, 0.465f);
                UiSkin.Sliced(cap.gameObject.AddComponent<Image>(), UiSkin.Gauge);
                RectTransform capArea = Rect("FillArea", cap, 0f, 0f, 1f, 1f);
                capArea.offsetMin = new Vector2(4f, 4f);
                capArea.offsetMax = new Vector2(-4f, -4f);
                RectTransform capFill = Rect("Fill", capArea, 0f, 0f, 0.5f, 1f);
                UiSkin.Sliced(capFill.gameObject.AddComponent<Image>(), UiSkin.GaugeFill);
                if (claim != null) Localize(claim.GetComponentInChildren<Text>(true), "offline.claim");
                UnityEventTools.AddPersistentListener(doubleButton.onClick, popup.ClaimDoubled);
                var popupSo = new SerializedObject(popup);
                popupSo.FindProperty("_doubleButton").objectReferenceValue = doubleButton;
                popupSo.FindProperty("_doubleLabel").objectReferenceValue = doubleLabel;
                popupSo.FindProperty("_timeText").objectReferenceValue = timeText;
                popupSo.FindProperty("_capFill").objectReferenceValue = capFill;
                popupSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[UI] OfflineRewardPopup/Panel not found; A-1 button skipped");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UI] Boot ads set up in " + BootScenePath);
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
            MoveChallengeButton(hud.transform);

            ToastQueue toast = BuildToast(root);
            // Under the panels: a dungeon-wall backdrop so the area never shows as an empty black box.
            RectTransform backdrop = Rect("PanelBackdrop", root, 0f, TabTop, 1f, PanelTop);
            Image stone = backdrop.gameObject.AddComponent<Image>();
            stone.sprite = UiSkin.Stone;
            stone.type = Image.Type.Tiled;
            stone.pixelsPerUnitMultiplier = 1f;
            stone.raycastTarget = false;
            RectTransform trim = Rect("Trim", backdrop, 0f, 1f, 1f, 1f);
            trim.offsetMin = new Vector2(0f, -8f);
            Image trimImage = trim.gameObject.AddComponent<Image>();
            trimImage.sprite = UiSkin.White;
            trimImage.color = new Color(0.49f, 0.33f, 0.09f, 1f);
            trimImage.raycastTarget = false;
            RectTransform panelArea = Rect("Panels", root, 0f, TabTop, 1f, PanelTop);

            JobPresenter jobPopup = BuildJobPopup(hud.transform, session, toast);
            GameObject character = BuildCharacter(panelArea, session, toast, jobPopup);
            GameObject equipment = BuildEquipment(panelArea, session, toast);
            GameObject gacha = BuildGacha(panelArea, session, toast);
            GameObject skill = BuildSkill(panelArea, session, toast);
            GameObject talent = BuildTalentPanel(panelArea, session, toast);
            SettingsPresenter settings = BuildSettings(hud.transform);

            BuildTabs(root, new[] { character, equipment, gacha, skill, talent });
            BuildTutorial(root, session, toast);
            BuildDamageText(hud.transform, session);
            GachaRevealView reveal = BuildGachaReveal(hud.transform, gacha.GetComponent<GachaPanelPresenter>());
            StageSelectPresenter stageSelect = BuildStageSelect(hud.transform, session);
            DailyPresenter daily = BuildDaily(hud.transform, session, toast);
            DungeonPresenter dungeon = BuildDungeon(hud.transform, session, toast);
            CompanionPresenter companion = BuildCompanion(hud.transform, session, toast);
            BuildRails(root, toast, settings, stageSelect, daily, dungeon, companion);
            // Built early for the character panel; lift it over the damage numbers like the other popups.
            jobPopup.transform.SetAsLastSibling();
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

        private static void BuildTabs(RectTransform root, GameObject[] panels)
        {
            RectTransform bar = Rect("TabBar", root, 0f, 0f, 1f, TabTop);
            PanelHost host = root.gameObject.AddComponent<PanelHost>();
            var backgrounds = new Image[TabKeys.Length];
            var badges = new GameObject[TabKeys.Length];
            float width = 1f / TabKeys.Length;
            for (int i = 0; i < TabKeys.Length; i++)
            {
                Button button = MakeButton("Tab" + i, bar, i * width, 0f, (i + 1) * width, 1f, "", 28, out Text tabLabel);
                Localize(tabLabel, TabKeys[i]);
                backgrounds[i] = button.GetComponent<Image>();
                UiSkin.Sliced(backgrounds[i], UiSkin.Tab);
                button.transition = Selectable.Transition.None;
                AddIcon(button.transform, TabIcons[i], 0.3f, 0.42f, 0.7f, 0.92f);
                tabLabel.rectTransform.anchorMin = new Vector2(0f, 0.04f);
                tabLabel.rectTransform.anchorMax = new Vector2(1f, 0.44f);
                tabLabel.rectTransform.offsetMin = Vector2.zero;
                tabLabel.rectTransform.offsetMax = Vector2.zero;
                UnityEventTools.AddIntPersistentListener(button.onClick, host.Toggle, i);
                badges[i] = Badge(button.transform);
            }

            // D-103: red dots on the character tab (promotion ready) and the talent tab (points to spend).
            TabBadges tabBadges = bar.gameObject.AddComponent<TabBadges>();
            var badgeSo = new SerializedObject(tabBadges);
            badgeSo.FindProperty("_heroBadge").objectReferenceValue = badges[0];
            badgeSo.FindProperty("_talentBadge").objectReferenceValue = badges[TabKeys.Length - 1];
            badgeSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(host);
            SetArray(so, "_panels", panels);
            SetArray(so, "_tabBackgrounds", backgrounds);
            so.FindProperty("_tabIdleSprite").objectReferenceValue = UiSkin.Tab;
            so.FindProperty("_tabActiveSprite").objectReferenceValue = UiSkin.TabActive;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-106 character panel (Legend of Mushroom style): a header with the job name, level and the job button, six
        /// compact stat chips in three columns, then the four upgrades as a 2x2 grid of cards (icon, name, level,
        /// current to next, green cost button that repeats while held). The portrait lives in the equipment panel.
        /// </summary>
        private static GameObject BuildCharacter(RectTransform area, CombatSession session, ToastQueue toast, JobPresenter jobPopup)
        {
            RectTransform panel = Panel("CharacterPanel", area);
            var presenter = panel.gameObject.AddComponent<CharacterPanelPresenter>();

            // Header: "Pyromancer" + "Lv 55" on a plate, job button on the right.
            RectTransform header = TopBand("Header", panel, PanelPad, 60f, PanelPad, PanelPad);
            RectTransform plate = Inset("Plate", header, 0f, 0f, 240f, 0f);
            UiSkin.Sliced(plate.gameObject.AddComponent<Image>(), UiSkin.Plate);
            plate.GetComponent<Image>().raycastTarget = false;
            Text heroName = AddText(Inset("Name", plate, 16f, 0f, 120f, 0f), 33, TextAnchor.MiddleLeft);
            heroName.horizontalOverflow = HorizontalWrapMode.Overflow;
            heroName.color = new Color(1f, 0.92f, 0.7f, 1f);
            Text heroLevel = AddText(Inset("Level", plate, 0f, 0f, 16f, 0f), 33, TextAnchor.MiddleRight);
            heroLevel.color = new Color(1f, 0.85f, 0.35f, 1f);
            UiPunch levelPunch = plate.gameObject.AddComponent<UiPunch>();
            Button jobButton = MakeButton("Job", header, 1f, 0f, 1f, 1f, "", 28, out Text jobLabel, Tone.Gold);
            RectTransform jobRect = (RectTransform)jobButton.transform;
            jobRect.offsetMin = new Vector2(-228f, 0f);
            jobRect.offsetMax = Vector2.zero;
            TapGuardButton jobGuard = jobButton.gameObject.AddComponent<TapGuardButton>();
            UnityEventTools.AddPersistentListener(jobGuard.OnTap, presenter.OpenJobs);

            // Six stat chips, three columns by two rows.
            RectTransform statArea = TopBand("Stats", panel, PanelPad + 68f, 84f, PanelPad, PanelPad);
            var stats = new Text[StatKeys.Length];
            for (int i = 0; i < StatKeys.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                RectTransform chip = Rect("Stat" + i, statArea, col / 3f, 1f - (row + 1) * 0.5f, (col + 1) / 3f, 1f - row * 0.5f);
                chip.offsetMin = new Vector2(col == 0 ? 0f : 4f, 2f);
                chip.offsetMax = new Vector2(col == 2 ? 0f : -4f, -2f);
                UiSkin.Sliced(chip.gameObject.AddComponent<Image>(), UiSkin.Chip);
                chip.GetComponent<Image>().raycastTarget = false;
                FixedIcon(chip, StatIcons[i], new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), 28f);
                Text label = AddText(Inset("Label", chip, 38f, 0f, 80f, 0f), 22, TextAnchor.MiddleLeft);
                label.color = new Color(0.72f, 0.7f, 0.86f, 1f);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                Localize(label, StatKeys[i]);
                stats[i] = AddText(Inset("Value", chip, 90f, 0f, 8f, 0f), 22, TextAnchor.MiddleRight);
                stats[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            // Upgrades: 2x2 cards.
            RectTransform grid = Inset("Upgrades", panel, PanelPad, PanelPad, PanelPad, PanelPad + 160f);
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
                card.offsetMin = new Vector2(col == 0 ? 0f : 6f, row == 1 ? 0f : 6f);
                card.offsetMax = new Vector2(col == 0 ? -6f : 0f, row == 0 ? 0f : -6f);
                UiSkin.Sliced(card.gameObject.AddComponent<Image>(), UiSkin.Card);
                punches[i] = card.gameObject.AddComponent<UiPunch>();
                FixedIcon(card, LaneIcons[i], new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), 52f);
                Text name = AddText(TopBand("Name", card, 10f, 40f, 76f, 12f), 33, TextAnchor.MiddleLeft);
                Localize(name, LaneKeys[i]);
                levels[i] = AddText(TopBand("Level", card, 10f, 40f, 76f, 14f), 26, TextAnchor.MiddleRight);
                levels[i].color = new Color(1f, 0.85f, 0.35f, 1f);
                // D-106: the current -> next value fills the middle of the card in a large size.
                values[i] = AddText(Inset("Value", card, 12f, 88f, 12f, 60f), 33, TextAnchor.MiddleCenter);
                values[i].supportRichText = true;
                values[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                values[i].color = new Color(0.86f, 0.85f, 0.95f, 1f);

                Button buy = MakeButton("Buy", card, 0f, 0f, 1f, 0f, "", 33, out costs[i]);
                RectTransform buyRect = (RectTransform)buy.transform;
                buyRect.pivot = new Vector2(0.5f, 0f);
                buyRect.offsetMin = new Vector2(12f, 12f);
                buyRect.offsetMax = new Vector2(-12f, 76f);
                IconButton(buy, costs[i], "coin", 32f);
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
        /// D-078 skill book: 6 loadout slots + auto-equip, a 5-column collection (icons at 3x, the rest scrolls),
        /// and a detail card with level-up and equip side by side.
        /// </summary>
        private static GameObject BuildSkill(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("SkillPanel", area);
            var presenter = panel.gameObject.AddComponent<SkillPanelPresenter>();
            var icons = AssetDatabase.LoadAssetAtPath<SkillIconSet>(SkillIconsPath);
            int slotCount = new SoloHero.Core.Config.BalanceValues().SKILL_SLOT_COUNT;

            RectTransform strip = TopBand("Slots", panel, PanelPad, SkillCellUnits, PanelPad, PanelPad);
            var slotCells = new SkillCell[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                RectTransform slot = Box("Slot" + i, strip, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * SlotStep, 0f), new Vector2(SkillCellUnits, SkillCellUnits));
                slotCells[i] = BuildSkillCell(slot, true, SkillIconUnits);
                Button tap = slot.gameObject.AddComponent<Button>();
                tap.transition = Selectable.Transition.None;
                UnityEventTools.AddIntPersistentListener(tap.onClick, presenter.TapSlot, i);
            }

            Button auto = MakeButton("AutoEquip", strip, 0f, 0f, 1f, 1f, "", 33, out Text autoLabel, Tone.Purple);
            var autoRect = (RectTransform)auto.transform;
            autoRect.offsetMin = new Vector2(slotCount * SlotStep + 8f, 16f);
            autoRect.offsetMax = new Vector2(0f, -16f);
            IconButton(auto, autoLabel, "star", 32f);
            Localize(autoLabel, "skill.auto_equip");
            UnityEventTools.AddPersistentListener(auto.onClick, presenter.AutoEquip);

            RectTransform ownedRow = TopBand("Owned", panel, PanelPad + SkillCellUnits + 4f, 24f, PanelPad, PanelPad);
            UiSkin.Sliced(ownedRow.gameObject.AddComponent<Image>(), UiSkin.Chip);
            ownedRow.GetComponent<Image>().raycastTarget = false;
            FixedIcon(ownedRow, "book", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), 32f);
            Text bonus = AddText(Inset("Bonus", ownedRow, 44f, 0f, 240f, 0f), 22, TextAnchor.MiddleLeft);
            bonus.color = new Color(1f, 0.85f, 0.35f, 1f);
            bonus.verticalOverflow = VerticalWrapMode.Overflow;
            Text ownedCount = AddText(Inset("Count", ownedRow, 600f, 0f, 12f, 0f), 22, TextAnchor.MiddleRight);
            ownedCount.verticalOverflow = VerticalWrapMode.Overflow;
            ownedCount.color = new Color(0.72f, 0.7f, 0.86f, 1f);

            RectTransform well = Inset("GridWell", panel, PanelPad, SkillDetailBottom + SkillDetailHeight + 8f, PanelPad, SkillGridTop);
            UiSkin.Sliced(well.gameObject.AddComponent<Image>(), UiSkin.Inset);
            well.GetComponent<Image>().raycastTarget = false;
            RectTransform viewport = Inset("Grid", well, 8f, 8f, 8f, 8f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            RectTransform content = Rect("Content", viewport, 0f, 1f, 1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(SkillGridCell, SkillGridCell);
            grid.spacing = new Vector2(24f, 8f);
            grid.padding = new RectOffset(8, 8, 4, 8);
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

            RectTransform detail = BottomBand("Detail", panel, SkillDetailBottom, SkillDetailHeight, PanelPad, PanelPad);
            UiSkin.Sliced(detail.gameObject.AddComponent<Image>(), UiSkin.Card);
            UiPunch punch = detail.gameObject.AddComponent<UiPunch>();
            RectTransform detailFrameRect = Box("IconFrame", detail, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(96f, 96f));
            Image detailFrame = Plain(detailFrameRect, UiSkin.GradeNone);
            detailFrame.type = Image.Type.Sliced;
            Image detailIcon = Plain(Box("Icon", detailFrameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f)), null);
            RectTransform chip = Box("Grade", detail, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -12f), new Vector2(84f, 32f));
            Image gradeChip = Plain(chip, UiSkin.ChipWhite);
            gradeChip.type = Image.Type.Sliced;
            Text gradeText = AddText(Inset("Label", chip, 0f, 2f, 0f, 0f), 22, TextAnchor.MiddleCenter);
            Text name = AddText(TopBand("Name", detail, 8f, 36f, 212f, 16f), 33, TextAnchor.MiddleLeft);
            name.verticalOverflow = VerticalWrapMode.Overflow;
            Text info = AddText(TopBand("Info", detail, 42f, 24f, 120f, 16f), 22, TextAnchor.MiddleLeft);
            info.verticalOverflow = VerticalWrapMode.Overflow;
            info.color = new Color(0.72f, 0.7f, 0.86f, 1f);
            Text desc = AddText(Inset("Desc", detail, 120f, 60f, 16f, 70f), 22, TextAnchor.UpperLeft);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform actions = BottomBand("Actions", detail, 8f, 48f, 120f, 10f);
            Button level = MakeButton("LevelUp", actions, 0f, 0f, 0.58f, 1f, "", 33, out Text levelCost, Tone.Gold);
            var levelRect = (RectTransform)level.transform;
            levelRect.offsetMax = new Vector2(-6f, 0f);
            IconButton(level, levelCost, "coin", 32f);
            TapGuardButton levelGuard = level.gameObject.AddComponent<TapGuardButton>();
            level.gameObject.AddComponent<HoldRepeat>();
            UnityEventTools.AddPersistentListener(levelGuard.OnTap, presenter.LevelUp);
            Button equip = MakeButton("Equip", actions, 0.58f, 0f, 1f, 1f, "", 33, out Text equipLabel, Tone.Blue);
            var equipRect = (RectTransform)equip.transform;
            equipRect.offsetMin = new Vector2(6f, 0f);
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
        /// Talent page: free points and a gem reset on top, three branch columns (a spine behind four tiers of nodes;
        /// two nodes per tier, one for the lower tiers), and a detail card with the learn button.
        /// </summary>
        private static void BuildTalentPage(RectTransform page, CombatSession session, ToastQueue toast)
        {
            var presenter = page.gameObject.AddComponent<TalentPanelPresenter>();

            RectTransform header = TopBand("Header", page, 8f, 52f, PanelPad, PanelPad);
            Text points = AddText(Inset("Points", header, 8f, 0f, 300f, 0f), 33, TextAnchor.MiddleLeft);
            points.color = new Color(1f, 0.85f, 0.35f, 1f);
            Button reset = MakeButton("Reset", header, 1f, 0f, 1f, 1f, "", 28, out Text resetLabel, Tone.Purple);
            Place((RectTransform)reset.transform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(280f, 52f));
            IconButton(reset, resetLabel, "gem", 32f);
            resetLabel.verticalOverflow = VerticalWrapMode.Overflow;
            TapGuardButton resetGuard = reset.gameObject.AddComponent<TapGuardButton>();
            UnityEventTools.AddPersistentListener(resetGuard.OnTap, presenter.ResetTree);

            float treeTop = 8f + 52f + 8f;
            float treeBottom = SkillDetailBottom + TalentDetailHeight + 8f;
            RectTransform tree = Inset("Tree", page, PanelPad, treeBottom, PanelPad, treeTop);
            var frames = new Image[TalentCatalog.All.Length];
            var icons = new Image[TalentCatalog.All.Length];
            var ranks = new Text[TalentCatalog.All.Length];
            var sprites = new Sprite[TalentCatalog.All.Length];
            var branchTexts = new Text[TalentCatalog.BranchCount];
            for (int b = 0; b < TalentCatalog.BranchCount; b++)
            {
                RectTransform column = Rect("Branch" + b, tree, b / 3f, 0f, (b + 1) / 3f, 1f);
                column.offsetMin = new Vector2(b == 0 ? 0f : 4f, 0f);
                column.offsetMax = new Vector2(b == 2 ? 0f : -4f, 0f);
                UiSkin.Sliced(column.gameObject.AddComponent<Image>(), UiSkin.Inset);
                column.GetComponent<Image>().raycastTarget = false;
                branchTexts[b] = AddText(TopBand("Title", column, 4f, 30f, 4f, 4f), 22, TextAnchor.MiddleCenter);

                RectTransform tiers = Inset("Tiers", column, 0f, 4f, 0f, 38f);
                RectTransform spine = Rect("Spine", tiers, 0.5f, 0.1f, 0.5f, 0.9f);
                spine.offsetMin = new Vector2(-3f, 0f);
                spine.offsetMax = new Vector2(3f, 0f);
                Image spineImage = spine.gameObject.AddComponent<Image>();
                spineImage.sprite = UiSkin.White;
                spineImage.color = new Color(0.3f, 0.28f, 0.4f, 1f);
                spineImage.raycastTarget = false;

                for (int i = 0; i < TalentCatalog.All.Length; i++)
                {
                    TalentDef def = TalentCatalog.All[i];
                    if ((int)def.Branch != b) continue;
                    bool pair = HasPair(def);
                    float x = pair ? (def.Column == 0 ? 0.28f : 0.72f) : 0.5f;
                    float y = 1f - (def.Tier + 0.5f) / TalentCatalog.TierCount;
                    RectTransform cell = Box("Node" + i, tiers, new Vector2(x, y), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TalentCell, TalentCell));
                    frames[i] = cell.gameObject.AddComponent<Image>();
                    UiSkin.Sliced(frames[i], UiSkin.GradeNone);
                    // D-106: 16 px talent icons at 3 units per pixel, the rank under the node instead of over the icon.
                    icons[i] = Plain(Box("Icon", cell, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(48f, 48f)), null);
                    ranks[i] = AddText(BottomBand("Rank", cell, -26f, 26f, -16f, -16f), 22, TextAnchor.MiddleCenter);
                    ranks[i].verticalOverflow = VerticalWrapMode.Overflow;
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

            RectTransform detail = BottomBand("Detail", page, SkillDetailBottom, TalentDetailHeight, PanelPad, PanelPad);
            UiSkin.Sliced(detail.gameObject.AddComponent<Image>(), UiSkin.Card);
            UiPunch punch = detail.gameObject.AddComponent<UiPunch>();
            RectTransform frameRect = Box("IconFrame", detail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(96f, 96f));
            Image detailFrame = Plain(frameRect, UiSkin.GradeNone);
            detailFrame.type = Image.Type.Sliced;
            Image detailIcon = Plain(Box("Icon", frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f)), null);
            Text name = AddText(TopBand("Name", detail, 6f, 44f, 120f, 280f), 33, TextAnchor.MiddleLeft);
            name.verticalOverflow = VerticalWrapMode.Overflow;
            Text rank = AddText(TopBand("Rank", detail, 6f, 44f, 120f, 280f), 22, TextAnchor.MiddleRight);
            rank.color = new Color(1f, 0.85f, 0.35f, 1f);
            Text desc = AddText(Inset("Desc", detail, 120f, 6f, 280f, 52f), 22, TextAnchor.UpperLeft);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            desc.color = new Color(0.86f, 0.85f, 0.95f, 1f);
            Button learn = MakeButton("Learn", detail, 1f, 0.5f, 1f, 0.5f, "", 33, out Text learnLabel, Tone.Green);
            Place((RectTransform)learn.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(248f, 72f));
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
        /// Skill square on <paramref name="rect"/>: grade frame, 3x icon, level badge (bottom right), slot tag (top
        /// left). Slots get a centred lock with the opening level; catalog cells a small lock in the corner.
        /// </summary>
        private static SkillCell BuildSkillCell(RectTransform rect, bool slotLock, float iconSize)
        {
            var cell = new SkillCell();
            cell.Frame = rect.gameObject.AddComponent<Image>();
            UiSkin.Sliced(cell.Frame, UiSkin.GradeNone);
            cell.Icon = Plain(Box("Icon", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize)), null);

            RectTransform badge = Box("Badge", rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(6f, -6f), new Vector2(58f, 30f));
            Plain(badge, UiSkin.Badge).type = Image.Type.Sliced;
            cell.Level = AddText(Inset("Label", badge, 2f, 2f, 2f, 0f), 22, TextAnchor.MiddleCenter);
            cell.Badge = badge.gameObject;

            RectTransform tag = Box("Tag", rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-6f, 6f), new Vector2(30f, 30f));
            Plain(tag, UiSkin.Tag).type = Image.Type.Sliced;
            cell.TagText = AddText(Inset("Label", tag, 0f, 2f, 0f, 0f), 22, TextAnchor.MiddleCenter);
            cell.Tag = tag.gameObject;
            cell.Tag.SetActive(false);

            if (slotLock)
            {
                RectTransform lockRoot = Inset("Lock", rect, 0f, 0f, 0f, 0f);
                FixedIcon(lockRoot, "lock", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), 32f);
                cell.LockText = AddText(BottomBand("Label", lockRoot, 8f, 30f, 0f, 0f), 22, TextAnchor.MiddleCenter);
                cell.LockText.color = new Color(0.72f, 0.7f, 0.86f, 1f);
                cell.Lock = lockRoot.gameObject;
            }
            else
            {
                cell.Lock = FixedIcon(rect, "lock", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 6f), 32f).gameObject;
            }

            return cell;
        }

        /// <summary>
        /// D-106 equipment panel (Legend of Mushroom style): the hero stands on a pedestal in the middle with the sword and
        /// helm slots on the left and the armor and boots slots on the right. A slot shows its grade frame, icon, grade
        /// and +level, and the item's effect under it; tapping it swaps to the next owned grade. Under the stage: ATK /
        /// HP / DEF and an "equip best" button.
        /// </summary>
        private static GameObject BuildEquipment(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("EquipmentPanel", area);
            var presenter = panel.gameObject.AddComponent<EquipmentPanelPresenter>();

            RectTransform stage = Inset("Stage", panel, PanelPad, PanelPad + 96f, PanelPad, PanelPad);
            UiSkin.Sliced(stage.gameObject.AddComponent<Image>(), UiSkin.Portrait);
            stage.GetComponent<Image>().raycastTarget = false;
            stage.gameObject.AddComponent<RectMask2D>();
            Image glow = Plain(Box("Glow", stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(380f, 380f)), UiSkin.Glow);
            glow.color = new Color(1f, 0.82f, 0.45f, 0.45f);
            Plain(Box("Pedestal", stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(288f, 72f)), UiSkin.Pedestal);
            CharacterArt heroArt = AssetDatabase.LoadAssetAtPath<CharacterArt>(HeroArtPath);
            RectTransform heroRect = Box("Hero", stage, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(96f, 96f));
            Plain(heroRect, heroArt != null && heroArt.idle.Length > 0 ? heroArt.idle[0] : null).preserveAspect = false;
            UiFlipbook flipbook = heroRect.gameObject.AddComponent<UiFlipbook>();
            var flipSo = new SerializedObject(flipbook);
            flipSo.FindProperty("_art").objectReferenceValue = heroArt;
            SetArray(flipSo, "_jobs", JobArts());
            flipSo.FindProperty("_scale").floatValue = 5f;
            flipSo.ApplyModifiedPropertiesWithoutUndo();
            Button poke = stage.gameObject.AddComponent<Button>();
            poke.transition = Selectable.Transition.None;
            poke.targetGraphic = stage.GetComponent<Image>();
            UnityEventTools.AddPersistentListener(poke.onClick, flipbook.Poke);

            var slots = new Text[4];
            var owned = new Text[4];
            var buttons = new TapGuardButton[4];
            var icons = new Image[4];
            var frames = new Image[4];
            const float slotSize = 168f;
            for (int i = 0; i < 4; i++)
            {
                bool left = i < 2;
                int row = i % 2;
                // Two rows per side, top row first; the effect line sits under each slot.
                Vector2 anchor = new Vector2(left ? 0f : 1f, 1f);
                Vector2 pos = new Vector2(left ? 56f : -56f, -28f - row * (slotSize + 72f));
                RectTransform frameRect = Box("Slot" + i, stage, anchor, new Vector2(left ? 0f : 1f, 1f), pos, new Vector2(slotSize, slotSize));
                frames[i] = Plain(frameRect, UiSkin.GradeNone);
                frames[i].type = Image.Type.Sliced;
                frames[i].raycastTarget = true;
                icons[i] = Plain(Box("Icon", frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(104f, 104f)), null);
                slots[i] = AddText(Box("Grade", frameRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(slotSize, 34f)), 26, TextAnchor.MiddleCenter);
                slots[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                owned[i] = AddText(Box("Effect", frameRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(slotSize + 100f, 48f)), 26, TextAnchor.UpperCenter);
                owned[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                owned[i].verticalOverflow = VerticalWrapMode.Overflow;
                owned[i].color = new Color(0.8f, 0.92f, 1f, 1f);
                Button button = frameRect.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = frames[i];
                buttons[i] = frameRect.gameObject.AddComponent<TapGuardButton>();
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Swap, i);
            }

            // Under the stage: ATK / HP / DEF chips and the "equip best" button.
            RectTransform footer = BottomBand("Footer", panel, PanelPad, 80f, PanelPad, PanelPad);
            string[] keys = { "stat.atk", "stat.hp", "stat.def" };
            string[] iconNames = { "atk", "heart", "def" };
            var statTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform chip = Rect("Stat" + i, footer, i * 0.22f, 0f, (i + 1) * 0.22f, 1f);
                chip.offsetMin = new Vector2(i == 0 ? 0f : 4f, 8f);
                chip.offsetMax = new Vector2(-4f, -8f);
                UiSkin.Sliced(chip.gameObject.AddComponent<Image>(), UiSkin.Chip);
                chip.GetComponent<Image>().raycastTarget = false;
                FixedIcon(chip, iconNames[i], new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), 32f);
                statTexts[i] = AddText(Inset("Value", chip, 44f, 0f, 10f, 0f), 26, TextAnchor.MiddleRight);
                statTexts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            Button best = MakeButton("EquipBest", footer, 0.67f, 0f, 1f, 1f, "", 30, out Text bestLabel, Tone.Blue);
            Localize(bestLabel, "equip.best");
            UnityEventTools.AddPersistentListener(best.onClick, presenter.EquipBest);

            var so = new SerializedObject(presenter);
            SetArray(so, "_slotTexts", slots);
            SetArray(so, "_ownedTexts", owned);
            SetArray(so, "_swapButtons", buttons);
            SetArray(so, "_slotIcons", icons);
            SetArray(so, "_slotFrames", frames);
            SetArray(so, "_statTexts", statTexts);
            so.FindProperty("_frames").objectReferenceValue = _gradeFrames;
            so.FindProperty("_icons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoloHero.Game.View.EquipmentIconSet>(EquipmentIconsPath);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        private static GameObject BuildGacha(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("GachaPanel", area);
            var presenter = panel.gameObject.AddComponent<GachaPanelPresenter>();

            // D-078: equipment / skill summon tabs.
            var tabImages = new Image[2];
            string[] modeKeys = { "gacha.mode_gear", "gacha.mode_skill" };
            for (int i = 0; i < 2; i++)
            {
                Button tab = MakeButton("Mode" + i, panel, 0.04f + i * 0.465f, 0.885f, 0.495f + i * 0.465f, 0.98f, "", 30, out Text tabLabel);
                tab.transition = Selectable.Transition.None;
                tabImages[i] = tab.GetComponent<Image>();
                UiSkin.Sliced(tabImages[i], UiSkin.Tab);
                Localize(tabLabel, modeKeys[i]);
                UnityEventTools.AddIntPersistentListener(tab.onClick, presenter.SetMode, i);
            }

            Text pity = MakeText("Pity", panel, 0.04f, 0.8f, 0.96f, 0.88f, "", 30, TextAnchor.MiddleCenter);
            RectTransform gauge = Rect("PityGauge", panel, 0.06f, 0.755f, 0.94f, 0.8f);
            UiSkin.Sliced(gauge.gameObject.AddComponent<Image>(), UiSkin.Gauge);
            RectTransform fillArea = Rect("FillArea", gauge, 0f, 0f, 1f, 1f);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);
            RectTransform fillRect = Rect("Fill", fillArea, 0f, 0f, 1f, 1f);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            UiSkin.Sliced(fill, UiSkin.GaugeFill);
            fillRect.anchorMax = new Vector2(0f, 1f);

            Text rates = MakeText("Rates", panel, 0.04f, 0.63f, 0.96f, 0.75f, "", 22, TextAnchor.MiddleCenter);
            // D-103: a softly pulsing summon circle fills the result area until the first pull lands there.
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRootUi + "ui_summon_circle.png");
            if (circle != null)
            {
                RectTransform deco = Box("SummonCircle", panel, new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 220f));
                Image decoImage = Plain(deco, circle);
                decoImage.preserveAspect = true;
                decoImage.color = new Color(1f, 1f, 1f, 0.45f);
                deco.gameObject.AddComponent<UiPulse>();
            }

            Text result = MakeText("Result", panel, 0.04f, 0.33f, 0.96f, 0.62f, "", 33, TextAnchor.MiddleCenter);
            result.supportRichText = true;
            result.verticalOverflow = VerticalWrapMode.Overflow;

            TapGuardButton single = GuardButton(panel, "PullOne", 0.04f, 0.35f, out Text singleCost, Tone.Gold, "coin");
            TapGuardButton ten = GuardButton(panel, "PullTen", 0.36f, 0.66f, out Text tenCost, Tone.Gold, "coin");
            TapGuardButton gem = GuardButton(panel, "PullGemTen", 0.67f, 0.96f, out Text gemCost, Tone.Purple, "gem");
            UnityEventTools.AddPersistentListener(single.OnTap, presenter.PullSingle);
            UnityEventTools.AddPersistentListener(ten.OnTap, presenter.PullTen);
            UnityEventTools.AddPersistentListener(gem.OnTap, presenter.PullTenWithGem);
            Button pack = MakeButton("GoldPack", panel, 0.5f, 0.225f, 0.96f, 0.31f, "", 22, out Text packLabel, Tone.Purple);
            IconButton(pack, packLabel, "gem");
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
            so.FindProperty("_tabIdle").objectReferenceValue = UiSkin.Tab;
            so.FindProperty("_tabActive").objectReferenceValue = UiSkin.TabActive;
            so.FindProperty("_equipmentIcons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoloHero.Game.View.EquipmentIconSet>(EquipmentIconsPath);
            so.FindProperty("_skillIcons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoloHero.Game.View.SkillIconSet>(SkillIconsPath);
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
            set.empty = UiSkin.GradeNone;
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
            dim.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
            Button tap = root.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(tap.onClick, view.Tap);

            RectTransform circle = Rect("Circle", root, 0.5f, 0.55f, 0.5f, 0.55f);
            circle.sizeDelta = new Vector2(640f, 640f);
            Image circleImage = circle.gameObject.AddComponent<Image>();
            circleImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            circleImage.color = new Color(0.75f, 0.65f, 1f, 1f);
            circleImage.raycastTarget = false;

            RectTransform area = Rect("Cards", root, 0.02f, 0.3f, 0.98f, 0.8f);
            Sprite back = AssetDatabase.LoadAssetAtPath<Sprite>(CardBackPath);
            Sprite face = AssetDatabase.LoadAssetAtPath<Sprite>(CardFacePath);
            var cards = new GachaCard[10];
            for (int i = 0; i < cards.Length; i++) cards[i] = BuildCard(area, i, back, face);

            RectTransform burstRect = Rect("Burst", root, 0f, 0f, 1f, 1f);
            UiBurst burst = burstRect.gameObject.AddComponent<UiBurst>();
            var particles = new Image[BurstParticles];
            for (int i = 0; i < BurstParticles; i++)
            {
                RectTransform dot = Rect("P" + i, burstRect, 0.5f, 0.5f, 0.5f, 0.5f);
                dot.sizeDelta = i % 3 == 0 ? new Vector2(18f, 18f) : new Vector2(12f, 12f);
                particles[i] = dot.gameObject.AddComponent<Image>();
                particles[i].raycastTarget = false;
            }

            var burstSo = new SerializedObject(burst);
            SetArray(burstSo, "_particles", particles);
            burstSo.ApplyModifiedPropertiesWithoutUndo();

            Button skip = MakeButton("Skip", root, 0.28f, 0.19f, 0.72f, 0.25f, "", 32, out Text skipLabel, Tone.Gray);
            Localize(skipLabel, "gacha.skip");
            UnityEventTools.AddPersistentListener(skip.onClick, view.SkipAll);
            Text hint = MakeText("CloseHint", root, 0.1f, 0.2f, 0.9f, 0.25f, "", 34, TextAnchor.MiddleCenter);
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
            image.raycastTarget = false;
            Text grade = MakeText("Grade", rect, 0.04f, 0.74f, 0.96f, 0.94f, "", 30, TextAnchor.MiddleCenter);
            RectTransform iconRect = Rect("Icon", rect, 0.2f, 0.42f, 0.8f, 0.74f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            Text slot = MakeText("Slot", rect, 0.04f, 0.25f, 0.96f, 0.42f, "", 26, TextAnchor.MiddleCenter);
            Text note = MakeText("Note", rect, 0.04f, 0.05f, 0.96f, 0.25f, "", 20, TextAnchor.MiddleCenter);
            foreach (Text t in new[] { grade, slot, note })
            {
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
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
            so.ApplyModifiedPropertiesWithoutUndo();
            rect.gameObject.SetActive(false);
            return card;
        }

        /// <summary>
        /// GDD tab bar: settings is the 5th bottom tab (in thumb reach) with 4 on/off rows and credits. The credits
        /// overlay is a full-screen layer under the HUD root so it covers the whole screen.
        /// </summary>
        /// <summary>
        /// D-089 settings window over everything (dim + framed box), opened from the right menu rail. The presenter
        /// lives on the always-active holder; the window and the credits overlay are its children.
        /// </summary>
        private static SettingsPresenter BuildSettings(Transform hud)
        {
            RectTransform holder = Rect(SettingsWindowName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            SettingsPresenter presenter = holder.gameObject.AddComponent<SettingsPresenter>();
            RectTransform window = Rect("Window", holder, 0f, 0f, 1f, 1f);
            window.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            RectTransform panel = Rect("Box", window, 0.06f, 0.3f, 0.94f, 0.72f);
            UiSkin.Sliced(panel.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Text title = MakeText("Title", panel, 0.05f, 0.86f, 0.95f, 0.98f, "", 40, TextAnchor.MiddleCenter);
            title.verticalOverflow = VerticalWrapMode.Overflow;
            Localize(title, "settings.title");

            var states = new Text[SettingKeys.Length];
            var images = new Image[SettingKeys.Length];
            for (int i = 0; i < SettingKeys.Length; i++)
            {
                float top = 0.84f - i * 0.155f;
                RectTransform row = Rect("Row" + i, panel, 0.05f, top - 0.14f, 0.95f, top);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Slot);
                Text rowLabel = MakeText("Label", row, 0.04f, 0f, 0.6f, 1f, "", 33, TextAnchor.MiddleLeft);
                rowLabel.verticalOverflow = VerticalWrapMode.Overflow;
                Localize(rowLabel, SettingKeys[i]);
                Button toggle = MakeButton("Toggle", row, 0.66f, 0.12f, 0.98f, 0.88f, "", 33, out states[i]);
                images[i] = toggle.GetComponent<Image>();
                UnityEventTools.AddIntPersistentListener(toggle.onClick, presenter.Toggle, i);
            }

            Button close = MakeButton("Close", panel, 0.52f, 0.04f, 0.92f, 0.16f, "", 33, out Text closeLabel, Tone.Green);
            Localize(closeLabel, "settings.close");
            UnityEventTools.AddPersistentListener(close.onClick, presenter.CloseWindow);
            Button credits = MakeButton("Credits", panel, 0.08f, 0.04f, 0.48f, 0.16f, "", 33, out Text creditsLabel, Tone.Gray);
            Localize(creditsLabel, "settings.credits");
            UnityEventTools.AddPersistentListener(credits.onClick, presenter.OpenCredits);

            // Credits: a scrollable text over everything.
            RectTransform creditsPopup = Rect(SettingsPopupName, hud, 0f, 0f, 1f, 1f);
            creditsPopup.SetAsLastSibling();
            creditsPopup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
            RectTransform creditsBox = Rect("Box", creditsPopup, 0.05f, 0.1f, 0.95f, 0.9f);
            UiSkin.Sliced(creditsBox.gameObject.AddComponent<Image>(), UiSkin.Frame);
            RectTransform viewport = Rect("Viewport", creditsBox, 0.04f, 0.12f, 0.96f, 0.97f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport, 0f, 1f, 1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            Text creditsText = content.gameObject.AddComponent<Text>();
            creditsText.font = _font;
            creditsText.fontSize = 26;
            creditsText.color = Color.white;
            creditsText.alignment = TextAnchor.UpperLeft;
            creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
            creditsText.raycastTarget = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = creditsBox.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            Button creditsClose = MakeButton("Close", creditsBox, 0.3f, 0.02f, 0.7f, 0.1f, "", 34, out Text creditsCloseLabel);
            Localize(creditsCloseLabel, "settings.close");
            UnityEventTools.AddPersistentListener(creditsClose.onClick, presenter.CloseCredits);
            creditsPopup.gameObject.SetActive(false);

            var so = new SerializedObject(presenter);
            so.FindProperty("_window").objectReferenceValue = window.gameObject;
            SetArray(so, "_stateTexts", states);
            SetArray(so, "_stateImages", images);
            so.FindProperty("_creditsPopup").objectReferenceValue = creditsPopup.gameObject;
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
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = false;
            RectTransform strip = Rect("Banner", holder, 0f, 0.62f, 1f, 0.76f);
            Image stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.color = new Color(0.45f, 0.08f, 0.1f, 0.92f);
            stripImage.raycastTarget = false;
            Text chapter = MakeText("Chapter", strip, 0f, 0.6f, 1f, 0.95f, "", 34, TextAnchor.MiddleCenter);
            chapter.color = new Color(1f, 0.8f, 0.8f, 1f);
            Text name = MakeText("Name", strip, 0f, 0.05f, 1f, 0.62f, "", 56, TextAnchor.MiddleCenter);
            name.color = new Color(1f, 0.85f, 0.3f, 1f);
            name.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);

            BossIntroBanner banner = holder.gameObject.AddComponent<BossIntroBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_group").objectReferenceValue = group;
            so.FindProperty("_chapterText").objectReferenceValue = chapter;
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>E3-09: the stage label opens a one-chapter sheet of 10 stage buttons (D-072: bosses are not farmable).</summary>
        private static StageSelectPresenter BuildStageSelect(Transform hud, CombatSession session)
        {
            RectTransform holder = Rect(StageSelectName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            StageSelectPresenter presenter = holder.gameObject.AddComponent<StageSelectPresenter>();

            RectTransform popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            popup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform box = Rect("Box", popup, 0.05f, 0.32f, 0.95f, 0.7f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Localize(MakeText("Title", box, 0.05f, 0.86f, 0.95f, 0.98f, "", 40, TextAnchor.MiddleCenter), "stage.title");

            Button prev = MakeButton("Prev", box, 0.05f, 0.7f, 0.2f, 0.84f, "<", 40, out _, Tone.Blue);
            Text chapter = MakeText("Chapter", box, 0.22f, 0.7f, 0.78f, 0.84f, "", 38, TextAnchor.MiddleCenter);
            Button next = MakeButton("Next", box, 0.8f, 0.7f, 0.95f, 0.84f, ">", 40, out _, Tone.Blue);
            UnityEventTools.AddPersistentListener(prev.onClick, presenter.PrevChapter);
            UnityEventTools.AddPersistentListener(next.onClick, presenter.NextChapter);

            var buttons = new Button[10];
            var labels = new Text[10];
            for (int i = 0; i < 10; i++)
            {
                int col = i % 5;
                int row = i / 5;
                float x = 0.04f + col * 0.186f;
                float y = row == 0 ? 0.43f : 0.18f;
                buttons[i] = MakeButton("Stage" + i, box, x, y, x + 0.17f, y + 0.22f, "", 32, out labels[i]);
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, presenter.Pick, i);
            }

            Button close = MakeButton("Close", box, 0.3f, 0.03f, 0.7f, 0.14f, "", 34, out Text closeLabel, Tone.Gray);
            Localize(closeLabel, "stage.close");
            UnityEventTools.AddPersistentListener(close.onClick, presenter.Close);

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_chapterText").objectReferenceValue = chapter;
            SetArray(so, "_stageButtons", buttons);
            SetArray(so, "_stageLabels", labels);
            so.FindProperty("_prev").objectReferenceValue = prev;
            so.FindProperty("_next").objectReferenceValue = next;
            so.FindProperty("_session").objectReferenceValue = session;
            so.ApplyModifiedPropertiesWithoutUndo();
            popup.gameObject.SetActive(false);

            // The stage label in the top bar opens the sheet (GDD: tap the stage bar).
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

            RectTransform popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            popup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform box = Rect("Box", popup, 0.1f, 0.4f, 0.9f, 0.6f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Localize(MakeText("Title", box, 0.05f, 0.55f, 0.95f, 0.95f, "", 40, TextAnchor.MiddleCenter), "quit.title");
            Button cancel = MakeButton("Cancel", box, 0.08f, 0.12f, 0.48f, 0.44f, "", 36, out Text cancelLabel, Tone.Gray);
            Localize(cancelLabel, "quit.cancel");
            Button quit = MakeButton("Quit", box, 0.52f, 0.12f, 0.92f, 0.44f, "", 36, out Text quitLabel, Tone.Red);
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

        private static TapGuardButton GuardButton(RectTransform parent, string name, float xMin, float xMax, out Text label,
            Tone tone = Tone.Green, string icon = null)
        {
            Button button = MakeButton(name, parent, xMin, 0.04f, xMax, 0.21f, "", 30, out label, tone);
            if (icon != null) IconButton(button, label, icon);
            return button.gameObject.AddComponent<TapGuardButton>();
        }

        /// <summary>
        /// D-089 battle-screen rails, genre layout: rewards on the left (A-3 gold booster, A-2 gems: icon squares
        /// with the count or countdown under them), a folding menu on the right (settings, stage, credits). Both
        /// sit in the sky between the top bar and the battle lane (characters stand at 50%, nothing below 64%).
        /// </summary>
        private static void BuildRails(RectTransform root, ToastQueue toast, SettingsPresenter settings, StageSelectPresenter stageSelect, DailyPresenter daily, DungeonPresenter dungeon, CompanionPresenter companion)
        {
            RectTransform left = Box("LeftRail", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(RailInset, -RailBelowBar), new Vector2(RailItem + 48f, RailStep * 2f));
            TapGuardButton booster = RailButton(left, "Booster", 0, "coin", Tone.Gold, true, out Text boosterLabel);
            TapGuardButton gem = RailButton(left, "GemAd", 1, "gem", Tone.Purple, true, out Text gemLabel);
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

            RectTransform right = Box("RightRail", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-RailInset, -RailBelowBar), new Vector2(RailItem + 48f, RailMenuButton + 28f + 6f * RailMenuStep));
            RailMenu menu = right.gameObject.AddComponent<RailMenu>();
            Button toggle = MakeButton("Menu", right, 0.5f, 1f, 0.5f, 1f, "", 22, out _, Tone.Gray);
            Place((RectTransform)toggle.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(RailMenuButton, RailMenuButton));
            FixedIcon(toggle.transform, "menu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), 64f);
            UnityEventTools.AddPersistentListener(toggle.onClick, menu.Toggle);
            var badges = new System.Collections.Generic.List<GameObject> { Badge(toggle.transform) };

            RectTransform column = Rect("Items", right, 0f, 0f, 1f, 1f);
            column.offsetMax = new Vector2(0f, -(RailMenuButton + 8f));
            UiSkin.Sliced(column.gameObject.AddComponent<Image>(), UiSkin.Inset);
            string[] icons = { "star", "crown", "heart", "cog", "skull", "book" };
            string[] keys = { "rail.daily", "rail.dungeon", "rail.companion", "rail.settings", "rail.stage", "rail.credits" };
            UnityAction[] actions = { daily.Open, dungeon.Open, companion.Open, settings.Open, stageSelect.Open, settings.OpenCredits };
            for (int i = 0; i < icons.Length; i++)
            {
                RectTransform item = Box("Item" + i, column, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f - i * RailMenuStep), new Vector2(RailItem, RailMenuStep - 8f));
                Button button = item.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                Image hit = item.gameObject.AddComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0f);
                button.targetGraphic = hit;
                FixedIcon(item, icons[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), 64f);
                Text caption = AddText(BottomBand("Caption", item, 0f, 30f, -20f, -20f), 22, TextAnchor.MiddleCenter);
                caption.verticalOverflow = VerticalWrapMode.Overflow;
                Localize(caption, keys[i]);
                UnityEventTools.AddPersistentListener(button.onClick, actions[i]);
                UnityEventTools.AddPersistentListener(button.onClick, menu.Fold);
                if (i == 0) badges.Add(Badge(item));
            }

            var dailySo = new SerializedObject(daily);
            SetArray(dailySo, "_badges", badges.ToArray());
            dailySo.ApplyModifiedPropertiesWithoutUndo();

            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("_content").objectReferenceValue = column.gameObject;
            menuSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>D-099: a red dot in the top-right corner of a button, shown while a daily reward waits.</summary>
        private static GameObject Badge(Transform parent)
        {
            RectTransform dot = Box("Badge", parent, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(28f, 28f));
            Image image = Plain(dot, UiSkin.White);
            image.color = new Color(0.95f, 0.25f, 0.25f, 1f);
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

            RectTransform popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            popup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform box = Rect("Box", popup, 0.04f, 0.16f, 0.96f, 0.84f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Localize(MakeText("Title", box, 0.05f, 0.92f, 0.95f, 0.99f, "", 42, TextAnchor.MiddleCenter), "daily.title");

            Text attendTitle = MakeText("AttendTitle", box, 0.05f, 0.86f, 0.95f, 0.92f, "", 32, TextAnchor.MiddleLeft);
            var cells = new Image[7];
            var icons = new Image[7];
            var amounts = new Text[7];
            var checks = new GameObject[7];
            Sprite check = UiSkin.Icon("star");
            for (int d = 0; d < 7; d++)
            {
                float x0 = 0.04f + d * 0.1324f;
                RectTransform cell = Rect("Day" + d, box, x0, 0.69f, x0 + 0.124f, 0.855f);
                cells[d] = cell.gameObject.AddComponent<Image>();
                UiSkin.Sliced(cells[d], UiSkin.Card);
                Text dayLabel = MakeText("Label", cell, 0f, 0.72f, 1f, 0.98f, "", 22, TextAnchor.MiddleCenter);
                dayLabel.text = (d + 1).ToString();
                icons[d] = Plain(Box("Icon", cell, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(52f, 52f)), null);
                amounts[d] = MakeText("Amount", cell, 0f, 0.02f, 1f, 0.3f, "", 22, TextAnchor.MiddleCenter);
                RectTransform mark = Box("Done", cell, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-2f, -2f), new Vector2(40f, 40f));
                Plain(mark, check);
                mark.gameObject.SetActive(false);
                checks[d] = mark.gameObject;
            }

            Button attend = MakeButton("Attend", box, 0.25f, 0.6f, 0.75f, 0.67f, "", 32, out Text attendLabel, Tone.Green);
            UnityEventTools.AddPersistentListener(attend.onClick, presenter.ClaimAttendance);

            Localize(MakeText("MissionsTitle", box, 0.05f, 0.54f, 0.95f, 0.59f, "", 32, TextAnchor.MiddleLeft), "daily.missions");
            var names = new Text[6];
            var rewards = new Text[6];
            var buttons = new Button[6];
            var labels = new Text[6];
            for (int i = 0; i < 6; i++)
            {
                float y1 = 0.535f - i * 0.075f;
                RectTransform row = Rect("Mission" + i, box, 0.04f, y1 - 0.068f, 0.96f, y1);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                names[i] = MakeText("Name", row, 0.04f, 0f, 0.56f, 1f, "", 28, TextAnchor.MiddleLeft);
                AddIcon(row, "gem", 0.57f, 0.2f, 0.64f, 0.8f);
                rewards[i] = MakeText("Reward", row, 0.645f, 0f, 0.74f, 1f, "", 28, TextAnchor.MiddleLeft);
                buttons[i] = MakeButton("Claim", row, 0.75f, 0.12f, 0.98f, 0.88f, "", 26, out labels[i], Tone.Gold);
                UnityEventTools.AddIntPersistentListener(buttons[i].onClick, presenter.ClaimMission, i);
            }

            Button close = MakeButton("Close", box, 0.3f, 0.015f, 0.7f, 0.075f, "", 32, out Text closeLabel, Tone.Gray);
            Localize(closeLabel, "daily.close");
            UnityEventTools.AddPersistentListener(close.onClick, presenter.Close);

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

        /// <summary>D-102 companions popup: four rows (portrait, name and level, attack or unlock stage, equip, level up).</summary>
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
        /// (portrait, name, main attack and mastery, description, pick / confirm button).
        /// </summary>
        private static JobPresenter BuildJobPopup(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(JobName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            JobPresenter presenter = holder.gameObject.AddComponent<JobPresenter>();

            RectTransform popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            popup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform box = Rect("Box", popup, 0.04f, 0.2f, 0.96f, 0.8f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Localize(MakeText("Title", box, 0.05f, 0.91f, 0.95f, 0.98f, "", 42, TextAnchor.MiddleCenter), "job.title");
            Text current = MakeText("Current", box, 0.05f, 0.85f, 0.95f, 0.91f, "", 26, TextAnchor.MiddleCenter);
            current.color = new Color(1f, 0.85f, 0.35f, 1f);

            var cards = new GameObject[3];
            var portraits = new Image[3];
            var names = new Text[3];
            var mains = new Text[3];
            var descs = new Text[3];
            var picks = new TapGuardButton[3];
            var pickLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y1 = 0.84f - i * 0.245f;
                RectTransform row = Rect("Card" + i, box, 0.04f, y1 - 0.235f, 0.96f, y1);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                cards[i] = row.gameObject;
                RectTransform face = Box("Portrait", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(192f, 144f));
                portraits[i] = Plain(face, null);
                portraits[i].preserveAspect = true;
                names[i] = MakeText("Name", row, 0.22f, 0.74f, 0.7f, 0.96f, "", 33, TextAnchor.MiddleLeft);
                names[i].color = new Color(1f, 0.9f, 0.6f, 1f);
                mains[i] = MakeText("Main", row, 0.22f, 0.4f, 0.98f, 0.76f, "", 22, TextAnchor.UpperLeft);
                mains[i].color = new Color(0.6f, 0.9f, 1f, 1f);
                mains[i].verticalOverflow = VerticalWrapMode.Overflow;
                descs[i] = MakeText("Desc", row, 0.22f, 0.04f, 0.7f, 0.42f, "", 22, TextAnchor.UpperLeft);
                descs[i].color = new Color(0.86f, 0.85f, 0.95f, 1f);
                descs[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                descs[i].verticalOverflow = VerticalWrapMode.Overflow;
                Button pick = MakeButton("Pick", row, 0.72f, 0.08f, 0.97f, 0.42f, "", 28, out pickLabels[i], Tone.Gold);
                picks[i] = pick.gameObject.AddComponent<TapGuardButton>();
                UnityEventTools.AddIntPersistentListener(picks[i].OnTap, presenter.Pick, i);
            }

            Button close = MakeButton("Close", box, 0.3f, 0.02f, 0.7f, 0.09f, "", 32, out Text closeLabel, Tone.Gray);
            Localize(closeLabel, "companion.close");
            UnityEventTools.AddPersistentListener(close.onClick, presenter.Close);

            var so = new SerializedObject(presenter);
            so.FindProperty("_popup").objectReferenceValue = popup.gameObject;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.FindProperty("_current").objectReferenceValue = current;
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

        private static CompanionPresenter BuildCompanion(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(CompanionName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            CompanionPresenter presenter = holder.gameObject.AddComponent<CompanionPresenter>();

            RectTransform popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            popup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform box = Rect("Box", popup, 0.04f, 0.22f, 0.96f, 0.78f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Localize(MakeText("Title", box, 0.05f, 0.9f, 0.95f, 0.98f, "", 42, TextAnchor.MiddleCenter), "companion.title");

            var portraits = new Image[4];
            var names = new Text[4];
            var infos = new Text[4];
            var equips = new Button[4];
            var equipLabels = new Text[4];
            var levels = new Button[4];
            var levelLabels = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y1 = 0.88f - i * 0.195f;
                RectTransform row = Rect("Row" + i, box, 0.04f, y1 - 0.18f, 0.96f, y1);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Card);
                RectTransform face = Box("Portrait", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(144f, 144f));
                portraits[i] = Plain(face, null);
                portraits[i].preserveAspect = true;
                names[i] = MakeText("Name", row, 0.2f, 0.52f, 0.62f, 0.92f, "", 30, TextAnchor.MiddleLeft);
                infos[i] = MakeText("Info", row, 0.2f, 0.1f, 0.62f, 0.5f, "", 24, TextAnchor.MiddleLeft);
                equips[i] = MakeButton("Equip", row, 0.63f, 0.54f, 0.97f, 0.92f, "", 26, out equipLabels[i], Tone.Blue);
                UnityEventTools.AddIntPersistentListener(equips[i].onClick, presenter.Equip, i);
                levels[i] = MakeButton("Level", row, 0.63f, 0.1f, 0.97f, 0.48f, "", 26, out levelLabels[i], Tone.Green);
                IconButton(levels[i], levelLabels[i], "coin", 26f);
                levels[i].gameObject.AddComponent<HoldRepeat>();
                UnityEventTools.AddIntPersistentListener(levels[i].onClick, presenter.LevelUp, i);
            }

            Button close = MakeButton("Close", box, 0.3f, 0.02f, 0.7f, 0.09f, "", 32, out Text closeLabel, Tone.Gray);
            Localize(closeLabel, "companion.close");
            UnityEventTools.AddPersistentListener(close.onClick, presenter.Close);

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
        /// D-100 daily dungeons: an entry popup with a gold and an EXP card (title, description, entries left, reward
        /// per kill, enter), and the in-run banner under the top bar (name, seconds left, amount earned).
        /// </summary>
        private static DungeonPresenter BuildDungeon(Transform hud, CombatSession session, ToastQueue toast)
        {
            RectTransform holder = Rect(DungeonName, hud, 0f, 0f, 1f, 1f);
            holder.SetAsLastSibling();
            DungeonPresenter presenter = holder.gameObject.AddComponent<DungeonPresenter>();

            RectTransform banner = Rect("Banner", holder, 0.18f, 0.765f, 0.82f, 0.84f);
            UiSkin.Sliced(banner.gameObject.AddComponent<Image>(), UiSkin.Banner);
            Text bannerTitle = MakeText("Title", banner, 0.05f, 0.5f, 0.6f, 0.95f, "", 30, TextAnchor.MiddleLeft);
            Text bannerTime = MakeText("Time", banner, 0.6f, 0.5f, 0.95f, 0.95f, "", 30, TextAnchor.MiddleRight);
            bannerTime.color = new Color(1f, 0.85f, 0.35f, 1f);
            Text bannerEarned = MakeText("Earned", banner, 0.05f, 0.05f, 0.95f, 0.5f, "", 30, TextAnchor.MiddleCenter);
            bannerEarned.color = new Color(1f, 0.92f, 0.55f, 1f);
            banner.gameObject.SetActive(false);

            RectTransform popup = Rect("Popup", holder, 0f, 0f, 1f, 1f);
            popup.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
            RectTransform box = Rect("Box", popup, 0.04f, 0.26f, 0.96f, 0.74f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Frame);
            Localize(MakeText("Title", box, 0.05f, 0.88f, 0.95f, 0.98f, "", 42, TextAnchor.MiddleCenter), "dungeon.title");

            string[] icons = { "coin", "star" };
            string[] names = { "dungeon.gold", "dungeon.exp" };
            string[] descs = { "dungeon.gold_desc", "dungeon.exp_desc" };
            var tickets = new Text[2];
            var rewards = new Text[2];
            var enters = new Button[2];
            for (int i = 0; i < 2; i++)
            {
                float y1 = 0.86f - i * 0.37f;
                RectTransform card = Rect("Card" + i, box, 0.04f, y1 - 0.34f, 0.96f, y1);
                UiSkin.Sliced(card.gameObject.AddComponent<Image>(), UiSkin.Card);
                AddIcon(card, icons[i], 0.03f, 0.3f, 0.19f, 0.9f);
                Localize(MakeText("Name", card, 0.22f, 0.66f, 0.97f, 0.95f, "", 34, TextAnchor.MiddleLeft), names[i]);
                Text desc = MakeText("Desc", card, 0.22f, 0.38f, 0.97f, 0.66f, "", 24, TextAnchor.MiddleLeft);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Localize(desc, descs[i]);
                rewards[i] = MakeText("Reward", card, 0.22f, 0.08f, 0.62f, 0.36f, "", 26, TextAnchor.MiddleLeft);
                rewards[i].color = new Color(1f, 0.92f, 0.55f, 1f);
                tickets[i] = MakeText("Tickets", card, 0.03f, 0.04f, 0.2f, 0.3f, "", 22, TextAnchor.MiddleCenter);
                enters[i] = MakeButton("Enter", card, 0.64f, 0.08f, 0.97f, 0.36f, "", 30, out Text enterLabel, Tone.Green);
                Localize(enterLabel, "dungeon.enter");
                UnityEventTools.AddIntPersistentListener(enters[i].onClick, presenter.Enter, i);
            }

            Button close = MakeButton("Close", box, 0.3f, 0.02f, 0.7f, 0.1f, "", 32, out Text closeLabel, Tone.Gray);
            Localize(closeLabel, "dungeon.close");
            UnityEventTools.AddPersistentListener(close.onClick, presenter.Close);

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

        /// <summary>One rail square: a coloured button with a 4x icon, an ad badge, and a caption under it.</summary>
        private static TapGuardButton RailButton(RectTransform rail, string name, int index, string icon, Tone tone, bool ad, out Text caption)
        {
            RectTransform slot = Box(name, rail, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -index * RailStep), new Vector2(RailItem + 48f, RailStep));
            Button button = MakeButton("Button", slot, 0.5f, 1f, 0.5f, 1f, "", 22, out Text unused, tone);
            Object.DestroyImmediate(unused.gameObject);
            Place((RectTransform)button.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(RailItem, RailItem));
            FixedIcon(button.transform, icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), 64f);
            if (ad)
            {
                RectTransform badge = Box("Ad", button.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), new Vector2(40f, 40f));
                Plain(badge, UiSkin.Icon("tv"));
            }

            caption = AddText(BottomBand("Caption", slot, 0f, 34f, 0f, 0f), 22, TextAnchor.MiddleCenter);
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            caption.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            return button.gameObject.AddComponent<TapGuardButton>();
        }

        private static void BuildDamageText(Transform hud, CombatSession session)
        {
            // Last sibling: numbers draw over the battle HUD. Raycasts off so they never block taps.
            RectTransform layer = Rect(DamageLayerName, hud, 0f, 0f, 1f, 1f);
            layer.SetAsLastSibling();
            RectTransform templateRect = Rect("DamageTextTemplate", layer, 0.5f, 0.5f, 0.5f, 0.5f);
            templateRect.sizeDelta = new Vector2(360f, 70f);
            // Plate first so it draws under the text; only skill-name labels switch it on.
            RectTransform plate = Box("Plate", templateRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 64f));
            Image plateImage = plate.gameObject.AddComponent<Image>();
            UiSkin.Sliced(plateImage, UiSkin.Banner);
            plateImage.color = new Color(1f, 1f, 1f, 0.92f);
            plateImage.raycastTarget = false;
            plate.gameObject.SetActive(false);
            RectTransform labelRect = Rect("Label", templateRect, 0f, 0f, 1f, 1f);
            Text label = labelRect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = 33;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
            Outline outline = labelRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
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
            so.FindProperty("_normalSize").intValue = 33;
            so.FindProperty("_critSize").intValue = 55;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildTutorial(RectTransform root, CombatSession session, ToastQueue toast)
        {
            RectTransform banner = Rect("TutorialBanner", root, 0.05f, 0.695f, 0.95f, 0.735f);
            UiSkin.Sliced(banner.gameObject.AddComponent<Image>(), UiSkin.Banner);
            Text label = MakeText("Label", banner, 0f, 0f, 1f, 1f, "", 30, TextAnchor.MiddleCenter);
            label.color = new Color(1f, 0.9f, 0.55f, 1f);
            banner.gameObject.SetActive(false);

            TutorialHints hints = root.gameObject.AddComponent<TutorialHints>();
            var so = new SerializedObject(hints);
            so.FindProperty("_banner").objectReferenceValue = banner.gameObject;
            so.FindProperty("_bannerText").objectReferenceValue = label;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.FindProperty("_session").objectReferenceValue = session;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ToastQueue BuildToast(RectTransform root)
        {
            RectTransform box = Rect("Toast", root, 0.18f, 0.74f, 0.82f, 0.785f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Banner);
            Text label = MakeText("Label", box, 0f, 0f, 1f, 1f, "", 34, TextAnchor.MiddleCenter);
            ToastQueue toast = root.gameObject.AddComponent<ToastQueue>();
            var so = new SerializedObject(toast);
            so.FindProperty("_root").objectReferenceValue = box.gameObject;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toast;
        }

        /// <summary>
        /// Portrait layout (E7-01): characters stand on the ground line at 50% of the screen height, so nothing
        /// interactive may sit in 45-62%. Challenge goes into the sky under the ad bar, skills go under the ground.
        /// </summary>
        private static void MoveChallengeButton(Transform hud)
        {
            SetAnchors(hud, "Challenge", 0.3f, 0.80f, 0.7f, 0.845f);
            SetAnchors(hud, "RetreatPrompt", 0.2f, 0.64f, 0.8f, 0.69f);
            Localize(FindLabel(hud, "FailPanel/Retry"), "hud.retry");
            Localize(FindLabel(hud, "FailPanel/Retreat"), "hud.retreat");
            Localize(FindLabel(hud, "Challenge"), "hud.challenge");

            Transform oldBar = hud.Find("TopBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);
            // D-106: a slim strip (TopBarHeight units) instead of the 8.5% band of big pills.
            RectTransform bar = Rect("TopBar", hud, 0f, 1f, 1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -TopBarHeight);
            bar.offsetMax = Vector2.zero;
            Image barImage = bar.gameObject.AddComponent<Image>();
            UiSkin.Sliced(barImage, UiSkin.TopBar);
            barImage.color = new Color(1f, 1f, 1f, 0.9f);
            barImage.raycastTarget = false;
            bar.SetSiblingIndex(1);
            SkinHud(hud, bar);
        }

        /// <summary>
        /// Top bar pills (gold, gems, stage, kills), skinned skill buttons with an icon and a cooldown overlay, the
        /// challenge / fail buttons. The HUD objects come from the original scene, so every added child is replaced.
        /// </summary>
        private static void SkinHud(Transform hud, RectTransform bar)
        {
            BattleHud battleHud = Object.FindObjectOfType<BattleHud>();
            var so = battleHud != null ? new SerializedObject(battleHud) : null;

            // D-106 top bar, left to right: hero portrait (job look) | "Lv 30 Job", HP and EXP gauges | stage and
            // kills | gold and gem pills. Gold / Stage / Kills are scene texts, placed by absolute offsets.
            RectTransform avatar = Box("Avatar", bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, -2f), new Vector2(92f, 92f));
            UiSkin.Sliced(avatar.gameObject.AddComponent<Image>(), UiSkin.Portrait);
            avatar.GetComponent<Image>().raycastTarget = false;
            avatar.gameObject.AddComponent<RectMask2D>();
            CharacterArt heroArt = AssetDatabase.LoadAssetAtPath<CharacterArt>(HeroArtPath);
            RectTransform face = Box("Hero", avatar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(27f, -38f), new Vector2(96f, 96f));
            Plain(face, heroArt != null && heroArt.idle.Length > 0 ? heroArt.idle[0] : null).preserveAspect = false;
            UiFlipbook faceBook = face.gameObject.AddComponent<UiFlipbook>();
            var faceSo = new SerializedObject(faceBook);
            faceSo.FindProperty("_art").objectReferenceValue = heroArt;
            SetArray(faceSo, "_jobs", JobArts());
            // Scale 3 with the foot pivot pushed down and right puts the chibi head in the middle of the frame.
            faceSo.FindProperty("_scale").floatValue = 3f;
            faceSo.ApplyModifiedPropertiesWithoutUndo();

            const float barsX = 112f;
            const float barsW = 330f;
            Text heroName = AddText(Box("HeroName", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(barsX, -6f), new Vector2(barsW, 34f)), 22, TextAnchor.MiddleLeft);
            heroName.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform hpWell = Box("HeroHp", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(barsX, -42f), new Vector2(barsW, 30f));
            RectTransform hpFill = Gauge(hpWell, UiSkin.GaugeRed);
            Text hpText = AddText(Inset("Label", hpWell, 8f, 0f, 10f, 0f), 22, TextAnchor.MiddleRight);
            RectTransform expWell = Box("HeroExp", bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(barsX, -74f), new Vector2(barsW, 24f));
            RectTransform expFill = Gauge(expWell, UiSkin.GaugeBlue);
            Text expTag = AddText(Inset("Tag", expWell, 8f, 0f, 0f, 0f), 22, TextAnchor.MiddleLeft);
            expTag.color = new Color(0.6f, 0.88f, 1f, 1f);
            Localize(expTag, "char.exp");
            Text expText = AddText(Inset("Label", expWell, 0f, 0f, 10f, 0f), 22, TextAnchor.MiddleRight);
            foreach (Text t in new[] { hpText, expTag, expText }) t.verticalOverflow = VerticalWrapMode.Overflow;

            TopText(hud, "Stage", 456f, -4f, 210f, 52f, 40, TextAnchor.MiddleCenter, Color.white);
            TopText(hud, "Kills", 456f, -58f, 210f, 40f, 22, TextAnchor.MiddleCenter, new Color(0.85f, 0.82f, 0.95f, 1f));
            TopPill(bar, "GoldPill", 676f, 214f, "coin");
            TopText(hud, "Gold", 722f, -30f, 162f, 52f, 26, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.4f, 1f));
            TopPill(bar, "GemPill", 900f, 170f, "gem");

            Transform oldGem = hud.Find("GemCount");
            if (oldGem != null) Object.DestroyImmediate(oldGem.gameObject);
            Transform oldBoss = hud.Find("BossBar");
            if (oldBoss != null) Object.DestroyImmediate(oldBoss.gameObject);

            // Boss HP bar: name on the left inside a red gauge, under the ad row; the timer sits above it.
            RectTransform bossBar = Rect("BossBar", hud, 0.18f, 0.762f, 0.82f, 0.8f);
            UiSkin.Sliced(bossBar.gameObject.AddComponent<Image>(), UiSkin.Gauge);
            RectTransform bossArea = Rect("FillArea", bossBar, 0f, 0f, 1f, 1f);
            bossArea.offsetMin = new Vector2(4f, 4f);
            bossArea.offsetMax = new Vector2(-4f, -4f);
            RectTransform bossFill = Rect("Fill", bossArea, 0f, 0f, 1f, 1f);
            UiSkin.Sliced(bossFill.gameObject.AddComponent<Image>(), UiSkin.GaugeRed);
            Text bossName = MakeText("Name", bossBar, 0.03f, 0f, 0.97f, 1f, "", 33, TextAnchor.MiddleLeft);
            foreach (Graphic g in bossBar.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            UiSkin.TextShadow(bossName);
            bossBar.gameObject.SetActive(false);
            if (so != null)
            {
                so.FindProperty("_bossBar").objectReferenceValue = bossBar.gameObject;
                so.FindProperty("_bossFill").objectReferenceValue = bossFill;
                so.FindProperty("_bossName").objectReferenceValue = bossName;
            }
            Text gem = AddText(Box("GemCount", hud, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(946f, -30f), new Vector2(118f, 52f)), 26, TextAnchor.MiddleLeft);
            gem.text = "0";
            gem.color = new Color(0.72f, 0.88f, 1f, 1f);
            if (so != null)
            {
                so.FindProperty("_gemText").objectReferenceValue = gem;
                so.FindProperty("_heroNameText").objectReferenceValue = heroName;
                so.FindProperty("_heroHpFill").objectReferenceValue = hpFill;
                so.FindProperty("_heroHpText").objectReferenceValue = hpText;
                so.FindProperty("_heroExpFill").objectReferenceValue = expFill;
                so.FindProperty("_heroExpText").objectReferenceValue = expText;
            }

            Text timer = hud.Find("BossTimer") != null ? hud.Find("BossTimer").GetComponent<Text>() : null;
            if (timer != null)
            {
                SetAnchors(hud, "BossTimer", 0.35f, 0.80f, 0.65f, 0.85f);
                timer.fontSize = 55;
                timer.color = new Color(1f, 0.55f, 0.45f, 1f);
                timer.alignment = TextAnchor.MiddleCenter;
                UiSkin.TextShadow(timer);
            }

            Text prompt = hud.Find("RetreatPrompt") != null ? hud.Find("RetreatPrompt").GetComponent<Text>() : null;
            UiSkin.TextShadow(prompt);

            StyleExisting(hud, "Challenge", Tone.Gold);
            StyleExisting(hud, "FailPanel/Retry", Tone.Green);
            StyleExisting(hud, "FailPanel/Retreat", Tone.Red);
            Transform fail = hud.Find("FailPanel");
            if (fail != null && fail.GetComponent<Image>() != null) UiSkin.Sliced(fail.GetComponent<Image>(), UiSkin.Frame);

            BuildSkillBar(hud, battleHud, so);
            if (so != null) so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// D-078 skill bar under the ground line: one square per slot with the skill icon in a grade frame, a radial
        /// cooldown with seconds, and a lock with the opening hero level. Tapping casts the slot now.
        /// </summary>
        private static void BuildSkillBar(Transform hud, BattleHud battleHud, SerializedObject so)
        {
            foreach (string stale in new[] { "Skill1", "Skill2", "Skill3", SkillBarName, SkillAutoName, BasicSkillName, UltimateSkillName })
            {
                Transform t = hud.Find(stale);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            int count = new SoloHero.Core.Config.BalanceValues().SKILL_SLOT_COUNT;
            RectTransform bar = Rect(SkillBarName, hud, 0.02f, 0.393f, 0.98f, 0.455f);
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
            for (int i = 0; i < total; i++)
            {
                bool ultimate = i == count;
                float cx = (i + 0.5f) / count;
                RectTransform slot = ultimate ? Rect(UltimateSkillName, hud, 0.02f, 0.458f, 0.02f, 0.458f) : Rect("Slot" + i, bar, cx, 0.5f, cx, 0.5f);
                slot.sizeDelta = new Vector2(120f, 120f);
                if (ultimate)
                {
                    slot.pivot = new Vector2(0f, 0f);
                    slot.anchoredPosition = new Vector2(150f, 0f);
                    slot.sizeDelta = new Vector2(96f, 96f);
                }
                frames[i] = slot.gameObject.AddComponent<Image>();
                UiSkin.Sliced(frames[i], UiSkin.GradeNone);
                Button button = slot.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                if (battleHud != null) UnityEventTools.AddIntPersistentListener(button.onClick, battleHud.CastSlot, i);
                punches[i] = slot.gameObject.AddComponent<UiPunch>();

                Vector2 inner = ultimate ? new Vector2(76f, 76f) : new Vector2(96f, 96f);
                icons[i] = Plain(Box("Icon", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, inner), null);

                RectTransform overlay = Box("Cooldown", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, inner);
                cooldowns[i] = overlay.gameObject.AddComponent<Image>();
                cooldowns[i].sprite = UiSkin.White;
                cooldowns[i].color = new Color(0f, 0f, 0.05f, 0.68f);
                cooldowns[i].type = Image.Type.Filled;
                cooldowns[i].fillMethod = Image.FillMethod.Radial360;
                cooldowns[i].fillOrigin = (int)Image.Origin360.Top;
                cooldowns[i].fillClockwise = false;
                cooldowns[i].fillAmount = 0f;
                cooldowns[i].raycastTarget = false;

                // D-097: the seconds sit small in the lower-right corner so the radial sweep and the icon stay readable.
                times[i] = MakeText("Time", slot, 0.3f, 0.04f, 0.94f, 0.46f, "", 30, TextAnchor.LowerRight);
                times[i].gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);

                // D-085 manual mode: a pulsing bracket on slots that can be tapped now.
                RectTransform ready = Box("Ready", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(136f, 136f));
                UiSkin.Sliced(Plain(ready, null), UiSkin.Selection);
                ready.gameObject.AddComponent<UiPulse>();
                ready.gameObject.SetActive(false);
                readyMarks[i] = ready.gameObject;

                locks[i] = MakeText("Lock", slot, 0f, 0.02f, 1f, 0.45f, "", 22, TextAnchor.MiddleCenter);
                RectTransform lockIcon = Rect("LockIcon", locks[i].transform, 0.3f, 1.05f, 0.7f, 2f);
                Image lockImage = lockIcon.gameObject.AddComponent<Image>();
                lockImage.sprite = lockSprite;
                lockImage.preserveAspect = true;
                lockImage.raycastTarget = false;
                if (ultimate)
                {
                    Text ultTag = AddText(BottomBand("Tag", slot, -18f, 30f, -24f, -24f), 22, TextAnchor.MiddleCenter);
                    ultTag.verticalOverflow = VerticalWrapMode.Overflow;
                    ultTag.color = new Color(1f, 0.75f, 0.35f, 1f);
                    Localize(ultTag, "hud.ultimate");
                    slot.gameObject.SetActive(false);
                }
            }

            // D-085: AUTO toggle on the ground line above the last slots; green = auto, gray = manual.
            Button auto = MakeButton(SkillAutoName, hud, 0.8f, 0.46f, 0.98f, 0.495f, "", 28, out Text autoLabel, Tone.Green);

            // D-093 basic skill: always-on main attack, shown on the same row as AUTO with its swing timer.
            Transform staleBasic = hud.Find(BasicSkillName);
            if (staleBasic != null) Object.DestroyImmediate(staleBasic.gameObject);
            RectTransform basic = Rect(BasicSkillName, hud, 0.02f, 0.458f, 0.02f, 0.458f);
            basic.pivot = new Vector2(0f, 0f);
            basic.sizeDelta = new Vector2(96f, 96f);
            UiSkin.Sliced(Plain(basic, null), UiSkin.GradeFrame(SoloHero.Core.Gacha.Grade.Rare));
            Image basicIcon = FixedIcon(basic, "basic", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 64f);
            Image basicCooldown = Box("Cooldown", basic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76f, 76f)).gameObject.AddComponent<Image>();
            basicCooldown.sprite = UiSkin.White;
            basicCooldown.color = new Color(0f, 0f, 0.05f, 0.6f);
            basicCooldown.type = Image.Type.Filled;
            basicCooldown.fillMethod = Image.FillMethod.Radial360;
            basicCooldown.fillOrigin = (int)Image.Origin360.Top;
            basicCooldown.fillClockwise = false;
            basicCooldown.raycastTarget = false;
            Text basicTag = AddText(BottomBand("Tag", basic, -18f, 30f, -24f, -24f), 22, TextAnchor.MiddleCenter);
            basicTag.verticalOverflow = VerticalWrapMode.Overflow;
            basicTag.color = new Color(1f, 0.9f, 0.55f, 1f);
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
            so.FindProperty("_skillIconSet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoloHero.Game.View.SkillIconSet>(SkillIconsPath);
            so.FindProperty("_gradeFrames").objectReferenceValue = _gradeFrames;
            SetArray(so, "_skillIcons", icons);
            SetArray(so, "_skillFrames", frames);
            SetArray(so, "_skillCooldowns", cooldowns);
            SetArray(so, "_skillTimes", times);
            SetArray(so, "_skillLocks", locks);
            SetArray(so, "_skillPunches", punches);
        }

        /// <summary>D-106: a small currency pill in the top bar, x from the left edge, icon on its left end.</summary>
        private static void TopPill(RectTransform bar, string name, float x, float width, string icon)
        {
            RectTransform pill = Box(name, bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -28f), new Vector2(width, 56f));
            Image image = pill.gameObject.AddComponent<Image>();
            UiSkin.Sliced(image, UiSkin.Pill);
            image.raycastTarget = false;
            if (icon != null) FixedIcon(pill, icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), 40f);
        }

        /// <summary>D-106: moves a scene HUD text into the top bar by absolute offsets from the screen's top-left.</summary>
        private static void TopText(Transform hud, string name, float x, float y, float width, float height, int size, TextAnchor anchor, Color color)
        {
            Transform t = hud.Find(name);
            if (t == null) return;
            var rect = (RectTransform)t;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Text text = t.GetComponent<Text>();
            if (text == null) return;
            text.alignment = anchor;
            text.fontSize = PixelSize(size);
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            UiSkin.TextShadow(text);
        }

        private static void StyleExisting(Transform hud, string path, Tone tone)
        {
            Transform t = hud.Find(path);
            Button button = t != null ? t.GetComponent<Button>() : null;
            if (button == null) return;
            UiSkin.Button(button, tone);
            Text label = button.GetComponentInChildren<Text>(true);
            // Scene buttons came with Unity's dark grey label, which the dark outline turns into a blob.
            if (label != null) label.color = Color.white;
            UiSkin.TextShadow(label);
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
        }

        private static RectTransform Panel(string name, RectTransform area)
        {
            RectTransform panel = Rect(name, area, 0f, 0f, 1f, 1f);
            UiSkin.Sliced(panel.gameObject.AddComponent<Image>(), UiSkin.Frame);
            panel.gameObject.SetActive(false);
            return panel;
        }

        private static Button MakeButton(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
            string text, int size, out Text label, Tone tone = Tone.Green)
        {
            RectTransform rect = Rect(name, parent, xMin, yMin, xMax, yMax);
            rect.gameObject.AddComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            UiSkin.Button(button, tone);
            label = MakeText("Label", rect, 0f, 0f, 1f, 1f, text, size, TextAnchor.MiddleCenter);
            // Keep the label on the face of the button, above its 3 px bottom edge.
            label.rectTransform.offsetMin = new Vector2(8f, 12f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            // The font's line height is taller than a short button face; truncation would hide the label entirely.
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return button;
        }

        /// <summary>A 16 px icon inside a button or row; the label moves right of it.</summary>
        private static Image AddIcon(Transform parent, string icon, float xMin, float yMin, float xMax, float yMax)
        {
            Transform stale = parent.Find("Icon");
            if (stale != null) Object.DestroyImmediate(stale.gameObject);
            RectTransform rect = Rect("Icon", parent, xMin, yMin, xMax, yMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSkin.Icon(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void IconButton(Button button, Text label, string icon)
        {
            AddIcon(button.transform, icon, 0.05f, 0.2f, 0.25f, 0.85f);
            label.rectTransform.anchorMin = new Vector2(0.22f, 0f);
        }

        /// <summary>Icon at a fixed integer scale on the left of the button face; the label centres in the rest.</summary>
        private static void IconButton(Button button, Text label, string icon, float size)
        {
            Transform stale = button.transform.Find("Icon");
            if (stale != null) Object.DestroyImmediate(stale.gameObject);
            FixedIcon(button.transform, icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 4f), size);
            label.rectTransform.offsetMin = new Vector2(18f + size, label.rectTransform.offsetMin.y);
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
        private static RectTransform Gauge(RectTransform well, Sprite fillSprite)
        {
            UiSkin.Sliced(well.gameObject.AddComponent<Image>(), UiSkin.Gauge);
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

        /// <summary>A white UI-font label with the dark outline and drop shadow filling <paramref name="rect"/>.</summary>
        private static Text AddText(RectTransform rect, int size, TextAnchor anchor)
        {
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = PixelSize(size);
            label.alignment = anchor;
            label.color = Color.white;
            label.text = "";
            label.raycastTarget = false;
            UiSkin.TextShadow(label);
            return label;
        }

        /// <summary>
        /// Layout sizes were tuned for Galmuri11, which snapped to an 11 px grid (26-34 -> 33, 36-44 -> 44). The rounded
        /// Jua face reads about 10% smaller at the same size, so the snapped size is scaled up to match.
        /// </summary>
        private static int PixelSize(int size) => Mathf.RoundToInt(Mathf.Max(22, Mathf.CeilToInt((size - 2) / 11f) * 11) * 1.1f);

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
