using System;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using System.Threading;
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

    [Header("자동 저장")]
    [SerializeField] private float _autoSaveIntervalSeconds = 60f;
    private bool _autoSaveLoopStarted;

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

        // 4. 오프라인 보상 계산 (UI 팝업에서 수령 시 골드 반영 + 저장)
        float goldPerSec = _playerStatsSO != null
                           ? _playerStatsSO.goldPerSecond
                           : OfflineRewardSystem.BaseGoldPerSecond;
        PendingOfflineGold = OfflineRewardSystem.Calculate(PlayerData.lastQuitTimeUtc, goldPerSec);
        if (PendingOfflineGold > 0)
            Debug.Log($"[GameManager] 오프라인 보상 준비 +{PendingOfflineGold} 골드 " +
                      $"({OfflineRewardSystem.GetElapsedSeconds(PlayerData.lastQuitTimeUtc)}초)");

        // 장시간 플레이/방치 대비 주기 자동 저장
        if (!_autoSaveLoopStarted)
        {
            _autoSaveLoopStarted = true;
            AutoSaveLoop(this.GetCancellationTokenOnDestroy()).Forget();
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

    /// <summary>인게임 골드 변경. 구독자(UI)에게 변경을 알린다.</summary>
    public static event Action<long> GoldChanged;

    public void AddGold(long amount)
    {
        PlayerData.gold += amount;
        GoldChanged?.Invoke(PlayerData.gold);
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
        await SaveManager.Instance.FlushAsync(PlayerData);
    }

    private async UniTaskVoid AutoSaveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            int delayMs = Mathf.Max(5, Mathf.RoundToInt(_autoSaveIntervalSeconds * 1000f));
            await UniTask.Delay(delayMs, cancellationToken: ct);

            if (ct.IsCancellationRequested) break;
            if (PlayerData == null) continue;
            SaveManager.Instance?.RequestSave(PlayerData);
        }
    }
}
