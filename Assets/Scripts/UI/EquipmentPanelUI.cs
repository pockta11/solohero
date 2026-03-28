using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 장비 탭 — 무기/투구/갑옷/신발 4슬롯 + 합산 스탯 요약.
/// </summary>
public class EquipmentPanelUI : MonoBehaviour
{
    [SerializeField] Image _weaponIcon;
    [SerializeField] Image _helmetIcon;
    [SerializeField] Image _armorIcon;
    [SerializeField] Image _bootsIcon;
    [SerializeField] TextMeshProUGUI _summaryText;

    void OnEnable()
    {
        PlayerEquipmentService.EquipmentChanged += Refresh;
        Refresh();
    }

    void OnDisable() => PlayerEquipmentService.EquipmentChanged -= Refresh;

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
            _summaryText.text = $"합산  ATK +{sum.atk}  DEF +{sum.def}  HP +{sum.hp}";
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
