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
    /// Character panel (E7-05): gold portrait window (idle flipbook, nameplate with level), EXP + 6 stat chips, and
    /// 4 upgrade tiles with level, current -> next value, cost and MAX state. Upgrade buttons repeat while held.
    /// D-104: the nameplate shows the job name and the button under the portrait opens the job advancement popup.
    /// </summary>
    public sealed class CharacterPanelPresenter : MonoBehaviour
    {
        [Header("Upgrade tiles: HP, ATK, DEF, attack speed")]
        [SerializeField] private Text[] _levelTexts = new Text[4];
        [SerializeField] private Text[] _valueTexts = new Text[4];
        [SerializeField] private Text[] _costTexts = new Text[4];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[4];
        [SerializeField] private UiPunch[] _punches = new UiPunch[4];

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
        private JobService _jobs;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private AudioService _audio;
        private double _shownGold = -1d;
        private double _shownExp = -1d;
        private int _shownLevel = -1;

        private void OnEnable()
        {
            _upgrade = PanelServices.TryGet<UpgradeService>();
            _jobs = PanelServices.TryGet<JobService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
            _shownGold = -1d;
            _shownLevel = -1;
            Refresh();
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

            if (_save.gold != _shownGold || _save.jobId != _shownJob) Refresh();
            else if (_save.heroExp != _shownExp) DrawExp();
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

        public void OpenJobs()
        {
            _audio?.Play(SfxId.Tap);
            if (_jobPopup != null) _jobPopup.Open();
        }

        private string _shownJob;

        private void RefreshJob()
        {
            if (_jobs == null) return;
            _shownJob = _save.jobId;
            if (_heroNameText != null) _heroNameText.text = Strings.Get(_jobs.Current.NameKey);
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
            _shownLevel = _save.heroLevel;

            HeroStats now = CombatLoadout.ComputeStats(_balance, _save);
            for (int i = 0; i < 4; i++)
            {
                var lane = (UpgradeLane)i;
                int level = _upgrade.GetLevel(lane);
                bool max = lane == UpgradeLane.Spd && level >= _balance.UPG_MAX_LEVEL_SPD;
                double cost = max ? 0d : Formulas.UpgradeCost(_balance, lane, level);

                if (i < _levelTexts.Length && _levelTexts[i] != null)
                    _levelTexts[i].text = Strings.Format("char.lv", level);
                if (i < _valueTexts.Length && _valueTexts[i] != null)
                {
                    string current = LaneValue(lane, now);
                    _valueTexts[i].text = max
                        ? Strings.Format("char.preview_max", current)
                        : Strings.Format("char.preview", current, LaneValue(lane, CombatLoadout.ComputeStatsAfterUpgrade(_balance, _save, lane)));
                }

                if (i < _costTexts.Length && _costTexts[i] != null)
                    _costTexts[i].text = max ? Strings.Get("char.max") : BigNumberFormat.Format(cost);
                if (i < _buttons.Length && _buttons[i] != null)
                    _buttons[i].SetAvailable(!max && _save.gold >= cost);
            }

            if (_heroLevelText != null) _heroLevelText.text = Strings.Format("char.lv", _save.heroLevel);
            SetStat(0, BigNumberFormat.Format(now.Hp));
            SetStat(1, BigNumberFormat.Format(now.Atk));
            SetStat(2, BigNumberFormat.Format(now.Def));
            SetStat(3, Strings.Format("char.atkspd_value", now.AtkSpd.ToString("0.00", CultureInfo.InvariantCulture)));
            SetStat(4, Strings.Format("char.percent", now.CritRate.ToString("0.#", CultureInfo.InvariantCulture)));
            SetStat(5, Strings.Format("char.percent", ((_balance.CRIT_MULT + now.CritDamageBonus) * 100d).ToString("0", CultureInfo.InvariantCulture)));
            DrawExp();
        }

        private void DrawExp()
        {
            _shownExp = _save.heroExp;
            double need = HeroLevelService.ExpRequired(_balance, _save.heroLevel);
            float ratio = need > 0d ? Mathf.Clamp01((float)(_save.heroExp / need)) : 0f;
            if (_expFill != null) _expFill.anchorMax = new Vector2(ratio, 1f);
            if (_expText != null) _expText.text = Strings.Format("char.percent", (ratio * 100f).ToString("0.0", CultureInfo.InvariantCulture));
        }

        private static string LaneValue(UpgradeLane lane, HeroStats stats)
        {
            switch (lane)
            {
                case UpgradeLane.Hp: return BigNumberFormat.Format(stats.Hp);
                case UpgradeLane.Atk: return BigNumberFormat.Format(stats.Atk);
                case UpgradeLane.Def: return BigNumberFormat.Format(stats.Def);
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
