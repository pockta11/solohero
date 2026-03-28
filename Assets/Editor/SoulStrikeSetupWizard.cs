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
    // 생성 구조:
    //  Canvas_HUD
    //  ├─ HpBar           (Slider)
    //  │   └─ HpText      (TextMeshProUGUI)
    //  ├─ SpBar           (Slider)
    //  ├─ GoldText        (TextMeshProUGUI)
    //  ├─ StageText       (TextMeshProUGUI)
    //  ├─ ComboText       (TextMeshProUGUI)
    //  ├─ DamageFlash     (Image, 전체화면)
    //  ├─ DeadPanel       (GameObject > Text "DEAD")
    //  └─ ControlHint     (TextMeshProUGUI)

    static void SetupHUD()
    {
        if (GameObject.Find("Canvas_HUD") != null) return;

        // ── Canvas root
        var root   = new GameObject("Canvas_HUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);  // Landscape
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ── HUDManager
        var hud    = root.AddComponent<HUDManager>();
        var hudSO  = new SerializedObject(hud);

        // ── HP Bar (top-left)
        var hpBar  = CreateSlider(root.transform, "HpBar",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(220f, -45f), new Vector2(380f, 32f),
            new Color(0.2f, 0.8f, 0.2f, 1f));

        // HP Text (on top of slider)
        var hpText = CreateTMP(hpBar.gameObject.transform, "HpText",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, 13, Color.white);

        // ── SP Bar
        var spBar  = CreateSlider(root.transform, "SpBar",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(220f, -86f), new Vector2(380f, 26f),
            new Color(0.2f, 0.4f, 0.9f, 1f));

        // ── Gold Text (top-right)
        var goldTMP = CreateTMP(root.transform, "GoldText",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(-160f, -40f), 22, new Color(1f, 0.85f, 0.2f, 1f),
            new Vector2(280f, 44f));

        // ── Stage Text (top-center)
        var stageTMP = CreateTMP(root.transform, "StageText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -40f), 22, Color.white,
            new Vector2(280f, 44f));

        // ── Kill Counter (top-right, below gold)
        var killTMP = CreateTMP(root.transform, "KillText",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-160f, -88f), 18, Color.white,
            new Vector2(280f, 36f));
        killTMP.text = "0 / 0";

        // ── Combo Text (center-upper)
        var comboTMP = CreateTMP(root.transform, "ComboText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), 42, new Color(1f, 0.6f, 0f, 1f),
            new Vector2(400f, 60f));
        comboTMP.gameObject.SetActive(false);

        // ── Damage Flash (fullscreen red overlay)
        var flashGo   = new GameObject("DamageFlash");
        flashGo.transform.SetParent(root.transform, false);
        var flashRect = flashGo.AddComponent<RectTransform>();
        flashRect.anchorMin      = Vector2.zero;
        flashRect.anchorMax      = Vector2.one;
        flashRect.offsetMin      = Vector2.zero;
        flashRect.offsetMax      = Vector2.zero;
        var flashImg  = flashGo.AddComponent<Image>();
        flashImg.color = new Color(1f, 0f, 0f, 0f);
        flashImg.raycastTarget = false;

        // ── Dead Panel (center overlay)
        var deadGo   = new GameObject("DeadPanel");
        deadGo.transform.SetParent(root.transform, false);
        var deadRect = deadGo.AddComponent<RectTransform>();
        deadRect.anchorMin      = Vector2.zero;
        deadRect.anchorMax      = Vector2.one;
        deadRect.offsetMin      = Vector2.zero;
        deadRect.offsetMax      = Vector2.zero;
        var deadBg   = deadGo.AddComponent<Image>();
        deadBg.color = new Color(0f, 0f, 0f, 0.6f);
        deadGo.SetActive(false);

        var deadText = CreateTMP(deadGo.transform, "DeadText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero, 60, Color.red);
        deadText.text = "YOU DIED";

        // ── Stage Clear Panel (center overlay)
        var clearGo   = new GameObject("StageClearPanel");
        clearGo.transform.SetParent(root.transform, false);
        var clearRect = clearGo.AddComponent<RectTransform>();
        clearRect.anchorMin      = Vector2.zero;
        clearRect.anchorMax      = Vector2.one;
        clearRect.offsetMin      = Vector2.zero;
        clearRect.offsetMax      = Vector2.zero;
        var clearBg   = clearGo.AddComponent<Image>();
        clearBg.color = new Color(0f, 0f, 0f, 0.5f);
        clearGo.SetActive(false);

        var clearTitle = CreateTMP(clearGo.transform, "ClearTitle",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), 56, new Color(1f, 0.9f, 0.2f, 1f));
        clearTitle.text = "STAGE CLEAR!";

        var clearGold = CreateTMP(clearGo.transform, "ClearGoldText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -20f), 36, new Color(1f, 0.85f, 0.2f, 1f));
        clearGold.text = "+ 0 G";

        // ── Control Hint (bottom-right)
        var hintTMP = CreateTMP(root.transform, "ControlHint",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-20f, 30f), 13, new Color(1f, 1f, 1f, 0.55f),
            new Vector2(550f, 30f));

        // ── Wire up HUDManager serialized fields
        hudSO.FindProperty("_hpSlider").objectReferenceValue        = hpBar;
        hudSO.FindProperty("_spSlider").objectReferenceValue        = spBar;
        hudSO.FindProperty("_hpText").objectReferenceValue          = hpText;
        hudSO.FindProperty("_goldTMP").objectReferenceValue         = goldTMP;
        hudSO.FindProperty("_stageTMP").objectReferenceValue        = stageTMP;
        hudSO.FindProperty("_killTMP").objectReferenceValue         = killTMP;
        hudSO.FindProperty("_comboTMP").objectReferenceValue        = comboTMP;
        hudSO.FindProperty("_damageFlash").objectReferenceValue     = flashImg;
        hudSO.FindProperty("_deadPanel").objectReferenceValue       = deadGo;
        hudSO.FindProperty("_stageClearPanel").objectReferenceValue = clearGo;
        hudSO.FindProperty("_stageClearGoldTMP").objectReferenceValue = clearGold;
        hudSO.FindProperty("_controlHint").objectReferenceValue     = hintTMP;
        hudSO.ApplyModifiedProperties();

        Debug.Log("[SoulStrikeSetup] HUD Canvas 생성 및 HUDManager 연결 완료");
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
}
