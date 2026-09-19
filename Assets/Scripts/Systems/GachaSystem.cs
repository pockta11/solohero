using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정규분포(가우시안) 기반 가챠 시스템.
/// 뽑기 횟수가 쌓일수록 분포 중심이 고등급 쪽으로 이동 (천장 시스템).
/// </summary>
public class GachaSystem : MonoBehaviour
{
    public static GachaSystem Instance { get; private set; }

    [Header("가챠 설정")]
    [SerializeField] private List<EquipmentData> _pool;
    [SerializeField] private int _pityCeiling = 100;    // 천장 (이 횟수면 Legendary 보장)
    [SerializeField] private long _costPerPull = 100;   // 1회 뽑기 비용

    private int _pullCount;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance?.PlayerData != null)
            _pullCount = GameManager.Instance.PlayerData.gachaPullCount;
    }

    public long PullCost => _costPerPull;

    void SyncPityToSave()
    {
        if (GameManager.Instance?.PlayerData != null)
            GameManager.Instance.PlayerData.gachaPullCount = _pullCount;
    }

    /// <summary>_pool에서 id로 장비 검색 (저장 복원·UI용).</summary>
    public EquipmentData FindEquipmentById(string id)
    {
        if (_pool == null || string.IsNullOrEmpty(id)) return null;
        foreach (var e in _pool)
        {
            if (e != null && e.MatchesId(id)) return e;
        }

        return null;
    }

    /// <summary>1회 뽑기. 골드 부족 시 null 반환.</summary>
    public EquipmentData Pull()
    {
        // 풀이 비어있으면 불가
        if (_pool == null || _pool.Count == 0)
        {
            Debug.LogWarning("[Gacha] Pool is empty");
            return null;
        }

        // 천장 도달 시 최고등급 보장
        EquipmentData result;
        if (_pullCount + 1 >= _pityCeiling)
        {
            result = GetHighestGrade();
        }
        else
        {
            result = DrawWithGaussian();
        }

        if (result == null)
        {
            Debug.LogWarning("[Gacha] 뽑기 결과가 null 입니다.");
            return null;
        }

        // 골드 체크
        var pd = GameManager.Instance?.PlayerData;
        if (pd != null && pd.gold < _costPerPull)
        {
            Debug.Log("[Gacha] 골드 부족");
            return null;
        }

        // 결제/카운트 확정
        if (pd != null)
            GameManager.Instance.AddGold(-_costPerPull);

        _pullCount++;
        if (_pullCount >= _pityCeiling) _pullCount = 0;
        SyncPityToSave();

        Debug.Log($"[Gacha] {result.equipmentName} ({result.grade}) — pity: {_pullCount}/{_pityCeiling}");

        PlayerEquipmentService.OnGachaResult(result);
        SaveManager.Instance?.RequestSave(pd);

        return result;
    }

    // ── 정규분포 기반 뽑기 ────────────────────────────────────

    private EquipmentData DrawWithGaussian()
    {
        if (_pool == null || _pool.Count == 0) return null;

        // 천장이 쌓일수록 평균(μ)을 높여 고등급 확률 상승
        float progress = Mathf.Clamp01((float)_pullCount / _pityCeiling);
        float mu    = Mathf.Lerp(0.3f, 0.8f, progress); // 초반 0.3 → 천장 직전 0.8
        float sigma = 0.2f;

        float sample = GaussianSample(mu, sigma);
        sample = Mathf.Clamp01(sample);

        // sample 값이 높을수록 고등급 장비 선택
        var candidates = new List<(EquipmentData eq, float score)>();
        foreach (var eq in _pool)
        {
            float score = 1f - Mathf.Abs(eq.weight - sample);
            candidates.Add((eq, score));
        }

        candidates.Sort((a, b) => b.score.CompareTo(a.score));
        return candidates[0].eq;
    }

    /// <summary>Box-Muller 변환으로 정규분포 샘플 생성.</summary>
    private static float GaussianSample(float mean, float stdDev)
    {
        float u1 = 1f - UnityEngine.Random.value;
        float u2 = 1f - UnityEngine.Random.value;
        float z  = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        return mean + z * stdDev;
    }

    private EquipmentData GetHighestGrade()
    {
        EquipmentData best = null;
        foreach (var eq in _pool)
            if (best == null || eq.grade > best.grade) best = eq;
        return best;
    }

    public int PullCount => _pullCount;
    public int PityCeiling => _pityCeiling;
}
