using System;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 진입점. LoginScene에 배치, DontDestroyOnLoad.
/// 초기화 순서: Firebase → 익명 로그인 → 데이터 로드 → 오프라인 보상 → GameScene 전환.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PlayerData PlayerData { get; private set; }

    /// <summary>오프라인 보상 결과. UI 팝업에서 읽고 표시 후 null로 초기화.</summary>
    public long PendingOfflineGold { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => InitializeAsync().Forget();

    private async UniTask InitializeAsync()
    {
        // 1. Firebase SDK 준비 확인
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"[GameManager] Firebase 초기화 실패: {status}");
            return;
        }

        // 2. 익명 로그인 (기존 세션 있으면 재사용)
        var auth = FirebaseAuth.DefaultInstance;
        string userId = null;

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId;
        }
        else
        {
            try
            {
                var result = await auth.SignInAnonymouslyAsync();
                userId = result.User.UserId;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] Firebase 로그인 실패, 로컬 모드로 전환: {e.Message}");
                userId = "local";   // Firebase 없이 PlayerPrefs 로컬 저장으로 동작
            }
        }

        // 3. SaveManager 초기화 후 데이터 로드
        SaveManager.Instance.Initialize(userId);
        PlayerData = await SaveManager.Instance.LoadAsync();
        if (PlayerData.chapter < 1) PlayerData.chapter = 1;

        // 4. 오프라인 보상 계산 (UI 팝업은 GameScene에서 PendingOfflineGold 확인 후 표시)
        PendingOfflineGold = OfflineRewardSystem.Calculate(PlayerData.lastQuitTimeUtc);
        if (PendingOfflineGold > 0)
        {
            PlayerData.gold += PendingOfflineGold;
            Debug.Log($"[GameManager] 오프라인 보상 +{PendingOfflineGold} 골드 " +
                      $"({OfflineRewardSystem.GetElapsedSeconds(PlayerData.lastQuitTimeUtc)}초)");
        }

        // 5. GameScene으로 전환
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>UI에서 오프라인 보상 팝업을 표시한 후 호출.</summary>
    public void ConsumeOfflineGold() => PendingOfflineGold = 0;

#if UNITY_EDITOR
    /// <summary>에디터에서 GameScene 직접 실행 시 Firebase 없이 임시 데이터로 초기화.</summary>
    public void InitDev()
    {
        PlayerData = new PlayerData
        {
            gold              = 1000,
            chapter           = 1,
            stageKillsCurrent = 0,
            gachaPullCount    = 0,
            lastQuitTimeUtc   = 0,
            equippedWeapon    = "",
            equippedHelmet    = "",
            equippedArmor     = "",
            equippedBoots     = "",
            ownedEquipmentCsv = ""
        };
        PendingOfflineGold = 0;
    }
#endif

    /// <summary>인게임 골드 변경 시 이 메서드를 통해 수정 (추후 이벤트 추가).</summary>
    public void AddGold(long amount)
    {
        PlayerData.gold += amount;
        // TODO: onGoldChanged 이벤트 → HUD 업데이트
    }

    // ── 저장 타이밍 ──────────────────────────────────────────

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveOnQuit().Forget();
    }

    void OnApplicationQuit() => SaveOnQuit().Forget();

    private async UniTask SaveOnQuit()
    {
        if (PlayerData == null) return;
        if (SaveManager.Instance == null) return;
        PlayerData.lastQuitTimeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await SaveManager.Instance.SaveAsync(PlayerData);
    }
}
