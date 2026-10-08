using SoloHero.Game.Boot;
using SoloHero.Game.UI.Panels;
using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Android back key (E7-14, GDD UI rule): closes the top-most open layer - quit confirm, offline reward (claims x1), gacha reveal, stage select, settings,
    /// then the open growth panel. With nothing open it asks before quitting, so a stray back press in battle never
    /// closes the game. Quitting saves first (the same flush as pause).
    /// </summary>
    public sealed class BackKeyRouter : MonoBehaviour
    {
        [SerializeField] private GachaRevealView _reveal;
        [SerializeField] private SettingsPresenter _settings;
        [SerializeField] private StageSelectPresenter _stageSelect;
        [SerializeField] private DailyPresenter _daily;
        [SerializeField] private DungeonPresenter _dungeon;
        [SerializeField] private PetPresenter _pet;
        [SerializeField] private AchievementPresenter _achievements;
        [SerializeField] private TowerPresenter _tower;
        [SerializeField] private ShopPresenter _shop;
        [SerializeField] private JobPresenter _job;
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

            // The offline reward popup lives on the boot object; back claims the normal (x1) reward and closes it.
            OfflineRewardPopup offline = FindObjectOfType<OfflineRewardPopup>();
            if (offline != null && offline.IsOpen)
            {
                if (!_quitting) offline.Claim();
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

            if (_daily != null && _daily.IsOpen)
            {
                _daily.Close();
                return;
            }

            if (_dungeon != null && _dungeon.IsOpen)
            {
                _dungeon.Close();
                return;
            }

            if (_pet != null && _pet.IsOpen)
            {
                _pet.Close();
                return;
            }

            if (_achievements != null && _achievements.IsOpen)
            {
                _achievements.Close();
                return;
            }

            if (_tower != null && _tower.IsOpen)
            {
                _tower.Close();
                return;
            }

            if (_shop != null && _shop.IsOpen)
            {
                _shop.Close();
                return;
            }

            if (_job != null && _job.IsOpen)
            {
                _job.Close();
                return;
            }

            if (_settings != null && _settings.IsOpen)
            {
                _settings.Close();
                return;
            }

            // D-108: panels are pinned open, so the back key only closes one when the host allows it.
            if (_panels != null && _panels.CanClose)
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
