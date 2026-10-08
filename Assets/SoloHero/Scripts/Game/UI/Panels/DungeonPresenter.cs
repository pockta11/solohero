using SoloHero.Core.Common;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-100 daily dungeons: the entry popup (gold and EXP cards with entries left and reward per kill) opened from
    /// the right rail, and the banner shown during a run (dungeon name, seconds left, amount earned). The holder
    /// stays active so the banner and the end-of-run toast work while the popup is closed.
    /// </summary>
    public sealed class DungeonPresenter : MonoBehaviour
    {
        private static readonly DungeonKind[] Kinds = { DungeonKind.Gold, DungeonKind.Exp };

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private Text[] _ticketTexts = new Text[2];
        [SerializeField] private Text[] _rewardTexts = new Text[2];
        [SerializeField] private Button[] _enterButtons = new Button[2];
        [SerializeField] private GameObject _banner;
        [SerializeField] private Text _bannerTitle;
        [SerializeField] private Text _bannerTime;
        [SerializeField] private Text _bannerEarned;

        private DungeonService _dungeons;
        private StageRunner _runner;
        private int _shownSeconds = -1;
        private double _shownEarned = -1d;
        private DungeonKind _shownKind = DungeonKind.None;
        private int _shownFloor = -1;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
            if (_banner != null) _banner.SetActive(false);
        }

        private void OnEnable() => _dungeons = PanelServices.TryGet<DungeonService>();

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

        public void Enter(int index)
        {
            if (_dungeons == null || index < 0 || index >= Kinds.Length) return;
            StageRunner runner = _session != null ? _session.Runner : null;
            Result result = _dungeons.TryEnter(Kinds[index], runner);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                Refresh();
                return;
            }

            Close();
        }

        private void Hook(StageRunner runner)
        {
            _runner = runner;
            runner.DungeonEnded += OnDungeonEnded;
        }

        private void Unhook()
        {
            if (_runner != null) _runner.DungeonEnded -= OnDungeonEnded;
            _runner = null;
        }

        private void OnDungeonEnded(DungeonKind kind, double earned)
        {
            if (_toast == null) return;
            if (kind == DungeonKind.Tower)
            {
                int cleared = _runner != null ? _runner.TowerCleared : 0;
                _toast.Show(cleared > 0
                    ? Strings.Format("tower.result", cleared, BigNumberFormat.Format(earned))
                    : Strings.Get("tower.result_none"));
                return;
            }

            string key = kind == DungeonKind.Gold ? "dungeon.result_gold" : "dungeon.result_exp";
            _toast.Show(Strings.Format(key, BigNumberFormat.Format(earned)));
        }

        private void Update()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner != _runner)
            {
                Unhook();
                if (runner != null) Hook(runner);
            }

            bool show = runner != null && runner.InDungeon;
            if (_banner != null && _banner.activeSelf != show) _banner.SetActive(show);
            if (!show)
            {
                _shownKind = DungeonKind.None;
                return;
            }

            if (runner.Dungeon != _shownKind || (runner.InTower && runner.TowerFloor != _shownFloor))
            {
                _shownKind = runner.Dungeon;
                _shownFloor = runner.TowerFloor;
                _shownSeconds = -1;
                _shownEarned = -1d;
                if (_bannerTitle != null)
                {
                    _bannerTitle.text = runner.InTower
                        ? Strings.Format("tower.banner", runner.TowerFloor)
                        : Strings.Get(_shownKind == DungeonKind.Gold ? "dungeon.gold" : "dungeon.exp");
                }
            }

            int seconds = Mathf.CeilToInt(runner.DungeonTimeRemaining);
            if (seconds != _shownSeconds && _bannerTime != null)
            {
                _shownSeconds = seconds;
                _bannerTime.text = runner.State == StageState.DungeonResult ? Strings.Get("dungeon.over") : Strings.Format("dungeon.time", seconds);
            }

            if (runner.DungeonEarned != _shownEarned && _bannerEarned != null)
            {
                _shownEarned = runner.DungeonEarned;
                _bannerEarned.text = Strings.Format(runner.InTower ? "tower.earned" : "dungeon.earned", BigNumberFormat.Format(_shownEarned));
            }
        }

        private void Refresh()
        {
            if (_dungeons == null) return;
            bool busy = _session != null && _session.Runner != null && _session.Runner.InDungeon;
            for (int i = 0; i < Kinds.Length; i++)
            {
                int left = _dungeons.Remaining(Kinds[i]);
                if (i < _ticketTexts.Length && _ticketTexts[i] != null)
                    _ticketTexts[i].text = Strings.Format("dungeon.tickets", left, _dungeons.DailyTickets);
                if (i < _rewardTexts.Length && _rewardTexts[i] != null)
                    _rewardTexts[i].text = Strings.Format("dungeon.per_kill", BigNumberFormat.Format(_dungeons.RewardPerKill(Kinds[i])));
                if (i < _enterButtons.Length && _enterButtons[i] != null) _enterButtons[i].interactable = left > 0 && !busy;
            }
        }
    }
}
