using TMPro;
using UnityEngine;

/// <summary>
/// 화면 좌상단 플레이어 정보 카드 (SoulStrike 스타일).
/// 이름 / 레벨 / 골드 표시. HUDManager.RefreshGold() 호출 시 자동 갱신.
/// HP/SP Slider는 HUDManager가 직접 참조 (PlayerInfoPanel 안에 위치).
/// </summary>
public class PlayerInfoPanelUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] TextMeshProUGUI _goldText;

    // TODO: PlayerData에 name/level 추가 시 교체
    const string DefaultName = "Hero";

    void Start() => Refresh();

    public void Refresh()
    {
        if (_nameText  != null) _nameText.text  = DefaultName;
        if (_levelText != null) _levelText.text = "Lv. 1";
        RefreshGold();
    }

    public void RefreshGold()
    {
        if (_goldText == null) return;
        var pd = GameManager.Instance?.PlayerData;
        _goldText.text = FormatCurrency(pd?.gold ?? 0);
    }

    public static string FormatCurrency(long v)
    {
        if (v >= 1_000_000_000) return $"{v / 1_000_000_000f:0.##}B";
        if (v >= 1_000_000)     return $"{v / 1_000_000f:0.##}M";
        if (v >= 1_000)         return $"{v / 1_000f:0.#}K";
        return v.ToString();
    }
}
