using System;
using UnityEngine;

public enum EquipmentSlot { Weapon, Helmet, Armor, Boots }
public enum EquipmentGrade { Common, Rare, Epic, Legendary }

/// <summary>
/// 장비 ScriptableObject. 가챠 드롭테이블에 등록.
/// </summary>
[CreateAssetMenu(menuName = "SoloHero/Equipment", fileName = "NewEquipment")]
public class EquipmentData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("저장·가챠 식별용. 비우면 equipmentName 사용")]
    public string id;

    public string equipmentName;
    public EquipmentSlot slot;
    public EquipmentGrade grade;
    public Sprite icon;

    [Header("스탯 보너스")]
    public int attackBonus;
    public int defenseBonus;
    public int hpBonus;

    [Header("가챠 가중치 (높을수록 잘 나옴)")]
    [Range(0f, 1f)] public float weight = 0.5f;

    public string GetId() => string.IsNullOrEmpty(id) ? equipmentName : id;

    public bool MatchesId(string key) =>
        !string.IsNullOrEmpty(key) && string.Equals(GetId(), key, StringComparison.Ordinal);
}
