using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터/플레이어 월드 UI (HP 바, SP 바, 데미지 텍스트, 콤보).
/// </summary>
public class ObjectUI : MonoBehaviour
{
    [SerializeField] Slider _hpSlider;
    [SerializeField] Slider _spSlider;
    [SerializeField] TextMeshProUGUI _damageTMP;
    [SerializeField] TextMeshProUGUI _comboTMP;

    int _hpMax;
    int _spMax;

    void Awake()
    {
        if (_damageTMP != null) _damageTMP.gameObject.SetActive(false);
        if (_comboTMP  != null) _comboTMP.gameObject.SetActive(false);
    }

    public void InitHp(int maxHp)
    {
        _hpMax = maxHp;
        if (_hpSlider != null) _hpSlider.value = 1f;
    }

    public void InitSp(int maxSp)
    {
        _spMax = maxSp;
        if (_spSlider != null) _spSlider.value = 1f;
    }

    public void SetCurHp(int cur)
    {
        if (_hpSlider != null && _hpMax > 0)
            _hpSlider.value = (float)cur / _hpMax;
    }

    public void SetCurSp(int cur)
    {
        if (_spSlider != null && _spMax > 0)
            _spSlider.value = (float)cur / _spMax;
    }

    public void SetDamageText(int damage)
    {
        if (_damageTMP == null) return;
        _damageTMP.gameObject.SetActive(true);
        _damageTMP.text = damage.ToString();
        StartCoroutine(FadeText(_damageTMP));
    }

    public void SetComboText(int combo)
    {
        if (_comboTMP == null) return;
        _comboTMP.gameObject.SetActive(true);
        _comboTMP.text = $"{combo} combo";
    }

    public void DestroyUI()
    {
        Destroy(gameObject);
    }

    System.Collections.IEnumerator FadeText(TextMeshProUGUI tmp)
    {
        float a = 1f;
        while (a > 0f)
        {
            a -= Time.deltaTime * 2f;
            tmp.alpha = a;
            yield return null;
        }
        tmp.gameObject.SetActive(false);
    }
}
