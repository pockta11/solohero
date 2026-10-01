using SoloHero.Core.Common;
using SoloHero.Core.Daily;
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
    /// Attendance and daily missions popup (D-099), opened from the right rail. The holder stays active so this
    /// also records mission progress while the popup is closed: kills and stage clears from the running stage,
    /// upgrades from UpgradeService, summons and ad views from the save counters (deltas, so a midnight counter
    /// reset never counts backwards). A dot on the rail shows while something is waiting to be claimed.
    /// </summary>
    public sealed class DailyPresenter : MonoBehaviour
    {
        private static readonly Color Dim = new Color(0.55f, 0.55f, 0.6f, 1f);
        private static readonly Color Today = new Color(1f, 0.85f, 0.35f, 1f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
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

        private DailyService _daily;
        private UpgradeService _upgrade;
        private SaveDataV2 _save;
        private StageRunner _runner;
        private int _lastKills;
        private int _lastPulls = -1;
        private int _lastAds = -1;
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
            if (_daily == null || _save == null) return;
            double gem = _save.gem;
            double gold = _save.gold;
            Result result = _daily.TryClaimAttendance();
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            ShowGain(_save.gem - gem, _save.gold - gold);
        }

        public void ClaimMission(int index)
        {
            if (_daily == null || _save == null) return;
            double gem = _save.gem;
            Result result = _daily.TryClaimMission(index);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            ShowGain(_save.gem - gem, 0d);
        }

        private void ShowGain(double gem, double gold)
        {
            if (_toast == null) return;
            if (gem > 0d) _toast.Show(Strings.Format("daily.got_gem", BigNumberFormat.Format(gem)));
            if (gold > 0d) _toast.Show(Strings.Format("daily.got_gold", BigNumberFormat.Format(gold)));
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
            }

            if (_save == null) return;
            int pulls = _save.totalPullCount + _save.skillPullCount;
            if (_lastPulls >= 0 && pulls > _lastPulls) _daily.Record(MissionKind.Summon, pulls - _lastPulls);
            _lastPulls = pulls;
            int ads = _save.adCountA1 + _save.adCountA2 + _save.adCountA3;
            if (_lastAds >= 0 && ads > _lastAds) _daily.Record(MissionKind.WatchAd, ads - _lastAds);
            _lastAds = ads;
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
            int claimed = _daily.AttendanceClaimedInCycle;
            bool available = _daily.AttendanceAvailable;
            if (_attendTitle != null) _attendTitle.text = Strings.Format("daily.attendance", _daily.AttendanceDay);
            for (int d = 1; d <= DailyService.CycleDays; d++)
            {
                int i = d - 1;
                double gem = _daily.AttendanceGems(d);
                double gold = _daily.AttendanceGold(d);
                if (i < _dayIcons.Length && _dayIcons[i] != null) _dayIcons[i].sprite = gem > 0d ? _gemIcon : _goldIcon;
                if (i < _dayAmounts.Length && _dayAmounts[i] != null) _dayAmounts[i].text = BigNumberFormat.Format(gem > 0d ? gem : gold);
                bool done = d <= claimed;
                bool next = available && d == claimed + 1;
                if (i < _dayChecks.Length && _dayChecks[i] != null) _dayChecks[i].SetActive(done);
                if (i < _dayCells.Length && _dayCells[i] != null) _dayCells[i].color = next ? Today : done ? Dim : Color.white;
            }

            if (_attendButton != null) _attendButton.interactable = available;
            if (_attendLabel != null) _attendLabel.text = Strings.Get(available ? "daily.attend_claim" : "daily.attend_done");

            for (int i = 0; i < MissionCatalog.Count && i < _missionNames.Length; i++)
            {
                MissionDef def = MissionCatalog.All[i];
                if (_missionNames[i] != null)
                    _missionNames[i].text = Strings.Format("daily.mission_row", Strings.Get(def.NameKey), _daily.Progress(i), def.Target);
                if (i < _missionRewards.Length && _missionRewards[i] != null) _missionRewards[i].text = BigNumberFormat.Format(def.Gems);
                bool claimable = _daily.Claimable(i);
                bool claimedMission = _daily.Claimed(i);
                if (i < _missionButtons.Length && _missionButtons[i] != null) _missionButtons[i].interactable = claimable;
                if (i < _missionLabels.Length && _missionLabels[i] != null)
                    _missionLabels[i].text = Strings.Get(claimedMission ? "daily.claimed" : claimable ? "daily.claim" : "daily.progress");
            }
        }
    }
}
