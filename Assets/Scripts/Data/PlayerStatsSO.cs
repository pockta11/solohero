using UnityEngine;

/// <summary>
/// 플레이어 기본 스탯 ScriptableObject.
/// PlayerController의 JsonPlayerData 대신 사용할 수 있는 Unity-native 설정 에셋.
/// Assets/ScriptableObjects/PlayerStats.asset 에 인스턴스 존재.
/// </summary>
[CreateAssetMenu(menuName = "SoloHero/PlayerStats", fileName = "PlayerStats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("기본 전투 스탯")]
    public int   maxHp          = 500;
    public int   maxSp          = 250;
    public int   attackPower    = 50;
    public int   defense        = 50;
    public float speed          = 5f;
    public float skillCooltime  = 10f;
    public float attackInterval = 0.6f;

    [Header("오프라인 보상")]
    [Tooltip("초당 골드 획득량 — 오프라인 방치 보상 계산에 사용")]
    public float goldPerSecond = 5f;

    [Header("속성 (향후 속성 시스템)")]
    public ElementType element = ElementType.None;

    /// <summary>JsonPlayerData 형식으로 변환. PlayerController 초기화 시 사용.</summary>
    public JsonPlayerData ToJsonPlayerData() => new JsonPlayerData
    {
        hp            = maxHp,
        sp            = maxSp,
        atk           = attackPower,
        def           = defense,
        speed         = speed,
        skill_cooltime = skillCooltime,
        combo         = 0
    };
}
