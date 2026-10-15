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
    /// D-175 (L-14) weekly raid popup (right menu): two pages. The raid page picks a level (arrows; up to one past the
    /// best kill), shows its boss, recommended power against the hero's, the best share dealt and what it pays, the
    /// week's entries and medals; sweep (a beaten level) or challenge. The shop page lists the weekly medal shop. The
    /// holder stays active so the end of a run can toast and fly its medals.
    /// </summary>
    public sealed class RaidPresenter : MonoBehaviour
    {
        /// <summary>D-171: seconds between two claims from this window.</summary>
        private const float ClaimSeconds = 0.35f;

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        [Header("Pages")]
        [SerializeField] private GameObject _raidPage;
        [SerializeField] private GameObject _shopPage;
        [SerializeField] private Image[] _tabImages = new Image[2];
        [SerializeField] private Sprite _tabIdle;
        [SerializeField] private Sprite _tabActive;

        [Header("Raid page")]
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _bossText;
        [SerializeField] private Text _recText;
        [SerializeField] private Text _powerText;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _bestText;
        [SerializeField] private Text _entriesText;
        [SerializeField] private Text _medalText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TapGuardButton _enterButton;
        [SerializeField] private Text _enterLabel;
        [SerializeField] private TapGuardButton _sweepButton;

        [Header("Shop page")]
        [SerializeField] private Text[] _itemNames = new Text[0];
        [SerializeField] private Text[] _itemLimits = new Text[0];
        [SerializeField] private Text[] _itemCosts = new Text[0];
        [SerializeField] private TapGuardButton[] _itemButtons = new TapGuardButton[0];
        [SerializeField] private Text _shopMedalText;

        private readonly ClaimGate _gate = new ClaimGate();
        private RaidService _raid;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private StageRunner _runner;
        private int _level = 1;
        private int _page;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _raid = PanelServices.TryGet<RaidService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
        }

        private void OnDisable() => Unhook();

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            // The highest open level is the usual pick.
            if (_raid != null) _level = _raid.MaxLevel;
            ShowPage(_page);
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void ShowPage(int page)
        {
            _page = page == 1 ? 1 : 0;
            if (_raidPage != null) _raidPage.SetActive(_page == 0);
            if (_shopPage != null) _shopPage.SetActive(_page == 1);
            for (int i = 0; i < _tabImages.Length; i++)
            {
                if (_tabImages[i] == null) continue;
                Sprite sprite = i == _page ? _tabActive : _tabIdle;
                if (sprite != null) _tabImages[i].sprite = sprite;
            }

            Refresh();
        }

        public void PrevLevel() => StepLevel(-1);

        public void NextLevel() => StepLevel(1);

        private void StepLevel(int delta)
        {
            if (_raid == null) return;
            _level = Mathf.Clamp(_level + delta, 1, _raid.MaxLevel);
            Refresh();
        }

        public void Enter()
        {
            if (_raid == null) return;
            Result result = _raid.TryEnter(_level, _session != null ? _session.Runner : null);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                Refresh();
                return;
            }

            Close();
        }

        /// <summary>A beaten level's kill reward for an entry, without fighting.</summary>
        public void Sweep()
        {
            if (_raid == null || !_gate.Pass(ClaimSeconds)) return;
            Result result = _raid.TrySweep(_level, out int medals);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                Refresh();
                return;
            }

            PanelServices.TryGet<SoloHero.Game.Audio.AudioService>()?.Play(SoloHero.Game.Audio.SfxId.GradeEpic);
            _rewardFx?.Sparkle(MedalColor, Strings.Format("raid.medals_gain", medals));
            _toast?.Show(Strings.Format("raid.swept", _level, medals));
            Refresh();
        }

        public void Buy(int index)
        {
            if (_raid == null || !_gate.Pass(ClaimSeconds)) return;
            var before = new RewardSnapshot(_save);
            Result result = _raid.TryBuy(index);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                Refresh();
                return;
            }

            PanelServices.TryGet<SoloHero.Game.Audio.AudioService>()?.Play(SoloHero.Game.Audio.SfxId.Gold);
            _rewardFx?.PlayGain(before, _save);
            Refresh();
        }

        private static readonly Color MedalColor = new Color(1f, 0.78f, 0.3f, 1f);

        private void Update()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner == _runner) return;
            Unhook();
            if (runner == null) return;
            _runner = runner;
            runner.DungeonEnded += OnRunEnded;
        }

        private void Unhook()
        {
            if (_runner != null) _runner.DungeonEnded -= OnRunEnded;
            _runner = null;
        }

        /// <summary>The run's medals (and a first kill's gems and ribbon) when a raid ends.</summary>
        private void OnRunEnded(DungeonKind kind, double earned)
        {
            if (kind != DungeonKind.Raid || _runner == null) return;
            RaidResult r = _runner.LastRaid;
            if (_rewardFx != null)
            {
                _rewardFx.Label(Strings.Format("raid.medals_gain", r.Medals), RewardFx.BattleCenter, MedalColor);
                if (r.FirstClearGems > 0)
                {
                    _rewardFx.Play(RewardKind.Gem, r.FirstClearGems, RewardFx.BattleCenter, 1);
                    _rewardFx.Celebrate(Strings.Format("raid.cleared_ribbon", _runner.RaidLevel), new Color(1f, 0.5f, 0.35f));
                }
            }

            if (_toast == null) return;
            _toast.Show(r.Killed
                ? Strings.Format(r.FirstClearGems > 0 ? "raid.result_first" : "raid.result_kill", _runner.RaidLevel, r.Medals, r.FirstClearGems)
                : Strings.Format("raid.result", _runner.RaidLevel, Percent(r.Permille), r.Medals));
        }

        private static string Percent(int permille) => (permille / 10d).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        private void Refresh()
        {
            if (_raid == null || _balance == null || _save == null) return;
            if (_page == 1) RefreshShop();
            else RefreshRaid();
        }

        private void RefreshRaid()
        {
            bool unlocked = _raid.Unlocked;
            _level = Mathf.Clamp(_level, 1, _raid.MaxLevel);
            int g = RaidService.StageOf(_balance, _level);
            StageIndex.FromGlobal(g, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            double rec = RaidService.RecommendedCp(_balance, _level);
            double power = CombatPower.OfSave(_balance, _save);

            if (_levelText != null) _levelText.text = Strings.Format("raid.level", _level, _balance.RAID_LEVELS);
            if (_bossText != null)
                _bossText.text = Strings.Format("raid.boss", BossNames.NameFor(_balance, chapter), DifficultyTier.StageLabel(_balance, g));
            if (_recText != null)
            {
                _recText.text = Strings.Format("tower.recommended", BigNumberFormat.Format(rec));
                _recText.color = power >= rec ? UiPalette.InkGood : UiPalette.InkBad;
            }

            if (_powerText != null) _powerText.text = Strings.Format("tower.power", BigNumberFormat.Format(power));
            if (_rewardText != null)
            {
                int kill = RaidService.MedalsFor(_balance, _level, 1000, true);
                _rewardText.text = _level > _raid.Cleared
                    ? Strings.Format("raid.reward_first", kill, _balance.RAID_FIRST_GEM * _level)
                    : Strings.Format("raid.reward", kill);
            }

            if (_bestText != null)
            {
                StageIndex.FromGlobal(_balance.RAID_UNLOCK_STAGE, _balance.STAGES_PER_CHAPTER, out int c, out int s);
                _bestText.text = !unlocked ? Strings.Format("raid.locked", c + "-" + s)
                    : _level <= _raid.Cleared ? Strings.Get("raid.best_killed")
                    : Strings.Format("raid.best", Percent(_raid.BestPermille(_level)));
            }

            if (_entriesText != null)
            {
                long left = _raid.SecondsToReset;
                _entriesText.text = Strings.Format("raid.entries", _raid.Remaining, _balance.RAID_WEEKLY_TICKETS, left / 86400, left % 86400 / 3600);
            }

            if (_medalText != null) _medalText.text = Strings.Format("raid.medals", _raid.Medals);
            if (_prevButton != null) _prevButton.interactable = _level > 1;
            if (_nextButton != null) _nextButton.interactable = _level < _raid.MaxLevel;
            bool busy = _session != null && _session.Runner != null && _session.Runner.InDungeon;
            if (_enterButton != null) _enterButton.SetAvailable(unlocked && !busy && _raid.Remaining > 0);
            if (_enterLabel != null) _enterLabel.text = Strings.Get(busy ? "tower.busy" : "raid.enter");
            if (_sweepButton != null) _sweepButton.SetAvailable(_raid.CanSweep(_level) && !busy);
        }

        private void RefreshShop()
        {
            if (_shopMedalText != null) _shopMedalText.text = Strings.Format("raid.medals", _raid.Medals);
            for (int i = 0; i < RaidService.Shop.Length; i++)
            {
                RaidShopItem item = RaidService.Shop[i];
                if (i < _itemNames.Length && _itemNames[i] != null) _itemNames[i].text = Strings.Get(item.NameKey);
                if (i < _itemLimits.Length && _itemLimits[i] != null)
                    _itemLimits[i].text = Strings.Format("raid.shop_limit", _raid.Bought(i), item.Limit);
                if (i < _itemCosts.Length && _itemCosts[i] != null) _itemCosts[i].text = item.Cost.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (i < _itemButtons.Length && _itemButtons[i] != null) _itemButtons[i].SetAvailable(_raid.CanBuy(i));
            }
        }
    }
}
