using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tools > Setup > Setup SoulStrike GameScene
/// Tools > Setup > Setup HUD Only  ← HUD만 재생성
/// </summary>
public static class SoulStrikeSetupWizard
{
    [MenuItem("Tools/Setup/Set Landscape Orientation")]
    public static void SetLandscape()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft  = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait       = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        Debug.Log("[SoulStrikeSetup] Android orientation → Landscape");
    }

    [MenuItem("Tools/Setup/Setup SoulStrike GameScene")]
    public static void SetupScene()
    {
        SetupCamera();
        SetupLight();
        SetupGround();
        SetupPlayer();
        SetupManagers();
        SetupHUD();
        SetupJoystickCanvas();

        Debug.Log("[SoulStrikeSetup] 완료. NavMesh 베이크: Window > AI > Navigation > Bake");
    }

    [MenuItem("Tools/Setup/Setup HUD Only")]
    public static void SetupHUDOnly()
    {
        var existing = GameObject.Find("Canvas_HUD");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[SoulStrikeSetup] 기존 Canvas_HUD 삭제 후 재생성");
        }
        SetupHUD();
        Debug.Log("[SoulStrikeSetup] HUD 재생성 완료");
    }

    // ── Camera ────────────────────────────────────────────────────

    static void SetupCamera()
    {
        var cam = Camera.main?.gameObject;
        if (cam == null)
        {
            cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
            cam.AddComponent<AudioListener>();
        }

        EnsureComponent<CameraFollow>(cam);
        EnsureComponent<CameraShake>(cam);
        EnsureComponent<CameraPassObj>(cam);

        cam.transform.position = new Vector3(0f, 7f, -5f);
        cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }

    // ── Lighting ─────────────────────────────────────────────────

    static void SetupLight()
    {
        if (GameObject.Find("Directional Light") != null) return;

        var go    = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ── Ground ────────────────────────────────────────────────────

    static void SetupGround()
    {
        if (GameObject.Find("Ground") != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Ground";
        go.transform.localScale = new Vector3(3f, 1f, 3f);
        go.isStatic = true;

        var flags = GameObjectUtility.GetStaticEditorFlags(go);
        GameObjectUtility.SetStaticEditorFlags(go, flags | (StaticEditorFlags)64);
    }

    // ── Player ────────────────────────────────────────────────────

    static void SetupPlayer()
    {
        if (Object.FindObjectOfType<PlayerController>() != null)
        {
            Debug.Log("[SoulStrikeSetup] Player 이미 존재");
            return;
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        go.tag  = "Player";
        go.transform.position = new Vector3(0f, 1f, 0f);

        var nma = go.AddComponent<NavMeshAgent>();
        nma.radius           = 0.3f;
        nma.height           = 2f;
        nma.speed            = 5f;
        nma.stoppingDistance = 1.5f;

        var rb = go.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        go.AddComponent<Animator>();
        EnsureComponent<PlayerAutoController>(go);
        EnsureComponent<PlayerController>(go);

        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            var so = new SerializedObject(camFollow);
            so.FindProperty("_targetTransform").objectReferenceValue = go.transform;
            so.ApplyModifiedProperties();
        }

        Debug.Log("[SoulStrikeSetup] Player 생성 완료");
    }

    // ── Managers ─────────────────────────────────────────────────

    static void SetupManagers()
    {
        EnsureSceneObject<JsonDataManager>("JsonDataManager");
        EnsureSceneObject<SpawnManager>("SpawnManager");
        EnsureSceneObject<StageManager>("StageManager");

        // UI 버튼 입력 처리에 필수
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Debug.Log("[SoulStrikeSetup] EventSystem 생성");
        }
    }

    // ── HUD Canvas ────────────────────────────────────────────────
    // Layout (1920×1080 Landscape):
    //  Canvas_HUD
    //  ├─ TopBar (全폭×88px, 반투명 검정)
    //  │   ├─ PlayerCard  (좌 320px — 아바타, 이름, 골드, HP/SP)
    //  │   ├─ StagePanel  (중앙 760px — 스테이지명, 킬바)
    //  │   └─ RightPanel  (우 230px — AUTO, 상점, 설정)
    //  ├─ BottomNav (全폭×76px, 반투명 검정 — 5탭)
    //  ├─ DamageFlash / DeadPanel / StageClearPanel (fullscreen)
    //  └─ ComboText (center float)

    static void SetupHUD()
    {
        if (GameObject.Find("Canvas_HUD") != null) return;

        // ── Canvas root
        var root   = new GameObject("Canvas_HUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        var hud   = root.AddComponent<HUDManager>();
        var hudSO = new SerializedObject(hud);

        // ══════════════════════════════════════════
        //  TOP BAR  (全폭 × 88px)
        // ══════════════════════════════════════════
        var topBar = NewPanel(root.transform, "TopBar",
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(0.5f,1), Vector2.zero, new Vector2(0,88),
            new Color(0f,0f,0f,0.65f));

        // ── PlayerCard (좌 320px)
        var playerCard = NewPanel(topBar.transform, "PlayerCard",
            new Vector2(0,0), new Vector2(0,1),
            new Vector2(0,0.5f), Vector2.zero, new Vector2(320,0),
            Color.clear);

        var avatarRect = NewRect(playerCard.transform, "Avatar",
            new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(0,0.5f), new Vector2(8,0), new Vector2(68,68));
        avatarRect.gameObject.AddComponent<Image>().color = new Color(0.22f,0.22f,0.32f,1f);

        var nameText = CreateTMP(playerCard.transform, "NameText",
            new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(84,16), 15, Color.white, new Vector2(224,22));
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.text = "Hero";

        var goldTMP = CreateTMP(playerCard.transform, "GoldText",
            new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(84,-4), 13, new Color(1f,0.85f,0.2f,1f), new Vector2(224,18));
        goldTMP.alignment = TextAlignmentOptions.Left;
        goldTMP.text = "0 G";

        var hpBar = CreateSlider(playerCard.transform, "HpBar",
            new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(84,-24), new Vector2(224,18),
            new Color(0.2f,0.78f,0.2f,1f));
        var hpText = CreateTMP(hpBar.transform, "HpText",
            Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, 10, Color.white);

        var spBar = CreateSlider(playerCard.transform, "SpBar",
            new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(84,-44), new Vector2(224,12),
            new Color(0.2f,0.45f,0.9f,1f));

        // PlayerInfoPanelUI
        var playerInfoUI = playerCard.AddComponent<PlayerInfoPanelUI>();
        var playerInfoSO = new SerializedObject(playerInfoUI);
        playerInfoSO.FindProperty("_nameText").objectReferenceValue = nameText;
        playerInfoSO.FindProperty("_goldText").objectReferenceValue = goldTMP;
        playerInfoSO.ApplyModifiedProperties();
        hudSO.FindProperty("_playerInfoPanel").objectReferenceValue = playerInfoUI;

        // ── StagePanel (중앙 760px)
        var stagePanel = NewPanel(topBar.transform, "StagePanel",
            new Vector2(0.5f,0), new Vector2(0.5f,1),
            new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(760,0),
            Color.clear);

        var stageTMP = CreateTMP(stagePanel.transform, "StageText",
            new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1),
            new Vector2(0,-6), 20, Color.white, new Vector2(740,28));
        stageTMP.text = "Stage 1-1";

        var subtitleTMP = CreateTMP(stagePanel.transform, "SubtitleText",
            new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1),
            new Vector2(0,-33), 13, new Color(1f,1f,1f,0.60f), new Vector2(740,18));
        subtitleTMP.text = "Dungeon Entrance";

        // 킬 진행 바 (주황/금색, 두드러지게)
        var killSlider = CreateSlider(stagePanel.transform, "KillSlider",
            new Vector2(0.5f,1), new Vector2(0.5f,1),
            new Vector2(0,-57), new Vector2(700,26),
            new Color(1f,0.68f,0.06f,1f));
        var ksRect = killSlider.GetComponent<RectTransform>();
        ksRect.pivot = new Vector2(0.5f,0.5f);
        var ksBg = killSlider.transform.Find("Background")?.GetComponent<Image>();
        if (ksBg != null) ksBg.color = new Color(0.20f,0.14f,0.02f,0.85f);

        var killTMP = CreateTMP(killSlider.transform, "KillText",
            Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero, 13, Color.white);
        killTMP.text = "0 / 0";

        // ── RightPanel (우 230px)
        var rightPanel = NewPanel(topBar.transform, "RightPanel",
            new Vector2(1,0), new Vector2(1,1),
            new Vector2(1,0.5f), new Vector2(-6,0), new Vector2(230,0),
            Color.clear);

        var autoBtn     = MakeTextBtn(rightPanel.transform, "AutoBtn",    "AUTO",
            new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(1,0.5f),
            new Vector2(-8,   0), new Vector2(80,36), new Color(0.10f,0.10f,0.20f,0.95f), 15);
        var shopBtn     = MakeTextBtn(rightPanel.transform, "ShopBtn",    "상점",
            new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(1,0.5f),
            new Vector2(-96,  0), new Vector2(80,36), new Color(0.10f,0.10f,0.20f,0.95f), 14);
        var settingsBtn = MakeTextBtn(rightPanel.transform, "SettingsBtn","설정",
            new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(1,0.5f),
            new Vector2(-184, 0), new Vector2(80,36), new Color(0.10f,0.10f,0.20f,0.95f), 14);

        var topMenuUI = root.AddComponent<TopRightMenuUI>();
        var topMenuSO = new SerializedObject(topMenuUI);
        topMenuSO.FindProperty("_shopBtn").objectReferenceValue     = shopBtn;
        topMenuSO.FindProperty("_settingsBtn").objectReferenceValue = settingsBtn;
        topMenuSO.ApplyModifiedProperties();

        // ══════════════════════════════════════════
        //  BOTTOM NAV BAR  (全폭 × 76px)
        // ══════════════════════════════════════════
        var botBar = NewPanel(root.transform, "BottomNav",
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(0.5f,0), Vector2.zero, new Vector2(0,76),
            new Color(0.06f,0.06f,0.10f,0.92f));

        // 상단 구분선
        var sep = NewRect(botBar.transform, "Separator",
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(0.5f,1), Vector2.zero, new Vector2(0,2));
        sep.gameObject.AddComponent<Image>().color = new Color(1f,1f,1f,0.12f);

        Color tabNormal = new Color(0.10f,0.10f,0.16f,0f);
        Color tabAutoC  = new Color(0.15f,0.65f,0.35f,1f);

        var tabEquip  = MakeNavTab(botBar.transform,"TabEquip", "캐릭터", 0,5,tabNormal,14);
        var tabGrowth = MakeNavTab(botBar.transform,"TabGrowth","성장",   1,5,tabNormal,14);
        var tabAuto   = MakeNavTab(botBar.transform,"TabAuto",  "● AUTO", 2,5,tabAutoC, 13);
        var tabSummon = MakeNavTab(botBar.transform,"TabSummon","소환",   3,5,tabNormal,14);
        var tabInven  = MakeNavTab(botBar.transform,"TabInven", "인벤",   4,5,tabNormal,14);

        var nav   = root.AddComponent<MainBottomNav>();
        var navSO = new SerializedObject(nav);
        navSO.FindProperty("_tabEquip") .objectReferenceValue = tabEquip;
        navSO.FindProperty("_tabGrowth").objectReferenceValue = tabGrowth;
        navSO.FindProperty("_tabAuto")  .objectReferenceValue = tabAuto;
        navSO.FindProperty("_tabSummon").objectReferenceValue = tabSummon;
        navSO.FindProperty("_tabInven") .objectReferenceValue = tabInven;
        navSO.ApplyModifiedProperties();

        // ══════════════════════════════════════════
        //  OVERLAYS
        // ══════════════════════════════════════════
        // DamageFlash
        var flashRect = NewRect(root.transform, "DamageFlash",
            Vector2.zero, Vector2.one,
            new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero);
        var flashImg = flashRect.gameObject.AddComponent<Image>();
        flashImg.color = new Color(1f,0f,0f,0f);
        flashImg.raycastTarget = false;

        // DeadPanel
        var deadGo = NewPanel(root.transform, "DeadPanel",
            Vector2.zero, Vector2.one,
            new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero,
            new Color(0f,0f,0f,0.6f));
        deadGo.SetActive(false);
        var deadText = CreateTMP(deadGo.transform, "DeadText",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, 60, Color.red);
        deadText.text = "YOU DIED";

        // StageClearPanel
        var clearGo = NewPanel(root.transform, "StageClearPanel",
            Vector2.zero, Vector2.one,
            new Vector2(0.5f,0.5f), Vector2.zero, Vector2.zero,
            new Color(0f,0f,0f,0.5f));
        clearGo.SetActive(false);
        var clearTitle = CreateTMP(clearGo.transform, "ClearTitle",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0,60), 56, new Color(1f,0.9f,0.2f,1f));
        clearTitle.text = "STAGE CLEAR!";
        var clearGold = CreateTMP(clearGo.transform, "ClearGoldText",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0,-20), 36, new Color(1f,0.85f,0.2f,1f));
        clearGold.text = "+ 0 G";

        // ComboText
        var comboTMP = CreateTMP(root.transform, "ComboText",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0,80), 42, new Color(1f,0.6f,0f,1f), new Vector2(400,60));
        comboTMP.gameObject.SetActive(false);

        // ControlHint (에디터 전용 안내)
        var hintTMP = CreateTMP(root.transform, "ControlHint",
            new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0.5f,0),
            new Vector2(0,82), 12, new Color(1f,1f,1f,0.40f), new Vector2(600,20));

        // ── HUDManager 연결
        hudSO.FindProperty("_hpSlider").objectReferenceValue          = hpBar;
        hudSO.FindProperty("_spSlider").objectReferenceValue          = spBar;
        hudSO.FindProperty("_hpText").objectReferenceValue            = hpText;
        hudSO.FindProperty("_goldTMP").objectReferenceValue           = goldTMP;
        hudSO.FindProperty("_stageTMP").objectReferenceValue          = stageTMP;
        hudSO.FindProperty("_killTMP").objectReferenceValue           = killTMP;
        hudSO.FindProperty("_killSlider").objectReferenceValue        = killSlider;
        hudSO.FindProperty("_comboTMP").objectReferenceValue          = comboTMP;
        hudSO.FindProperty("_damageFlash").objectReferenceValue       = flashImg;
        hudSO.FindProperty("_deadPanel").objectReferenceValue         = deadGo;
        hudSO.FindProperty("_stageClearPanel").objectReferenceValue   = clearGo;
        hudSO.FindProperty("_stageClearGoldTMP").objectReferenceValue = clearGold;
        hudSO.FindProperty("_controlHint").objectReferenceValue       = hintTMP;
        hudSO.ApplyModifiedProperties();

        Debug.Log("[SoulStrikeSetup] HUD 재생성 완료 (참고 레이아웃 적용)");
    }

    // ── Joystick Canvas ───────────────────────────────────────────

    static void SetupJoystickCanvas()
    {
        if (GameObject.Find("Canvas_Joystick") != null) return;

        var canvasGo = new GameObject("Canvas_Joystick");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var jScaler = canvasGo.AddComponent<CanvasScaler>();
        jScaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        jScaler.referenceResolution = new Vector2(1920f, 1080f);
        jScaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background — bottom-left, landscape 기준
        var bgGo   = new GameObject("JoystickBackground");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 0f);
        bgRect.anchorMax        = new Vector2(0f, 0f);
        bgRect.pivot            = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(160f, 330f);
        bgRect.sizeDelta        = new Vector2(180f, 180f);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);

        // Handle
        var handleGo   = new GameObject("JoystickHandle");
        handleGo.transform.SetParent(bgGo.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.sizeDelta        = new Vector2(70f, 70f);
        handleRect.anchoredPosition = Vector2.zero;
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(1f, 1f, 1f, 0.4f);

        // VirtualJoystick
        var joystick = bgGo.AddComponent<VirtualJoystick>();
        var so = new SerializedObject(joystick);
        so.FindProperty("_background").objectReferenceValue = bgRect;
        so.FindProperty("_handle").objectReferenceValue     = handleRect;
        so.ApplyModifiedProperties();

        Debug.Log("[SoulStrikeSetup] Joystick Canvas 생성 완료");
    }

    // ── UI Helpers ────────────────────────────────────────────────

    /// <summary>Slider 생성 (Background + Fill Area + Fill 포함)</summary>
    static Slider CreateSlider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Color fillColor)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;

        // Background
        var bgGo   = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin  = Vector2.zero;
        bgRect.anchorMax  = Vector2.one;
        bgRect.offsetMin  = Vector2.zero;
        bgRect.offsetMax  = Vector2.zero;
        var bgImg  = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Fill Area
        var fillAreaGo   = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(go.transform, false);
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin  = Vector2.zero;
        fillAreaRect.anchorMax  = Vector2.one;
        fillAreaRect.offsetMin  = new Vector2(5f, 0f);
        fillAreaRect.offsetMax  = new Vector2(-5f, 0f);

        var fillGo   = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin  = new Vector2(0f, 0f);
        fillRect.anchorMax  = new Vector2(1f, 1f);
        fillRect.offsetMin  = Vector2.zero;
        fillRect.offsetMax  = Vector2.zero;
        var fillImg  = fillGo.AddComponent<Image>();
        fillImg.color = fillColor;

        // Slider
        var slider = go.AddComponent<Slider>();
        slider.fillRect       = fillRect;
        slider.direction      = Slider.Direction.LeftToRight;
        slider.minValue       = 0f;
        slider.maxValue       = 1f;
        slider.value          = 1f;
        slider.interactable   = false;
        slider.transition      = Selectable.Transition.None;

        return slider;
    }

    /// <summary>TextMeshProUGUI 생성</summary>
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
        rect.sizeDelta        = sizeDelta == default ? new Vector2(200f, 40f) : sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text      = name;
        return tmp;
    }

    // ── Helpers ───────────────────────────────────────────────────

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static void EnsureSceneObject<T>(string name) where T : Component
    {
        if (Object.FindObjectOfType<T>() != null) return;
        var go = new GameObject(name);
        go.AddComponent<T>();
    }

    // ── UI Layout helpers ─────────────────────────────────────────

    /// <summary>RectTransform GameObject 생성</summary>
    static RectTransform NewRect(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        return rect;
    }

    /// <summary>배경 Image가 있는 패널 생성</summary>
    static GameObject NewPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        var rect = NewRect(parent, name, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
        var img  = rect.gameObject.AddComponent<Image>();
        img.color        = bgColor;
        img.raycastTarget = false;
        return rect.gameObject;
    }

    /// <summary>텍스트 라벨을 가진 Button 생성</summary>
    static Button MakeTextBtn(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor, float fontSize)
    {
        var rect = NewRect(parent, name, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
        var img  = rect.gameObject.AddComponent<Image>();
        img.color = bgColor;
        var btn  = rect.gameObject.AddComponent<Button>();
        var tmp  = CreateTMP(rect, name + "Label",
            Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero,
            fontSize, Color.white);
        tmp.text = label;
        return btn;
    }

    /// <summary>하단 네비게이션 탭 버튼 (index/total 비율로 위치 결정)</summary>
    static Button MakeNavTab(Transform parent, string name, string label,
        int index, int total, Color bgColor, float fontSize)
    {
        float w = 1f / total;
        var rect = NewRect(parent, name,
            new Vector2(w * index,      0),
            new Vector2(w * (index+1),  1),
            new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        var img = rect.gameObject.AddComponent<Image>();
        img.color = bgColor;
        var btn = rect.gameObject.AddComponent<Button>();
        var tmp = CreateTMP(rect, name + "Label",
            Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f), Vector2.zero,
            fontSize, Color.white);
        tmp.text = label;
        return btn;
    }
}
