using SoloHero.Core.Common;
using SoloHero.Core.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Settings window (E7-09; D-089 moved it from the 5th bottom tab to the right menu rail): BGM / SFX mute,
    /// low-effect mode and 30 fps mode as on/off rows, plus credits with the font's SIL OFL text. Changes apply at
    /// once through <see cref="SettingsService"/> and are saved with the next pause / quit save.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        private static readonly Color OnColor = new Color(0.25f, 0.45f, 0.3f, 1f);
        private static readonly Color OffColor = new Color(0.3f, 0.3f, 0.34f, 1f);

        [SerializeField] private Text[] _stateTexts = new Text[5];
        [SerializeField] private Image[] _stateImages = new Image[5];
        [SerializeField] private GameObject _window;
        [SerializeField] private GameObject _creditsPopup;
        [SerializeField] private Text _creditsText;
        [SerializeField] private TextAsset _credits;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Sprite _offSprite;

        private SettingsService _settings;

        public bool IsOpen => (_creditsPopup != null && _creditsPopup.activeSelf) || (_window != null && _window.activeSelf);

        private void OnEnable()
        {
            _settings = PanelServices.TryGet<SettingsService>();
            Refresh();
        }

        private void OnDisable() => CloseCredits();

        public void Open()
        {
            if (_settings == null) _settings = PanelServices.TryGet<SettingsService>();
            if (_window != null) _window.SetActive(true);
            Refresh();
        }

        public void CloseWindow()
        {
            CloseCredits();
            if (_window != null) _window.SetActive(false);
        }

        /// <summary>Credits and the font's SIL OFL text (the OFL asks for the license to travel with the font).</summary>
        public void OpenCredits()
        {
            if (_creditsText != null && _credits != null) _creditsText.text = _credits.text;
            if (_creditsPopup != null) _creditsPopup.SetActive(true);
        }

        public void CloseCredits()
        {
            if (_creditsPopup != null) _creditsPopup.SetActive(false);
        }

        /// <summary>Back key: the credits overlay first, then the window.</summary>
        public void Close()
        {
            if (_creditsPopup != null && _creditsPopup.activeSelf)
            {
                CloseCredits();
                return;
            }

            if (_window != null) _window.SetActive(false);
        }

        /// <summary>Row order: 0 BGM, 1 SFX, 2 low effect, 3 30 fps, 4 notifications (D-119). BGM / SFX rows show "on" while sound plays.</summary>
        public void Toggle(int row)
        {
            if (_settings == null) _settings = PanelServices.TryGet<SettingsService>();
            if (_settings == null) return;
            switch (row)
            {
                case 0: _settings.SetBgmMuted(!_settings.BgmMuted); break;
                case 1: _settings.SetSfxMuted(!_settings.SfxMuted); break;
                case 2: _settings.SetLowEffect(!_settings.LowEffect); break;
                case 3: _settings.SetFps30(!_settings.Fps30); break;
                case 4: _settings.SetNotifications(!_settings.Notifications); break;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_settings == null) return;
            SetRow(0, !_settings.BgmMuted);
            SetRow(1, !_settings.SfxMuted);
            SetRow(2, _settings.LowEffect);
            SetRow(3, _settings.Fps30);
            SetRow(4, _settings.Notifications);
        }

        private void SetRow(int row, bool on)
        {
            if (row < _stateTexts.Length && _stateTexts[row] != null) _stateTexts[row].text = Strings.Get(on ? "settings.on" : "settings.off");
            if (row >= _stateImages.Length || _stateImages[row] == null) return;
            if (_onSprite != null && _offSprite != null)
            {
                _stateImages[row].sprite = on ? _onSprite : _offSprite;
                _stateImages[row].color = Color.white;
            }
            else
            {
                _stateImages[row].color = on ? OnColor : OffColor;
            }
        }
    }
}
