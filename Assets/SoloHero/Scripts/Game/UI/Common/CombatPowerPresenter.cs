using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-111 / D-127 combat power in the HUD: the hero's number counts up to every new value and pops a green "+N";
    /// under it the current fight's recommended combat power (stage, boss or tower floor), green once met and red while
    /// short; a jump of BigGainShare or more shows a centre banner "old > new". Recomputed only when the save changes
    /// (every growth action bumps saveRevision) or the hero levels up.
    /// </summary>
    public sealed class CombatPowerPresenter : MonoBehaviour
    {
        [SerializeField] private Text _value;
        [SerializeField] private Text _gain;
        [SerializeField] private CanvasGroup _gainGroup;
        [SerializeField] private float _gainSeconds = 1.3f;
        [SerializeField] private float _gainRise = 54f;

        [Header("D-127")]
        [SerializeField] private CombatSession _session;
        [SerializeField] private Text _rec;
        [SerializeField] private CanvasGroup _banner;
        [SerializeField] private Text _bannerText;
        [SerializeField] private float _bannerSeconds = 1.8f;
        [SerializeField] private float _bigGainShare = 0.05f;
        [SerializeField] private float _countSeconds = 0.45f;

        private SaveDataV2 _save;
        private BalanceValues _balance;
        private long _shownRevision = -1;
        private int _shownLevel = -1;
        private double _power = -1d;
        private double _countFrom;
        private double _countShown = -1d;
        private float _countT = 1f;
        private float _gainTimer;
        private Vector2 _gainBase;
        private float _bannerTimer;
        private double _bannerFrom;
        private int _recKey = int.MinValue;
        private double _recValue;

        private void OnEnable()
        {
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _shownRevision = -1;
            _recKey = int.MinValue;
            if (_gain != null) _gainBase = _gain.rectTransform.anchoredPosition;
            SetGainAlpha(0f);
            if (_banner != null) _banner.alpha = 0f;
        }

        private void LateUpdate()
        {
            AnimateGain();
            AnimateCount();
            AnimateBanner();
            if (_save == null || _balance == null) return;
            RefreshRecommended();
            if (_save.saveRevision == _shownRevision && _save.heroLevel == _shownLevel) return;
            _shownRevision = _save.saveRevision;
            _shownLevel = _save.heroLevel;

            double power = CombatPower.OfSave(_balance, _save);
            if (_power >= 0d && power > _power)
            {
                ShowGain(power - _power);
                if (power - _power >= _power * _bigGainShare) ShowBanner(_power, power);
            }

            _countFrom = _countShown < 0d ? power : _countShown;
            _countT = _countShown < 0d ? 1f : 0f;
            _power = power;
            if (_countT >= 1f) SetValue(power);
            PaintRecommended();
        }

        /// <summary>The fight's recommended combat power: the tower floor, else the stage (a boss asks a little more).</summary>
        private void RefreshRecommended()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner == null) return;
            int key = runner.InTower ? -runner.TowerFloor : runner.GlobalStage;
            if (key == _recKey) return;
            _recKey = key;
            _recValue = runner.InTower ? TowerService.RecommendedCp(_balance, runner.TowerFloor) : CombatPower.Recommended(_balance, runner.GlobalStage);
            if (_rec != null) _rec.text = Strings.Format("cp.recommended", BigNumberFormat.Format(_recValue));
            PaintRecommended();
        }

        private void PaintRecommended()
        {
            if (_rec == null || _power < 0d) return;
            _rec.color = _power >= _recValue ? UiPalette.HudGood : UiPalette.HudBad;
        }

        private void SetValue(double value)
        {
            _countShown = value;
            if (_value != null) _value.text = BigNumberFormat.Format(value);
        }

        private void AnimateCount()
        {
            if (_countT >= 1f) return;
            _countT = Mathf.Min(1f, _countT + Time.unscaledDeltaTime / _countSeconds);
            float eased = 1f - (1f - _countT) * (1f - _countT);
            SetValue(_countT >= 1f ? _power : _countFrom + (_power - _countFrom) * eased);
        }

        private void ShowGain(double amount)
        {
            if (_gain == null) return;
            _gain.text = "+" + BigNumberFormat.Format(amount);
            _gainTimer = _gainSeconds;
        }

        private void AnimateGain()
        {
            if (_gainTimer <= 0f) return;
            _gainTimer -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(_gainTimer / _gainSeconds);
            if (_gain != null) _gain.rectTransform.anchoredPosition = _gainBase + new Vector2(0f, _gainRise * (1f - (1f - t) * (1f - t)));
            SetGainAlpha(t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f);
            if (_gainTimer <= 0f) SetGainAlpha(0f);
        }

        /// <summary>D-127: a big jump (new gear, a level burst) gets the centre banner; jumps during it extend it.</summary>
        private void ShowBanner(double from, double to)
        {
            if (_banner == null || _bannerText == null) return;
            if (_bannerTimer <= 0f) _bannerFrom = from;
            _bannerText.text = Strings.Format("cp.up", BigNumberFormat.Format(_bannerFrom), BigNumberFormat.Format(to));
            _bannerTimer = _bannerSeconds;
        }

        private void AnimateBanner()
        {
            if (_banner == null || _bannerTimer <= 0f) return;
            _bannerTimer -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(_bannerTimer / _bannerSeconds);
            float alpha = t < 0.12f ? t / 0.12f : t > 0.75f ? 1f - (t - 0.75f) / 0.25f : 1f;
            _banner.alpha = _bannerTimer <= 0f ? 0f : Mathf.Clamp01(alpha);
            float scale = t < 0.12f ? Mathf.Lerp(0.7f, 1.08f, t / 0.12f) : t < 0.22f ? Mathf.Lerp(1.08f, 1f, (t - 0.12f) / 0.1f) : 1f;
            _banner.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetGainAlpha(float a)
        {
            if (_gainGroup != null) _gainGroup.alpha = a;
        }
    }
}
