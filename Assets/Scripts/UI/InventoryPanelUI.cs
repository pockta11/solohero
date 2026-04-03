using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 장비 인벤토리 패널.
/// ownedEquipmentCsv를 파싱해 슬롯 목록 표시, 슬롯 클릭 시 장착.
/// </summary>
public class InventoryPanelUI : MonoBehaviour
{
    [Header("슬롯 프리팹 & 컨테이너")]
    [SerializeField] InventoryItemSlotUI _slotPrefab;
    [SerializeField] Transform           _contentRoot;  // ScrollView > Content

    private readonly List<InventoryItemSlotUI> _slots = new();

    void OnEnable() => Rebuild();

    // ── 목록 재구성 ───────────────────────────────────────────────

    public void Rebuild()
    {
        if (_slotPrefab == null || _contentRoot == null) return;

        // 기존 슬롯 제거
        foreach (var s in _slots) Destroy(s.gameObject);
        _slots.Clear();

        var pd = GameManager.Instance?.PlayerData;
        if (pd == null) return;

        // 보유 장비 id 파싱
        var ids = ParseIds(pd.ownedEquipmentCsv);

        foreach (string id in ids)
        {
            var eq = PlayerEquipmentService.ResolveEquipment(id);
            if (eq == null) continue;

            bool isEquipped = PlayerEquipmentService.GetEquippedId(pd, eq.slot) == id;

            var slot = Instantiate(_slotPrefab, _contentRoot);
            string capturedId = id;
            slot.Setup(eq, isEquipped, () => Equip(capturedId, eq.slot));
            _slots.Add(slot);
        }
    }

    // ── 장착 처리 ─────────────────────────────────────────────────

    void Equip(string id, EquipmentSlot slot)
    {
        var pd = GameManager.Instance?.PlayerData;
        if (pd == null) return;

        string prevId = PlayerEquipmentService.GetEquippedId(pd, slot);

        // 기존 장착 장비를 인벤에 추가 (없으면 건너뜀)
        if (!string.IsNullOrEmpty(prevId) && prevId != id)
            PlayerEquipmentService.AddOwned(pd, prevId);

        // 인벤에서 장착 장비 제거
        RemoveOwned(pd, id);

        // 장착 처리
        PlayerEquipmentService.SetEquippedId(pd, slot, id);

        PlayerEquipmentApplier.ApplyNow();
        PlayerEquipmentService.NotifyEquipmentChanged();
        Rebuild(); // 슬롯 상태 갱신
        SaveManager.Instance?.RequestSave(pd);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────

    static List<string> ParseIds(string csv)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(csv)) return list;
        foreach (var id in csv.Split('|'))
        {
            string trimmed = id.Trim();
            if (!string.IsNullOrEmpty(trimmed)) list.Add(trimmed);
        }
        return list;
    }

    static void RemoveOwned(PlayerData pd, string removeId)
    {
        if (string.IsNullOrEmpty(pd.ownedEquipmentCsv)) return;
        var ids = new List<string>(pd.ownedEquipmentCsv.Split('|'));
        ids.RemoveAll(x => x.Trim() == removeId);
        pd.ownedEquipmentCsv = string.Join("|", ids);
    }
}
