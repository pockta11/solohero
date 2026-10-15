using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Pass;
using SoloHero.Core.Save;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-163 (L-17) free season pass popup (right menu): the season and its time left, the pass level with its XP bar, how
    /// XP is earned, a grid of the thirty level rewards (reached ones light up, claimed ones carry a mark) and "claim all".
    /// The holder stays active so the menu badges follow the claimable levels.
    /// </summary>
    public sealed class PassPresenter : MonoBehaviour
    {
        private const float RefreshSeconds = 1f;
        private static readonly Color Reached = new Color(1f, 0.93f, 0.62f, 1f);
        private static readonly Color Claimed = new Color(0.78f, 0.76f, 0.74f, 1f);
        private static readonly Color Big = new Color(1f, 0.84f, 0.5f, 1f);
        private static readonly Color LockedIcon = new Color(1f, 1f, 1f, 0.45f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        /// <summary>D-171: seconds between two claims from this window (the reward animation's beat).</summary>
        private const float ClaimSeconds = 0.35f;

        private readonly ClaimGate _gate = new ClaimGate();
        [SerializeField] private Text _season;
        [SerializeField] private Text _timeLeft;
        [SerializeField] private Text _level;
        [SerializeField] private RectTransform _xpFill;
        [SerializeField] private Text _xpText;
        [SerializeField] private Text _howTo;
        [SerializeField] private Image[] _cells = new Image[0];
        [SerializeField] private Image[] _icons = new Image[0];
        [SerializeField] private Text[] _amounts = new Text[0];
        [SerializeField] private GameObject[] _checks = new GameObject[0];
        [SerializeField] private TapGuardButton _claimAll;
        [SerializeField] private GameObject[] _badges = new GameObject[0];
        [SerializeField] private Sprite _gemIcon;
        [SerializeField] private Sprite _ticketIcon;
        [SerializeField] private Sprite _stoneIcon;
        [SerializeField] private Sprite _coinIcon;

        private PassService _pass;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private float _poll;
        private bool _dirty = true;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _pass = PanelServices.TryGet<PassService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            if (_pass != null) _pass.Changed += MarkDirty;
            _dirty = true;
        }

        private void OnDisable()
        {
            if (_pass != null) _pass.Changed -= MarkDirty;
        }

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void ClaimAll()
        {
            if (_pass == null || !_gate.Pass(ClaimSeconds)) return;
            var before = new RewardSnapshot(_save);
            Result result = _pass.TryClaimAll(out PassPayout paid);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            PanelServices.TryGet<AudioService>()?.Play(SfxId.GradeEpic);
            // D-171: everything flies to its counter; the toast still lists the whole payout.
            _rewardFx?.PlayGain(before, _save);
            string text = Describe(paid);
            if (_toast != null && text.Length > 0) _toast.Show(Strings.Format("pass.got", text));
            Refresh();
        }

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (_pass == null) return;
            // The season's time left moves with the clock, and kills add XP without a Changed for every one.
            _poll -= Time.unscaledDeltaTime;
            if (_poll <= 0f)
            {
                _poll = RefreshSeconds;
                _dirty = true;
            }

            if (!_dirty) return;
            _dirty = false;
            RefreshBadges();
            if (IsOpen) Refresh();
        }

        private void RefreshBadges()
        {
            bool show = _pass.ClaimableCount > 0;
            for (int i = 0; i < _badges.Length; i++)
                if (_badges[i] != null && _badges[i].activeSelf != show) _badges[i].SetActive(show);
        }

        private void Refresh()
        {
            if (_pass == null || _balance == null) return;
            int level = _pass.Level;
            int claimed = _pass.Claimed;
            if (_season != null) _season.text = Strings.Format("pass.season", _pass.Season + 1);
            if (_timeLeft != null)
            {
                long seconds = _pass.SecondsLeft;
                _timeLeft.text = Strings.Format("pass.time_left", seconds / 86400L, seconds % 86400L / 3600L);
            }

            if (_level != null) _level.text = Strings.Format("pass.level", level, PassCatalog.Levels);
            float ratio = level >= PassCatalog.Levels ? 1f : Mathf.Clamp01((float)_pass.XpIntoLevel / Mathf.Max(1, _balance.PASS_XP_PER_LEVEL));
            if (_xpFill != null) _xpFill.anchorMax = new Vector2(ratio, 1f);
            if (_xpText != null)
            {
                _xpText.text = level >= PassCatalog.Levels
                    ? Strings.Get("pass.max")
                    : Strings.Format("pass.xp", _pass.XpIntoLevel, _balance.PASS_XP_PER_LEVEL);
            }

            if (_howTo != null)
                _howTo.text = Strings.Format("pass.how_to", _balance.PASS_XP_DAILY, _balance.PASS_XP_WEEKLY, _balance.PASS_KILLS_PER_XP);

            for (int i = 0; i < _cells.Length && i < PassCatalog.Levels; i++)
            {
                int lv = i + 1;
                RewardBundle reward = PassCatalog.At(lv);
                bool reached = lv <= level;
                bool done = lv <= claimed;
                if (_cells[i] != null) _cells[i].color = done ? Claimed : reached ? Reached : PassCatalog.IsBig(lv) ? Big : Color.white;
                if (i < _icons.Length && _icons[i] != null)
                {
                    _icons[i].sprite = IconFor(reward);
                    _icons[i].color = reached ? Color.white : LockedIcon;
                }

                if (i < _amounts.Length && _amounts[i] != null) _amounts[i].text = AmountFor(reward);
                if (i < _checks.Length && _checks[i] != null && _checks[i].activeSelf != done) _checks[i].SetActive(done);
            }

            if (_claimAll != null) _claimAll.SetAvailable(level > claimed);
        }

        private Sprite IconFor(RewardBundle r)
        {
            if (r == null) return null;
            if (r.Gems > 0) return _gemIcon;
            if (r.GearTickets > 0 || r.SkillTickets > 0 || r.PetTickets > 0) return _ticketIcon;
            if (r.Stones > 0) return _stoneIcon;
            return _coinIcon;
        }

        /// <summary>The first part's amount (gold as the farming stage pays it now); "+" when the level pays more.</summary>
        private string AmountFor(RewardBundle r)
        {
            if (r == null) return "";
            int tickets = r.GearTickets + r.SkillTickets + r.PetTickets;
            int parts = (r.Gems > 0 ? 1 : 0) + (tickets > 0 ? 1 : 0) + (r.Stones > 0 ? 1 : 0) + (r.GoldStages > 0d ? 1 : 0);
            string more = parts > 1 ? "+" : "";
            if (r.Gems > 0) return r.Gems + more;
            if (tickets > 0) return tickets + more;
            if (r.Stones > 0) return r.Stones + more;
            return _save != null ? BigNumberFormat.Format(r.GoldFor(_balance, _save)) : "";
        }

        private static string Describe(PassPayout p)
        {
            var parts = new List<string>();
            if (p.Gems > 0) parts.Add(Strings.Format("reward.gem", p.Gems));
            if (p.Gold > 0d) parts.Add(Strings.Format("reward.gold", BigNumberFormat.Format(p.Gold)));
            if (p.GearTickets > 0) parts.Add(Strings.Format("reward.gear", p.GearTickets));
            if (p.SkillTickets > 0) parts.Add(Strings.Format("reward.skill", p.SkillTickets));
            if (p.PetTickets > 0) parts.Add(Strings.Format("reward.pet", p.PetTickets));
            if (p.Stones > 0) parts.Add(Strings.Format("reward.stone", p.Stones));
            return string.Join(", ", parts);
        }
    }
}
