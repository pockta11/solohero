using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버섭커 스타일 하단 탭: 장비 / 업그레이드 / 뽑기.
/// 활성 탭은 밝은 보라, 비활성은 어두운 보라로 강조 표시.
/// </summary>
public class MainBottomNav : MonoBehaviour
{
    [SerializeField] GameObject _equipmentPanel;
    [SerializeField] GameObject _upgradePanel;
    [SerializeField] GameObject _gachaPanel;

    [SerializeField] Button _tabEquipment;
    [SerializeField] Button _tabUpgrade;
    [SerializeField] Button _tabGacha;

    static readonly Color TabActive   = new(0.50f, 0.32f, 0.80f, 1f);
    static readonly Color TabInactive = new(0.22f, 0.18f, 0.35f, 1f);

    void Start()
    {
        _tabEquipment?.onClick.AddListener(() => Show(0));
        _tabUpgrade?.onClick.AddListener(() => Show(1));
        _tabGacha?.onClick.AddListener(() => Show(2));
        Show(0);
    }

    void Show(int index)
    {
        if (_equipmentPanel != null) _equipmentPanel.SetActive(index == 0);
        if (_upgradePanel   != null) _upgradePanel.SetActive(index == 1);
        if (_gachaPanel     != null) _gachaPanel.SetActive(index == 2);

        SetTabColor(_tabEquipment, index == 0);
        SetTabColor(_tabUpgrade,   index == 1);
        SetTabColor(_tabGacha,     index == 2);

        if (index == 0 && _equipmentPanel != null &&
            _equipmentPanel.TryGetComponent<EquipmentPanelUI>(out var ep))
            ep.Refresh();
    }

    static void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? TabActive : TabInactive;
    }
}
