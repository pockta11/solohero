using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Setup > Create Equipment Assets   — ScriptableObject 장비 16종 생성
/// Tools > Setup > Setup Menu Canvas         — 하단 탭 UI (장비/업그레이드/뽑기) 생성
/// </summary>
public static class MenuSetupWizard
{
    // ── 1. EquipmentData ScriptableObject 생성 ────────────────────

    [MenuItem("Tools/Setup/Create Equipment Assets")]
    public static void CreateEquipmentAssets()
    {
        const string dir = "Assets/ScriptableObjects/Equipment";
        if (!AssetDatabase.IsValidFolder(dir))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Equipment");
        }

        // (name, slot, grade, atk, def, hp, weight)
        var defs = new (string name, EquipmentSlot slot, EquipmentGrade grade,
                        int atk, int def, int hp, float weight)[]
        {
            // Weapon
            ("Iron Sword",   EquipmentSlot.Weapon, EquipmentGrade.Common,    5,  0,   0, 0.20f),
            ("Steel Sword",  EquipmentSlot.Weapon, EquipmentGrade.Rare,     15,  0,   0, 0.45f),
            ("Magic Blade",  EquipmentSlot.Weapon, EquipmentGrade.Epic,     30,  0,   0, 0.68f),
            ("Divine Blade", EquipmentSlot.Weapon, EquipmentGrade.Legendary,60,  5,  50, 0.90f),
            // Helmet
            ("Iron Helm",    EquipmentSlot.Helmet, EquipmentGrade.Common,    0,  3,  20, 0.20f),
            ("Steel Helm",   EquipmentSlot.Helmet, EquipmentGrade.Rare,      0,  8,  50, 0.45f),
            ("Magic Crown",  EquipmentSlot.Helmet, EquipmentGrade.Epic,      5, 15, 100, 0.68f),
            ("Divine Crown", EquipmentSlot.Helmet, EquipmentGrade.Legendary,10, 25, 200, 0.90f),
            // Armor
            ("Iron Armor",   EquipmentSlot.Armor,  EquipmentGrade.Common,    0,  5,  30, 0.20f),
            ("Steel Armor",  EquipmentSlot.Armor,  EquipmentGrade.Rare,      0, 12,  80, 0.45f),
            ("Magic Robe",   EquipmentSlot.Armor,  EquipmentGrade.Epic,      5, 25, 150, 0.68f),
            ("Divine Armor", EquipmentSlot.Armor,  EquipmentGrade.Legendary,10, 40, 300, 0.90f),
            // Boots
            ("Iron Boots",   EquipmentSlot.Boots,  EquipmentGrade.Common,    3,  3,   0, 0.20f),
            ("Steel Boots",  EquipmentSlot.Boots,  EquipmentGrade.Rare,      8,  8,   0, 0.45f),
            ("Magic Boots",  EquipmentSlot.Boots,  EquipmentGrade.Epic,     15, 15,   0, 0.68f),
            ("Divine Boots", EquipmentSlot.Boots,  EquipmentGrade.Legendary,25, 25,  80, 0.90f),
        };

        int created = 0;
        foreach (var d in defs)
        {
            string path = $"{dir}/{d.name.Replace(" ", "_")}.asset";
            if (AssetDatabase.LoadAssetAtPath<EquipmentData>(path) != null)
                continue; // 이미 존재

            var asset = ScriptableObject.CreateInstance<EquipmentData>();
            asset.id            = d.name.Replace(" ", "_").ToLower();
            asset.equipmentName = d.name;
            asset.slot          = d.slot;
            asset.grade         = d.grade;
            asset.attackBonus   = d.atk;
            asset.defenseBonus  = d.def;
            asset.hpBonus       = d.hp;
            asset.weight        = d.weight;

            AssetDatabase.CreateAsset(asset, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MenuSetup] 장비 ScriptableObject {created}개 생성 완료 → {dir}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(dir);
    }

    // ── 2. Canvas_Menu 생성 ───────────────────────────────────────

    [MenuItem("Tools/Setup/Setup Menu Canvas")]
    public static void SetupMenuCanvas()
    {
        var existing = GameObject.Find("Canvas_Menu");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[MenuSetup] 기존 Canvas_Menu 삭제 후 재생성");
        }

        // ── Canvas root ──────────────────────────────────────────
        var root   = new GameObject("Canvas_Menu");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);  // Landscape
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ── 패널 컨테이너 — 가로 화면: 하단 38%, 탭바(72px) 위
        var panelArea = CreateRect(root.transform, "PanelArea",
            new Vector2(0f, 0f), new Vector2(1f, 0.38f),
            new Vector2(0f, 72f), new Vector2(0f, 0f));

        // ── 장비 패널 ─────────────────────────────────────────────
        var equipPanel = BuildEquipmentPanel(panelArea);

        // ── 업그레이드 패널 (플레이스홀더) ────────────────────────
        var upgradePanel = BuildUpgradePanel(panelArea);

        // ── 가챠 패널 ─────────────────────────────────────────────
        var gachaPanel = BuildGachaPanel(panelArea);

        // ── 하단 탭바 ─────────────────────────────────────────────
        var (tabEquip, tabUpgrade, tabGacha) = BuildTabBar(root.transform);

        // ── MainBottomNav 연결 ────────────────────────────────────
        var nav   = root.AddComponent<MainBottomNav>();
        var navSO = new SerializedObject(nav);
        navSO.FindProperty("_equipmentPanel").objectReferenceValue = equipPanel;
        navSO.FindProperty("_upgradePanel").objectReferenceValue   = upgradePanel;
        navSO.FindProperty("_gachaPanel").objectReferenceValue     = gachaPanel;
        navSO.FindProperty("_tabEquipment").objectReferenceValue   = tabEquip;
        navSO.FindProperty("_tabUpgrade").objectReferenceValue     = tabUpgrade;
        navSO.FindProperty("_tabGacha").objectReferenceValue       = tabGacha;
        navSO.ApplyModifiedProperties();

        Debug.Log("[MenuSetup] Canvas_Menu 생성 완료. GachaSystem에 장비 풀 할당 필요.");
    }

    // ── 장비 패널 ─────────────────────────────────────────────────

    static GameObject BuildEquipmentPanel(Transform parent)
    {
        var go = CreatePanelBg(parent, "EquipmentPanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));

        CreateTitle(go.transform, "Equipment");

        // 4 슬롯 배치 — 가로 화면: 1행 4열
        var slotLabels = new[] { "Weapon", "Helmet", "Armor", "Boots" };
        var slotPos    = new[]
        {
            new Vector2(-360f, -80f),
            new Vector2(-120f, -80f),
            new Vector2( 120f, -80f),
            new Vector2( 360f, -80f),
        };

        Image[] slotImgs = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var slot = new GameObject(slotLabels[i] + "Slot");
            slot.transform.SetParent(go.transform, false);
            var r = slot.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = slotPos[i];
            r.sizeDelta = new Vector2(160f, 160f);

            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
            slotImgs[i] = bg;

            CreateTMP(slot.transform, slotLabels[i],
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), 16, new Color(0.8f, 0.8f, 0.8f));
        }

        var sumText = CreateTMP(go.transform, "SummaryText",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 30f), 18, Color.white,
            new Vector2(900f, 40f));
        sumText.text = "Total  ATK +0  DEF +0  HP +0";

        var ui   = go.AddComponent<EquipmentPanelUI>();
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("_weaponIcon").objectReferenceValue = slotImgs[0];
        uiSO.FindProperty("_helmetIcon").objectReferenceValue = slotImgs[1];
        uiSO.FindProperty("_armorIcon").objectReferenceValue  = slotImgs[2];
        uiSO.FindProperty("_bootsIcon").objectReferenceValue  = slotImgs[3];
        uiSO.FindProperty("_summaryText").objectReferenceValue = sumText;
        uiSO.ApplyModifiedProperties();

        return go;
    }

    // ── 업그레이드 패널 (플레이스홀더) ────────────────────────────

    static GameObject BuildUpgradePanel(Transform parent)
    {
        var go = CreatePanelBg(parent, "UpgradePanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
        CreateTitle(go.transform, "Upgrade");
        CreateTMP(go.transform, "PlaceholderText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, 28, new Color(0.5f, 0.5f, 0.5f))
            .text = "Coming Soon";
        return go;
    }

    // ── 가챠 패널 ─────────────────────────────────────────────────

    static GameObject BuildGachaPanel(Transform parent)
    {
        var go = CreatePanelBg(parent, "GachaPanel", new Color(0.08f, 0.06f, 0.15f, 0.95f));

        CreateTitle(go.transform, "Gacha");

        // 가챠 시스템 컴포넌트 (씬 오브젝트로 배치)
        var gacha   = go.AddComponent<GachaSystem>();

        // 정보 텍스트
        var pityTMP = CreateTMP(go.transform, "PityText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -120f), 20, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(600f, 36f));
        pityTMP.text = "Pity: 0 / 100";

        var costTMP = CreateTMP(go.transform, "CostText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -165f), 20, new Color(1f, 0.85f, 0.2f),
            new Vector2(600f, 36f));
        costTMP.text = "x1 100G  /  x10 1000G";

        // 뽑기 버튼들
        var pullBtn  = CreateButton(go.transform, "PullButton",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-160f, -50f), new Vector2(260f, 90f),
            "Pull x1", new Color(0.4f, 0.2f, 0.8f));

        var pull10Btn = CreateButton(go.transform, "Pull10Button",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(160f, -50f), new Vector2(260f, 90f),
            "Pull x10", new Color(0.6f, 0.15f, 0.5f));

        // 결과 카드
        var cardGo = new GameObject("ResultCard");
        cardGo.transform.SetParent(go.transform, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot     = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(0f, 60f);   // 버튼 위에 표시
        cardRect.sizeDelta = new Vector2(480f, 150f);
        var cardBg = cardGo.AddComponent<Image>();
        cardBg.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);

        var nameText  = CreateTMP(cardGo.transform, "ResultName",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), 28, Color.white, new Vector2(440f, 44f));
        var gradeText = CreateTMP(cardGo.transform, "ResultGrade",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -70f), 22, Color.white, new Vector2(440f, 36f));
        var statsText = CreateTMP(cardGo.transform, "ResultStats",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -115f), 18, new Color(0.9f, 0.9f, 0.9f), new Vector2(440f, 36f));
        cardGo.SetActive(false);

        // GachaTabPanel 연결
        var tab   = go.AddComponent<GachaTabPanel>();
        var tabSO = new SerializedObject(tab);
        tabSO.FindProperty("_pullButton").objectReferenceValue   = pullBtn;
        tabSO.FindProperty("_pull10Button").objectReferenceValue = pull10Btn;
        tabSO.FindProperty("_pityText").objectReferenceValue     = pityTMP;
        tabSO.FindProperty("_costText").objectReferenceValue     = costTMP;
        tabSO.FindProperty("_resultCard").objectReferenceValue   = cardGo;
        tabSO.FindProperty("_resultBg").objectReferenceValue     = cardBg;
        tabSO.FindProperty("_resultName").objectReferenceValue   = nameText;
        tabSO.FindProperty("_resultGrade").objectReferenceValue  = gradeText;
        tabSO.FindProperty("_resultStats").objectReferenceValue  = statsText;
        tabSO.ApplyModifiedProperties();

        return go;
    }

    // ── 탭바 ─────────────────────────────────────────────────────

    static (Button equip, Button upgrade, Button gacha) BuildTabBar(Transform root)
    {
        var bar     = new GameObject("BottomTabBar");
        bar.transform.SetParent(root, false);
        var barRect = bar.AddComponent<RectTransform>();
        barRect.anchorMin      = new Vector2(0f, 0f);
        barRect.anchorMax      = new Vector2(1f, 0f);
        barRect.pivot          = new Vector2(0.5f, 0f);
        barRect.offsetMin      = Vector2.zero;
        barRect.offsetMax      = new Vector2(0f, 72f);
        var barBg = bar.AddComponent<Image>();
        barBg.color = new Color(0.1f, 0.08f, 0.18f, 1f);

        var labels = new[] { "Equip", "Upgrade", "Gacha" };
        Button[] btns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var bGo   = new GameObject($"Tab_{labels[i]}");
            bGo.transform.SetParent(bar.transform, false);
            var bRect = bGo.AddComponent<RectTransform>();
            bRect.anchorMin      = new Vector2(i / 3f,       0f);
            bRect.anchorMax      = new Vector2((i + 1) / 3f, 1f);
            bRect.offsetMin      = Vector2.zero;
            bRect.offsetMax      = Vector2.zero;
            var bImg  = bGo.AddComponent<Image>();
            bImg.color = new Color(0.22f, 0.18f, 0.35f, 1f);
            var btn   = bGo.AddComponent<Button>();

            var label = CreateTMP(bGo.transform, "Label",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, 22, Color.white, new Vector2(200f, 50f));
            label.text = labels[i];

            btns[i] = btn;
        }

        return (btns[0], btns[1], btns[2]);
    }

    // ── UI 헬퍼 ──────────────────────────────────────────────────

    static GameObject CreatePanelBg(Transform parent, string name, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static void CreateTitle(Transform parent, string text)
    {
        var t = CreateTMP(parent, "Title",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -50f), 36, Color.white, new Vector2(600f, 60f));
        t.text = text;
    }

    static RectTransform CreateRect(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r    = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
        return r;
    }

    static Button CreateButton(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string label, Color bgColor)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var labelTMP = CreateTMP(go.transform, "Label",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, 24, Color.white, sizeDelta);
        labelTMP.text = label;

        return btn;
    }

    static TextMeshProUGUI CreateTMP(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, float fontSize, Color color,
        Vector2 sizeDelta = default)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta == default ? new Vector2(300f, 40f) : sizeDelta;

        var tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text      = name;
        return tmp;
    }
}
