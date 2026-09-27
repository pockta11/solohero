using SoloHero.Game.Boot;
using SoloHero.Game.UI.Panels;
using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Android back key (E7-14, GDD UI rule): closes the top-most open layer - gacha reveal, settings, quit confirm,
    /// then the open growth panel. With nothing open it asks before quitting, so a stray back press in battle never
    /// closes the game. Quitting saves first (the same flush as pause).
    /// </summary>
    public sealed class BackKeyRouter : MonoBehaviour
    {
        [SerializeField] private GachaRevealView _reveal;
        [SerializeField] private SettingsPresenter _settings;
        [SerializeField] private StageSelectPresenter _stageSelect;
        [SerializeField] private PanelHost _panels;
        [SerializeField] private GameObject _quitConfirm;

        private bool _quitting;

        private void Awake()
        {
            if (_quitConfirm != null) _quitConfirm.SetActive(false);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            HandleBack();
        }

        public void HandleBack()
        {
            if (_quitConfirm != null && _quitConfirm.activeSelf)
            {
                CancelQuit();
                return;
            }

            if (_reveal != null && _reveal.IsShowing)
            {
                _reveal.Close();
                return;
            }

            if (_stageSelect != null && _stageSelect.IsOpen)
            {
                _stageSelect.Close();
                return;
            }

            if (_settings != null && _settings.IsOpen)
            {
                _settings.Close();
                return;
            }

            if (_panels != null && _panels.OpenIndex >= 0)
            {
                _panels.Close();
                return;
            }

            if (_quitConfirm != null) _quitConfirm.SetActive(true);
        }

        public void CancelQuit()
        {
            if (_quitConfirm != null) _quitConfirm.SetActive(false);
        }

        public async void ConfirmQuit()
        {
            if (_quitting) return;
            _quitting = true;
            BootSequence boot = FindObjectOfType<BootSequence>();
            if (boot != null) await boot.SaveBeforeQuitAsync();
            Application.Quit();
        }
    }
}
