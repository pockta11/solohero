using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 탭 — 무기/투구/갑옷/신발 4슬롯 + 합산 스탯 요약 + 인벤토리 패널 토글.
/// </summary>
public class EquipmentPanelUI : MonoBehaviour
{
    [SerializeField] Image _weaponIcon;
    [SerializeField] Image _helmetIcon;
    [SerializeField] Image _armorIcon;
    [SerializeField] Image _bootsIcon;
    [SerializeField] TextMeshProUGUI _summaryText;

    [Header("인벤토리")]
    [SerializeField] GameObject       _inventoryPanel;  // InventoryPanelUI가 붙은 GameObject
    [SerializeField] Button           _inventoryToggleButton;

    void Start()
    {
        _inventoryToggleButton?.onClick.AddListener(ToggleInventory);
        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
    }

    void OnEnable()
    {
        PlayerEquipmentService.EquipmentChanged += Refresh;
        Refresh();
    }

    void OnDisable() => PlayerEquipmentService.EquipmentChanged -= Refresh;

    void ToggleInventory()
    {
        if (_inventoryPanel == null) return;
        bool next = !_inventoryPanel.activeSelf;
        _inventoryPanel.SetActive(next);
        if (next && _inventoryPanel.TryGetComponent<InventoryPanelUI>(out var inv))
            inv.Rebuild();
    }

    public void Refresh()
    {
        var pd = GameManager.Instance?.PlayerData;
        SetSlot(_weaponIcon, PlayerEquipmentService.GetEquippedId(pd, EquipmentSlot.Weapon));
        SetSlot(_helmetIcon, PlayerEquipmentService.GetEquippedId(pd, EquipmentSlot.Helmet));
        SetSlot(_armorIcon, PlayerEquipmentService.GetEquippedId(pd, EquipmentSlot.Armor));
        SetSlot(_bootsIcon, PlayerEquipmentService.GetEquippedId(pd, EquipmentSlot.Boots));

        if (_summaryText != null)
        {
            var sum = PlayerEquipmentService.SumEquippedBonuses(pd);
            _summaryText.text = $"Total  ATK +{sum.atk}  DEF +{sum.def}  HP +{sum.hp}";
        }
    }

    static void SetSlot(Image img, string id)
    {
        if (img == null) return;
        var eq = PlayerEquipmentService.ResolveEquipment(id);
        img.sprite = eq != null ? eq.icon : null;
        img.color  = eq != null ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.9f);
    }
}
