using System;
using UnityEngine;

/// <summary>
/// 저장된 PlayerData ↔ 장비 ScriptableObject 매핑, 가챠 결과 자동 장착·인벤 처리.
/// </summary>
public static class PlayerEquipmentService
{
    public static event Action EquipmentChanged;
    public static void NotifyEquipmentChanged() => EquipmentChanged?.Invoke();

    public static string GetEquippedId(PlayerData pd, EquipmentSlot slot)
    {
        if (pd == null) return "";
        return slot switch
        {
            EquipmentSlot.Sword => pd.equippedWeapon ?? "",
            EquipmentSlot.Helm  => pd.equippedHelmet ?? "",
            EquipmentSlot.Armor => pd.equippedArmor ?? "",
            EquipmentSlot.Boots => pd.equippedBoots ?? "",
            _                      => ""
        };
    }

    public static void SetEquippedId(PlayerData pd, EquipmentSlot slot, string id)
    {
        if (pd == null) return;
        id ??= "";
        switch (slot)
        {
            case EquipmentSlot.Sword: pd.equippedWeapon = id; break;
            case EquipmentSlot.Helm:  pd.equippedHelmet = id; break;
            case EquipmentSlot.Armor: pd.equippedArmor  = id; break;
            case EquipmentSlot.Boots: pd.equippedBoots  = id; break;
        }
    }

    public static EquipmentData ResolveEquipment(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return GachaSystem.Instance != null
            ? GachaSystem.Instance.FindEquipmentById(id)
            : null;
    }

    public static (int hp, int atk, int def) SumEquippedBonuses(PlayerData pd)
    {
        if (pd == null) return (0, 0, 0);
        int hp = 0, atk = 0, def = 0;
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            var eq = ResolveEquipment(GetEquippedId(pd, slot));
            if (eq == null) continue;
            hp  += eq.hpBonus;
            atk += eq.attackBonus;
            def += eq.defenseBonus;
        }

        return (hp, atk, def);
    }

    /// <summary>가챠 획득 → 더 좋으면 장착, 기존 장비는 인벤으로. 아니면 인벤만.</summary>
    public static void OnGachaResult(EquipmentData pulled)
    {
        if (pulled == null || GameManager.Instance?.PlayerData == null) return;

        var pd     = GameManager.Instance.PlayerData;
        string cur = GetEquippedId(pd, pulled.slot);
        var curEq  = ResolveEquipment(cur);

        if (curEq == null || IsStrictlyBetter(pulled, curEq))
        {
            if (!string.IsNullOrEmpty(cur))
                AddOwned(pd, cur);
            SetEquippedId(pd, pulled.slot, pulled.GetId());
        }
        else
            AddOwned(pd, pulled.GetId());

        EquipmentChanged?.Invoke();
    }

    static bool IsStrictlyBetter(EquipmentData a, EquipmentData b)
    {
        if (a.grade != b.grade) return a.grade > b.grade;
        return StatSum(a) > StatSum(b);
    }

    static int StatSum(EquipmentData e) =>
        e.attackBonus + e.defenseBonus + e.hpBonus;

    public static void AddOwned(PlayerData pd, string equipmentId)
    {
        if (pd == null || string.IsNullOrEmpty(equipmentId)) return;
        if (string.IsNullOrEmpty(pd.ownedEquipmentCsv))
            pd.ownedEquipmentCsv = equipmentId;
        else
            pd.ownedEquipmentCsv += "|" + equipmentId;
    }
}
