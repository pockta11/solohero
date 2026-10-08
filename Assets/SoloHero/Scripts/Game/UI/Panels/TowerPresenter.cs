using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-130 infinite tower popup (right menu): the next floor and its boss, the floor's recommended combat power
    /// against the hero's (green once met), what the floor pays and the best floor; "challenge" starts the climb.
    /// The holder stays active so every floor cleared during a run pops a toast.
    /// </summary>
    public sealed class TowerPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private Text _floorText;
        [SerializeField] private Text _bossText;
        [SerializeField] private Text _recText;
        [SerializeField] private Text _powerText;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _bestText;
        [SerializeField] private TapGuardButton _enterButton;
        [SerializeField] private Text _enterLabel;

        private TowerService _tower;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private StageRunner _runner;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _tower = PanelServices.TryGet<TowerService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
        }

        private void OnDisable() => Unhook();

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void Enter()
        {
            if (_tower == null) return;
            Result result = _tower.TryEnter(_session != null ? _session.Runner : null);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                Refresh();
                return;
            }

            Close();
        }

        private void Update()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner == _runner) return;
            Unhook();
            if (runner == null) return;
            _runner = runner;
            runner.TowerFloorCleared += OnFloorCleared;
        }

        private void Unhook()
        {
            if (_runner != null) _runner.TowerFloorCleared -= OnFloorCleared;
            _runner = null;
        }

        private void OnFloorCleared(TowerReward reward)
        {
            if (_toast == null) return;
            string tickets = TicketText(reward);
            _toast.Show(Strings.Format("tower.cleared", reward.Floor, BigNumberFormat.Format(reward.Gems)) + tickets);
        }

        private static string TicketText(TowerReward reward)
        {
            string text = "";
            if (reward.GearTickets > 0) text += Strings.Format("tower.reward_gear", reward.GearTickets);
            if (reward.SkillTickets > 0) text += Strings.Format("tower.reward_skill", reward.SkillTickets);
            if (reward.PetTickets > 0) text += Strings.Format("tower.reward_pet", reward.PetTickets);
            return text;
        }

        private void Refresh()
        {
            if (_tower == null || _balance == null || _save == null) return;
            int floor = _tower.NextFloor;
            int g = TowerService.StageOf(_balance, floor);
            StageIndex.FromGlobal(g, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            double rec = TowerService.RecommendedCp(_balance, floor);
            double power = CombatPower.OfSave(_balance, _save);
            bool unlocked = _tower.Unlocked;

            if (_floorText != null) _floorText.text = Strings.Format("tower.floor", floor);
            if (_bossText != null) _bossText.text = Strings.Get(BossNames.KeyFor(chapter));
            if (_recText != null)
            {
                _recText.text = Strings.Format("tower.recommended", BigNumberFormat.Format(rec));
                _recText.color = power >= rec ? UiPalette.InkGood : UiPalette.InkBad;
            }

            if (_powerText != null) _powerText.text = Strings.Format("tower.power", BigNumberFormat.Format(power));
            if (_rewardText != null)
            {
                TowerReward reward = TowerService.Preview(_balance, floor);
                _rewardText.text = Strings.Format("tower.reward", BigNumberFormat.Format(reward.Gems)) + TicketText(reward);
            }

            if (_bestText != null)
            {
                StageIndex.FromGlobal(_balance.TOWER_UNLOCK_STAGE, _balance.STAGES_PER_CHAPTER, out int c, out int s);
                _bestText.text = unlocked ? Strings.Format("tower.best", _save.towerFloor) : Strings.Format("tower.locked", c + "-" + s);
            }

            bool busy = _session != null && _session.Runner != null && _session.Runner.InDungeon;
            if (_enterButton != null) _enterButton.SetAvailable(unlocked && !busy);
            if (_enterLabel != null) _enterLabel.text = Strings.Get(busy ? "tower.busy" : "tower.enter");
        }
    }
}
