using SoloHero.Core.Common;
using SoloHero.Core.Daily;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Attendance and daily missions popup (D-099), opened from the right rail. The holder stays active so this
    /// also records mission progress while the popup is closed: kills and stage clears from the running stage,
    /// upgrades from UpgradeService, summons and ad views from the save counters (deltas, so a midnight counter
    /// reset never counts backwards). A dot on the rail shows while something is waiting to be claimed.
    /// </summary>
    public sealed class DailyPresenter : MonoBehaviour
    {
        // D-108 tints over the cream day cards: claimed days fade, today's card glows gold.
        private static readonly Color Dim = new Color(0.78f, 0.74f, 0.7f, 1f);
        private static readonly Color Today = new Color(1f, 0.86f, 0.45f, 1f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        /// <summary>D-171: seconds between two claims from this window (the reward animation's beat).</summary>
        private const float ClaimSeconds = 0.35f;

        private readonly ClaimGate _gate = new ClaimGate();
        [SerializeField] private Text _attendTitle;
        [SerializeField] private Image[] _dayCells = new Image[DailyService.CycleDays];
        [SerializeField] private Image[] _dayIcons = new Image[DailyService.CycleDays];
        [SerializeField] private Text[] _dayAmounts = new Text[DailyService.CycleDays];
        [SerializeField] private GameObject[] _dayChecks = new GameObject[DailyService.CycleDays];
        [SerializeField] private Button _attendButton;
        [SerializeField] private Text _attendLabel;
        [SerializeField] private Text[] _missionNames = new Text[6];
        [SerializeField] private Text[] _missionRewards = new Text[6];
        [SerializeField] private Button[] _missionButtons = new Button[6];
        [SerializeField] private Text[] _missionLabels = new Text[6];
        [SerializeField] private Sprite _gemIcon;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private GameObject[] _badges = new GameObject[0];

        [Header("D-166 monthly board: the shown week's day numbers, the week ends' gift and the total days")]
        [SerializeField] private Text[] _dayLabels = new Text[DailyService.CycleDays];
        [SerializeField] private Sprite _giftIcon;
        [SerializeField] private Text _attendTotal;

        [Header("D-155 weekly missions: tabs over the same six rows")]
        [SerializeField] private Image _dailyTab;
        [SerializeField] private Image _weeklyTab;
        [SerializeField] private Text _missionNote;

        private static readonly Color TabIdle = new Color(0.72f, 0.72f, 0.76f, 1f);
        private const int ExtraSize = 22;

        private DailyService _daily;
        private UpgradeService _upgrade;
        private SaveDataV2 _save;
        private StageRunner _runner;
        private int _lastKills;
        private int _lastPulls = -1;
        private int _lastAds = -1;
        private int _lastDungeons = -1;
        private bool _wasInTower;
        private bool _weekly;
        private bool _dirty = true;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _daily = PanelServices.TryGet<DailyService>();
            _upgrade = PanelServices.TryGet<UpgradeService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            if (_daily != null) _daily.Changed += MarkDirty;
            if (_upgrade != null) _upgrade.LaneUpgraded += OnUpgraded;
        }

        private void OnDisable()
        {
            if (_daily != null) _daily.Changed -= MarkDirty;
            if (_upgrade != null) _upgrade.LaneUpgraded -= OnUpgraded;
            Unhook();
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

        public void ClaimAttendance()
        {
            if (_daily == null || _save == null || !_gate.Pass(ClaimSeconds)) return;
            var before = new RewardSnapshot(_save);
            Result result = _daily.TryClaimAttendance();
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            ShowGain(before);
            // D-166: a total-days milestone gets its own line.
            if (_daily.LastMilestone > 0) _toast?.Show(Strings.Format("daily.milestone", _daily.LastMilestone));
        }

        public void ClaimMission(int index)
        {
            if (_daily == null || _save == null || !_gate.Pass(ClaimSeconds)) return;
            var before = new RewardSnapshot(_save);
            Result result = _weekly ? _daily.TryClaimWeekly(index) : _daily.TryClaimMission(index);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            ShowGain(before);
        }

        /// <summary>D-155: the mission rows list today's missions.</summary>
        public void ShowDaily() => SetWeekly(false);

        /// <summary>D-155: the mission rows list this week's missions.</summary>
        public void ShowWeekly() => SetWeekly(true);

        private void SetWeekly(bool weekly)
        {
            _weekly = weekly;
            Refresh();
        }

        /// <summary>D-171: the claim's gold, gems, tickets and stones fly from the button to their counters.</summary>
        private void ShowGain(RewardSnapshot before)
        {
            PanelServices.TryGet<AudioService>()?.Play(SfxId.GradeEpic);
            if (_rewardFx != null) _rewardFx.PlayGain(before, _save);
            else if (_toast != null) _toast.Show(Strings.Format("daily.got_gem", BigNumberFormat.Format(_save.gem - before.Gem)));
        }

        private void MarkDirty() => _dirty = true;

        private void OnUpgraded(UpgradeLane lane, int level) => _daily?.Record(MissionKind.Upgrade, 1);

        private void OnStageCleared(int g) => _daily?.Record(MissionKind.StageClear, 1);

        private void Hook(StageRunner runner)
        {
            _runner = runner;
            _lastKills = runner.Kills;
            runner.StageCleared += OnStageCleared;
        }

        private void Unhook()
        {
            if (_runner != null) _runner.StageCleared -= OnStageCleared;
            _runner = null;
        }

        private void Update()
        {
            if (_daily == null) return;
            Track();
            if (_dirty)
            {
                _dirty = false;
                RefreshBadges();
                if (IsOpen) Refresh();
            }
        }

        private void Track()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner != _runner)
            {
                Unhook();
                if (runner != null) Hook(runner);
            }

            if (_runner != null)
            {
                int kills = _runner.Kills;
                if (kills > _lastKills) _daily.Record(MissionKind.Kill, kills - _lastKills);
                _lastKills = kills;
                // D-155: a tower climb counts when it starts.
                bool inTower = _runner.InTower;
                if (inTower && !_wasInTower) _daily.Record(MissionKind.Tower, 1);
                _wasInTower = inTower;
            }

            if (_save == null) return;
            int pulls = _save.totalPullCount + _save.skillPullCount;
            if (_lastPulls >= 0 && pulls > _lastPulls) _daily.Record(MissionKind.Summon, pulls - _lastPulls);
            _lastPulls = pulls;
            int ads = _save.adCountA1 + _save.adCountA2 + _save.adCountA3;
            if (_lastAds >= 0 && ads > _lastAds) _daily.Record(MissionKind.WatchAd, ads - _lastAds);
            _lastAds = ads;
            // D-155: dungeon entries used today (a new day resets them, so only rises count).
            int dungeons = _save.dungeonGoldUsed + _save.dungeonExpUsed + _save.dungeonScoreUsed + _save.dungeonMaterialUsed;
            if (_lastDungeons >= 0 && dungeons > _lastDungeons) _daily.Record(MissionKind.Dungeon, dungeons - _lastDungeons);
            _lastDungeons = dungeons;
        }

        private void RefreshBadges()
        {
            bool show = _daily.ClaimableCount > 0;
            for (int i = 0; i < _badges.Length; i++)
                if (_badges[i] != null && _badges[i].activeSelf != show) _badges[i].SetActive(show);
        }

        private void Refresh()
        {
            if (_daily == null) return;
            // D-166: the week of the 28-day board the next claim is in (days 1-7, 8-14, ...).
            int claimed = _daily.AttendanceClaimedInCycle;
            bool available = _daily.AttendanceAvailable;
            int start = _daily.AttendanceWeekStart;
            if (_attendTitle != null)
                _attendTitle.text = Strings.Format("daily.attendance_week", _daily.AttendanceDay, (start - 1) / DailyService.CycleDays + 1);
            for (int i = 0; i < DailyService.CycleDays; i++)
            {
                int d = start + i;
                double gem = _daily.AttendanceGems(d);
                double gold = _daily.AttendanceGold(d);
                bool extra = AttendanceCatalog.WeekEndExtra(d) != null;
                if (i < _dayLabels.Length && _dayLabels[i] != null) _dayLabels[i].text = d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (i < _dayIcons.Length && _dayIcons[i] != null) _dayIcons[i].sprite = extra && _giftIcon != null ? _giftIcon : gem > 0d ? _gemIcon : _goldIcon;
                if (i < _dayAmounts.Length && _dayAmounts[i] != null)
                    _dayAmounts[i].text = BigNumberFormat.Format(gem > 0d ? gem : gold) + (extra ? "+" : "");
                bool done = i < claimed;
                bool next = available && i == claimed;
                if (i < _dayChecks.Length && _dayChecks[i] != null) _dayChecks[i].SetActive(done);
                if (i < _dayCells.Length && _dayCells[i] != null) _dayCells[i].color = next ? Today : done ? Dim : Color.white;
            }

            if (_attendTotal != null)
            {
                AttendanceMilestone nextMilestone = AttendanceCatalog.Next(_daily.AttendanceTotal);
                _attendTotal.text = nextMilestone != null
                    ? Strings.Format("daily.total_next", _daily.AttendanceTotal, nextMilestone.Days, nextMilestone.Reward.Gems)
                    : Strings.Format("daily.total", _daily.AttendanceTotal);
            }

            if (_attendButton != null) _attendButton.interactable = available;
            if (_attendLabel != null) _attendLabel.text = Strings.Get(available ? "daily.attend_claim" : "daily.attend_done");

            if (_dailyTab != null) _dailyTab.color = _weekly ? TabIdle : Color.white;
            if (_weeklyTab != null) _weeklyTab.color = _weekly ? Color.white : TabIdle;
            if (_missionNote != null) _missionNote.text = Strings.Get(_weekly ? "daily.weekly_note" : "daily.daily_note");
            MissionDef[] catalog = _weekly ? WeeklyMissionCatalog.All : MissionCatalog.All;
            for (int i = 0; i < _missionNames.Length; i++)
            {
                bool used = i < catalog.Length;
                if (_missionNames[i] != null && _missionNames[i].transform.parent != null)
                    _missionNames[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;

                MissionDef def = catalog[i];
                int progress = _weekly ? _daily.WeeklyProgress(i) : _daily.Progress(i);
                if (_missionNames[i] != null)
                    _missionNames[i].text = Strings.Format("daily.mission_row", Strings.Get(def.NameKey), BigNumberFormat.Format(progress), BigNumberFormat.Format(def.Target))
                        + ExtraLine(def);
                if (i < _missionRewards.Length && _missionRewards[i] != null) _missionRewards[i].text = BigNumberFormat.Format(def.Gems);
                bool claimable = _weekly ? _daily.WeeklyClaimable(i) : _daily.Claimable(i);
                bool claimedMission = _weekly ? _daily.WeeklyClaimed(i) : _daily.Claimed(i);
                if (i < _missionButtons.Length && _missionButtons[i] != null) _missionButtons[i].interactable = claimable;
                if (i < _missionLabels.Length && _missionLabels[i] != null)
                    _missionLabels[i].text = Strings.Get(claimedMission ? "daily.claimed" : claimable ? "daily.claim" : "daily.progress");
            }
        }

        /// <summary>
        /// D-155: a small second line under the mission name for the tickets and stones a weekly mission adds to its gems
        /// (the gem column next to the claim button is too narrow for it).
        /// </summary>
        private static string ExtraLine(MissionDef def)
        {
            string extra = def.Tickets > 0 ? Strings.Format("daily.extra_tickets", def.Tickets)
                : def.Stones > 0 ? Strings.Format("daily.extra_stones", def.Stones) : "";
            return extra.Length == 0 ? "" : "\n<size=" + ExtraSize + ">" + extra + "</size>";
        }
    }
}
