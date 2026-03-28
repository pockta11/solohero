using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// ── JSON 데이터 구조체 ────────────────────────────────────────────────────

[Serializable]
public class JsonPlayerData
{
    public int   hp           = 500;
    public int   sp           = 250;
    public int   atk          = 50;
    public int   def          = 50;
    public float speed        = 5f;
    public float skill_cooltime = 10f;
    public int   combo        = 0;
}

[Serializable]
public class JsonEnemyData
{
    public int   hp                  = 200;
    public int   atk                 = 20;
    public int   def                 = 10;
    public float pursutied_distance  = 1.5f;
    public float near_distance       = 5f;
    public float far_distance        = 10f;
    public float slow_speed          = 2f;
    public float fast_speed          = 4f;
}

[Serializable]
public class JsonSpawnData
{
    public int   monster_max_count   = 10;
    public float monster_max_cooltime = 2f;
    public float spawn_radius        = 8f;
}

[Serializable]
public class JsonWeaponData
{
    public int atk = 15;
}

[Serializable]
public class JsonStageData
{
    public int   stages_per_chapter         = 5;
    public int   base_kill_count            = 5;
    public int   kill_increment_per_stage   = 2;
    public int   kill_increment_per_chapter = 10;
    public int   base_gold_reward           = 50;
    public int   gold_increment_per_stage   = 20;
    public int   gold_increment_per_chapter = 100;
    public float spawn_interval             = 1.5f;
}

// ── JsonDataManager ───────────────────────────────────────────────────────

/// <summary>
/// StreamingAssets에서 JSON 파일 4종을 읽어 메모리에 캐시.
/// 게임 시작 시 가장 먼저 초기화되어야 함.
/// </summary>
public class JsonDataManager : SingletonMB<JsonDataManager>
{
    public string FILEPATH_PLAYERDATA { get; private set; }
    public string FILEPATH_ENEMYDATA  { get; private set; }
    public string FILEPATH_SPAWNDATA  { get; private set; }
    public string FILEPATH_WEAPONDATA { get; private set; }
    public string FILEPATH_STAGEDATA  { get; private set; }

    public JsonPlayerData  playerData  { get; private set; }
    public JsonEnemyData[] enemyData   { get; private set; }
    public JsonSpawnData   spawnData   { get; private set; }
    public JsonWeaponData  weaponData  { get; private set; }
    public JsonStageData   stageData   { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        string dir = Application.streamingAssetsPath + "/JSON/";
        FILEPATH_PLAYERDATA = dir + "JSON_PlayerData.json";
        FILEPATH_ENEMYDATA  = dir + "JSON_EnemyData.json";
        FILEPATH_SPAWNDATA  = dir + "JSON_SpawnData.json";
        FILEPATH_WEAPONDATA = dir + "JSON_WeaponData.json";
        FILEPATH_STAGEDATA  = dir + "JSON_StageData.json";
        LoadAll();
    }

    void LoadAll()
    {
        playerData = Load<JsonPlayerData>(FILEPATH_PLAYERDATA);
        enemyData  = LoadOrDefault(FILEPATH_ENEMYDATA,
                         () => new[] { new JsonEnemyData() });
        spawnData  = Load<JsonSpawnData>(FILEPATH_SPAWNDATA);
        weaponData = Load<JsonWeaponData>(FILEPATH_WEAPONDATA);
        stageData  = Load<JsonStageData>(FILEPATH_STAGEDATA);
    }

    T Load<T>(string path) where T : new()
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[JsonDataManager] JSON 없음, 기본값 사용: {path}");
                return new T();
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonDataManager] 로드 실패 ({path}): {e.Message}");
            return new T();
        }
    }

    T LoadOrDefault<T>(string path, System.Func<T> defaultFactory)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[JsonDataManager] JSON 없음, 기본값 사용: {path}");
                return defaultFactory();
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonDataManager] 로드 실패 ({path}): {e.Message}");
            return defaultFactory();
        }
    }
}
