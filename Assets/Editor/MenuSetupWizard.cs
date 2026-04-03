using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > Setup > Create Equipment Assets      — ScriptableObject 장비 16종 생성
/// Tools > Setup > Setup Menu Canvas            — 하단 탭 UI (장비/업그레이드/뽑기) 생성
/// Tools > Setup > Create Inventory Slot Prefab — InventoryItemSlotUI 프리팹 생성
/// Tools > Setup > Wire GameManager Stats       — GameManager에 PlayerStatsSO 자동 할당
/// </summary>
public static class MenuSetupWizard
{
    // ── 1. EquipmentData ScriptableObject 생성 ────────────────────

    [MenuItem("Tools/Setup/Create Equipment Assets")]
    public static void CreateEquipmentAssets()
    {
        const string dir = "Assets/ScriptableObjects/Equipment";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Equipment");

        var defs = new (string name, EquipmentSlot slot, EquipmentGrade grade,
                        int atk, int def, int hp, float weight)[]
        {
            ("Iron Sword",   EquipmentSlot.Weapon, EquipmentGrade.Common,    5,  0,   0, 0.20f),
            ("Steel Sword",  EquipmentSlot.Weapon, EquipmentGrade.Rare,     15,  0,   0, 0.45f),
            ("Magic Blade",  EquipmentSlot.Weapon, EquipmentGrade.Epic,     30,  0,   0, 0.68f),
            ("Divine Blade", EquipmentSlot.Weapon, EquipmentGrade.Legendary,60,  5,  50, 0.90f),
            ("Iron Helm",    EquipmentSlot.Helmet, EquipmentGrade.Common,    0,  3,  20, 0.20f),
            ("Steel Helm",   EquipmentSlot.Helmet, EquipmentGrade.Rare,      0,  8,  50, 0.45f),
            ("Magic Crown",  EquipmentSlot.Helmet, EquipmentGrade.Epic,      5, 15, 100, 0.68f),
            ("Divine Crown", EquipmentSlot.Helmet, EquipmentGrade.Legendary,10, 25, 200, 0.90f),
            ("Iron Armor",   EquipmentSlot.Armor,  EquipmentGrade.Common,    0,  5,  30, 0.20f),
            ("Steel Armor",  EquipmentSlot.Armor,  EquipmentGrade.Rare,      0, 12,  80, 0.45f),
            ("Magic Robe",   EquipmentSlot.Armor,  EquipmentGrade.Epic,      5, 25, 150, 0.68f),
            ("Divine Armor", EquipmentSlot.Armor,  EquipmentGrade.Legendary,10, 40, 300, 0.90f),
            ("Iron Boots",   EquipmentSlot.Boots,  EquipmentGrade.Common,    3,  3,   0, 0.20f),
            ("Steel Boots",  EquipmentSlot.Boots,  EquipmentGrade.Rare,      8,  8,   0, 0.45f),
            ("Magic Boots",  EquipmentSlot.Boots,  EquipmentGrade.Epic,     15, 15,   0, 0.68f),
            ("Divine Boots", EquipmentSlot.Boots,  EquipmentGrade.Legendary,25, 25,  80, 0.90f),
        };

        int created = 0;
        foreach (var d in defs)
        {
            string path = $"{dir}/{d.name.Replace(" ", "_")}.asset";
            if (AssetDatabase.LoadAssetAtPath<EquipmentData>(path) != null) continue;

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
        Debug.Log($"[MenuSetup] 장비 ScriptableObject {created}개 생성 → {dir}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(dir);
    }

    // ── 2. InventoryItemSlotUI 프리팹 생성 ────────────────────────

    [MenuItem("Tools/Setup/Create Inventory Slot Prefab")]
    public static InventoryItemSlotUI CreateInventorySlotPrefab()
    {
        const string prefabDir  = "Assets/Prefabs/UI";
        const string prefabPath = prefabDir + "/InventoryItemSlot.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        // ── 슬롯 루트 (Image 배경 + InventoryItemSlotUI) ──────────
        var root     = new GameObject("InventoryItemSlot");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(860f, 110f);

        var rootImg  = root.AddComponent<Image>();
        rootImg.color = new Color(0.20f, 0.20f, 0.22f, 0.95f);

        // 슬롯 레이블 (Sword / Helm …)
        var slotText = CreateTMP(root.transform, "SlotText",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(20f, 0f), 16, new Color(0.6f, 0.6f, 0.6f), new Vector2(100f, 30f));
        slotText.alignment = TextAlignmentOptions.MidlineLeft;

        // 장비 이름
        var nameText = CreateTMP(root.transform, "NameText",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(130f, 10f), 22, Color.white, new Vector2(260f, 36f));
        nameText.alignment = TextAlignmentOptions.MidlineLeft;

        // 등급
        var gradeText = CreateTMP(root.transform, "GradeText",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(130f, -26f), 16, new Color(0.8f, 0.8f, 0.8f), new Vector2(200f, 28f));
        gradeText.alignment = TextAlignmentOptions.MidlineLeft;

        // 스탯
        var statsText = CreateTMP(root.transform, "StatsText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), 17, new Color(0.85f, 0.85f, 0.85f), new Vector2(280f, 36f));
        statsText.alignment = TextAlignmentOptions.Midline;

        // 장착 버튼
        var btnGo   = new GameObject("EquipButton");
        btnGo.transform.SetParent(root.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin        = new Vector2(1f, 0.5f);
        btnRect.anchorMax        = new Vector2(1f, 0.5f);
        btnRect.pivot            = new Vector2(1f, 0.5f);
        btnRect.anchoredPosition = new Vector2(-20f, 0f);
        btnRect.sizeDelta        = new Vector2(130f, 60f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.5f, 0.9f, 1f);
        var btn     = btnGo.AddComponent<Button>();

        var btnText = CreateTMP(btnGo.transform, "BtnText",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, 20, Color.white, new Vector2(130f, 60f));
        btnText.text = "Equip";

        // InventoryItemSlotUI 컴포넌트
        var comp   = root.AddComponent<InventoryItemSlotUI>();
        var compSO = new SerializedObject(comp);
        compSO.FindProperty("_bg").objectReferenceValue         = rootImg;
        compSO.FindProperty("_nameText").objectReferenceValue   = nameText;
        compSO.FindProperty("_gradeText").objectReferenceValue  = gradeText;
        compSO.FindProperty("_statsText").objectReferenceValue  = statsText;
        compSO.FindProperty("_slotText").objectReferenceValue   = slotText;
        compSO.FindProperty("_equipButton").objectReferenceValue  = btn;
        compSO.FindProperty("_equipBtnText").objectReferenceValue = btnText;
        compSO.ApplyModifiedProperties();

        // 프리팹 저장
        bool success;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (success)
            Debug.Log($"[MenuSetup] InventoryItemSlot 프리팹 생성 → {prefabPath}");
        else
            Debug.LogError("[MenuSetup] 프리팹 저장 실패");

        return prefab?.GetComponent<InventoryItemSlotUI>();
    }

    // ── 3. Canvas_Menu — redirects to full layout ─────────────────

    [MenuItem("Tools/Setup/Setup Menu Canvas")]
    public static void SetupMenuCanvas() => SetupFullLayout();

    // ── 하단 슬라이딩 패널 기반 생성 ─────────────────────────────

    static GameObject CreateBottomPanel(Transform parent, string name, Color color, float height)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 0f);
        rect.anchorMax        = new Vector2(1f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, -height);
        rect.sizeDelta        = new Vector2(0f, height);
        go.AddComponent<Image>().color = color;
        return go;
    }

    // ── 장비 패널 ─────────────────────────────────────────────────

    static (GameObject go, Button closeBtn) BuildEquipmentPanel(
        Transform parent, GameObject inventoryOverlay, float panelH)
    {
        var go = CreateBottomPanel(parent, "EquipmentPanel",
                                   new Color(0.08f, 0.08f, 0.13f, 0.97f), panelH);

        // ×  닫기
        var closeBtn = CreateButton(go.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(60f, 50f),
            "✕", new Color(0.45f, 0.12f, 0.12f, 0.9f));

        CreateTitle(go.transform, "Equipment");

        var slotLabels = new[] { "Weapon", "Helmet", "Armor", "Boots" };
        var slotXPos   = new[] { -300f, -100f, 100f, 300f };

        Image[] slotImgs = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var slot = new GameObject(slotLabels[i] + "Slot");
            slot.transform.SetParent(go.transform, false);
            var r = slot.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
            r.pivot     = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(slotXPos[i], -80f);
            r.sizeDelta = new Vector2(160f, 160f);
            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.22f, 0.28f, 0.9f);
            slotImgs[i] = bg;

            CreateTMP(slot.transform, slotLabels[i],
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), 15, new Color(0.7f, 0.7f, 0.7f));
        }

        var sumText = CreateTMP(go.transform, "SummaryText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -260f), 18, Color.white, new Vector2(900f, 36f));
        sumText.text = "Total  ATK +0  DEF +0  HP +0";

        var invBtn = CreateButton(go.transform, "InventoryToggleButton",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -310f), new Vector2(200f, 52f),
            "Inventory ▲", new Color(0.22f, 0.32f, 0.52f));

        var ui   = go.AddComponent<EquipmentPanelUI>();
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("_weaponIcon").objectReferenceValue            = slotImgs[0];
        uiSO.FindProperty("_helmetIcon").objectReferenceValue            = slotImgs[1];
        uiSO.FindProperty("_armorIcon").objectReferenceValue             = slotImgs[2];
        uiSO.FindProperty("_bootsIcon").objectReferenceValue             = slotImgs[3];
        uiSO.FindProperty("_summaryText").objectReferenceValue           = sumText;
        uiSO.FindProperty("_inventoryPanel").objectReferenceValue        = inventoryOverlay;
        uiSO.FindProperty("_inventoryToggleButton").objectReferenceValue = invBtn;
        uiSO.ApplyModifiedProperties();

        return (go, closeBtn);
    }

    // ── 인벤토리 오버레이 ─────────────────────────────────────────

    static GameObject BuildInventoryOverlay(Transform canvasRoot, float equipPanelH)
    {
        // 장비 패널 위부터 화면 상단 근처까지
        float startRatio = (equipPanelH + 10f) / 1080f;  // 패널 위 10px

        var overlay = new GameObject("InventoryOverlay");
        overlay.transform.SetParent(canvasRoot, false);
        var overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, startRatio);
        overlayRect.anchorMax = new Vector2(1f, 0.95f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.09f, 0.97f);

        CreateTitle(overlay.transform, "Inventory");

        CreateButton(overlay.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(60f, 50f),
            "✕", new Color(0.45f, 0.12f, 0.12f, 0.9f));

        // ScrollView
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(overlay.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(20f,  20f);
        scrollRect.offsetMax = new Vector2(-20f, -90f);
        scrollGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;

        var vpGo   = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpRect = vpGo.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vpRect;

        var contentGo   = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 8f;
        vlg.padding                = new RectOffset(10, 10, 8, 8);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        const string prefabPath = "Assets/Prefabs/UI/InventoryItemSlot.prefab";
        var slotPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemSlotUI>(prefabPath);
        if (slotPrefab == null)
        {
            slotPrefab = CreateInventorySlotPrefab();
            Debug.Log("[MenuSetup] InventoryItemSlot 프리팹 자동 생성");
        }

        var invUI   = overlay.AddComponent<InventoryPanelUI>();
        var invSO   = new SerializedObject(invUI);
        invSO.FindProperty("_slotPrefab").objectReferenceValue  = slotPrefab;
        invSO.FindProperty("_contentRoot").objectReferenceValue = contentRect;
        invSO.ApplyModifiedProperties();

        overlay.SetActive(false);
        return overlay;
    }

    // ── 업그레이드 패널 ───────────────────────────────────────────

    static (GameObject go, Button closeBtn) BuildUpgradePanel(Transform parent, float panelH)
    {
        var go = CreateBottomPanel(parent, "UpgradePanel",
                                   new Color(0.08f, 0.08f, 0.13f, 0.97f), panelH);

        var closeBtn = CreateButton(go.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(60f, 50f),
            "✕", new Color(0.45f, 0.12f, 0.12f, 0.9f));

        CreateTitle(go.transform, "Upgrade");

        var goldText = CreateTMP(go.transform, "GoldText",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-90f, -48f), 22, new Color(1f, 0.85f, 0.2f), new Vector2(280f, 40f));
        goldText.alignment = TextAlignmentOptions.MidlineRight;
        goldText.text = "0 G";

        var rows = new[] { ("HP", -88f), ("ATK", -168f), ("DEF", -248f), ("SPD", -328f) };

        var levelTexts  = new TextMeshProUGUI[4];
        var bonusTexts  = new TextMeshProUGUI[4];
        var costTexts   = new TextMeshProUGUI[4];
        var upgradeBtns = new Button[4];

        for (int i = 0; i < rows.Length; i++)
        {
            float y = rows[i].Item2;

            var rowBg = new GameObject($"Row_{rows[i].Item1}");
            rowBg.transform.SetParent(go.transform, false);
            var rowRect = rowBg.AddComponent<RectTransform>();
            rowRect.anchorMin        = new Vector2(0.5f, 1f);
            rowRect.anchorMax        = new Vector2(0.5f, 1f);
            rowRect.pivot            = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, y);
            rowRect.sizeDelta        = new Vector2(1700f, 72f);
            rowBg.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.18f, 0.9f);

            var labelT = CreateTMP(rowBg.transform, "Label",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(30f, 0f), 26, new Color(0.9f, 0.9f, 0.9f), new Vector2(80f, 48f));
            labelT.text = rows[i].Item1;
            labelT.alignment = TextAlignmentOptions.MidlineLeft;

            levelTexts[i] = CreateTMP(rowBg.transform, "LevelText",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(130f, 0f), 22, new Color(0.6f, 0.85f, 1f), new Vector2(110f, 38f));
            levelTexts[i].text = "Lv.0";
            levelTexts[i].alignment = TextAlignmentOptions.Midline;

            bonusTexts[i] = CreateTMP(rowBg.transform, "BonusText",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-80f, 0f), 20, new Color(0.5f, 1f, 0.5f), new Vector2(220f, 38f));
            bonusTexts[i].text = "+0";
            bonusTexts[i].alignment = TextAlignmentOptions.Midline;

            costTexts[i] = CreateTMP(rowBg.transform, "CostText",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-210f, 0f), 20, new Color(1f, 0.85f, 0.2f), new Vector2(180f, 38f));
            costTexts[i].text = "100 G";
            costTexts[i].alignment = TextAlignmentOptions.MidlineRight;

            upgradeBtns[i] = CreateButton(rowBg.transform, "UpgradeButton",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-20f, 0f), new Vector2(160f, 56f),
                "Up ▲", new Color(0.32f, 0.18f, 0.60f));
        }

        var ui   = go.AddComponent<UpgradePanelUI>();
        var uiSO = new SerializedObject(ui);

        void WireRow(string prop, TextMeshProUGUI lv, TextMeshProUGUI bonus,
                     TextMeshProUGUI cost, Button btn)
        {
            var row = uiSO.FindProperty(prop);
            row.FindPropertyRelative("levelText").objectReferenceValue     = lv;
            row.FindPropertyRelative("bonusText").objectReferenceValue     = bonus;
            row.FindPropertyRelative("costText").objectReferenceValue      = cost;
            row.FindPropertyRelative("upgradeButton").objectReferenceValue = btn;
        }

        WireRow("_hp",  levelTexts[0], bonusTexts[0], costTexts[0], upgradeBtns[0]);
        WireRow("_atk", levelTexts[1], bonusTexts[1], costTexts[1], upgradeBtns[1]);
        WireRow("_def", levelTexts[2], bonusTexts[2], costTexts[2], upgradeBtns[2]);
        WireRow("_spd", levelTexts[3], bonusTexts[3], costTexts[3], upgradeBtns[3]);
        uiSO.FindProperty("_goldText").objectReferenceValue = goldText;
        uiSO.ApplyModifiedProperties();

        return (go, closeBtn);
    }

    // ── 가챠 패널 ─────────────────────────────────────────────────

    static (GameObject go, Button closeBtn) BuildGachaPanel(Transform parent, float panelH)
    {
        var go = CreateBottomPanel(parent, "GachaPanel",
                                   new Color(0.06f, 0.04f, 0.12f, 0.97f), panelH);

        var closeBtn = CreateButton(go.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(60f, 50f),
            "✕", new Color(0.45f, 0.12f, 0.12f, 0.9f));

        CreateTitle(go.transform, "Gacha");

        go.AddComponent<GachaSystem>();

        var pityTMP = CreateTMP(go.transform, "PityText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -108f), 20, new Color(0.8f, 0.8f, 0.8f), new Vector2(600f, 34f));
        pityTMP.text = "Pity: 0 / 100";

        var costTMP = CreateTMP(go.transform, "CostText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -148f), 20, new Color(1f, 0.85f, 0.2f), new Vector2(600f, 34f));
        costTMP.text = "x1 100G  /  x10 1000G";

        var pullBtn = CreateButton(go.transform, "PullButton",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-170f, -210f), new Vector2(280f, 90f),
            "Pull x1", new Color(0.38f, 0.18f, 0.78f));

        var pull10Btn = CreateButton(go.transform, "Pull10Button",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(170f, -210f), new Vector2(280f, 90f),
            "Pull x10", new Color(0.58f, 0.12f, 0.48f));

        var cardGo   = new GameObject("ResultCard");
        cardGo.transform.SetParent(go.transform, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 1f);
        cardRect.pivot     = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = new Vector2(0f, -315f);
        cardRect.sizeDelta = new Vector2(520f, 130f);
        var cardBg = cardGo.AddComponent<Image>();
        cardBg.color = new Color(0.18f, 0.15f, 0.28f, 0.95f);

        var nameText  = CreateTMP(cardGo.transform, "ResultName",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), 26, Color.white, new Vector2(480f, 40f));
        var gradeText = CreateTMP(cardGo.transform, "ResultGrade",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -62f), 20, Color.white, new Vector2(480f, 32f));
        var statsText = CreateTMP(cardGo.transform, "ResultStats",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -98f), 18, new Color(0.85f, 0.85f, 0.85f), new Vector2(480f, 30f));
        cardGo.SetActive(false);

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

        return (go, closeBtn);
    }

    // ── 인벤토리 패널 (하단 슬라이드 방식 — 5탭 전용) ────────────

    static (GameObject go, Button closeBtn) BuildInvenPanel(Transform parent, float panelH)
    {
        var go = CreateBottomPanel(parent, "InvenPanel",
                                   new Color(0.04f, 0.04f, 0.09f, 0.97f), panelH);

        var closeBtn = CreateButton(go.transform, "CloseButton",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(60f, 50f),
            "✕", new Color(0.45f, 0.12f, 0.12f, 0.9f));

        CreateTitle(go.transform, "Inventory");

        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(go.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(20f,  20f);
        scrollRect.offsetMax = new Vector2(-20f, -90f);
        scrollGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;

        var vpGo   = new GameObject("Viewport");
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpRect = vpGo.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
        vpGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        vpGo.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vpRect;

        var contentGo   = new GameObject("Content");
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 8f;
        vlg.padding                = new RectOffset(10, 10, 8, 8);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        const string prefabPath = "Assets/Prefabs/UI/InventoryItemSlot.prefab";
        var slotPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemSlotUI>(prefabPath);
        if (slotPrefab == null) slotPrefab = CreateInventorySlotPrefab();

        var invUI   = go.AddComponent<InventoryPanelUI>();
        var invSO   = new SerializedObject(invUI);
        invSO.FindProperty("_slotPrefab").objectReferenceValue  = slotPrefab;
        invSO.FindProperty("_contentRoot").objectReferenceValue = contentRect;
        invSO.ApplyModifiedProperties();

        return (go, closeBtn);
    }

    // ── 우측 사이드 버튼 ─────────────────────────────────────────

    static (Button equip, Button upgrade, Button gacha) BuildSideButtons(Transform root)
    {
        var container = new GameObject("SideMenuButtons");
        container.transform.SetParent(root, false);
        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin        = new Vector2(1f, 0.5f);
        cRect.anchorMax        = new Vector2(1f, 0.5f);
        cRect.pivot            = new Vector2(1f, 0.5f);
        cRect.anchoredPosition = new Vector2(-12f, 0f);
        cRect.sizeDelta        = new Vector2(100f, 330f);
        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 10f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;

        Button MakeBtn(string icon, string label)
        {
            var go  = new GameObject($"SideBtn_{label}");
            go.transform.SetParent(container.transform, false);
            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 100f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.06f, 0.16f, 0.88f);
            var btn = go.AddComponent<Button>();
            var cb  = btn.colors;
            cb.highlightedColor = new Color(0.25f, 0.20f, 0.50f, 1f);
            cb.pressedColor     = new Color(0.45f, 0.30f, 0.80f, 1f);
            btn.colors = cb;

            // 아이콘
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var ir = iconGo.AddComponent<RectTransform>();
            ir.anchorMin = new Vector2(0f, 0.45f);
            ir.anchorMax = Vector2.one;
            ir.offsetMin = ir.offsetMax = Vector2.zero;
            var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
            iconTmp.text      = icon;
            iconTmp.fontSize  = 26f;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color     = Color.white;

            // 레이블
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lr = labelGo.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = new Vector2(1f, 0.45f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text      = label;
            labelTmp.fontSize  = 16f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color     = new Color(0.75f, 0.75f, 0.75f);

            return btn;
        }

        return (MakeBtn("⚔", "Equip"),
                MakeBtn("▲", "Upgrade"),
                MakeBtn("★", "Gacha"));
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
        go.AddComponent<Image>().color = color;
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
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r  = go.AddComponent<RectTransform>();
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
        go.AddComponent<Image>().color = bgColor;
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

    // ── 4. Full SoulStrike Layout 생성 ───────────────────────────

    [MenuItem("Tools/Setup/Setup Full Layout")]
    public static void SetupFullLayout()
    {
        SetupHUDCanvas();
        SetupMenuCanvasFull();
        Debug.Log("[MenuSetup] Full SoulStrike Layout 생성 완료.");
    }

    // ── Canvas_HUD (SoulStrike style, 1920x1080) ──────────────────

    static void SetupHUDCanvas()
    {
        var existing = GameObject.Find("Canvas_HUD");
        if (existing != null) { Object.DestroyImmediate(existing); }

        var root   = new GameObject("Canvas_HUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // ── Top-left: player info card (400x185) ─────────────────
        var infoCard = BuildPlayerInfoCard(root.transform);

        // ── Top-center: stage info bar ────────────────────────────
        var stageInfo = BuildStageInfoPanel(root.transform);

        // ── Top-right: vertical icon menu (5 buttons) ────────────
        BuildTopRightMenu(root.transform);

        // ── Damage flash overlay ──────────────────────────────────
        var flashGo   = new GameObject("DamageFlash");
        flashGo.transform.SetParent(root.transform, false);
        var flashRect = flashGo.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero; flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = flashRect.offsetMax = Vector2.zero;
        var flashImg  = flashGo.AddComponent<Image>();
        flashImg.color = new Color(1,0,0,0);
        flashImg.raycastTarget = false;

        // ── Dead panel ────────────────────────────────────────────
        var deadPanel = new GameObject("DeadPanel");
        deadPanel.transform.SetParent(root.transform, false);
        var deadRect  = deadPanel.AddComponent<RectTransform>();
        deadRect.anchorMin = Vector2.zero; deadRect.anchorMax = Vector2.one;
        deadRect.offsetMin = deadRect.offsetMax = Vector2.zero;
        deadPanel.AddComponent<Image>().color = new Color(0,0,0,0.78f);
        var deadTMP = CreateTMP(deadPanel.transform, "DeadText",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, 80, new Color(0.9f,0.1f,0.1f), new Vector2(700f,110f));
        deadTMP.text = "YOU DIED";
        deadPanel.SetActive(false);

        // ── Stage clear panel ─────────────────────────────────────
        var clearPanel = new GameObject("StageClearPanel");
        clearPanel.transform.SetParent(root.transform, false);
        var clearRect  = clearPanel.AddComponent<RectTransform>();
        clearRect.anchorMin = clearRect.anchorMax = new Vector2(0.5f,0.5f);
        clearRect.pivot     = new Vector2(0.5f,0.5f);
        clearRect.anchoredPosition = Vector2.zero;
        clearRect.sizeDelta = new Vector2(700f,220f);
        clearPanel.AddComponent<Image>().color = new Color(0.05f,0.05f,0.10f,0.93f);
        var clearTitle = CreateTMP(clearPanel.transform, "ClearTitle",
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0f,-32f), 52, new Color(1f,0.85f,0.2f), new Vector2(640f,68f));
        clearTitle.text = "STAGE CLEAR!";
        var clearGoldTMP = CreateTMP(clearPanel.transform, "ClearGoldText",
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0f,-108f), 36, Color.white, new Vector2(640f,52f));
        clearGoldTMP.text = "+ 0 G";
        clearPanel.SetActive(false);

        // ── Combo text ────────────────────────────────────────────
        var comboTMP = CreateTMP(root.transform, "ComboText",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0f,120f), 60, new Color(1f,0.8f,0.1f), new Vector2(560f,80f));
        comboTMP.text = "";
        comboTMP.gameObject.SetActive(false);

        // ── Collect slider refs from sub-panels ───────────────────
        Slider[] cardSliders = infoCard.GetComponentsInChildren<Slider>();
        Slider killSlider  = stageInfo.GetComponentInChildren<Slider>();

        TextMeshProUGUI killTMP  = null, stageTMP = null;
        foreach (var t in stageInfo.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if (t.gameObject.name == "KillText")  killTMP  = t;
            if (t.gameObject.name == "StageText") stageTMP = t;
        }

        // ── Wire HUDManager ───────────────────────────────────────
        var hud   = root.AddComponent<HUDManager>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("_playerInfoPanel").objectReferenceValue  = infoCard.GetComponent<PlayerInfoPanelUI>();
        if (cardSliders.Length > 0) hudSO.FindProperty("_hpSlider").objectReferenceValue = cardSliders[0];
        if (cardSliders.Length > 1) hudSO.FindProperty("_spSlider").objectReferenceValue = cardSliders[1];
        if (killSlider != null)     hudSO.FindProperty("_killSlider").objectReferenceValue = killSlider;
        if (killTMP    != null)     hudSO.FindProperty("_killTMP").objectReferenceValue    = killTMP;
        if (stageTMP   != null)     hudSO.FindProperty("_stageTMP").objectReferenceValue   = stageTMP;
        hudSO.FindProperty("_damageFlash").objectReferenceValue       = flashImg;
        hudSO.FindProperty("_deadPanel").objectReferenceValue         = deadPanel;
        hudSO.FindProperty("_stageClearPanel").objectReferenceValue   = clearPanel;
        hudSO.FindProperty("_stageClearGoldTMP").objectReferenceValue = clearGoldTMP;
        hudSO.FindProperty("_comboTMP").objectReferenceValue          = comboTMP;
        hudSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(root);
        Debug.Log("[MenuSetup] Canvas_HUD created.");
    }

    // ── Top-left player info card ─────────────────────────────────
    // Layout: [Avatar 90px] | Name / Level / Gold | HP bar | SP bar
    // Card size: 400 x 185, anchored top-left with 16px margin

    static GameObject BuildPlayerInfoCard(Transform canvasRoot)
    {
        var card = new GameObject("PlayerInfoCard");
        card.transform.SetParent(canvasRoot, false);
        var rect = card.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f,1f);
        rect.anchorMax        = new Vector2(0f,1f);
        rect.pivot            = new Vector2(0f,1f);
        rect.anchoredPosition = new Vector2(16f,-16f);
        rect.sizeDelta        = new Vector2(400f,185f);
        var cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.12f,0.06f,0.22f,0.92f);

        // Avatar circle
        var ava = new GameObject("Avatar");
        ava.transform.SetParent(card.transform, false);
        var ar  = ava.AddComponent<RectTransform>();
        ar.anchorMin = ar.anchorMax = new Vector2(0f,0.5f);
        ar.pivot     = new Vector2(0f,0.5f);
        ar.anchoredPosition = new Vector2(14f,0f);
        ar.sizeDelta = new Vector2(90f,90f);
        ava.AddComponent<Image>().color = new Color(0.90f,0.55f,0.15f);

        // Level badge on avatar
        var lvBadge = CreateTMP(ava.transform, "LvBadge",
            new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0.5f,0f),
            new Vector2(0f,4f), 16, new Color(1f,0.9f,0.3f), new Vector2(90f,24f));
        lvBadge.text = "Lv.1";
        lvBadge.alignment = TextAlignmentOptions.Center;

        // Name
        var nameT = CreateTMP(card.transform, "NameText",
            new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(118f,-14f), 24, Color.white, new Vector2(240f,32f));
        nameT.alignment = TextAlignmentOptions.MidlineLeft;
        nameT.text = "Hero";

        // Gold (with icon prefix)
        var goldT = CreateTMP(card.transform, "GoldText",
            new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(118f,-50f), 20, new Color(1f,0.85f,0.2f), new Vector2(240f,28f));
        goldT.alignment = TextAlignmentOptions.MidlineLeft;
        goldT.text = "G  0";

        // HP label + bar
        var hpLbl = CreateTMP(card.transform, "HPLabel",
            new Vector2(0f,0f), new Vector2(0f,0f), new Vector2(0f,0f),
            new Vector2(14f,86f), 17, new Color(1f,0.30f,0.25f), new Vector2(36f,24f));
        hpLbl.text = "HP";
        hpLbl.alignment = TextAlignmentOptions.MidlineLeft;

        var hpSlider = BuildMiniSlider(card.transform, "HPSlider",
            new Vector2(50f,80f), new Vector2(336f,20f), new Color(0.85f,0.15f,0.12f));

        // SP label + bar
        var spLbl = CreateTMP(card.transform, "SPLabel",
            new Vector2(0f,0f), new Vector2(0f,0f), new Vector2(0f,0f),
            new Vector2(14f,56f), 17, new Color(0.35f,0.6f,1f), new Vector2(36f,24f));
        spLbl.text = "SP";
        spLbl.alignment = TextAlignmentOptions.MidlineLeft;

        var spSlider = BuildMiniSlider(card.transform, "SPSlider",
            new Vector2(50f,50f), new Vector2(336f,20f), new Color(0.20f,0.45f,0.92f));

        // HP text overlay on bar
        var hpText = CreateTMP(hpSlider.gameObject.transform, "HPText",
            Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f),
            Vector2.zero, 13, Color.white, Vector2.zero);
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.text = "500 / 500";

        // PlayerInfoPanelUI
        var ui   = card.AddComponent<PlayerInfoPanelUI>();
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("_nameText").objectReferenceValue  = nameT;
        uiSO.FindProperty("_levelText").objectReferenceValue = lvBadge;
        uiSO.FindProperty("_goldText").objectReferenceValue  = goldT;
        uiSO.ApplyModifiedProperties();

        return card;
    }

    static Slider BuildMiniSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f,0f);
        rect.pivot     = new Vector2(0f,0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        go.AddComponent<Image>().color = new Color(0.12f,0.12f,0.15f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var far  = fillArea.AddComponent<RectTransform>();
        far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one;
        far.offsetMin = far.offsetMax = Vector2.zero;

        var fill  = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fr    = fill.AddComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(1f,1f);
        fr.offsetMin = fr.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = fillColor;

        var slider = go.AddComponent<Slider>();
        slider.fillRect   = fr;
        slider.direction  = Slider.Direction.LeftToRight;
        slider.minValue   = 0f; slider.maxValue = 1f; slider.value = 1f;
        slider.interactable = false;
        return slider;
    }

    // ── Top-center stage info panel ───────────────────────────────
    // Size: 700 x 110, anchored top-center

    static GameObject BuildStageInfoPanel(Transform canvasRoot)
    {
        var panel = new GameObject("StageInfoPanel");
        panel.transform.SetParent(canvasRoot, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f,1f);
        rect.anchorMax        = new Vector2(0.5f,1f);
        rect.pivot            = new Vector2(0.5f,1f);
        rect.anchoredPosition = new Vector2(0f,-16f);
        rect.sizeDelta        = new Vector2(700f,110f);
        panel.AddComponent<Image>().color = new Color(0.10f,0.06f,0.22f,0.88f);

        // Stage text "Stage 1-1" (top, large)
        var stageTMP = CreateTMP(panel.transform, "StageText",
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0f,-12f), 28, Color.white, new Vector2(640f,40f));
        stageTMP.text = "Stage 1-1";
        stageTMP.alignment = TextAlignmentOptions.Center;

        // Subtitle (stage area name)
        var subtitleTMP = CreateTMP(panel.transform, "SubtitleText",
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
            new Vector2(0f,-50f), 16, new Color(0.75f,0.62f,1f), new Vector2(640f,24f));
        subtitleTMP.text = "Dungeon Entrance";
        subtitleTMP.alignment = TextAlignmentOptions.Center;

        // "Progress" label row
        var progLabel = CreateTMP(panel.transform, "ProgressLabel",
            new Vector2(0f,0f), new Vector2(0f,0f), new Vector2(0f,0f),
            new Vector2(20f,46f), 14, new Color(0.75f,0.62f,1f), new Vector2(120f,20f));
        progLabel.text = "Progress";
        progLabel.alignment = TextAlignmentOptions.MidlineLeft;

        // Kill count label (right of progress label)
        var killTMP = CreateTMP(panel.transform, "KillText",
            new Vector2(1f,0f), new Vector2(1f,0f), new Vector2(1f,0f),
            new Vector2(-20f,46f), 14, Color.white, new Vector2(120f,20f));
        killTMP.text = "0 / 10";
        killTMP.alignment = TextAlignmentOptions.MidlineRight;
        killTMP.gameObject.name = "KillText";

        // Kill progress slider (purple fill)
        var killSlider = BuildMiniSlider(panel.transform, "KillSlider",
            new Vector2(-320f,-66f), new Vector2(640f,18f), new Color(0.55f,0.22f,0.88f));

        return panel;
    }

    // ── Top-right vertical icon menu (5 buttons) ──────────────────
    // Each button: 80x80, stacked vertically, anchored top-right

    static GameObject BuildTopRightMenu(Transform canvasRoot)
    {
        var container = new GameObject("TopRightMenu");
        container.transform.SetParent(canvasRoot, false);
        var cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin        = new Vector2(1f,1f);
        cRect.anchorMax        = new Vector2(1f,1f);
        cRect.pivot            = new Vector2(1f,1f);
        cRect.anchoredPosition = new Vector2(-16f,-16f);
        cRect.sizeDelta        = new Vector2(80f,270f);

        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 8f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childAlignment         = TextAnchor.UpperCenter;

        // Gift | Shop | Settings — 3 buttons matching design
        var btnDefs = new[]
        {
            ("🎁\nGift",     new Color(0.38f,0.14f,0.52f,0.95f)),  // purple
            ("🛒\nShop",     new Color(0.14f,0.22f,0.52f,0.95f)),  // indigo
            ("⚙\nSettings", new Color(0.18f,0.18f,0.26f,0.95f)),  // slate
        };
        var btnRefs = new Button[btnDefs.Length];

        for (int i = 0; i < btnDefs.Length; i++)
        {
            var go  = new GameObject(btnDefs[i].Item1.Split('\n')[1]);
            go.transform.SetParent(container.transform, false);
            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 80f;
            var img = go.AddComponent<Image>();
            img.color = btnDefs[i].Item2;
            var btn = go.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = btnDefs[i].Item2;
            cb.highlightedColor = new Color(0.28f,0.22f,0.55f);
            cb.pressedColor     = new Color(0.50f,0.35f,0.90f);
            btn.colors = cb;
            btnRefs[i] = btn;

            var lbl = CreateTMP(go.transform, "Label",
                Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f),
                Vector2.zero, 16, Color.white, new Vector2(80f,80f));
            lbl.text = btnDefs[i].Item1;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        var ui   = container.AddComponent<TopRightMenuUI>();
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("_giftBtn").objectReferenceValue     = btnRefs[0];
        uiSO.FindProperty("_shopBtn").objectReferenceValue     = btnRefs[1];
        uiSO.FindProperty("_settingsBtn").objectReferenceValue = btnRefs[2];
        uiSO.ApplyModifiedProperties();

        return container;
    }

    // ── Canvas_Menu: bottom 5-tab bar + 4 slide panels ────────────
    // Tabs: Equipment | Growth | [Auto] | Summon | Inventory
    // Panel height: 540px.  Tab bar height: 110px.

    static void SetupMenuCanvasFull()
    {
        var existing = GameObject.Find("Canvas_Menu");
        if (existing != null) { Object.DestroyImmediate(existing); }

        var root   = new GameObject("Canvas_Menu");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        const float panelH  = 540f;
        const float tabBarH = 110f;

        // ── Four sliding panels ───────────────────────────────────
        var (equipGo,  closeE) = BuildEquipmentPanel(root.transform, null, panelH);
        var (growthGo, closeG) = BuildUpgradePanel(root.transform, panelH);
        var (summonGo, closeS) = BuildGachaPanel(root.transform, panelH);
        var (invenGo,  closeI) = BuildInvenPanel(root.transform, panelH);

        // ── Bottom tab bar ────────────────────────────────────────
        var tabBar = new GameObject("BottomTabBar");
        tabBar.transform.SetParent(root.transform, false);
        var tbRect = tabBar.AddComponent<RectTransform>();
        tbRect.anchorMin        = Vector2.zero;
        tbRect.anchorMax        = new Vector2(1f,0f);
        tbRect.pivot            = new Vector2(0.5f,0f);
        tbRect.anchoredPosition = Vector2.zero;
        tbRect.sizeDelta        = new Vector2(0f,tabBarH);
        tabBar.AddComponent<Image>().color = new Color(0.06f,0.05f,0.12f,0.97f);

        // Separator line at top of tab bar
        var sep = new GameObject("Separator");
        sep.transform.SetParent(tabBar.transform, false);
        var sepRect = sep.AddComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0f,1f); sepRect.anchorMax = new Vector2(1f,1f);
        sepRect.pivot = new Vector2(0.5f,1f); sepRect.anchoredPosition = Vector2.zero;
        sepRect.sizeDelta = new Vector2(0f,2f);
        sep.AddComponent<Image>().color = new Color(0.55f,0.35f,1f,0.5f);

        var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 0f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.padding = new RectOffset(0,0,0,0);

        Color inactive = new Color(0.12f,0.10f,0.20f,0.95f);
        Color active   = new Color(0.55f,0.32f,0.92f,1.00f);
        Color autoOff  = new Color(0.10f,0.28f,0.18f,0.95f);

        // Tab definitions: (label, icon, isPanel)
        var tabDefs = new (string label, string icon, bool isPanel)[]
        {
            ("캐릭터", "👤", true),
            ("성장",   "📈", true),
            ("AUTO",   "▶",  false),   // center — auto-battle toggle
            ("소환",   "★",  true),
            ("장비",   "⚔",  true),
        };

        var tabBtns = new Button[5];
        for (int i = 0; i < tabDefs.Length; i++)
        {
            var isCenter = !tabDefs[i].isPanel;
            var go  = new GameObject($"Tab_{tabDefs[i].label}");
            go.transform.SetParent(tabBar.transform, false);

            // Center AUTO: slightly taller via LayoutElement to make it "pop"
            if (isCenter)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = 130f;
            }

            var img = go.AddComponent<Image>();
            img.color = isCenter ? autoOff : inactive;
            var btn = go.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = isCenter ? autoOff : inactive;
            cb.highlightedColor = new Color(0.28f,0.22f,0.55f);
            cb.pressedColor     = active;
            btn.colors = cb;
            tabBtns[i] = btn;

            // Icon text (large)
            var icon = CreateTMP(go.transform, "Icon",
                new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
                new Vector2(0f,-8f), isCenter ? 32f : 26f,
                isCenter ? new Color(0.40f,0.95f,0.55f) : new Color(0.75f,0.75f,0.88f),
                new Vector2(0f,52f));
            icon.text = tabDefs[i].icon;
            icon.alignment = TextAlignmentOptions.Center;

            // Label text (small, bottom)
            var lbl = CreateTMP(go.transform, "Label",
                new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0.5f,0f),
                new Vector2(0f,6f), isCenter ? 18f : 16f,
                isCenter ? new Color(0.40f,0.95f,0.55f) : new Color(0.62f,0.62f,0.75f),
                new Vector2(0f,26f));
            lbl.text = tabDefs[i].label;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ── Wire MainBottomNav ────────────────────────────────────
        var nav   = root.AddComponent<MainBottomNav>();
        var navSO = new SerializedObject(nav);
        navSO.FindProperty("_tabEquip").objectReferenceValue   = tabBtns[0];
        navSO.FindProperty("_tabGrowth").objectReferenceValue  = tabBtns[1];
        navSO.FindProperty("_tabAuto").objectReferenceValue    = tabBtns[2];
        navSO.FindProperty("_tabSummon").objectReferenceValue  = tabBtns[3];
        navSO.FindProperty("_tabInven").objectReferenceValue   = tabBtns[4];
        navSO.FindProperty("_closeEquipBtn").objectReferenceValue  = closeE;
        navSO.FindProperty("_closeGrowthBtn").objectReferenceValue = closeG;
        navSO.FindProperty("_closeSummonBtn").objectReferenceValue = closeS;
        navSO.FindProperty("_closeInvenBtn").objectReferenceValue  = closeI;
        navSO.FindProperty("_equipPanel").objectReferenceValue  = equipGo.GetComponent<RectTransform>();
        navSO.FindProperty("_growthPanel").objectReferenceValue = growthGo.GetComponent<RectTransform>();
        navSO.FindProperty("_summonPanel").objectReferenceValue = summonGo.GetComponent<RectTransform>();
        navSO.FindProperty("_invenPanel").objectReferenceValue  = invenGo.GetComponent<RectTransform>();
        navSO.FindProperty("_panelH").floatValue = panelH;
        navSO.ApplyModifiedProperties();

        // Cross-wire TopRightMenuUI -> MainBottomNav (if HUD canvas already built)
        var topMenu = Object.FindObjectOfType<TopRightMenuUI>();
        if (topMenu != null)
        {
            var tmSO = new SerializedObject(topMenu);
            tmSO.FindProperty("_nav").objectReferenceValue = nav;
            tmSO.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(root);
        Debug.Log("[MenuSetup] Canvas_Menu (5-tab SoulStrike) created. Assign GachaSystem pool manually.");
    }

    static string GetTabIcon(string label)
    {
        switch (label)
        {
            case "Equip":  return "⚔";
            case "Growth": return "▲";
            case "Summon": return "★";
            case "Inven":  return "☰";
            default:       return "●";
        }
    }

    // ── 5. GameManager에 PlayerStatsSO 할당 ──────────────────────
    [MenuItem("Tools/Setup/Wire GameManager Stats")]
    public static void WireGameManagerStats()
    {
        const string soPath        = "Assets/ScriptableObjects/PlayerStats.asset";
        const string loginScenePath = "Assets/Scenes/LoginScene.unity";

        var so = AssetDatabase.LoadAssetAtPath<PlayerStatsSO>(soPath);
        if (so == null)
        {
            Debug.LogError($"[MenuSetup] PlayerStats.asset 없음: {soPath}");
            return;
        }

        // 현재 씬 저장 여부 확인
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            if (!EditorUtility.DisplayDialog("씬 저장",
                    "현재 씬에 저장되지 않은 변경이 있습니다. 저장 후 LoginScene을 열겠습니까?",
                    "저장 후 계속", "취소"))
                return;
            EditorSceneManager.SaveScene(activeScene);
        }

        // LoginScene 열기
        var loginScene = EditorSceneManager.OpenScene(loginScenePath, OpenSceneMode.Single);

        var gm = Object.FindObjectOfType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("[MenuSetup] LoginScene에서 GameManager를 찾을 수 없음");
            return;
        }

        var gmSO = new SerializedObject(gm);
        gmSO.FindProperty("_playerStatsSO").objectReferenceValue = so;
        gmSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(gm);
        EditorSceneManager.SaveScene(loginScene);

        Debug.Log($"[MenuSetup] GameManager._playerStatsSO → {soPath}  (LoginScene 저장 완료)");
    }
}
