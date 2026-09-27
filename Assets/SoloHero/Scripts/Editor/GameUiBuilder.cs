using SoloHero.Game.Combat;
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
    /// Layout is placeholder uGUI until the E8 art pass; labels are English until the Strings table (E7-17).
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod SoloHero.Editor.GameUiBuilder.BuildBatch
    /// </summary>
    public static class GameUiBuilder
    {
        private const string ScenePath = "Assets/SoloHero/Scenes/Game.unity";
        private const string RootName = "GrowthUI";
        private const float TabTop = 0.07f;
        private const float PanelTop = 0.40f;

        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.11f, 0.94f);
        private static readonly Color RowColor = new Color(0.14f, 0.14f, 0.19f, 1f);
        private static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.3f, 1f);
        private static readonly string[] TabNames = { "Hero", "Gear", "Summon", "Skill" };

        private static Font _font;

        [MenuItem("Tools/Setup/Build Growth UI")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildInScene();
        }

        public static void BuildBatch() => BuildInScene();

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
            Transform old = hud.transform.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            CombatSession session = Object.FindObjectOfType<CombatSession>();
            RectTransform root = Rect(RootName, hud.transform, 0f, 0f, 1f, 1f);
            root.SetAsFirstSibling();
            MoveChallengeButton(hud.transform);

            ToastQueue toast = BuildToast(root);
            RectTransform panelArea = Rect("Panels", root, 0f, TabTop, 1f, PanelTop);

            GameObject character = BuildCharacter(panelArea, session, toast);
            GameObject equipment = BuildEquipment(panelArea, session, toast);
            GameObject gacha = BuildGacha(panelArea, session, toast);
            GameObject skill = BuildSkill(panelArea, session, toast);

            BuildTabs(root, new[] { character, equipment, gacha, skill });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UI] Growth UI rebuilt in " + ScenePath);
        }

        private static void BuildTabs(RectTransform root, GameObject[] panels)
        {
            RectTransform bar = Rect("TabBar", root, 0f, 0f, 1f, TabTop);
            PanelHost host = root.gameObject.AddComponent<PanelHost>();
            var backgrounds = new Image[TabNames.Length];
            float width = 1f / TabNames.Length;
            for (int i = 0; i < TabNames.Length; i++)
            {
                Button button = MakeButton("Tab" + TabNames[i], bar, i * width, 0f, (i + 1) * width, 1f, TabNames[i], 40, out Text _);
                backgrounds[i] = button.GetComponent<Image>();
                UnityEventTools.AddIntPersistentListener(button.onClick, host.Toggle, i);
            }

            var so = new SerializedObject(host);
            SetArray(so, "_panels", panels);
            SetArray(so, "_tabBackgrounds", backgrounds);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildCharacter(RectTransform area, CombatSession session, ToastQueue toast)
        {
            RectTransform panel = Panel("CharacterPanel", area);
            var presenter = panel.gameObject.AddComponent<CharacterPanelPresenter>();
            Text summary = MakeText("Summary", panel, 0.04f, 0.8f, 0.96f, 0.97f, "", 32, TextAnchor.MiddleLeft);

            var levels = new Text[4];
            var costs = new Text[4];
            var buttons = new TapGuardButton[4];
            for (int i = 0; i < 4; i++)
            {
                float top = 0.78f - i * 0.19f;
                Row(panel, "Lane" + i, top - 0.17f, top, out levels[i], out costs[i], out buttons[i]);
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Upgrade, i);
            }

            var so = new SerializedObject(presenter);
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
                Row(panel, "Skill" + i, top - 0.26f, top, out levels[i], out costs[i], out buttons[i]);
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.LevelUp, i);
            }

            var so = new SerializedObject(presenter);
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
            for (int i = 0; i < 4; i++)
            {
                float top = 0.96f - i * 0.24f;
                RectTransform row = Rect("Slot" + i, panel, 0.03f, top - 0.21f, 0.97f, top);
                row.gameObject.AddComponent<Image>().color = RowColor;
                slots[i] = MakeText("Equipped", row, 0.03f, 0.5f, 0.66f, 1f, "", 36, TextAnchor.MiddleLeft);
                owned[i] = MakeText("Owned", row, 0.03f, 0f, 0.66f, 0.5f, "", 28, TextAnchor.MiddleLeft);
                Button button = MakeButton("Swap", row, 0.7f, 0.12f, 0.98f, 0.88f, "Swap", 34, out Text _);
                buttons[i] = button.gameObject.AddComponent<TapGuardButton>();
                UnityEventTools.AddIntPersistentListener(buttons[i].OnTap, presenter.Swap, i);
            }

            var so = new SerializedObject(presenter);
            SetArray(so, "_slotTexts", slots);
            SetArray(so, "_ownedTexts", owned);
            SetArray(so, "_swapButtons", buttons);
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
            RectTransform gauge = Rect("PityGauge", panel, 0.06f, 0.83f, 0.94f, 0.87f);
            gauge.gameObject.AddComponent<Image>().color = RowColor;
            RectTransform fillRect = Rect("Fill", gauge, 0f, 0f, 1f, 1f);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = new Color32(0xFF, 0xC5, 0x31, 0xFF);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;

            Text rates = MakeText("Rates", panel, 0.04f, 0.68f, 0.96f, 0.82f, "", 26, TextAnchor.MiddleCenter);
            Text result = MakeText("Result", panel, 0.04f, 0.24f, 0.96f, 0.67f, "", 26, TextAnchor.UpperCenter);
            result.supportRichText = true;
            result.verticalOverflow = VerticalWrapMode.Overflow;

            TapGuardButton single = GuardButton(panel, "PullOne", 0.03f, 0.35f, out Text singleCost);
            TapGuardButton ten = GuardButton(panel, "PullTen", 0.36f, 0.66f, out Text tenCost);
            TapGuardButton gem = GuardButton(panel, "PullGemTen", 0.69f, 0.97f, out Text gemCost);
            UnityEventTools.AddPersistentListener(single.OnTap, presenter.PullSingle);
            UnityEventTools.AddPersistentListener(ten.OnTap, presenter.PullTen);
            UnityEventTools.AddPersistentListener(gem.OnTap, presenter.PullTenWithGem);

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
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_toast").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel.gameObject;
        }

        private static TapGuardButton GuardButton(RectTransform parent, string name, float xMin, float xMax, out Text label)
        {
            Button button = MakeButton(name, parent, xMin, 0.03f, xMax, 0.21f, "", 32, out label);
            return button.gameObject.AddComponent<TapGuardButton>();
        }

        private static ToastQueue BuildToast(RectTransform root)
        {
            RectTransform box = Rect("Toast", root, 0.15f, 0.51f, 0.85f, 0.56f);
            box.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            Text label = MakeText("Label", box, 0f, 0f, 1f, 1f, "", 34, TextAnchor.MiddleCenter);
            ToastQueue toast = root.gameObject.AddComponent<ToastQueue>();
            var so = new SerializedObject(toast);
            so.FindProperty("_root").objectReferenceValue = box.gameObject;
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return toast;
        }

        private static void Row(RectTransform panel, string name, float yMin, float yMax, out Text level, out Text cost, out TapGuardButton guard)
        {
            RectTransform row = Rect(name, panel, 0.03f, yMin, 0.97f, yMax);
            row.gameObject.AddComponent<Image>().color = RowColor;
            level = MakeText("Level", row, 0.03f, 0f, 0.64f, 1f, "", 36, TextAnchor.MiddleLeft);
            Button button = MakeButton("Buy", row, 0.66f, 0.1f, 0.98f, 0.9f, "", 34, out cost);
            guard = button.gameObject.AddComponent<TapGuardButton>();
        }

        private static void MoveChallengeButton(Transform hud)
        {
            // The old spot (bottom 6-16%) sits under the tab bar; keep boss challenge above the skill buttons.
            Transform challenge = hud.Find("Challenge");
            if (challenge == null) return;
            var rect = (RectTransform)challenge;
            rect.anchorMin = new Vector2(0.2f, 0.51f);
            rect.anchorMax = new Vector2(0.8f, 0.57f);
        }

        private static RectTransform Panel(string name, RectTransform area)
        {
            RectTransform panel = Rect(name, area, 0f, 0f, 1f, 1f);
            panel.gameObject.AddComponent<Image>().color = PanelColor;
            panel.gameObject.SetActive(false);
            return panel;
        }

        private static Button MakeButton(string name, Transform parent, float xMin, float yMin, float xMax, float yMax,
            string text, int size, out Text label)
        {
            RectTransform rect = Rect(name, parent, xMin, yMin, xMax, yMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = MakeText("Label", rect, 0f, 0f, 1f, 1f, text, size, TextAnchor.MiddleCenter);
            return button;
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
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
