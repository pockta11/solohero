using System.Collections.Generic;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Events;
using SoloHero.Core.Save;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-156 (L-16) event popup from the right rail: the running event's name and time left, then three tabs - the
    /// attendance board, the missions (coins) and the coin exchange. Without a running event it says so. A dot on the
    /// rail while attendance or a finished mission waits. Logs event_join (first open), event_claim and event_shop_buy.
    /// </summary>
    public sealed class EventPresenter : MonoBehaviour
    {
        private static readonly Color TabIdle = new Color(0.72f, 0.72f, 0.76f, 1f);
        private static readonly Color Dim = new Color(0.78f, 0.74f, 0.7f, 1f);
        private static readonly Color Today = new Color(1f, 0.86f, 0.45f, 1f);
        private const float RefreshSeconds = 1f;

        [SerializeField] private GameObject _popup;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        /// <summary>D-171: seconds between two claims from this window (the reward animation's beat).</summary>
        private const float ClaimSeconds = 0.35f;

        private readonly ClaimGate _gate = new ClaimGate();
        [SerializeField] private Text _name;
        [SerializeField] private Text _timeLeft;
        [SerializeField] private Text _tokens;
        [SerializeField] private GameObject _none;
        [SerializeField] private GameObject _body;
        [SerializeField] private Image[] _tabs = new Image[3];
        [SerializeField] private GameObject[] _pages = new GameObject[3];

        [Header("Attendance")]
        [SerializeField] private Image[] _dayCells = new Image[7];
        [SerializeField] private Image[] _dayIcons = new Image[7];
        [SerializeField] private Text[] _dayAmounts = new Text[7];
        [SerializeField] private GameObject[] _dayChecks = new GameObject[7];
        [SerializeField] private Button _attendButton;
        [SerializeField] private Text _attendLabel;

        [Header("Missions")]
        [SerializeField] private Text[] _missionNames = new Text[6];
        [SerializeField] private Text[] _missionRewards = new Text[6];
        [SerializeField] private Button[] _missionButtons = new Button[6];
        [SerializeField] private Text[] _missionLabels = new Text[6];

        [Header("Exchange")]
        [SerializeField] private Text[] _shopNames = new Text[6];
        [SerializeField] private Text[] _shopCosts = new Text[6];
        [SerializeField] private Text[] _shopLimits = new Text[6];
        [SerializeField] private Button[] _shopButtons = new Button[6];
        [SerializeField] private Text[] _shopLabels = new Text[6];

        [Header("Icons")]
        [SerializeField] private Sprite _gemIcon;
        [SerializeField] private Sprite _ticketIcon;
        [SerializeField] private Sprite _stoneIcon;
        [SerializeField] private Sprite _coinIcon;
        [SerializeField] private Sprite _tokenIcon;
        [SerializeField] private GameObject[] _badges = new GameObject[0];

        private EventService _events;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private IAnalytics _analytics;
        private int _tab;
        private bool _dirty = true;
        private float _poll;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _events = PanelServices.TryGet<EventService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>() ?? new BalanceValues();
            _analytics = PanelServices.TryGet<IAnalytics>();
            if (_events != null) _events.Changed += MarkDirty;
        }

        private void OnDisable()
        {
            if (_events != null) _events.Changed -= MarkDirty;
        }

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            if (_events != null && _events.MarkJoined()) Log("event_join", null, -1);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void ShowTab(int tab)
        {
            _tab = Mathf.Clamp(tab, 0, 2);
            Refresh();
        }

        public void ClaimAttendance()
        {
            if (_events == null || !_gate.Pass(ClaimSeconds)) return;
            int day = _events.AttendanceClaimed;
            EventTemplate t = _events.Template;
            var before = new RewardSnapshot(_save);
            Result result = _events.TryClaimAttendance();
            if (!Report(result)) return;
            Fly(before);
            if (t != null && day < t.Attendance.Length) ShowGot(t.Attendance[day]);
            Log("event_claim", "attendance", day);
        }

        public void ClaimMission(int index)
        {
            if (_events == null || !_gate.Pass(ClaimSeconds)) return;
            EventTemplate t = _events.Template;
            Result result = _events.TryClaimMission(index);
            if (!Report(result)) return;
            PanelServices.TryGet<AudioService>()?.Play(SfxId.Gold);
            _rewardFx?.Sparkle(new Color(1f, 0.85f, 0.4f));
            if (t != null && index < t.Missions.Length) ShowGot(new RewardBundle { Tokens = t.Missions[index].Tokens });
            Log("event_claim", "mission", index);
        }

        public void Buy(int index)
        {
            if (_events == null || !_gate.Pass(ClaimSeconds)) return;
            EventTemplate t = _events.Template;
            var before = new RewardSnapshot(_save);
            Result result = _events.TryBuy(index);
            if (!Report(result)) return;
            Fly(before);
            if (t != null && index < t.Shop.Length) ShowGot(t.Shop[index].Reward);
            Log("event_shop_buy", t != null && index < t.Shop.Length ? t.Shop[index].Id : "", index);
        }

        private bool Report(Result result)
        {
            if (result.Ok) return true;
            _toast?.ShowFailure(result.Reason);
            return false;
        }

        /// <summary>D-171: what the claim paid flies to its counter, with the claim chime.</summary>
        private void Fly(RewardSnapshot before)
        {
            PanelServices.TryGet<AudioService>()?.Play(SfxId.GradeEpic);
            _rewardFx?.PlayGain(before, _save);
        }

        private void ShowGot(RewardBundle reward)
        {
            if (_toast == null || reward == null) return;
            string text = Describe(reward);
            if (text.Length > 0) _toast.Show(Strings.Format("event.got", text));
        }

        /// <summary>"Gems 300, gear tickets 10" - every non-zero part of a reward, from the string table.</summary>
        private string Describe(RewardBundle r)
        {
            var parts = new List<string>();
            if (r.Gems > 0) parts.Add(Strings.Format("reward.gem", r.Gems));
            if (r.GoldStages > 0d && _save != null) parts.Add(Strings.Format("reward.gold", BigNumberFormat.Format(r.GoldFor(_balance, _save))));
            if (r.GearTickets > 0) parts.Add(Strings.Format("reward.gear", r.GearTickets));
            if (r.SkillTickets > 0) parts.Add(Strings.Format("reward.skill", r.SkillTickets));
            if (r.PetTickets > 0) parts.Add(Strings.Format("reward.pet", r.PetTickets));
            if (r.Stones > 0) parts.Add(Strings.Format("reward.stone", r.Stones));
            if (r.Tokens > 0) parts.Add(Strings.Format("reward.token", r.Tokens));
            return string.Join(", ", parts);
        }

        private void Log(string name, string kind, int index)
        {
            if (_analytics == null || _events == null || _events.Active == null) return;
            var parameters = new List<AnalyticsParam>
            {
                AnalyticsParam.Of("event_id", _events.Active.Id),
                AnalyticsParam.Of("template", _events.Active.Template)
            };
            if (kind != null) parameters.Add(AnalyticsParam.Of("kind", kind));
            if (index >= 0) parameters.Add(AnalyticsParam.Of("index", (long)index));
            _analytics.Log(name, parameters);
        }

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (_events == null) return;
            // Time left and the schedule move with the clock, not only with Changed.
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
            bool show = _events.ClaimableCount > 0;
            for (int i = 0; i < _badges.Length; i++)
                if (_badges[i] != null && _badges[i].activeSelf != show) _badges[i].SetActive(show);
        }

        private void Refresh()
        {
            if (_events == null) return;
            EventTemplate t = _events.Template;
            bool running = t != null;
            if (_none != null) _none.SetActive(!running);
            if (_body != null) _body.SetActive(running);
            if (!running)
            {
                if (_name != null) _name.text = "";
                if (_timeLeft != null) _timeLeft.text = "";
                return;
            }

            if (_name != null) _name.text = Strings.Get(t.NameKey);
            if (_timeLeft != null) _timeLeft.text = TimeLeft(_events.SecondsLeft);
            if (_tokens != null) _tokens.text = Strings.Format("event.tokens", _events.Tokens);
            for (int i = 0; i < _tabs.Length; i++)
                if (_tabs[i] != null) _tabs[i].color = i == _tab ? Color.white : TabIdle;
            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null && _pages[i].activeSelf != (i == _tab)) _pages[i].SetActive(i == _tab);

            RefreshAttendance(t);
            RefreshMissions(t);
            RefreshShop(t);
        }

        private static string TimeLeft(long seconds)
        {
            long days = seconds / 86400;
            long hours = seconds % 86400 / 3600;
            long minutes = seconds % 3600 / 60;
            return days > 0 ? Strings.Format("event.time_days", days, hours) : Strings.Format("event.time_hours", hours, minutes);
        }

        private void RefreshAttendance(EventTemplate t)
        {
            int claimed = _events.AttendanceClaimed;
            bool available = _events.AttendanceAvailable;
            for (int i = 0; i < _dayCells.Length; i++)
            {
                bool used = i < t.Attendance.Length;
                if (_dayCells[i] != null && _dayCells[i].gameObject.activeSelf != used) _dayCells[i].gameObject.SetActive(used);
                if (!used) continue;
                RewardBundle r = t.Attendance[i];
                if (i < _dayIcons.Length && _dayIcons[i] != null) _dayIcons[i].sprite = IconFor(r);
                if (i < _dayAmounts.Length && _dayAmounts[i] != null) _dayAmounts[i].text = AmountFor(r);
                bool done = i < claimed;
                bool next = available && i == claimed;
                if (i < _dayChecks.Length && _dayChecks[i] != null) _dayChecks[i].SetActive(done);
                _dayCells[i].color = next ? Today : done ? Dim : Color.white;
            }

            if (_attendButton != null) _attendButton.interactable = available;
            if (_attendLabel != null)
                _attendLabel.text = Strings.Get(available ? "event.attend_claim" : claimed >= t.Attendance.Length ? "event.attend_all" : "event.attend_done");
        }

        private void RefreshMissions(EventTemplate t)
        {
            for (int i = 0; i < _missionNames.Length; i++)
            {
                bool used = i < t.Missions.Length;
                if (_missionNames[i] != null && _missionNames[i].transform.parent != null)
                    _missionNames[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;
                EventMission m = t.Missions[i];
                if (_missionNames[i] != null)
                    _missionNames[i].text = Strings.Format("event.mission_row", Strings.Get(m.NameKey),
                        BigNumberFormat.Format(_events.MissionProgress(i)), BigNumberFormat.Format(m.Target));
                if (i < _missionRewards.Length && _missionRewards[i] != null) _missionRewards[i].text = m.Tokens.ToString();
                bool claimable = _events.MissionClaimable(i);
                bool claimed = _events.MissionClaimed(i);
                if (i < _missionButtons.Length && _missionButtons[i] != null) _missionButtons[i].interactable = claimable;
                if (i < _missionLabels.Length && _missionLabels[i] != null)
                    _missionLabels[i].text = Strings.Get(claimed ? "daily.claimed" : claimable ? "daily.claim" : "daily.progress");
            }
        }

        private void RefreshShop(EventTemplate t)
        {
            for (int i = 0; i < _shopNames.Length; i++)
            {
                bool used = i < t.Shop.Length;
                if (_shopNames[i] != null && _shopNames[i].transform.parent != null)
                    _shopNames[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;
                EventShopItem item = t.Shop[i];
                int bought = _events.Bought(i);
                bool soldOut = bought >= item.Limit;
                if (_shopNames[i] != null) _shopNames[i].text = Strings.Get(item.NameKey);
                if (i < _shopCosts.Length && _shopCosts[i] != null) _shopCosts[i].text = item.Cost.ToString();
                if (i < _shopLimits.Length && _shopLimits[i] != null) _shopLimits[i].text = Strings.Format("event.limit", bought, item.Limit);
                if (i < _shopButtons.Length && _shopButtons[i] != null) _shopButtons[i].interactable = _events.CanBuy(i);
                if (i < _shopLabels.Length && _shopLabels[i] != null) _shopLabels[i].text = Strings.Get(soldOut ? "event.sold_out" : "event.buy");
            }
        }

        private Sprite IconFor(RewardBundle r)
        {
            if (r.Gems > 0) return _gemIcon;
            if (r.GearTickets > 0 || r.SkillTickets > 0 || r.PetTickets > 0) return _ticketIcon;
            if (r.Stones > 0) return _stoneIcon;
            if (r.Tokens > 0) return _tokenIcon;
            return _coinIcon;
        }

        /// <summary>The first part's amount; a "+" when the day pays more (the claim toast lists it all).</summary>
        private static string AmountFor(RewardBundle r)
        {
            int tickets = r.GearTickets + r.SkillTickets + r.PetTickets;
            int parts = (r.Gems > 0 ? 1 : 0) + (tickets > 0 ? 1 : 0) + (r.Stones > 0 ? 1 : 0) + (r.Tokens > 0 ? 1 : 0) + (r.GoldStages > 0d ? 1 : 0);
            string more = parts > 1 ? "+" : "";
            if (r.Gems > 0) return r.Gems + more;
            if (tickets > 0) return tickets + more;
            if (r.Stones > 0) return r.Stones + more;
            if (r.Tokens > 0) return r.Tokens + more;
            return "";
        }
    }
}
