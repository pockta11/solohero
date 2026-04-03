using UnityEngine;

/// <summary>
/// 장비·업그레이드 변경 시 PlayerController 스탯 재계산.
/// </summary>
public class PlayerEquipmentApplier : MonoBehaviour
{
    void Start() => ApplyNow();

    public static void ApplyNow()
    {
        if (GameManager.Instance?.PlayerData == null) return;
        var pd = GameManager.Instance.PlayerData;

        // 장비 보너스
        var (eqHp, eqAtk, eqDef) = PlayerEquipmentService.SumEquippedBonuses(pd);

        // 업그레이드 보너스
        var (upHp, upAtk, upDef, upSpd) = UpgradeService.SumUpgradeBonuses(pd);

        var player = FindObjectOfType<PlayerController>();
        player?.ApplyEquipmentBonuses(eqHp + upHp, eqAtk + upAtk, eqDef + upDef, upSpd);
    }
}
