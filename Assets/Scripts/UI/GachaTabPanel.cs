using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 탭용 축소 가챠 UI (전체 화면 GachaCanvas와 별개).
/// </summary>
public class GachaTabPanel : MonoBehaviour
{
    [SerializeField] Button _pullButton;
    [SerializeField] Button _pull10Button;   // 10연뽑기
    [SerializeField] TextMeshProUGUI _pityText;
    [SerializeField] TextMeshProUGUI _costText;

    [SerializeField] GameObject _resultCard;
    [SerializeField] Image _resultBg;
    [SerializeField] TextMeshProUGUI _resultName;
    [SerializeField] TextMeshProUGUI _resultGrade;
    [SerializeField] TextMeshProUGUI _resultStats;

    static readonly Color CommonColor    = new(0.7f, 0.7f, 0.7f);
    static readonly Color RareColor      = new(0.2f, 0.5f, 1.0f);
    static readonly Color EpicColor      = new(0.7f, 0.2f, 1.0f);
    static readonly Color LegendaryColor = new(1.0f, 0.75f, 0.0f);

    void Start()
    {
        if (_resultCard != null) _resultCard.SetActive(false);
        _pullButton?.onClick.AddListener(OnPull);
        _pull10Button?.onClick.AddListener(OnPull10);
        RefreshStaticTexts();
    }

    void OnEnable() => RefreshStaticTexts();

    void RefreshStaticTexts()
    {
        if (GachaSystem.Instance == null) return;
        if (_pityText != null)
            _pityText.text = $"천장 {GachaSystem.Instance.PullCount} / {GachaSystem.Instance.PityCeiling}";
        if (_costText != null)
            _costText.text = $"1회 {GachaSystem.Instance.PullCost}G  /  10회 {GachaSystem.Instance.PullCost * 10}G";
    }

    void OnPull()
    {
        var result = GachaSystem.Instance?.Pull();
        if (result == null)
        {
            _pullButton?.transform.DOShakePosition(0.3f, 10f, 20);
            return;
        }
        ShowResult(result);
        RefreshStaticTexts();
    }

    void OnPull10()
    {
        if (GachaSystem.Instance == null) return;

        EquipmentData best = null;
        for (int i = 0; i < 10; i++)
        {
            var r = GachaSystem.Instance.Pull();
            if (r == null) { _pull10Button?.transform.DOShakePosition(0.3f, 10f, 20); break; }
            // 10회 중 가장 높은 등급 결과 표시
            if (best == null || r.grade > best.grade) best = r;
        }

        if (best != null) ShowResult(best);
        RefreshStaticTexts();
    }

    void ShowResult(EquipmentData eq)
    {
        if (_resultCard == null) return;

        Color c = eq.grade switch
        {
            EquipmentGrade.Rare      => RareColor,
            EquipmentGrade.Epic      => EpicColor,
            EquipmentGrade.Legendary => LegendaryColor,
            _                        => CommonColor,
        };

        if (_resultBg != null) _resultBg.color = c;
        if (_resultName != null) _resultName.text = eq.equipmentName;
        if (_resultGrade != null)
        {
            _resultGrade.text  = eq.grade.ToString();
            _resultGrade.color = c;
        }

        if (_resultStats != null)
            _resultStats.text = $"ATK +{eq.attackBonus}  DEF +{eq.defenseBonus}  HP +{eq.hpBonus}";

        _resultCard.SetActive(true);
        _resultCard.transform.localScale = Vector3.zero;
        _resultCard.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
    }
}
