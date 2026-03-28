using System;

/// <summary>
/// 오프라인 보상 계산 (순수 정적 유틸).
/// Firebase나 MonoBehaviour 의존성 없음 — 테스트 용이.
/// </summary>
public static class OfflineRewardSystem
{
    /// <summary>최대 오프라인 보상 시간 (6시간)</summary>
    public const long MaxOfflineSeconds = 21_600;

    /// <summary>초당 기본 골드 획득량 — 전투 시스템 완성 후 스탯 기반으로 교체</summary>
    public const float BaseGoldPerSecond = 1f;

    /// <param name="lastQuitTimeUtc">Unix 초 단위 UTC 타임스탬프</param>
    /// <param name="goldPerSecond">현재 플레이어 골드 획득 속도</param>
    /// <returns>획득할 골드 양</returns>
    public static long Calculate(long lastQuitTimeUtc, float goldPerSecond = BaseGoldPerSecond)
    {
        if (lastQuitTimeUtc <= 0) return 0;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = Math.Min(now - lastQuitTimeUtc, MaxOfflineSeconds);
        if (elapsed <= 0) return 0;

        return (long)(elapsed * goldPerSecond);
    }

    /// <summary>오프라인 경과 시간 반환 (초). UI 표시용.</summary>
    public static long GetElapsedSeconds(long lastQuitTimeUtc)
    {
        if (lastQuitTimeUtc <= 0) return 0;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Min(now - lastQuitTimeUtc, MaxOfflineSeconds);
    }
}
