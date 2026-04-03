using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom 5-tab navigation bar (SoulStrike style, 1920x1080).
/// Tabs: Equipment | Growth | [Auto Center] | Summon | Inventory
/// </summary>
public class MainBottomNav : MonoBehaviour
{
    [Header("Bottom Tab Buttons")]
    [SerializeField] Button _tabEquip;
    [SerializeField] Button _tabGrowth;
    [SerializeField] Button _tabAuto;       // center — auto-battle toggle, no panel
    [SerializeField] Button _tabSummon;
    [SerializeField] Button _tabInven;

    [Header("Panel Close Buttons")]
    [SerializeField] Button _closeEquipBtn;
    [SerializeField] Button _closeGrowthBtn;
    [SerializeField] Button _closeSummonBtn;
    [SerializeField] Button _closeInvenBtn;

    [Header("Slide Panels (RectTransform)")]
    [SerializeField] RectTransform _equipPanel;
    [SerializeField] RectTransform _growthPanel;
    [SerializeField] RectTransform _summonPanel;
    [SerializeField] RectTransform _invenPanel;

    [Header("Animation")]
    [SerializeField] float _panelH   = 540f;
    [SerializeField] float _animTime = 0.22f;

    [Header("Tab Colors")]
    [SerializeField] Color _activeColor   = new Color(0.60f, 0.38f, 1.00f);
    [SerializeField] Color _inactiveColor = new Color(0.14f, 0.13f, 0.22f);
    [SerializeField] Color _autoOnColor   = new Color(0.20f, 0.75f, 0.40f);

    private RectTransform _openPanel;
    private Button        _activeTab;
    private bool          _autoOn;

    void Start()
    {
        _tabEquip?.onClick.AddListener(  () => Toggle(_equipPanel,  _tabEquip));
        _tabGrowth?.onClick.AddListener( () => Toggle(_growthPanel, _tabGrowth));
        _tabAuto?.onClick.AddListener(   OnAutoToggle);
        _tabSummon?.onClick.AddListener( () => Toggle(_summonPanel, _tabSummon));
        _tabInven?.onClick.AddListener(  () => Toggle(_invenPanel,  _tabInven));

        _closeEquipBtn?.onClick.AddListener( CloseAll);
        _closeGrowthBtn?.onClick.AddListener(CloseAll);
        _closeSummonBtn?.onClick.AddListener(CloseAll);
        _closeInvenBtn?.onClick.AddListener( CloseAll);

        HideNow(_equipPanel);
        HideNow(_growthPanel);
        HideNow(_summonPanel);
        HideNow(_invenPanel);
    }

    // ── Public shortcuts (called by TopRightMenuUI etc.) ──────────

    public void OpenEquip()  => Toggle(_equipPanel,  _tabEquip);
    public void OpenGrowth() => Toggle(_growthPanel, _tabGrowth);
    public void OpenSummon() => Toggle(_summonPanel, _tabSummon);
    public void OpenInven()  => Toggle(_invenPanel,  _tabInven);

    // Legacy alias (TopRightMenuUI may call this)
    public void OpenGacha()  => OpenSummon();

    // ── Panel toggle ──────────────────────────────────────────────

    public void Toggle(RectTransform panel, Button tab = null)
    {
        if (_openPanel == panel) { CloseAll(); return; }

        if (_openPanel != null) ClosePanel(_openPanel);
        SetTabActive(_activeTab, false);

        _openPanel = panel;
        _activeTab = tab;
        SetTabActive(tab, true);
        OpenPanel(panel);
    }

    public void CloseAll()
    {
        if (_openPanel == null) return;
        ClosePanel(_openPanel);
        SetTabActive(_activeTab, false);
        _openPanel = null;
        _activeTab = null;
    }

    // ── Auto-battle toggle ────────────────────────────────────────

    void OnAutoToggle()
    {
        _autoOn = !_autoOn;
        var autoCtrl = FindObjectOfType<PlayerAutoController>();
        if (autoCtrl != null) autoCtrl.SetAuto(_autoOn);

        var img = _tabAuto?.GetComponent<Image>();
        if (img != null) img.color = _autoOn ? _autoOnColor : _inactiveColor;
    }

    // ── Animation ────────────────────────────────────────────────

    void OpenPanel(RectTransform p)
    {
        p.gameObject.SetActive(true);
        p.DOKill();
        p.DOAnchorPosY(0f, _animTime).SetEase(Ease.OutCubic);

        if (p.TryGetComponent<EquipmentPanelUI>(out var ep)) ep.Refresh();
        if (p.TryGetComponent<UpgradePanelUI> (out var up)) up.Refresh();
    }

    void ClosePanel(RectTransform p)
    {
        p.DOKill();
        p.DOAnchorPosY(-_panelH, _animTime)
         .SetEase(Ease.InCubic)
         .OnComplete(() => p.gameObject.SetActive(false));
    }

    void HideNow(RectTransform p)
    {
        if (p == null) return;
        p.anchoredPosition = new Vector2(0f, -_panelH);
        p.gameObject.SetActive(false);
    }

    void SetTabActive(Button tab, bool active)
    {
        if (tab == null || tab == _tabAuto) return;
        var img = tab.GetComponent<Image>();
        if (img != null) img.color = active ? _activeColor : _inactiveColor;
    }
}
