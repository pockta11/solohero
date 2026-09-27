using SoloHero.Core.Common;
using SoloHero.Core.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Settings popup (E7-09 minimum, E8-13, E8-15): BGM / SFX mute, low-effect mode and 30 fps mode as on/off rows.
    /// Changes apply at once through <see cref="SettingsService"/> and are saved with the next pause / quit save.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        private static readonly Color OnColor = new Color(0.25f, 0.45f, 0.3f, 1f);
        private static readonly Color OffColor = new Color(0.3f, 0.3f, 0.34f, 1f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private Text[] _stateTexts = new Text[4];
        [SerializeField] private Image[] _stateImages = new Image[4];

        private SettingsService _settings;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void Open()
        {
            _settings = PanelServices.TryGet<SettingsService>();
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        /// <summary>Row order: 0 BGM, 1 SFX, 2 low effect, 3 30 fps. BGM / SFX rows show "on" while sound plays.</summary>
        public void Toggle(int row)
        {
            if (_settings == null) return;
            switch (row)
            {
                case 0: _settings.SetBgmMuted(!_settings.BgmMuted); break;
                case 1: _settings.SetSfxMuted(!_settings.SfxMuted); break;
                case 2: _settings.SetLowEffect(!_settings.LowEffect); break;
                case 3: _settings.SetFps30(!_settings.Fps30); break;
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
        }

        private void SetRow(int row, bool on)
        {
            if (row < _stateTexts.Length && _stateTexts[row] != null) _stateTexts[row].text = Strings.Get(on ? "settings.on" : "settings.off");
            if (row < _stateImages.Length && _stateImages[row] != null) _stateImages[row].color = on ? OnColor : OffColor;
        }
    }
}
