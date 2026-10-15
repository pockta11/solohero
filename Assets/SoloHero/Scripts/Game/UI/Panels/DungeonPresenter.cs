using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
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
    /// D-161: each card also sweeps - an entry paid at once from the dungeon's best run. D-165: a third card, the score
    /// dungeon (damage to a training boss, paid in stones by tier).
    /// </summary>
    public sealed class DungeonPresenter : MonoBehaviour
    {
        private static readonly DungeonKind[] Kinds = { DungeonKind.Gold, DungeonKind.Exp, DungeonKind.Score };

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        /// <summary>D-171: seconds between two claims from this window (the reward animation's beat).</summary>
        private const float ClaimSeconds = 0.35f;

        private readonly ClaimGate _gate = new ClaimGate();
        [SerializeField] private Text[] _ticketTexts = new Text[3];
        [SerializeField] private Text[] _rewardTexts = new Text[3];
        [Tooltip("D-143: breakthrough stones per run, or the unlock stage while closed.")]
        [SerializeField] private Text[] _stoneTexts = new Text[3];
        [SerializeField] private Button[] _enterButtons = new Button[3];
        [SerializeField] private Button[] _sweepButtons = new Button[3];
        [SerializeField] private GameObject _banner;
        [SerializeField] private Text _bannerTitle;
        [SerializeField] private Text _bannerTime;
        [SerializeField] private Text _bannerEarned;
        [SerializeField] private GameObject _leave;

        private bool _leaving;

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

        /// <summary>D-170: ends the running dungeon or tower run now, keeping what it paid.</summary>
        public void Leave()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner == null) return;
            // LeaveRun raises DungeonEnded at once; the flag picks the toast for a run the player ended.
            _leaving = true;
            runner.LeaveRun();
            _leaving = false;
        }

        /// <summary>D-161: spends an entry for the dungeon's best run at today's reward, without fighting.</summary>
        public void Sweep(int index)
        {
            if (_dungeons == null || index < 0 || index >= Kinds.Length || !_gate.Pass(ClaimSeconds)) return;
            DungeonKind kind = Kinds[index];
            var before = new RewardSnapshot(PanelServices.TryGet<SaveDataV2>());
            Result result = _dungeons.TrySweep(kind, out double earned);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                Refresh();
                return;
            }

            BalanceValues balance = PanelServices.TryGet<BalanceValues>();
            int stones = balance != null ? balance.DUNGEON_STONES : 0;
            PanelServices.TryGet<SoloHero.Game.Audio.AudioService>()?.Play(SoloHero.Game.Audio.SfxId.GradeEpic);
            // D-171: the sweep's gold and stones fly from the button (EXP has no counter: it rises as a label).
            _rewardFx?.PlayGain(before, PanelServices.TryGet<SaveDataV2>());
            if (kind == DungeonKind.Exp) _rewardFx?.Sparkle(new Color(0.6f, 0.9f, 1f), Strings.Format("dungeon.exp_gain", BigNumberFormat.Format(earned)));
            if (kind == DungeonKind.Score)
                _toast?.Show(Strings.Format("dungeon.swept_score", _dungeons.Best(kind), (int)earned));
            else
                _toast?.Show(Strings.Format(kind == DungeonKind.Gold ? "dungeon.swept_gold" : "dungeon.swept_exp",
                    BigNumberFormat.Format(earned), stones));
            Refresh();
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
            // D-171: the run's stones fly from the battle field to the character tab (the tower paid floor by floor).
            if (kind != DungeonKind.Tower && _rewardFx != null && _runner != null && _runner.DungeonStones > 0)
                _rewardFx.Play(RewardKind.Stone, _runner.DungeonStones, RewardFx.BattleCenter);
            if (kind == DungeonKind.Tower)
            {
                int cleared = _runner != null ? _runner.TowerCleared : 0;
                int stones = _runner != null ? _runner.DungeonStones : 0;
                _toast.Show(cleared > 0
                    ? Strings.Format("tower.result", cleared, BigNumberFormat.Format(earned), stones)
                    : Strings.Get(_leaving ? "tower.result_left" : "tower.result_none"));
                return;
            }

            if (kind == DungeonKind.Score)
            {
                // D-165: the damage, the tier it reached and the stones.
                _toast.Show(Strings.Format("dungeon.result_score", BigNumberFormat.Format(earned), _runner != null ? _runner.ScoreTier : 0,
                    _runner != null ? _runner.DungeonStones : 0));
                return;
            }

            string key = kind == DungeonKind.Gold ? "dungeon.result_gold" : "dungeon.result_exp";
            _toast.Show(Strings.Format(key, BigNumberFormat.Format(earned), _runner != null ? _runner.DungeonStones : 0));
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
                        : Strings.Get(NameKey(_shownKind));
                }
            }

            bool running = runner.State == StageState.Dungeon;
            if (_leave != null && _leave.activeSelf != running) _leave.SetActive(running);

            int seconds = Mathf.CeilToInt(runner.DungeonTimeRemaining);
            if (seconds != _shownSeconds && _bannerTime != null)
            {
                _shownSeconds = seconds;
                _bannerTime.text = runner.State == StageState.DungeonResult ? Strings.Get("dungeon.over") : Strings.Format("dungeon.time", seconds);
            }

            if (runner.DungeonEarned != _shownEarned && _bannerEarned != null)
            {
                _shownEarned = runner.DungeonEarned;
                string key = runner.InTower ? "tower.earned" : runner.Dungeon == DungeonKind.Score ? "dungeon.score_earned" : "dungeon.earned";
                _bannerEarned.text = Strings.Format(key, BigNumberFormat.Format(_shownEarned));
            }
        }

        private static string NameKey(DungeonKind kind) =>
            kind == DungeonKind.Gold ? "dungeon.gold" : kind == DungeonKind.Score ? "dungeon.score" : "dungeon.exp";

        private void Refresh()
        {
            if (_dungeons == null) return;
            bool busy = _session != null && _session.Runner != null && _session.Runner.InDungeon;
            bool open = _dungeons.Unlocked;
            BalanceValues balance = PanelServices.TryGet<BalanceValues>();
            for (int i = 0; i < Kinds.Length; i++)
            {
                int left = _dungeons.Remaining(Kinds[i]);
                if (i < _ticketTexts.Length && _ticketTexts[i] != null)
                    _ticketTexts[i].text = Strings.Format("dungeon.tickets", left, _dungeons.DailyTicketsOf(Kinds[i]));
                if (i < _rewardTexts.Length && _rewardTexts[i] != null && Kinds[i] == DungeonKind.Score)
                {
                    // D-165: the tier ladder and what the best tier would sweep.
                    int bestTier = _dungeons.Best(Kinds[i]);
                    _rewardTexts[i].text = bestTier > 0
                        ? Strings.Format("dungeon.score_sweep", bestTier, (int)_dungeons.SweepReward(Kinds[i]))
                        : Strings.Get("dungeon.score_first");
                }
                else if (i < _rewardTexts.Length && _rewardTexts[i] != null)
                {
                    // D-161: the second line is what a sweep would pay now (or how to open it).
                    string perKill = BigNumberFormat.Format(_dungeons.RewardPerKill(Kinds[i]));
                    int best = _dungeons.Best(Kinds[i]);
                    _rewardTexts[i].text = best > 0
                        ? Strings.Format("dungeon.per_kill_sweep", perKill, BigNumberFormat.Format(_dungeons.SweepReward(Kinds[i])), best)
                        : Strings.Format("dungeon.per_kill_first", perKill);
                }
                if (i < _stoneTexts.Length && _stoneTexts[i] != null && balance != null)
                {
                    // D-143: closed until the first boss falls.
                    StageIndex.FromGlobal(balance.DUNGEON_UNLOCK_STAGE, balance.STAGES_PER_CHAPTER, out int c, out int s);
                    string stonesLine = Kinds[i] == DungeonKind.Score
                        ? Strings.Format("dungeon.score_stones", balance.SCORE_STONES_PER_TIER)
                        : Strings.Format("dungeon.stones", balance.DUNGEON_STONES);
                    _stoneTexts[i].text = open ? stonesLine : Strings.Format("dungeon.locked", c + "-" + s);
                    _stoneTexts[i].color = open ? UiPalette.InkGood : UiPalette.InkBad;
                }

                if (i < _enterButtons.Length && _enterButtons[i] != null) _enterButtons[i].interactable = open && left > 0 && !busy;
                if (i < _sweepButtons.Length && _sweepButtons[i] != null) _sweepButtons[i].interactable = _dungeons.CanSweep(Kinds[i]);
            }
        }
    }
}
