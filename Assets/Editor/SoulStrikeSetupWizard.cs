using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tools > Setup > Setup SoulStrike GameScene
///
/// 실행하면 현재 열려있는 씬에 쿼터뷰 3D 게임을 위한 기본 오브젝트를 배치합니다:
///   - Main Camera (CameraFollow, CameraShake, CameraPassObj)
///   - Directional Light
///   - 30x30 Plane (NavMesh 베이크 대상)
///   - Player (Capsule + PlayerController + PlayerAutoController + NavMeshAgent)
///   - SpawnManager
///   - JsonDataManager
///   - HUDManager Canvas
///   - Joystick Canvas
/// </summary>
public static class SoulStrikeSetupWizard
{
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
        var existing = GameObject.Find("Directional Light");
        if (existing != null) return;

        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ── Ground ────────────────────────────────────────────────────

    static void SetupGround()
    {
        var existing = GameObject.Find("Ground");
        if (existing != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Ground";
        go.transform.localScale = new Vector3(3f, 1f, 3f); // 30x30
        go.isStatic = true;

        // NavMesh static 플래그 설정 (Unity 2022: NavigationStatic 대신 직접 플래그값 사용)
        var flags = GameObjectUtility.GetStaticEditorFlags(go);
        GameObjectUtility.SetStaticEditorFlags(go, flags | (StaticEditorFlags)64); // NavigationStatic = 64
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

        // NavMeshAgent
        var nma = go.AddComponent<NavMeshAgent>();
        nma.radius   = 0.3f;
        nma.height   = 2f;
        nma.speed    = 5f;
        nma.stoppingDistance = 1.5f;

        // Rigidbody (for collision events)
        var rb = go.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Animator (empty for now)
        go.AddComponent<Animator>();

        // Scripts
        EnsureComponent<PlayerAutoController>(go);
        EnsureComponent<PlayerController>(go);

        // CameraFollow target
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            var so = new UnityEditor.SerializedObject(camFollow);
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
    }

    // ── HUD Canvas ────────────────────────────────────────────────

    static void SetupHUD()
    {
        if (GameObject.Find("Canvas_HUD") != null) return;

        var go = new GameObject("Canvas_HUD");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        EnsureComponent<HUDManager>(go);
    }

    // ── Joystick Canvas ───────────────────────────────────────────

    static void SetupJoystickCanvas()
    {
        if (GameObject.Find("Canvas_Joystick") != null) return;

        var canvasGo = new GameObject("Canvas_Joystick");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Background
        var bgGo    = new GameObject("JoystickBackground");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect  = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin      = new Vector2(0f, 0f);
        bgRect.anchorMax      = new Vector2(0f, 0f);
        bgRect.pivot          = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(180f, 180f);
        bgRect.sizeDelta      = new Vector2(200f, 200f);
        bgGo.AddComponent<UnityEngine.UI.Image>();

        // Handle
        var handleGo   = new GameObject("JoystickHandle");
        handleGo.transform.SetParent(bgGo.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.sizeDelta      = new Vector2(80f, 80f);
        handleRect.anchoredPosition = Vector2.zero;
        handleGo.AddComponent<UnityEngine.UI.Image>();

        // VirtualJoystick
        var joystick = bgGo.AddComponent<VirtualJoystick>();
        var so = new UnityEditor.SerializedObject(joystick);
        so.FindProperty("_background").objectReferenceValue = bgRect;
        so.FindProperty("_handle").objectReferenceValue     = handleRect;
        so.ApplyModifiedProperties();

        Debug.Log("[SoulStrikeSetup] Joystick Canvas 생성 완료");
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
