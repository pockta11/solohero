using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-111 combat power in the HUD (genre CP number): recomputed only when the save changes (every growth action
    /// requests a save, which bumps saveRevision) or the hero levels up, and every rise pops a green "+N" that floats
    /// up and fades.
    /// </summary>
    public sealed class CombatPowerPresenter : MonoBehaviour
    {
        [SerializeField] private Text _value;
        [SerializeField] private Text _gain;
        [SerializeField] private CanvasGroup _gainGroup;
        [SerializeField] private float _gainSeconds = 1.3f;
        [SerializeField] private float _gainRise = 54f;

        private SaveDataV2 _save;
        private BalanceValues _balance;
        private long _shownRevision = -1;
        private int _shownLevel = -1;
        private double _shown = -1d;
        private float _gainTimer;
        private Vector2 _gainBase;

        private void OnEnable()
        {
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _shownRevision = -1;
            if (_gain != null) _gainBase = _gain.rectTransform.anchoredPosition;
            SetGainAlpha(0f);
        }

        private void LateUpdate()
        {
            AnimateGain();
            if (_save == null || _balance == null) return;
            if (_save.saveRevision == _shownRevision && _save.heroLevel == _shownLevel) return;
            _shownRevision = _save.saveRevision;
            _shownLevel = _save.heroLevel;

            double power = CombatPower.Of(_balance, CombatLoadout.ComputeStats(_balance, _save),
                SoloHero.Core.Pets.PetService.EquippedRate(_balance, _save));
            if (_value != null) _value.text = BigNumberFormat.Format(power);
            if (_shown >= 0d && power > _shown) ShowGain(power - _shown);
            _shown = power;
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

        private void SetGainAlpha(float a)
        {
            if (_gainGroup != null) _gainGroup.alpha = a;
        }
    }
}
