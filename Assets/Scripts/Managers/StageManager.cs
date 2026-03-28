using System.Collections;
using UnityEngine;

/// <summary>
/// 스테이지 진행 관리.
/// Chapter × Stage 구조, 적을 모두 처치하면 골드 보상 후 다음 스테이지로 자동 진행.
/// </summary>
public class StageManager : SingletonMB<StageManager>
{
    public int Chapter { get; private set; } = 1;
    public int Stage   { get; private set; } = 1;

    private int  _killsNeeded;
    private int  _killsCurrent;
    private bool _stageActive;

    // ──────────────────────────────────────────────────────────────

    void Start()
    {
#if UNITY_EDITOR
        if (GameManager.Instance != null && GameManager.Instance.PlayerData == null)
            GameManager.Instance.InitDev();
#endif
        int startChapter = GameManager.Instance?.PlayerData?.chapter ?? 1;
        LoadStage(startChapter, 1);
    }

    // ── 스테이지 로드 ─────────────────────────────────────────────

    public void LoadStage(int chapter, int stage)
    {
        Chapter = chapter;
        Stage   = stage;

        var cfg      = JsonDataManager.Instance.stageData ?? new JsonStageData();
        _killsNeeded = cfg.base_kill_count
                     + (chapter - 1) * cfg.kill_increment_per_chapter
                     + (stage   - 1) * cfg.kill_increment_per_stage;
        _killsCurrent = 0;
        _stageActive  = true;

        HUDManager.Instance?.SetStage(chapter, stage);
        HUDManager.Instance?.SetKillCount(0, _killsNeeded);

        SpawnManager.Instance?.StartStageSpawn(_killsNeeded);

        Debug.Log($"[StageManager] Ch.{chapter}-{stage} 시작 (목표: {_killsNeeded}킬)");
    }

    // ── 적 처치 콜백 ──────────────────────────────────────────────

    public void OnEnemyKilled()
    {
        if (!_stageActive) return;

        _killsCurrent++;
        HUDManager.Instance?.SetKillCount(_killsCurrent, _killsNeeded);

        if (_killsCurrent >= _killsNeeded)
            StartCoroutine(StageClear());
    }

    // ── 스테이지 클리어 ───────────────────────────────────────────

    IEnumerator StageClear()
    {
        _stageActive = false;

        var cfg  = JsonDataManager.Instance.stageData ?? new JsonStageData();
        long gold = cfg.base_gold_reward
                  + (Chapter - 1) * cfg.gold_increment_per_chapter
                  + (Stage   - 1) * cfg.gold_increment_per_stage;

        GameManager.Instance?.AddGold(gold);
        HUDManager.Instance?.RefreshGold();

        if (GameManager.Instance?.PlayerData != null)
            GameManager.Instance.PlayerData.chapter = Chapter;

        HUDManager.Instance?.ShowStageClear(gold);
        Debug.Log($"[StageManager] Ch.{Chapter}-{Stage} 클리어! +{gold}G");

        yield return new WaitForSeconds(3f);

        HUDManager.Instance?.HideStageClear();

        // 다음 스테이지 계산
        int nextStage   = Stage + 1;
        int nextChapter = Chapter;
        if (nextStage > cfg.stages_per_chapter)
        {
            nextStage   = 1;
            nextChapter++;
        }

        LoadStage(nextChapter, nextStage);
    }
}
