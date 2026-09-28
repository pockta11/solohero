using SoloHero.Game.Boot;
using SoloHero.Game.Combat;
using SoloHero.Game.Config;
using SoloHero.Game.Infrastructure;
using SoloHero.Game.Pooling;
using SoloHero.Game.UI;
using SoloHero.Game.UI.Common;
using SoloHero.Game.UI.Panels;
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
        private const string QuitPopupName = "QuitConfirm";
        private const string SafeAreaName = "SafeArea";
        private const string StageSelectName = "StageSelect";
        private const string BossIntroName = "BossIntro";
        private const string CardBackPath = "Assets/SoloHero/Art/UI/ui_card_back.png";
        private const string CardFacePath = "Assets/SoloHero/Art/UI/ui_card_face.png";
        private const string CirclePath = "Assets/SoloHero/Art/UI/ui_summon_circle.png";
        private const string EquipmentIconsPath = "Assets/SoloHero/Data/Art/EquipmentIcons.asset";
        private const int BurstParticles = 24;
        private static readonly string[] TabIcons = { "crown", "helm", "star", "book", "cog" };
        private static readonly string[] LaneIcons = { "heart", "atk", "def", "spd" };
        private static readonly string[] SkillIcons = { "strike", "whirl", "cry" };
        private static readonly string[] SettingKeys = { "settings.bgm", "settings.sfx", "settings.low_effect", "settings.fps30" };
        private const float TabTop = 0.07f;
        private const float PanelTop = 0.40f;

        private static readonly string[] TabKeys = { "tab.hero", "tab.gear", "tab.summon", "tab.skill", "tab.settings" };
        private const string StringsPath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const string CreditsPath = "Assets/SoloHero/Data/Strings/credits_ko.txt";

        private static Font _font;

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
                if (claimButton != null) UiSkin.Button(claimButton, Tone.Green);
                Text amountText = panel.Find("Amount") != null ? panel.Find("Amount").GetComponent<Text>() : null;
                if (amountText != null)
                {
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
            MatchWidth(hud);
            UnwrapSafeArea(hud.transform);
            Transform old = hud.transform.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            foreach (string name in new[] { DamageLayerName, RevealName, SettingsButtonName, SettingsPopupName, QuitPopupName, StageSelectName, BossIntroName })
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

            GameObject character = BuildCharacter(panelArea, session, toast);
            GameObject equipment = BuildEquipment(panelArea, session, toast);
            GameObject gacha = BuildGacha(panelArea, session, toast);
            GameObject skill = BuildSkill(panelArea, session, toast);
            SettingsPresenter settings = BuildSettings(panelArea, hud.transform);

            BuildTabs(root, new[] { character, equipment, gacha, skill, settings.gameObject });
            BuildTutorial(root, session, toast);
            BuildDamageText(hud.transform, session);
            BuildAdBar(root, toast);
            GachaRevealView reveal = BuildGachaReveal(hud.transform, gacha.GetComponent<GachaPanelPresenter>());
            StageSelectPresenter stageSelect = BuildStageSelect(hud.transform, session);
            BuildBackKey(hud.transform, reveal, settings, stageSelect, root.GetComponent<PanelHost>());
            BuildBossIntro(hud.transform, session);
            WrapSafeArea(hud.transform);
            // The boss banner sits right above the HUD controls and under numbers and popups.
            Transform bossIntro = hud.transform.Find(BossIntroName);
            if (bossIntro != null) bossIntro.SetSiblingIndex(1);
            WireJuice(hud, toast);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UI] Growth UI rebuilt in " + ScenePath);
        }

        private static void BuildTabs(RectTransform root, GameObject[] panels)
        {
            RectTransform bar = Rect("TabBar", root, 0f, 0f, 1f, TabTop);
            PanelHost host = root.gameObject.AddComponent<PanelHost>();
            var backgrounds = new Image[TabKeys.Length];
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
            }

            var so = new SerializedObject(host);
            SetArray(so, "_panels", panels);
            SetArray(so, "_tabBackgrounds", backgrounds);
            so.FindProperty("_tabIdleSprite").objectReferenceValue = UiSkin.Tab;
            so.FindProperty("_tabActiveSprite").objectReferenceValue = UiSkin.TabActive;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildCharacter(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("CharacterPanel", area);
            var presenter = panel.gameObject.AddComponent<CharacterPanelPresenter>();
            Text summary = MakeText("Summary", panel, 0.06f, 0.8f, 0.94f, 0.96f, "", 30, TextAnchor.MiddleLeft);

            var levels = new Text[4];
            var costs = new Text[4];
            var buttons = new TapGuardButton[4];
            for (int i = 0; i < 4; i++)
            {
                float top = 0.78f - i * 0.19f;
                Row(panel, "Lane" + i, top - 0.17f, top, out levels[i], out costs[i], out buttons[i], LaneIcons[i]);
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Upgrade, i);
            }

            var so = new SerializedObject(presenter);
            SetArray(so, "_punches", RowPunches(levels));
            SetArray(so, "_levelTexts", levels);
            SetArray(so, "_costTexts", costs);
            SetArray(so, "_buttons", buttons);
            so.FindProperty("_summaryText").objectReferenceValue = summary;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        private static GameObject BuildSkill(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("SkillPanel", area);
            var presenter = panel.gameObject.AddComponent<SkillPanelPresenter>();
            var levels = new Text[3];
            var costs = new Text[3];
            var buttons = new TapGuardButton[3];
            for (int i = 0; i < 3; i++)
            {
                float top = 0.95f - i * 0.3f;
                Row(panel, "Skill" + i, top - 0.26f, top, out levels[i], out costs[i], out buttons[i], SkillIcons[i]);
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.LevelUp, i);
            }

            var so = new SerializedObject(presenter);
            SetArray(so, "_punches", RowPunches(levels));
            SetArray(so, "_levelTexts", levels);
            SetArray(so, "_costTexts", costs);
            SetArray(so, "_buttons", buttons);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        private static GameObject BuildEquipment(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("EquipmentPanel", area);
            var presenter = panel.gameObject.AddComponent<EquipmentPanelPresenter>();
            var slots = new Text[4];
            var owned = new Text[4];
            var buttons = new TapGuardButton[4];
            var icons = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float top = 0.96f - i * 0.24f;
                RectTransform row = Rect("Slot" + i, panel, 0.04f, top - 0.21f, 0.96f, top);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Slot);
                RectTransform iconRect = Rect("Icon", row, 0.02f, 0.1f, 0.16f, 0.9f);
                icons[i] = iconRect.gameObject.AddComponent<Image>();
                icons[i].preserveAspect = true;
                icons[i].raycastTarget = false;
                slots[i] = MakeText("Equipped", row, 0.18f, 0.5f, 0.68f, 1f, "", 36, TextAnchor.MiddleLeft);
                owned[i] = MakeText("Owned", row, 0.18f, 0f, 0.68f, 0.5f, "", 28, TextAnchor.MiddleLeft);
                Button button = MakeButton("Swap", row, 0.7f, 0.12f, 0.98f, 0.88f, "", 34, out Text swapLabel, Tone.Blue);
                Localize(swapLabel, "equip.swap");
                buttons[i] = button.gameObject.AddComponent<TapGuardButton>();
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Swap, i);
            }

            var so = new SerializedObject(presenter);
            SetArray(so, "_slotTexts", slots);
            SetArray(so, "_ownedTexts", owned);
            SetArray(so, "_swapButtons", buttons);
            SetArray(so, "_slotIcons", icons);
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

            Text pity = MakeText("Pity", panel, 0.04f, 0.88f, 0.96f, 0.98f, "", 32, TextAnchor.MiddleCenter);
            RectTransform gauge = Rect("PityGauge", panel, 0.06f, 0.825f, 0.94f, 0.875f);
            UiSkin.Sliced(gauge.gameObject.AddComponent<Image>(), UiSkin.Gauge);
            RectTransform fillArea = Rect("FillArea", gauge, 0f, 0f, 1f, 1f);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);
            RectTransform fillRect = Rect("Fill", fillArea, 0f, 0f, 1f, 1f);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            UiSkin.Sliced(fill, UiSkin.GaugeFill);
            fillRect.anchorMax = new Vector2(0f, 1f);

            Text rates = MakeText("Rates", panel, 0.04f, 0.68f, 0.96f, 0.82f, "", 26, TextAnchor.MiddleCenter);
            Text result = MakeText("Result", panel, 0.04f, 0.31f, 0.96f, 0.67f, "", 19, TextAnchor.UpperCenter);
            result.lineSpacing = 0.92f;
            result.supportRichText = true;
            result.verticalOverflow = VerticalWrapMode.Overflow;

            TapGuardButton single = GuardButton(panel, "PullOne", 0.04f, 0.35f, out Text singleCost, Tone.Gold, "coin");
            TapGuardButton ten = GuardButton(panel, "PullTen", 0.36f, 0.66f, out Text tenCost, Tone.Gold, "coin");
            TapGuardButton gem = GuardButton(panel, "PullGemTen", 0.67f, 0.96f, out Text gemCost, Tone.Purple, "gem");
            UnityEventTools.AddPersistentListener(single.OnTap, presenter.PullSingle);
            UnityEventTools.AddPersistentListener(ten.OnTap, presenter.PullTen);
            UnityEventTools.AddPersistentListener(gem.OnTap, presenter.PullTenWithGem);
            Button pack = MakeButton("GoldPack", panel, 0.52f, 0.225f, 0.96f, 0.305f, "", 24, out Text packLabel, Tone.Purple);
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
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        private static UiPunch[] RowPunches(Text[] rowLabels)
        {
            var punches = new UiPunch[rowLabels.Length];
            for (int i = 0; i < rowLabels.Length; i++) punches[i] = rowLabels[i].transform.parent.gameObject.AddComponent<UiPunch>();
            return punches;
        }

        /// <summary>E8-09 gold punch on the HUD gold label, and the level-up toast for CombatFx.</summary>
        private static void WireJuice(GameObject hud, ToastQueue toast)
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
            so.FindProperty("_icons").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SoloHero.Game.View.EquipmentIconSet>(EquipmentIconsPath);
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
        private static SettingsPresenter BuildSettings(RectTransform area, Transform hud)
        {
            RectTransform panel = Panel("SettingsPanel", area);
            SettingsPresenter presenter = panel.gameObject.AddComponent<SettingsPresenter>();

            var states = new Text[SettingKeys.Length];
            var images = new Image[SettingKeys.Length];
            for (int i = 0; i < SettingKeys.Length; i++)
            {
                float top = 0.96f - i * 0.2f;
                RectTransform row = Rect("Row" + i, panel, 0.04f, top - 0.18f, 0.96f, top);
                UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Slot);
                Localize(MakeText("Label", row, 0.04f, 0f, 0.6f, 1f, "", 36, TextAnchor.MiddleLeft), SettingKeys[i]);
                Button toggle = MakeButton("Toggle", row, 0.66f, 0.12f, 0.98f, 0.88f, "", 34, out states[i]);
                images[i] = toggle.GetComponent<Image>();
                UnityEventTools.AddIntPersistentListener(toggle.onClick, presenter.Toggle, i);
            }

            Button credits = MakeButton("Credits", panel, 0.3f, 0.03f, 0.7f, 0.15f, "", 32, out Text creditsLabel, Tone.Gray);
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
            SetArray(so, "_stateTexts", states);
            SetArray(so, "_stateImages", images);
            so.FindProperty("_creditsPopup").objectReferenceValue = creditsPopup.gameObject;
            so.FindProperty("_creditsText").objectReferenceValue = creditsText;
            so.FindProperty("_credits").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(CreditsPath);
            so.FindProperty("_onSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Green);
            so.FindProperty("_offSprite").objectReferenceValue = UiSkin.ButtonSprite(Tone.Gray);
            so.ApplyModifiedPropertiesWithoutUndo();
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
            var outside = new[] { DamageLayerName, RevealName, SettingsPopupName, QuitPopupName, StageSelectName, BossIntroName };
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
        private static void BuildBackKey(Transform hud, GachaRevealView reveal, SettingsPresenter settings, StageSelectPresenter stageSelect, PanelHost panels)
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

        private static void BuildAdBar(RectTransform root, ToastQueue toast)
        {
            // Under the gold / stage / kills row: A-3 booster on the left, A-2 gems on the right.
            RectTransform bar = Rect("AdBar", root, 0.04f, 0.865f, 0.96f, 0.91f);
            Button booster = MakeButton("Booster", bar, 0f, 0f, 0.49f, 1f, "", 26, out Text boosterLabel, Tone.Gold);
            Button gem = MakeButton("GemAd", bar, 0.51f, 0f, 1f, 1f, "", 26, out Text gemLabel, Tone.Purple);
            IconButton(booster, boosterLabel, "tv");
            IconButton(gem, gemLabel, "tv");
            TapGuardButton boosterGuard = booster.gameObject.AddComponent<TapGuardButton>();
            TapGuardButton gemGuard = gem.gameObject.AddComponent<TapGuardButton>();

            AdSlotsPresenter presenter = bar.gameObject.AddComponent<AdSlotsPresenter>();
            UnityEventTools.AddPersistentListener(boosterGuard.OnTap, presenter.WatchBooster);
            UnityEventTools.AddPersistentListener(gemGuard.OnTap, presenter.WatchGem);
            var so = new SerializedObject(presenter);
            so.FindProperty("_gemButton").objectReferenceValue = gemGuard;
            so.FindProperty("_gemLabel").objectReferenceValue = gemLabel;
            so.FindProperty("_boosterButton").objectReferenceValue = boosterGuard;
            so.FindProperty("_boosterLabel").objectReferenceValue = boosterLabel;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildDamageText(Transform hud, CombatSession session)
        {
            // Last sibling: numbers draw over the battle HUD. Raycasts off so they never block taps.
            RectTransform layer = Rect(DamageLayerName, hud, 0f, 0f, 1f, 1f);
            layer.SetAsLastSibling();
            RectTransform templateRect = Rect("DamageTextTemplate", layer, 0.5f, 0.5f, 0.5f, 0.5f);
            templateRect.sizeDelta = new Vector2(360f, 70f);
            Text label = templateRect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = 34;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
            templateRect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            DamageText template = templateRect.gameObject.AddComponent<DamageText>();
            var templateSo = new SerializedObject(template);
            templateSo.FindProperty("_label").objectReferenceValue = label;
            templateSo.ApplyModifiedPropertiesWithoutUndo();
            templateRect.gameObject.SetActive(false);

            DamageTextPool pool = layer.gameObject.AddComponent<DamageTextPool>();
            var so = new SerializedObject(pool);
            so.FindProperty("_template").objectReferenceValue = template;
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_layer").objectReferenceValue = layer;
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
            RectTransform box = Rect("Toast", root, 0.12f, 0.74f, 0.88f, 0.785f);
            UiSkin.Sliced(box.gameObject.AddComponent<Image>(), UiSkin.Banner);
            Text label = MakeText("Label", box, 0f, 0f, 1f, 1f, "", 34, TextAnchor.MiddleCenter);
            ToastQueue toast = root.gameObject.AddComponent<ToastQueue>();
            var so = new SerializedObject(toast);
            so.FindProperty("_root").objectReferenceValue = box.gameObject;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toast;
        }

        private static void Row(RectTransform panel, string name, float yMin, float yMax, out Text level, out Text cost, out TapGuardButton guard,
            string icon = null)
        {
            RectTransform row = Rect(name, panel, 0.04f, yMin, 0.96f, yMax);
            UiSkin.Sliced(row.gameObject.AddComponent<Image>(), UiSkin.Slot);
            float textLeft = 0.03f;
            if (icon != null)
            {
                AddIcon(row, icon, 0.02f, 0.12f, 0.11f, 0.88f);
                textLeft = 0.13f;
            }

            level = MakeText("Level", row, textLeft, 0f, 0.64f, 1f, "", 36, TextAnchor.MiddleLeft);
            Button button = MakeButton("Buy", row, 0.66f, 0.1f, 0.98f, 0.9f, "", 34, out cost);
            IconButton(button, cost, "coin");
            guard = button.gameObject.AddComponent<TapGuardButton>();
        }

        /// <summary>
        /// Portrait layout (E7-01): characters stand on the ground line at 50% of the screen height, so nothing
        /// interactive may sit in 45-62%. Challenge goes into the sky under the ad bar, skills go under the ground.
        /// </summary>
        private static void MoveChallengeButton(Transform hud)
        {
            SetAnchors(hud, "Challenge", 0.3f, 0.80f, 0.7f, 0.845f);
            SetAnchors(hud, "Skill1", 0.04f, 0.395f, 0.32f, 0.438f);
            SetAnchors(hud, "Skill2", 0.36f, 0.395f, 0.64f, 0.438f);
            SetAnchors(hud, "Skill3", 0.68f, 0.395f, 0.96f, 0.438f);
            SetAnchors(hud, "RetreatPrompt", 0.2f, 0.64f, 0.8f, 0.69f);
            Localize(FindLabel(hud, "FailPanel/Retry"), "hud.retry");
            Localize(FindLabel(hud, "FailPanel/Retreat"), "hud.retreat");
            Localize(FindLabel(hud, "Challenge"), "hud.challenge");

            Transform oldBar = hud.Find("TopBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);
            RectTransform bar = Rect("TopBar", hud, 0f, 0.915f, 1f, 1f);
            UiSkin.Sliced(bar.gameObject.AddComponent<Image>(), UiSkin.TopBar);
            bar.GetComponent<Image>().raycastTarget = false;
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

            Pill(bar, "GoldPill", 0.02f, 0.27f, "coin");
            Pill(bar, "GemPill", 0.29f, 0.48f, "gem");
            Pill(bar, "StagePill", 0.5f, 0.72f, null);
            Pill(bar, "KillsPill", 0.74f, 0.98f, "skull");
            HudText(hud, "Gold", 0.1f, 0.265f, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.35f, 1f));
            HudText(hud, "Stage", 0.5f, 0.72f, TextAnchor.MiddleCenter, Color.white);
            HudText(hud, "Kills", 0.79f, 0.97f, TextAnchor.MiddleCenter, Color.white);

            Transform oldGem = hud.Find("GemCount");
            if (oldGem != null) Object.DestroyImmediate(oldGem.gameObject);
            Text gem = MakeText("GemCount", hud, 0.355f, 0.925f, 0.475f, 0.99f, "0", 38, TextAnchor.MiddleLeft);
            gem.color = new Color(0.72f, 0.88f, 1f, 1f);
            if (so != null) so.FindProperty("_gemText").objectReferenceValue = gem;

            Text timer = hud.Find("BossTimer") != null ? hud.Find("BossTimer").GetComponent<Text>() : null;
            if (timer != null)
            {
                SetAnchors(hud, "BossTimer", 0.35f, 0.80f, 0.65f, 0.85f);
                timer.fontSize = 56;
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

            var cooldowns = new Image[3];
            for (int i = 0; i < 3; i++) cooldowns[i] = SkinSkillButton(hud, "Skill" + (i + 1), SkillIcons[i]);
            if (so != null)
            {
                SetArray(so, "_skillCooldowns", cooldowns);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Pill(RectTransform bar, string name, float xMin, float xMax, string icon)
        {
            RectTransform pill = Rect(name, bar, xMin, 0.18f, xMax, 0.9f);
            Image image = pill.gameObject.AddComponent<Image>();
            UiSkin.Sliced(image, UiSkin.Pill);
            image.raycastTarget = false;
            if (icon != null) AddIcon(pill, icon, 0.02f, 0.1f, 0.3f, 0.9f);
        }

        private static void HudText(Transform hud, string name, float xMin, float xMax, TextAnchor anchor, Color color)
        {
            Transform t = hud.Find(name);
            if (t == null) return;
            SetAnchors(hud, name, xMin, 0.925f, xMax, 0.99f);
            var rect = (RectTransform)t;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = t.GetComponent<Text>();
            if (text == null) return;
            text.alignment = anchor;
            text.fontSize = 38;
            text.color = color;
            UiSkin.TextShadow(text);
        }

        private static void StyleExisting(Transform hud, string path, Tone tone)
        {
            Transform t = hud.Find(path);
            Button button = t != null ? t.GetComponent<Button>() : null;
            if (button == null) return;
            UiSkin.Button(button, tone);
            UiSkin.TextShadow(button.GetComponentInChildren<Text>(true));
        }

        private static Image SkinSkillButton(Transform hud, string name, string icon)
        {
            Transform t = hud.Find(name);
            Button button = t != null ? t.GetComponent<Button>() : null;
            if (button == null) return null;
            UiSkin.Button(button, Tone.Blue);
            Transform stale = t.Find("Cooldown");
            if (stale != null) Object.DestroyImmediate(stale.gameObject);

            Text label = t.GetComponentInChildren<Text>(true);
            AddIcon(t, icon, 0.04f, 0.18f, 0.24f, 0.88f);
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(0.24f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 1f);
                label.rectTransform.offsetMin = new Vector2(0f, 10f);
                label.rectTransform.offsetMax = new Vector2(-6f, -2f);
                label.fontSize = 30;
                UiSkin.TextShadow(label);
            }

            RectTransform overlay = Rect("Cooldown", t, 0f, 0f, 1f, 1f);
            overlay.offsetMin = new Vector2(4f, 4f);
            overlay.offsetMax = new Vector2(-4f, -4f);
            Image image = overlay.gameObject.AddComponent<Image>();
            image.sprite = UiSkin.White;
            image.color = new Color(0f, 0f, 0.05f, 0.6f);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Right;
            image.fillAmount = 0f;
            image.raycastTarget = false;
            return image;
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

        private static Text MakeText(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
            string text, int size, TextAnchor anchor)
        {
            RectTransform rect = Rect(name, parent, xMin, yMin, xMax, yMax);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            UiSkin.TextShadow(label);
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

        private static void SetArray<T>(SerializedObject so, string field, T[] values) where T : Object
        {
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Font LoadFont()
        {
            var galmuri = AssetDatabase.LoadAssetAtPath<Font>(ArtBuilder.FontPath);
            if (galmuri != null) return galmuri;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
