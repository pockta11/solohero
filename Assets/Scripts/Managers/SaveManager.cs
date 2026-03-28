using Cysharp.Threading.Tasks;
using Firebase.Database;
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

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Firebase Auth로 로그인 후 반드시 호출.</summary>
    public void Initialize(string userId)
    {
        _userRef = FirebaseDatabase.DefaultInstance
            .RootReference.Child("users").Child(userId);
    }

    /// <summary>Firebase에서 로드. 실패 시 로컬 백업에서 복구.</summary>
    public async UniTask<PlayerData> LoadAsync()
    {
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

    private void SaveLocalBackup(string json) => PlayerPrefs.SetString(LocalBackupKey, json);

    private PlayerData LoadLocalBackup()
    {
        string json = PlayerPrefs.GetString(LocalBackupKey, null);
        if (string.IsNullOrEmpty(json)) return new PlayerData();
        return JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
    }
}
