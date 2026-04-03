/// <summary>
/// 업그레이드 레벨 → 스탯 보너스 변환 유틸.
/// 레벨업 비용 공식: baseCost × (level + 1)
/// </summary>
public static class UpgradeService
{
    // ── 레벨당 보너스 ────────────────────────────────────────────
    public const int   HpPerLevel  = 50;
    public const int   AtkPerLevel = 5;
    public const int   DefPerLevel = 5;
    public const float SpdPerLevel = 0.2f;

    // ── 기본 비용 (× (현재레벨+1) 이 다음 레벨업 비용) ───────────
    public const long HpBaseCost  = 100;
    public const long AtkBaseCost = 150;
    public const long DefBaseCost = 150;
    public const long SpdBaseCost = 200;

    // ── 최대 레벨 ────────────────────────────────────────────────
    public const int MaxLevel = 50;

    /// <summary>업그레이드 총 보너스 합산.</summary>
    public static (int hp, int atk, int def, float spd) SumUpgradeBonuses(PlayerData pd)
    {
        if (pd == null) return (0, 0, 0, 0f);
        return (
            pd.upgradeHpLevel  * HpPerLevel,
            pd.upgradeAtkLevel * AtkPerLevel,
            pd.upgradeDefLevel * DefPerLevel,
            pd.upgradeSpdLevel * SpdPerLevel
        );
    }

    /// <summary>다음 레벨업 비용.</summary>
    public static long NextCost(long baseCost, int currentLevel) =>
        baseCost * (currentLevel + 1);

    public static long HpUpgradeCost(int level)  => NextCost(HpBaseCost,  level);
    public static long AtkUpgradeCost(int level) => NextCost(AtkBaseCost, level);
    public static long DefUpgradeCost(int level) => NextCost(DefBaseCost, level);
    public static long SpdUpgradeCost(int level) => NextCost(SpdBaseCost, level);
}
