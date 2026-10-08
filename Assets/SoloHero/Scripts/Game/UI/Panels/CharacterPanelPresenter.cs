using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Character panel (E7-05): name plate with the job and level, six stat chips, and two pages.
    /// D-142 lanes page: six gold lanes (HP, ATK, DEF, attack speed, crit rate, crit damage) as compact cards with level,
    /// current -> next value and a cost button that repeats while held; the crit lanes open with hero levels.
    /// D-143: a lane at its limit swaps the gold button for the limit break (breakthrough stones, counted on the tab row).
    /// D-141 stats page: the AP window: points left, auto / manual allocation, the main stat (named by the job line) and
    /// vitality with +1 / +10, and a gem reset in manual mode.
    /// D-104: the button under the name plate opens the job advancement popup.
    /// </summary>
    public sealed class CharacterPanelPresenter : MonoBehaviour
    {
        private const int LaneCount = UpgradeLanes.Count;
        private const int PageLanes = 0;
        private const int PageAp = 1;
        private const int ApTen = 10;

        /// <summary>Lane names for the limit break toast (the cards carry the same keys).</summary>
        public static readonly string[] LaneKeys = { "stat.hp", "stat.atk", "stat.def", "stat.atkspd", "stat.crit_rate", "stat.critdmg" };

        [Header("Lane cards: HP, ATK, DEF, attack speed, crit rate, crit damage")]
        [SerializeField] private CanvasGroup[] _laneGroups = new CanvasGroup[LaneCount];
        [SerializeField] private Text[] _levelTexts = new Text[LaneCount];
        [SerializeField] private Text[] _valueTexts = new Text[LaneCount];
        [SerializeField] private Text[] _costTexts = new Text[LaneCount];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[LaneCount];
        [SerializeField] private Text[] _breakCostTexts = new Text[LaneCount];
        [SerializeField] private TapGuardButton[] _breakButtons = new TapGuardButton[LaneCount];
        [SerializeField] private UiPunch[] _punches = new UiPunch[LaneCount];

        [Header("Job-line icons (D-140)")]
        [Tooltip("The ATK lane icon and the ATK stat chip icon: attack for warriors and archers, spell power for mages.")]
        [SerializeField] private Image[] _atkIcons = new Image[2];
        [SerializeField] private Sprite _atkSprite;
        [SerializeField] private Sprite _spellSprite;
        [Tooltip("STR, INT, DEX for the AP main stat icon.")]
        [SerializeField] private Sprite[] _mainSprites = new Sprite[3];

        [Header("Pages")]
        [SerializeField] private GameObject[] _pages = new GameObject[2];
        [SerializeField] private Image[] _pageTabs = new Image[2];
        [SerializeField] private Sprite _tabIdle;
        [SerializeField] private Sprite _tabActive;
        [SerializeField] private GameObject _apBadge;
        [SerializeField] private Text _stoneText;

        [Header("AP page (D-141): main stat, vitality")]
        [SerializeField] private Text _apLeftText;
        [SerializeField] private Text _apModeLabel;
        [SerializeField] private Image _apModeImage;
        [SerializeField] private Button _apModeButton;
        [SerializeField] private Sprite[] _apModeSprites = new Sprite[4];
        [SerializeField] private Image _apMainIcon;
        [SerializeField] private Text[] _apPointTexts = new Text[2];
        [SerializeField] private Text[] _apEffectTexts = new Text[2];
        [SerializeField] private TapGuardButton[] _apOneButtons = new TapGuardButton[2];
        [SerializeField] private TapGuardButton[] _apTenButtons = new TapGuardButton[2];
        [SerializeField] private Text[] _apOneLabels = new Text[2];
        [SerializeField] private Text[] _apTenLabels = new Text[2];
        [SerializeField] private TapGuardButton _apResetButton;
        [SerializeField] private Text _apResetLabel;
        [SerializeField] private Text _apRuleText;

        [Header("Hero card")]
        [SerializeField] private Text _heroLevelText;
        [SerializeField] private RectTransform _expFill;
        [SerializeField] private Text _expText;
        [Tooltip("HP, ATK, DEF, attack speed, crit rate, crit damage.")]
        [SerializeField] private Text[] _statTexts = new Text[6];
        [SerializeField] private UiPunch _levelPunch;
        [SerializeField] private Text _heroNameText;
        [SerializeField] private TapGuardButton _jobButton;
        [SerializeField] private Text _jobLabel;
        [SerializeField] private JobPresenter _jobPopup;

        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;

        private UpgradeService _upgrade;
        private ApService _ap;
        private JobService _jobs;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private AudioService _audio;
        private double _shownGold = -1d;
        private double _shownGem = -1d;
        private double _shownExp = -1d;
        private int _shownLevel = -1;
        private int _shownStones = -1;
        private int _shownApMain = -1;
        private int _shownApVit = -1;
        private bool _shownManual;
        private string _shownJob;
        private int _page;

        private void OnEnable()
        {
            _upgrade = PanelServices.TryGet<UpgradeService>();
            _ap = PanelServices.TryGet<ApService>();
            _jobs = PanelServices.TryGet<JobService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
            _shownGold = -1d;
            _shownLevel = -1;
            ShowPage(_page);
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_save.heroLevel != _shownLevel)
            {
                bool leveled = _shownLevel > 0;
                Refresh();
                if (leveled && _levelPunch != null) _levelPunch.Play();
                return;
            }

            if (_save.gold != _shownGold || _save.gem != _shownGem || _save.jobId != _shownJob || _save.breakStones != _shownStones
                || _save.apMain != _shownApMain || _save.apVit != _shownApVit || _save.apManual != _shownManual)
                Refresh();
            else if (_save.heroExp != _shownExp) DrawExp();
        }

        public void ShowPage(int page)
        {
            _page = page == PageAp ? PageAp : PageLanes;
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null) _pages[i].SetActive(i == _page);
                if (i < _pageTabs.Length && _pageTabs[i] != null) _pageTabs[i].sprite = i == _page ? _tabActive : _tabIdle;
            }

            Refresh();
        }

        public void Upgrade(int lane)
        {
            if (_upgrade == null) return;
            Result result = _upgrade.TryUpgrade((UpgradeLane)lane);
            if (!result.Ok)
            {
                if (_toast != null) _toast.ShowFailure(result.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            GameAnalytics.Log(AnalyticsEvents.Upgrade,
                AnalyticsParam.Of(AnalyticsEvents.PLane, lane),
                AnalyticsParam.Of(AnalyticsEvents.PLevel, _upgrade.GetLevel((UpgradeLane)lane)));
            Celebrate(lane);
            Refresh();
        }

        /// <summary>D-143: spends breakthrough stones so the lane can climb LIMIT_STEP more levels.</summary>
        public void Break(int lane)
        {
            if (_upgrade == null || _balance == null) return;
            var l = (UpgradeLane)lane;
            Result result = _upgrade.TryBreak(l);
            if (!result.Ok)
            {
                if (_toast != null) _toast.ShowFailure(result.Reason);
                return;
            }

            GameAnalytics.Log(AnalyticsEvents.LimitBreak,
                AnalyticsParam.Of(AnalyticsEvents.PLane, lane),
                AnalyticsParam.Of(AnalyticsEvents.PCount, _upgrade.Breaks(l)));
            if (_toast != null)
                _toast.Show(Strings.Format("toast.limit_broken", Strings.Get(LaneKeys[lane]), LaneRules.Cap(_balance, l, _upgrade.Breaks(l))));
            Celebrate(lane);
            Refresh();
        }

        public void AddAp(int stat) => SpendAp(stat, 1);

        public void AddApTen(int stat) => SpendAp(stat, ApTen);

        private void SpendAp(int stat, int count)
        {
            if (_ap == null) return;
            Result result = _ap.TryAdd(stat == 0 ? ApStat.Main : ApStat.Vit, count);
            if (!result.Ok)
            {
                if (_toast != null) _toast.ShowFailure(result.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            _audio?.Play(SfxId.Upgrade);
            Refresh();
        }

        public void ToggleApMode()
        {
            if (_ap == null) return;
            bool manual = !_ap.Manual;
            _ap.SetManual(manual);
            if (_session != null) _session.RefreshLoadout();
            if (_toast != null) _toast.Show(Strings.Get(manual ? "toast.ap_auto_off" : "toast.ap_auto_on"));
            Refresh();
        }

        public void ResetAp()
        {
            if (_ap == null) return;
            Result result = _ap.TryReset();
            if (!result.Ok)
            {
                if (_toast != null) _toast.ShowFailure(result.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            if (_toast != null) _toast.Show(Strings.Get("toast.ap_reset"));
            Refresh();
        }

        public void OpenJobs()
        {
            _audio?.Play(SfxId.Tap);
            if (_jobPopup != null) _jobPopup.Open();
        }

        private void RefreshJob()
        {
            if (_jobs == null) return;
            _shownJob = _save.jobId;
            if (_heroNameText != null) _heroNameText.text = Strings.Get(_jobs.Current.NameKey);
            JobLine line = JobService.LineOf(_save);
            Sprite atk = line == JobLine.Mage && _spellSprite != null ? _spellSprite : _atkSprite;
            for (int i = 0; i < _atkIcons.Length; i++)
                if (_atkIcons[i] != null && atk != null) _atkIcons[i].sprite = atk;
            int main = line == JobLine.Mage ? 1 : line == JobLine.Archer ? 2 : 0;
            if (_apMainIcon != null && main < _mainSprites.Length && _mainSprites[main] != null) _apMainIcon.sprite = _mainSprites[main];
            if (_jobLabel == null) return;
            // The popup always opens (it also shows the current job); the label says what the next step needs.
            _jobLabel.text = _jobs.IsMax ? Strings.Get("job.button_max")
                : _jobs.CanAdvance ? Strings.Get("job.button_ready")
                : Strings.Format("job.button_lv", _jobs.NextLevel);
        }

        private void Refresh()
        {
            if (_upgrade == null || _balance == null || _save == null) return;
            RefreshJob();
            _shownGold = _save.gold;
            _shownGem = _save.gem;
            _shownLevel = _save.heroLevel;
            _shownStones = _save.breakStones;
            _shownApMain = _save.apMain;
            _shownApVit = _save.apVit;
            _shownManual = _save.apManual;

            HeroStats now = CombatLoadout.ComputeStats(_balance, _save);
            if (_stoneText != null) _stoneText.text = Strings.Format("char.stones", _save.breakStones);
            if (_page == PageLanes) DrawLanes(now);
            else DrawAp();
            if (_apBadge != null) _apBadge.SetActive(_ap != null && _ap.Manual && _ap.Unspent > 0);

            if (_heroLevelText != null) _heroLevelText.text = Strings.Format("char.lv", _save.heroLevel);
            SetStat(0, BigNumberFormat.Format(now.Hp));
            SetStat(1, BigNumberFormat.Format(now.Atk));
            SetStat(2, BigNumberFormat.Format(now.Def));
            SetStat(3, Strings.Format("char.atkspd_value", now.AtkSpd.ToString("0.00", CultureInfo.InvariantCulture)));
            SetStat(4, Strings.Format("char.percent", now.CritRate.ToString("0.#", CultureInfo.InvariantCulture)));
            SetStat(5, Strings.Format("char.percent", CritDamagePercent(now)));
            DrawExp();
        }

        private void DrawLanes(HeroStats now)
        {
            for (int i = 0; i < LaneCount; i++)
            {
                var lane = (UpgradeLane)i;
                LaneState state = _upgrade.State(lane);
                int level = _upgrade.GetLevel(lane);

                if (i < _laneGroups.Length && _laneGroups[i] != null) _laneGroups[i].alpha = state == LaneState.Locked ? 0.55f : 1f;
                if (i < _levelTexts.Length && _levelTexts[i] != null) _levelTexts[i].text = Strings.Format("char.lv", level);
                if (i < _valueTexts.Length && _valueTexts[i] != null) _valueTexts[i].text = LaneLine(lane, state, level, now);

                bool breaking = state == LaneState.NeedsBreak;
                TapGuardButton buy = i < _buttons.Length ? _buttons[i] : null;
                if (buy != null)
                {
                    bool show = state != LaneState.Locked && !breaking;
                    if (buy.gameObject.activeSelf != show) buy.gameObject.SetActive(show);
                    double cost = state == LaneState.Open ? Formulas.UpgradeCost(_balance, lane, level) : 0d;
                    if (show) buy.SetAvailable(state == LaneState.Open && _save.gold >= cost);
                    if (i < _costTexts.Length && _costTexts[i] != null)
                        _costTexts[i].text = state == LaneState.Max ? Strings.Get("char.max") : BigNumberFormat.Format(cost);
                }

                TapGuardButton brk = i < _breakButtons.Length ? _breakButtons[i] : null;
                if (brk != null)
                {
                    if (brk.gameObject.activeSelf != breaking) brk.gameObject.SetActive(breaking);
                    int stones = _upgrade.BreakCost(lane);
                    if (breaking) brk.SetAvailable(_save.breakStones >= stones);
                    if (i < _breakCostTexts.Length && _breakCostTexts[i] != null)
                        _breakCostTexts[i].text = Strings.Format("char.stones", stones);
                }
            }
        }

        /// <summary>The card's second line: current -> next value, the unlock level, the limit, or MAX.</summary>
        private string LaneLine(UpgradeLane lane, LaneState state, int level, HeroStats now)
        {
            switch (state)
            {
                case LaneState.Locked: return Strings.Format("char.lane_locked", LaneRules.UnlockLevel(_balance, lane));
                case LaneState.NeedsBreak: return Strings.Format("char.limit_value", level);
                case LaneState.Max: return Strings.Format("char.preview_max", LaneValue(lane, now));
                default:
                    return Strings.Format("char.preview", LaneValue(lane, now),
                        LaneValue(lane, CombatLoadout.ComputeStatsAfterUpgrade(_balance, _save, lane)));
            }
        }

        private void DrawAp()
        {
            if (_ap == null) return;
            bool manual = _ap.Manual;
            int left = _ap.Unspent;
            if (_apLeftText != null) _apLeftText.text = Strings.Format("char.ap_left", left);
            if (_apModeLabel != null) _apModeLabel.text = Strings.Get(manual ? "char.ap_manual" : "char.ap_auto");
            if (_apModeImage != null && _apModeSprites.Length >= 4)
            {
                int k = manual ? 2 : 0;
                _apModeImage.sprite = _apModeSprites[k];
                if (_apModeButton != null)
                {
                    SpriteState sprites = _apModeButton.spriteState;
                    sprites.pressedSprite = _apModeSprites[k + 1];
                    _apModeButton.spriteState = sprites;
                }
            }

            ApPoints points = _ap.Points;
            SetText(_apPointTexts, 0, points.Main.ToString(CultureInfo.InvariantCulture));
            SetText(_apPointTexts, 1, points.Vit.ToString(CultureInfo.InvariantCulture));
            SetText(_apEffectTexts, 0, Strings.Format("char.ap_main_effect", Percent(HeroAp.MainAtkShare(_balance, points.Main))));
            SetText(_apEffectTexts, 1, Strings.Format("char.ap_vit_effect", Percent(HeroAp.VitHpShare(_balance, points.Vit))));
            for (int s = 0; s < 2; s++)
            {
                SetButton(_apOneButtons, s, manual, left > 0);
                SetButton(_apTenButtons, s, manual, left > 0);
                SetText(_apOneLabels, s, Strings.Format("char.ap_plus", 1));
                SetText(_apTenLabels, s, Strings.Format("char.ap_plus", ApTen));
            }

            if (_apResetButton != null)
            {
                if (_apResetButton.gameObject.activeSelf != manual) _apResetButton.gameObject.SetActive(manual);
                if (manual) _apResetButton.SetAvailable(HeroAp.Spent(_save) > 0 && _save.gem >= _ap.ResetCost);
            }

            if (_apResetLabel != null) _apResetLabel.text = Strings.Format("char.ap_reset_cost", _ap.ResetCost);
            if (_apRuleText != null) _apRuleText.text = Strings.Format("char.ap_rule", _balance.AP_PER_LEVEL);
        }

        private static void SetButton(TapGuardButton[] buttons, int index, bool show, bool available)
        {
            if (index >= buttons.Length || buttons[index] == null) return;
            if (buttons[index].gameObject.activeSelf != show) buttons[index].gameObject.SetActive(show);
            if (show) buttons[index].SetAvailable(available);
        }

        private static void SetText(Text[] texts, int index, string value)
        {
            if (index < texts.Length && texts[index] != null) texts[index].text = value;
        }

        private static string Percent(double fraction) =>
            (fraction * 100d).ToString(fraction * 100d < 10d ? "0.#" : "0", CultureInfo.InvariantCulture);

        private string CritDamagePercent(HeroStats stats) =>
            ((_balance.CRIT_MULT + stats.CritDamageBonus) * 100d).ToString("0", CultureInfo.InvariantCulture);

        private void DrawExp()
        {
            _shownExp = _save.heroExp;
            double need = HeroLevelService.ExpRequired(_balance, _save.heroLevel);
            float ratio = need > 0d ? Mathf.Clamp01((float)(_save.heroExp / need)) : 0f;
            if (_expFill != null) _expFill.anchorMax = new Vector2(ratio, 1f);
            if (_expText != null) _expText.text = Strings.Format("char.percent", (ratio * 100f).ToString("0.0", CultureInfo.InvariantCulture));
        }

        private string LaneValue(UpgradeLane lane, HeroStats stats)
        {
            switch (lane)
            {
                case UpgradeLane.Hp: return BigNumberFormat.Format(stats.Hp);
                case UpgradeLane.Atk: return BigNumberFormat.Format(stats.Atk);
                case UpgradeLane.Def: return BigNumberFormat.Format(stats.Def);
                case UpgradeLane.Crit: return Strings.Format("char.percent", stats.CritRate.ToString("0.#", CultureInfo.InvariantCulture));
                case UpgradeLane.CritDmg: return Strings.Format("char.percent", CritDamagePercent(stats));
                default: return stats.AtkSpd.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private void SetStat(int index, string value)
        {
            if (index < _statTexts.Length && _statTexts[index] != null) _statTexts[index].text = value;
        }

        /// <summary>E8-11: every successful purchase punches its tile and plays the upgrade chime.</summary>
        private void Celebrate(int row)
        {
            if (row >= 0 && row < _punches.Length && _punches[row] != null) _punches[row].Play(0.5f);
            if (_audio != null) _audio.Play(SfxId.Upgrade);
        }
    }
}
