using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 스폰 관리. JsonSpawnData 기반으로 주기적으로 생성.
/// </summary>
public class SpawnManager : SingletonMB<SpawnManager>
{
    [Header("몬스터 Prefab 목록 (EnemyType 순서대로)")]
    [SerializeField] private List<GameObject> _monsterPrefabs;

    [Header("스폰 기준 Transform (null이면 원점)")]
    [SerializeField] private Transform _spawnCenter;

    private JsonSpawnData  _cfg;
    private GameObject     _pool;
    private float          _coolRemain = 0f;

    // ──────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _pool = new GameObject("MonsterPool");
        _cfg  = JsonDataManager.Instance.spawnData ?? new JsonSpawnData();
    }

    void Update()
    {
        _coolRemain -= Time.deltaTime;
        if (_coolRemain <= 0f && CountAlive() < _cfg.monster_max_count)
        {
            _coolRemain = Random.Range(0f, _cfg.monster_max_cooltime);
            StartCoroutine(Spawn());
        }
    }

    int CountAlive()
    {
        return _pool.transform.GetComponentsInChildren<EnemyController>().Length;
    }

    IEnumerator Spawn()
    {
        yield return null; // 한 프레임 대기

        if (_monsterPrefabs == null || _monsterPrefabs.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 몬스터 Prefab 없음");
            yield break;
        }

        int idx = Random.Range(0, Mathf.Min(_monsterPrefabs.Count, (int)EnemyType.MAX));
        GameObject prefab = _monsterPrefabs[idx];

        Vector3 center = _spawnCenter != null ? _spawnCenter.position : Vector3.zero;
        Vector3 randDir = Random.insideUnitSphere * _cfg.spawn_radius;
        randDir.y = 0f;
        Vector3 spawnPos = center + randDir;

        // NavMesh 위 유효 위치 확인
        if (!NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[SpawnManager] 유효한 NavMesh 위치 없음");
            yield break;
        }

        GameObject monster = Instantiate(prefab, hit.position, Quaternion.identity, _pool.transform);
        Debug.Log($"[SpawnManager] 스폰: {(EnemyType)idx} @ {hit.position}");
    }

    /// <summary>가장 가까운 몬스터 Transform 반환 (없으면 null).</summary>
    public Transform GetNearestMonster(Vector3 from)
    {
        var enemies = _pool.transform.GetComponentsInChildren<EnemyController>();
        if (enemies.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var e in enemies)
        {
            float d = Vector3.Distance(from, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e.transform; }
        }

        return nearest;
    }
}
