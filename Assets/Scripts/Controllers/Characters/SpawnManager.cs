using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 스폰 관리.
/// StageManager가 StartStageSpawn(count)를 호출하면 지정 수만큼 간격을 두고 스폰.
/// </summary>
public class SpawnManager : SingletonMB<SpawnManager>
{
    [Header("몬스터 Prefab 목록 (EnemyType 순서대로)")]
    [SerializeField] private List<GameObject> _monsterPrefabs;

    [Header("스폰 기준 Transform (null이면 원점)")]
    [SerializeField] private Transform _spawnCenter;

    private GameObject _pool;
    private float      _spawnInterval;

    // ──────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _pool = new GameObject("MonsterPool");
    }

    // ── 스테이지 스폰 ─────────────────────────────────────────────

    /// <summary>StageManager에서 호출. 지정 수만큼 간격을 두고 순차 스폰.</summary>
    public void StartStageSpawn(int totalCount)
    {
        // 이전 스폰 코루틴 중단 + 남은 몬스터 제거
        StopAllCoroutines();
        ClearPool();

        var cfg = JsonDataManager.Instance.stageData ?? new JsonStageData();
        _spawnInterval = cfg.spawn_interval;

        StartCoroutine(SpawnSequence(totalCount));
    }

    IEnumerator SpawnSequence(int totalCount)
    {
        int spawned = 0;

        while (spawned < totalCount)
        {
            yield return new WaitForSeconds(_spawnInterval);
            if (TrySpawnOne()) spawned++;
        }

    }

    bool TrySpawnOne()
    {
        if (_monsterPrefabs == null || _monsterPrefabs.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] 몬스터 Prefab 없음");
            return false;
        }

        int idx     = Random.Range(0, Mathf.Min(_monsterPrefabs.Count, (int)EnemyType.MAX));
        var prefab  = _monsterPrefabs[idx];

        Vector3 center   = _spawnCenter != null ? _spawnCenter.position : Vector3.zero;
        Vector3 randDir  = Random.insideUnitSphere;
        randDir.y = 0f;

        var cfg = JsonDataManager.Instance.spawnData ?? new JsonSpawnData();
        Vector3 spawnPos = center + randDir.normalized * cfg.spawn_radius;

        if (!NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[SpawnManager] 유효한 NavMesh 위치 없음");
            return false;
        }

        Instantiate(prefab, hit.position, Quaternion.identity, _pool.transform);
        return true;
    }

    void ClearPool()
    {
        for (int i = _pool.transform.childCount - 1; i >= 0; i--)
            Destroy(_pool.transform.GetChild(i).gameObject);
    }

    // ── 유틸 ──────────────────────────────────────────────────────

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
