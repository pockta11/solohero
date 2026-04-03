using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 능력치 업그레이드 패널.
/// HP / ATK / DEF / SPD 각각 골드 소비 → 영구 강화.
/// </summary>
public class UpgradePanelUI : MonoBehaviour
{
    [System.Serializable]
    public struct StatRow
    {
        public TextMeshProUGUI levelText;   // "Lv.3"
        public TextMeshProUGUI bonusText;   // "+150 HP"
        public TextMeshProUGUI costText;    // "400 G"
        public Button          upgradeButton;
    }

    [Header("스탯 행 (HP / ATK / DEF / SPD 순서)")]
    [SerializeField] StatRow _hp;
    [SerializeField] StatRow _atk;
    [SerializeField] StatRow _def;
    [SerializeField] StatRow _spd;

    [Header("보유 골드 표시")]
    [SerializeField] TextMeshProUGUI _goldText;

    void Start()
    {
        _hp.upgradeButton?.onClick.AddListener(UpgradeHp);
        _atk.upgradeButton?.onClick.AddListener(UpgradeAtk);
        _def.upgradeButton?.onClick.AddListener(UpgradeDef);
        _spd.upgradeButton?.onClick.AddListener(UpgradeSpd);
    }

    void OnEnable()
    {
        Refresh();
        PlayerEquipmentService.EquipmentChanged += Refresh;
    }

    void OnDisable() => PlayerEquipmentService.EquipmentChanged -= Refresh;

    // ── 버튼 콜백 ─────────────────────────────────────────────────

    void UpgradeHp()
    {
        var pd = GetPd();
        if (!CanUpgrade(pd, pd.upgradeHpLevel, UpgradeService.HpUpgradeCost)) return;
        pd.gold -= UpgradeService.HpUpgradeCost(pd.upgradeHpLevel);
        pd.upgradeHpLevel++;
        OnUpgraded();
    }

    void UpgradeAtk()
    {
        var pd = GetPd();
        if (!CanUpgrade(pd, pd.upgradeAtkLevel, UpgradeService.AtkUpgradeCost)) return;
        pd.gold -= UpgradeService.AtkUpgradeCost(pd.upgradeAtkLevel);
        pd.upgradeAtkLevel++;
        OnUpgraded();
    }

    void UpgradeDef()
    {
        var pd = GetPd();
        if (!CanUpgrade(pd, pd.upgradeDefLevel, UpgradeService.DefUpgradeCost)) return;
        pd.gold -= UpgradeService.DefUpgradeCost(pd.upgradeDefLevel);
        pd.upgradeDefLevel++;
        OnUpgraded();
    }

    void UpgradeSpd()
    {
        var pd = GetPd();
        if (!CanUpgrade(pd, pd.upgradeSpdLevel, UpgradeService.SpdUpgradeCost)) return;
        pd.gold -= UpgradeService.SpdUpgradeCost(pd.upgradeSpdLevel);
        pd.upgradeSpdLevel++;
        OnUpgraded();
    }

    // ── 공통 로직 ─────────────────────────────────────────────────

    bool CanUpgrade(PlayerData pd, int level, System.Func<int, long> costFunc)
    {
        if (pd == null) return false;
        if (level >= UpgradeService.MaxLevel) { Debug.Log("[Upgrade] Max level reached"); return false; }
        if (pd.gold < costFunc(level))
        {
            foreach (var btn in new[] { _hp.upgradeButton, _atk.upgradeButton,
                                        _def.upgradeButton, _spd.upgradeButton })
                btn?.transform.DOShakePosition(0.25f, 8f, 20);
            return false;
        }
        return true;
    }

    void OnUpgraded()
    {
        PlayerEquipmentApplier.ApplyNow();
        HUDManager.Instance?.RefreshGold();
        Refresh();
        SaveManager.Instance?.RequestSave(GameManager.Instance?.PlayerData);
    }

    // ── UI 갱신 ───────────────────────────────────────────────────

    public void Refresh()
    {
        var pd = GetPd();
        if (pd == null) return;

        float spdBonus = pd.upgradeSpdLevel * UpgradeService.SpdPerLevel;

        RefreshRow(_hp,  pd.upgradeHpLevel,  UpgradeService.HpUpgradeCost,
                   $"+{pd.upgradeHpLevel  * UpgradeService.HpPerLevel} HP");
        RefreshRow(_atk, pd.upgradeAtkLevel, UpgradeService.AtkUpgradeCost,
                   $"+{pd.upgradeAtkLevel * UpgradeService.AtkPerLevel} ATK");
        RefreshRow(_def, pd.upgradeDefLevel, UpgradeService.DefUpgradeCost,
                   $"+{pd.upgradeDefLevel * UpgradeService.DefPerLevel} DEF");
        RefreshRow(_spd, pd.upgradeSpdLevel, UpgradeService.SpdUpgradeCost,
                   $"+{spdBonus:0.#} SPD");

        if (_goldText != null) _goldText.text = $"{pd.gold:N0} G";
    }

    static void RefreshRow(StatRow row, int level,
                           System.Func<int, long> costFunc, string bonusLabel)
    {
        bool maxed = level >= UpgradeService.MaxLevel;
        if (row.levelText     != null) row.levelText.text     = $"Lv.{level}";
        if (row.bonusText     != null) row.bonusText.text     = bonusLabel;
        if (row.costText      != null) row.costText.text      = maxed ? "MAX" : $"{costFunc(level):N0} G";
        if (row.upgradeButton != null) row.upgradeButton.interactable = !maxed;
    }

    static PlayerData GetPd() => GameManager.Instance?.PlayerData;
}
