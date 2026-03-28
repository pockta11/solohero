using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameScene HUD.
/// HP바 / SP바 / 골드 / 킬카운터 / 콤보 / 피격 플래시 / 사망 UI / 스테이지 클리어 UI
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("HP / SP")]
    [SerializeField] Slider            _hpSlider;
    [SerializeField] Slider            _spSlider;
    [SerializeField] TextMeshProUGUI   _hpText;

    [Header("골드 / 스테이지")]
    [SerializeField] TextMeshProUGUI   _goldTMP;
    [SerializeField] TextMeshProUGUI   _stageTMP;

    [Header("킬 카운터")]
    [SerializeField] TextMeshProUGUI   _killTMP;

    [Header("콤보")]
    [SerializeField] TextMeshProUGUI   _comboTMP;

    [Header("피격 플래시 (전체 화면 빨간 Image)")]
    [SerializeField] Image             _damageFlash;

    [Header("사망 UI")]
    [SerializeField] GameObject        _deadPanel;

    [Header("스테이지 클리어 UI")]
    [SerializeField] GameObject        _stageClearPanel;
    [SerializeField] TextMeshProUGUI   _stageClearGoldTMP;

    [Header("조작 안내")]
    [SerializeField] TextMeshProUGUI   _controlHint;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (_damageFlash      != null) _damageFlash.color = new Color(1, 0, 0, 0);
        if (_deadPanel        != null) _deadPanel.SetActive(false);
        if (_stageClearPanel  != null) _stageClearPanel.SetActive(false);
        if (_comboTMP         != null) _comboTMP.gameObject.SetActive(false);
        if (_controlHint      != null) _controlHint.text = "WASD Move  |  Space Attack  |  J Skill";

        RefreshGold();
    }

    // ── HP / SP ───────────────────────────────────────────────────

    public void UpdateHP(int cur, int max)
    {
        if (_hpSlider != null) _hpSlider.value = max > 0 ? (float)cur / max : 0f;
        if (_hpText   != null) _hpText.text    = $"{cur} / {max}";
    }

    public void UpdateSP(int cur, int max)
    {
        if (_spSlider != null) _spSlider.value = max > 0 ? (float)cur / max : 0f;
    }

    // ── 골드 / 스테이지 ───────────────────────────────────────────

    public void RefreshGold()
    {
        if (_goldTMP == null) return;
        var pd = GameManager.Instance?.PlayerData;
        _goldTMP.text = pd != null ? $"{pd.gold:N0} G" : "0 G";
    }

    public void SetStage(int chapter, int stage)
    {
        if (_stageTMP != null) _stageTMP.text = $"Ch.{chapter} - {stage}";
    }

    // ── 킬 카운터 ─────────────────────────────────────────────────

    public void SetKillCount(int cur, int max)
    {
        if (_killTMP != null) _killTMP.text = $"{cur} / {max}";
    }

    // ── 콤보 ──────────────────────────────────────────────────────

    public void ShowCombo(int combo)
    {
        if (_comboTMP == null) return;
        StopCoroutine("HideCombo");
        _comboTMP.gameObject.SetActive(true);
        _comboTMP.text = combo > 1 ? $"{combo} Combo!" : "";
        if (combo > 1) StartCoroutine(HideCombo());
    }

    IEnumerator HideCombo()
    {
        yield return new WaitForSeconds(1.5f);
        if (_comboTMP != null) _comboTMP.gameObject.SetActive(false);
    }

    // ── 피격 플래시 ───────────────────────────────────────────────

    public void ShowDamageFlash()
    {
        if (_damageFlash == null) return;
        StopCoroutine("DamageFlash");
        StartCoroutine(DamageFlash());
    }

    IEnumerator DamageFlash()
    {
        _damageFlash.color = new Color(1, 0, 0, 0.35f);
        yield return new WaitForSeconds(0.1f);
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.35f, 0f, t / 0.3f);
            _damageFlash.color = new Color(1, 0, 0, a);
            yield return null;
        }
        _damageFlash.color = new Color(1, 0, 0, 0);
    }

    // ── 사망 UI ───────────────────────────────────────────────────

    public void ShowDeadUI()
    {
        if (_deadPanel != null) _deadPanel.SetActive(true);
    }

    public void HideDeadUI()
    {
        if (_deadPanel != null) _deadPanel.SetActive(false);
    }

    // ── 스테이지 클리어 UI ────────────────────────────────────────

    public void ShowStageClear(long gold)
    {
        if (_stageClearPanel == null) return;
        if (_stageClearGoldTMP != null)
            _stageClearGoldTMP.text = $"+ {gold:N0} G";
        _stageClearPanel.SetActive(true);
    }

    public void HideStageClear()
    {
        if (_stageClearPanel != null) _stageClearPanel.SetActive(false);
    }
}
