using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Firebase RTDB 읽기/쓰기 담당.
/// PlayerPrefs를 로컬 백업으로 사용해 앱 강제 종료 시 데이터 손실 방지.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string LocalBackupKey = "player_data_backup";

    private DatabaseReference _userRef;
    private bool _isLocalMode;

    // 저장 중복 방지/디바운스: 마지막 요청만 유지
    private bool _isSaving;
    private bool _hasQueuedSave;
    private string _queuedJson;
    private CancellationTokenSource _debounceCts;
    private const int DebounceMs = 250;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Firebase Auth로 로그인 후 반드시 호출. userId == "local" 이면 로컬 전용 모드.</summary>
    public void Initialize(string userId)
    {
        if (userId == "local")
        {
            _isLocalMode = true;
            _userRef     = null;
            Debug.Log("[SaveManager] 로컬 전용 모드 (Firebase 미사용)");
            return;
        }
        _isLocalMode = false;
        _userRef = FirebaseDatabase.DefaultInstance
            .RootReference.Child("users").Child(userId);
    }

    /// <summary>Firebase에서 로드. 실패 시 로컬 백업에서 복구.</summary>
    public async UniTask<PlayerData> LoadAsync()
    {
        if (_isLocalMode) return LoadLocalBackup();

        try
        {
            var snapshot = await _userRef.GetValueAsync();
            if (!snapshot.Exists) return new PlayerData();

            string json = snapshot.GetRawJsonValue();
            var data = JsonUtility.FromJson<PlayerData>(json);
            SaveLocalBackup(json); // 성공 시 로컬도 갱신
            return data ?? new PlayerData();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveManager] Firebase 로드 실패, 로컬 백업 사용: {e.Message}");
            return LoadLocalBackup();
        }
    }

    /// <summary>Firebase에 저장 + 로컬 백업 동시 갱신.</summary>
    public async UniTask SaveAsync(PlayerData data)
    {
        string json = JsonUtility.ToJson(data);
        SaveLocalBackup(json);

        if (_isLocalMode) return;

        try
        {
            await _userRef.SetRawJsonValueAsync(json);
        }
        catch (System.Exception e)
        {
            // 로컬 백업은 이미 저장됐으므로 다음 실행 시 재시도 가능
            Debug.LogWarning($"[SaveManager] Firebase 저장 실패 (로컬 백업 유지): {e.Message}");
        }
    }

    /// <summary>
    /// 저장 요청을 디바운스하여 마지막 상태만 저장.
    /// 저장 중 추가 요청이 오면 마지막 요청으로 덮어쓰고, 저장 완료 후 1회 추가 저장한다.
    /// </summary>
    public void RequestSave(PlayerData data)
    {
        if (data == null) return;
        _queuedJson = JsonUtility.ToJson(data);
        _hasQueuedSave = true;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        SaveDebouncedAsync(_debounceCts.Token).Forget();
    }

    /// <summary>대기 중인 저장이 있으면 즉시 처리(종료 시 호출 권장).</summary>
    public async UniTask FlushAsync(PlayerData data)
    {
        if (data != null)
        {
            _queuedJson = JsonUtility.ToJson(data);
            _hasQueuedSave = true;
        }

        _debounceCts?.Cancel();
        await SaveQueuedLoopAsync();
    }

    private async UniTaskVoid SaveDebouncedAsync(CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(DebounceMs, cancellationToken: ct);
            await SaveQueuedLoopAsync();
        }
        catch (OperationCanceledException)
        {
            // 최신 요청으로 교체됨
        }
    }

    private async UniTask SaveQueuedLoopAsync()
    {
        if (_isSaving) return;
        _isSaving = true;
        try
        {
            while (_hasQueuedSave)
            {
                _hasQueuedSave = false;
                var json = _queuedJson;
                if (!string.IsNullOrEmpty(json))
                {
                    SaveLocalBackup(json);
                    if (!_isLocalMode)
                    {
                        try { await _userRef.SetRawJsonValueAsync(json); }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[SaveManager] Firebase 저장 실패 (로컬 백업 유지): {e.Message}");
                        }
                    }
                }
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void SaveLocalBackup(string json)
    {
        PlayerPrefs.SetString(LocalBackupKey, json);
        PlayerPrefs.Save();
    }

    private PlayerData LoadLocalBackup()
    {
        string json = PlayerPrefs.GetString(LocalBackupKey, null);
        if (string.IsNullOrEmpty(json)) return new PlayerData();
        return JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
    }
}
