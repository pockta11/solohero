using System;

/// <summary>
/// Firebase RTDB 직렬화용 DTO.
/// JsonUtility 호환을 위해 [Serializable] 필수, 프로퍼티 사용 금지.
/// </summary>
[Serializable]
public class PlayerData
{
    public long gold;
    /// <summary>현재 스테이지 번호 (1부터). 클리어 시 증가.</summary>
    public int chapter = 1;
    public long lastQuitTimeUtc;

    /// <summary>현재 스테이지에서 처치한 몬스터 수 (클리어 조건 충족 시 0으로 리셋).</summary>
    public int stageKillsCurrent;

    /// <summary>가챠 천장 카운터 — GachaSystem과 동기화.</summary>
    public int gachaPullCount;

    /// <summary>장착 중인 장비 id (EquipmentData.GetId / 빈 문자열 = 없음).</summary>
    public string equippedWeapon = "";
    public string equippedHelmet = "";
    public string equippedArmor  = "";
    public string equippedBoots  = "";

    /// <summary>인벤토리: equipment id를 | 로 이어붙임.</summary>
    public string ownedEquipmentCsv = "";

    // ── 스테이지 진행 저장 ──────────────────────────────────────
    /// <summary>마지막으로 진행 중이던 스테이지 번호 (1~stages_per_chapter).</summary>
    public int stageNumber = 1;

    // ── 능력치 업그레이드 레벨 ──────────────────────────────────
    public int upgradeHpLevel;
    public int upgradeAtkLevel;
    public int upgradeDefLevel;
    public int upgradeSpdLevel;

    // ── 스키마 버전 (마이그레이션 대비) ──────────────────────────
    public int dataVersion = 1;
}
