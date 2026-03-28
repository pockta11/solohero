using UnityEngine;

/// <summary>
/// 장비 변경 시 PlayerController의 스탯에 보너스 합산.
/// </summary>
public class PlayerEquipmentApplier : MonoBehaviour
{
    void Start() => ApplyNow();

    public static void ApplyNow()
    {
        if (GameManager.Instance?.PlayerData == null) return;

        var (hp, atk, def) = PlayerEquipmentService.SumEquippedBonuses(GameManager.Instance.PlayerData);

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            player.ApplyEquipmentBonuses(hp, atk, def);
    }
}
