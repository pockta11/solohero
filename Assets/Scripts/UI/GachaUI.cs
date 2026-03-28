using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가챠 패널 UI. 뽑기 버튼 → GachaSystem.Pull() → 결과 카드 표시.
/// </summary>
public class GachaUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;

    [Header("뽑기")]
    [SerializeField] private Button _pullButton;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _pityText;

    [Header("결과 카드")]
    [SerializeField] private GameObject _resultCard;
    [SerializeField] private Image _resultCardBg;
    [SerializeField] private TextMeshProUGUI _resultNameText;
    [SerializeField] private TextMeshProUGUI _resultGradeText;
    [SerializeField] private TextMeshProUGUI _resultStatsText;

    [Header("HUD 연결")]
    [SerializeField] private Button _openButton; // HUD의 가챠 열기 버튼

    // 등급별 색상
    private static readonly Color CommonColor    = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color RareColor      = new Color(0.2f, 0.5f, 1.0f);
    private static readonly Color EpicColor      = new Color(0.7f, 0.2f, 1.0f);
    private static readonly Color LegendaryColor = new Color(1.0f, 0.75f, 0.0f);

    void Start()
    {
        _panel.SetActive(false);
        _resultCard.SetActive(false);

        _openButton?.onClick.AddListener(Open);
        _closeButton.onClick.AddListener(Close);
        _pullButton.onClick.AddListener(OnPull);

        RefreshUI();
    }

    private void Open()
    {
        RefreshUI();
        _resultCard.SetActive(false);
        _panel.SetActive(true);
        _panel.transform.localScale = Vector3.zero;
        _panel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
    }

    private void Close()
    {
        _panel.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => _panel.SetActive(false));
    }

    private void OnPull()
    {
        var result = GachaSystem.Instance?.Pull();
        if (result == null)
        {
            // 골드 부족 피드백
            _pullButton.transform.DOShakePosition(0.3f, 10f, 20);
            return;
        }

        ShowResult(result);
        RefreshUI();
    }

    private void ShowResult(EquipmentData eq)
    {
        _resultCard.SetActive(true);
        _resultCard.transform.localScale = Vector3.zero;

        Color gradeColor = eq.grade switch
        {
            EquipmentGrade.Rare      => RareColor,
            EquipmentGrade.Epic      => EpicColor,
            EquipmentGrade.Legendary => LegendaryColor,
            _                        => CommonColor,
        };

        _resultCardBg.color    = gradeColor;
        _resultNameText.text   = eq.equipmentName;
        _resultGradeText.text  = eq.grade.ToString();
        _resultGradeText.color = gradeColor;

        _resultStatsText.text =
            $"ATK +{eq.attackBonus}  DEF +{eq.defenseBonus}  HP +{eq.hpBonus}";

        _resultCard.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void RefreshUI()
    {
        if (GachaSystem.Instance == null) return;
        _pityText.text = $"천장 {GachaSystem.Instance.PullCount} / {GachaSystem.Instance.PityCeiling}";
        if (_costText != null)
            _costText.text = $"{GachaSystem.Instance.PullCost} 골드 / 1회";
    }
}
