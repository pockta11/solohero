using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 1개. InventoryPanelUI가 동적으로 생성해 데이터를 주입.
/// </summary>
public class InventoryItemSlotUI : MonoBehaviour
{
    [SerializeField] Image             _bg;
    [SerializeField] TextMeshProUGUI   _nameText;
    [SerializeField] TextMeshProUGUI   _gradeText;
    [SerializeField] TextMeshProUGUI   _statsText;
    [SerializeField] TextMeshProUGUI   _slotText;    // "Sword" / "Helm" 등
    [SerializeField] Button            _equipButton;
    [SerializeField] TextMeshProUGUI   _equipBtnText; // "Equip" or "Equipped"

    static readonly Color CommonBg    = new(0.20f, 0.20f, 0.22f, 0.95f);
    static readonly Color RareBg      = new(0.10f, 0.20f, 0.40f, 0.95f);
    static readonly Color EpicBg      = new(0.25f, 0.10f, 0.40f, 0.95f);
    static readonly Color LegendaryBg = new(0.40f, 0.30f, 0.05f, 0.95f);

    private EquipmentData _data;
    private System.Action _onEquip;

    public void Setup(EquipmentData eq, bool isEquipped, System.Action onEquip)
    {
        _data    = eq;
        _onEquip = onEquip;

        Color bg = eq.grade switch
        {
            EquipmentGrade.Rare      => RareBg,
            EquipmentGrade.Epic      => EpicBg,
            EquipmentGrade.Legendary => LegendaryBg,
            _                        => CommonBg,
        };

        if (_bg        != null) _bg.color        = bg;
        if (_nameText  != null) _nameText.text    = eq.equipmentName;
        if (_gradeText != null) _gradeText.text   = eq.grade.ToString();
        if (_statsText != null) _statsText.text   =
            $"ATK +{eq.attackBonus}  DEF +{eq.defenseBonus}  HP +{eq.hpBonus}";
        if (_slotText  != null) _slotText.text    = eq.slot.ToString();

        SetEquippedState(isEquipped);

        _equipButton?.onClick.RemoveAllListeners();
        _equipButton?.onClick.AddListener(OnEquipClicked);
    }

    public void SetEquippedState(bool isEquipped)
    {
        if (_equipButton  != null) _equipButton.interactable  = !isEquipped;
        if (_equipBtnText != null) _equipBtnText.text         = isEquipped ? "Equipped" : "Equip";
    }

    void OnEquipClicked()
    {
        _onEquip?.Invoke();
        transform.DOShakeScale(0.2f, 0.15f, 10);
    }
}
