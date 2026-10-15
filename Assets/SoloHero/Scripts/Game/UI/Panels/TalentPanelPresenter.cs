using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Talent tree (D-087), the second page of the skill panel: three branch columns of four tiers, free points on
    /// top with a gem reset, and the selected node's detail with its current and next value and a learn button.
    /// Locked tiers are dimmed and say how many branch points they need.
    /// D-158 (L-07): a second page, the job talent page of the hero's line (one row per node, 2 points a rank), opens
    /// with the third job; the header tabs switch pages and the detail card serves both.
    /// </summary>
    public sealed class TalentPanelPresenter : MonoBehaviour
    {
        // D-108: nodes are light slot frames on the cream panel, tinted by state; branch names use the darker inks.
        private static readonly Color LockedIcon = UiPalette.Silhouette;
        private static readonly Color LockedFrame = new Color(0.8f, 0.76f, 0.72f, 1f);
        private static readonly Color OpenFrame = Color.white;
        private static readonly Color TabIdle = new Color(0.72f, 0.72f, 0.76f, 1f);
        private static readonly Color JobFrame = new Color(1f, 0.82f, 0.4f, 1f);
        private static readonly Color[] BranchColors =
        {
            new Color(1f, 0.62f, 0.5f, 1f),
            new Color(0.55f, 0.78f, 1f, 1f),
            new Color(0.82f, 0.62f, 1f, 1f),
        };
        private static readonly Color[] BranchInks =
        {
            new Color32(0xD8, 0x4A, 0x32, 0xFF),
            new Color32(0x2F, 0x72, 0xD8, 0xFF),
            new Color32(0x8E, 0x4A, 0xE0, 0xFF),
        };

        [Header("Nodes, catalog order")]
        [SerializeField] private Image[] _cellFrames = new Image[0];
        [SerializeField] private Image[] _cellIcons = new Image[0];
        [SerializeField] private Text[] _cellRanks = new Text[0];
        [SerializeField] private Sprite[] _icons = new Sprite[0];
        [SerializeField] private RectTransform _selection;

        [Header("Branches")]
        [SerializeField] private Text[] _branchTexts = new Text[0];

        [Header("Header")]
        [SerializeField] private Text _pointsText;
        [SerializeField] private TapGuardButton _resetButton;
        [SerializeField] private Text _resetLabel;

        [Header("Detail")]
        [SerializeField] private Image _detailIcon;
        [SerializeField] private Image _detailFrame;
        [SerializeField] private Text _detailName;
        [SerializeField] private Text _detailRank;
        [SerializeField] private Text _detailDesc;
        [SerializeField] private TapGuardButton _learnButton;
        [SerializeField] private Text _learnLabel;
        [SerializeField] private UiPunch _detailPunch;

        [Header("D-158 pages: the shared tree and the job talent page")]
        [SerializeField] private GameObject _treePage;
        [SerializeField] private GameObject _jobPage;
        [SerializeField] private Image _treeTab;
        [SerializeField] private Image _jobTab;
        [SerializeField] private Text _jobTitle;
        [SerializeField] private GameObject[] _jobRows = new GameObject[0];
        [SerializeField] private Image[] _jobFrames = new Image[0];
        [SerializeField] private Image[] _jobIcons = new Image[0];
        [SerializeField] private Text[] _jobRanks = new Text[0];
        [SerializeField] private Text[] _jobNames = new Text[0];
        [SerializeField] private Text[] _jobValues = new Text[0];
        [Tooltip("Icons of TalentCatalog.Job, index-aligned.")]
        [SerializeField] private Sprite[] _jobIconSprites = new Sprite[0];

        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        private TalentService _talents;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private AudioService _audio;
        private TalentDef _selected;
        private bool _jobMode;
        private int _shownHeroLevel = -1;
        private double _shownGem = -1d;
        private string _shownJob;

        private void OnEnable()
        {
            _talents = PanelServices.TryGet<TalentService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
            if (_talents != null) _talents.Changed += Refresh;
            if (_selected == null) _selected = TalentCatalog.All[0];
            Refresh();
        }

        private void OnDisable()
        {
            if (_talents != null) _talents.Changed -= Refresh;
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_save.heroLevel == _shownHeroLevel && _save.gem == _shownGem && _save.jobId == _shownJob) return;
            Refresh();
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= TalentCatalog.All.Length) return;
            _selected = TalentCatalog.All[index];
            Play(SfxId.Tap);
            Refresh();
        }

        /// <summary>D-158: a row of the job page (the hero's line).</summary>
        public void SelectJobCell(int row)
        {
            TalentDef[] nodes = JobNodes();
            if (row < 0 || row >= nodes.Length) return;
            _selected = nodes[row];
            Play(SfxId.Tap);
            Refresh();
        }

        public void ShowTree() => SetJobMode(false);

        public void ShowJob() => SetJobMode(true);

        private void SetJobMode(bool job)
        {
            if (_jobMode != job) Play(SfxId.Tap);
            _jobMode = job;
            TalentDef[] nodes = JobNodes();
            if (job && (_selected == null || !_selected.IsJobTalent) && nodes.Length > 0) _selected = nodes[0];
            if (!job && (_selected == null || _selected.IsJobTalent)) _selected = TalentCatalog.All[0];
            Refresh();
        }

        public void Learn()
        {
            if (_talents == null || _selected == null) return;
            Result r = _talents.TryLearn(_selected.Id);
            if (!r.Ok)
            {
                if (_toast != null) _toast.ShowFailure(r.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            if (_detailPunch != null) _detailPunch.Play(0.4f);
            Play(SfxId.Upgrade);
            _rewardFx?.Sparkle(new Color(0.75f, 0.6f, 1f, 1f));
        }

        public void ResetTree()
        {
            if (_talents == null) return;
            Result r = _talents.TryReset();
            if (!r.Ok)
            {
                if (_toast != null) _toast.ShowFailure(r.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            if (_toast != null) _toast.Show(Strings.Get("toast.talent_reset"));
            Play(SfxId.Upgrade);
        }

        private TalentDef[] JobNodes() => _save != null ? TalentCatalog.JobNodes(JobService.LineOf(_save)) : TalentCatalog.JobNodes(JobLine.None);

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownHeroLevel = _save.heroLevel;
            _shownGem = _save.gem;
            _shownJob = _save.jobId;
            if (_selected == null || (_selected.IsJobTalent && _selected.Line != JobService.LineOf(_save))) _selected = TalentCatalog.All[0];

            int points = TalentService.Available(_balance, _save);
            if (_pointsText != null) _pointsText.text = Strings.Format("talent.points", points);
            if (_resetLabel != null) _resetLabel.text = Strings.Format("talent.reset", _balance.TALENT_RESET_GEM);
            if (_resetButton != null) _resetButton.SetAvailable(TalentService.Spent(_save) > 0 && _save.gem >= _balance.TALENT_RESET_GEM);
            if (_treePage != null && _treePage.activeSelf == _jobMode) _treePage.SetActive(!_jobMode);
            if (_jobPage != null && _jobPage.activeSelf != _jobMode) _jobPage.SetActive(_jobMode);
            if (_treeTab != null) _treeTab.color = _jobMode ? TabIdle : Color.white;
            if (_jobTab != null) _jobTab.color = _jobMode ? Color.white : TabIdle;

            for (int b = 0; b < _branchTexts.Length && b < TalentCatalog.BranchCount; b++)
            {
                if (_branchTexts[b] == null) continue;
                _branchTexts[b].text = Strings.Format("talent.branch_spent",
                    Strings.Get("talent.branch." + b), TalentService.BranchSpent(_save, (TalentBranch)b));
                _branchTexts[b].color = BranchInks[b];
            }

            for (int i = 0; i < TalentCatalog.All.Length && i < _cellFrames.Length; i++) DrawCell(i, points);
            DrawJobPage(points);
            DrawDetail(points);
        }

        private void DrawCell(int i, int points)
        {
            TalentDef def = TalentCatalog.All[i];
            int rank = TalentService.Rank(_save, def.Id);
            bool open = TalentService.IsTierOpen(_balance, _save, def);
            bool learnable = open && rank < def.MaxRank && points >= def.Cost;
            Color branch = BranchColors[(int)def.Branch];

            if (_cellFrames[i] != null)
                _cellFrames[i].color = rank > 0 ? branch : open ? OpenFrame : LockedFrame;
            if (i < _cellIcons.Length && _cellIcons[i] != null)
            {
                _cellIcons[i].sprite = i < _icons.Length ? _icons[i] : null;
                _cellIcons[i].color = open ? Color.white : LockedIcon;
            }

            if (i < _cellRanks.Length && _cellRanks[i] != null)
            {
                _cellRanks[i].text = Strings.Format("talent.rank", rank, def.MaxRank);
                _cellRanks[i].color = RankColor(rank, def.MaxRank, learnable);
            }

            if (def == _selected) Select(_cellFrames[i]);
        }

        /// <summary>D-158: the job page title and its rows for the hero's line (dimmed until the third job).</summary>
        private void DrawJobPage(int points)
        {
            JobLine line = JobService.LineOf(_save);
            TalentDef[] nodes = TalentCatalog.JobNodes(line);
            bool open = JobService.TierOf(_save) >= TalentCatalog.JobPageTier;
            if (_jobTitle != null)
            {
                int cost = nodes.Length > 0 ? nodes[0].Cost : 0;
                _jobTitle.text = line == JobLine.None ? Strings.Get("talent.job_none")
                    : open ? Strings.Format("talent.job_title", Strings.Get(LineKey(line)), cost)
                    : Strings.Format("talent.job_locked", _balance.JOB_LV_3);
            }

            for (int r = 0; r < _jobRows.Length; r++)
            {
                bool shown = r < nodes.Length;
                if (_jobRows[r] != null && _jobRows[r].activeSelf != shown) _jobRows[r].SetActive(shown);
                if (!shown) continue;
                TalentDef def = nodes[r];
                int rank = TalentService.Rank(_save, def.Id);
                bool learnable = open && rank < def.MaxRank && points >= def.Cost;
                if (r < _jobFrames.Length && _jobFrames[r] != null)
                {
                    _jobFrames[r].color = rank > 0 ? JobFrame : open ? OpenFrame : LockedFrame;
                    if (def == _selected) Select(_jobFrames[r]);
                }

                if (r < _jobIcons.Length && _jobIcons[r] != null)
                {
                    int index = System.Array.IndexOf(TalentCatalog.Job, def);
                    _jobIcons[r].sprite = index >= 0 && index < _jobIconSprites.Length ? _jobIconSprites[index] : null;
                    _jobIcons[r].color = open ? Color.white : LockedIcon;
                }

                if (r < _jobNames.Length && _jobNames[r] != null) _jobNames[r].text = Strings.Get(def.NameKey);
                if (r < _jobValues.Length && _jobValues[r] != null)
                {
                    _jobValues[r].text = rank > 0
                        ? Strings.Format(def.DescKey, Amount(def, rank))
                        : Strings.Format("talent.job_max", Strings.Format(def.DescKey, Amount(def, def.MaxRank)));
                }
                if (r < _jobRanks.Length && _jobRanks[r] != null)
                {
                    _jobRanks[r].text = Strings.Format("talent.rank", rank, def.MaxRank);
                    _jobRanks[r].color = RankColor(rank, def.MaxRank, learnable);
                }
            }
        }

        private void DrawDetail(int points)
        {
            TalentDef def = _selected ?? TalentCatalog.All[0];
            int rank = TalentService.Rank(_save, def.Id);
            bool open = TalentService.IsOpen(_balance, _save, def);
            bool maxed = rank >= def.MaxRank;

            if (_detailIcon != null)
            {
                _detailIcon.sprite = IconOf(def);
                _detailIcon.color = open ? Color.white : LockedIcon;
            }

            if (_detailFrame != null) _detailFrame.color = def.IsJobTalent ? JobFrame : BranchColors[(int)def.Branch];
            if (_detailName != null) _detailName.text = Strings.Get(def.NameKey);
            if (_detailRank != null) _detailRank.text = Strings.Format("talent.rank", rank, def.MaxRank);

            if (_detailDesc != null)
            {
                string desc;
                if (def.IsCapstone)
                {
                    desc = Strings.Get(def.DescKey);
                }
                else
                {
                    string now = Strings.Format(def.DescKey, Amount(def, rank));
                    desc = maxed
                        ? Strings.Format("talent.current", now)
                        : Strings.Format("talent.current_next", rank > 0 ? now : Strings.Get("talent.none"), Strings.Format(def.DescKey, Amount(def, rank + 1)));
                }

                if (!open)
                {
                    desc += "\n" + (def.IsJobTalent
                        ? Strings.Format("talent.job_locked", _balance.JOB_LV_3)
                        : Strings.Format("talent.need", Strings.Get("talent.branch." + (int)def.Branch), TalentService.TierRequirement(_balance, def.Tier)));
                }

                _detailDesc.text = desc;
            }

            if (_learnLabel != null) _learnLabel.text = Strings.Get(maxed ? "talent.maxed" : "talent.learn");
            if (_learnButton != null) _learnButton.SetAvailable(open && !maxed && points >= def.Cost);
        }

        private Sprite IconOf(TalentDef def)
        {
            if (def.IsJobTalent)
            {
                int j = System.Array.IndexOf(TalentCatalog.Job, def);
                return j >= 0 && j < _jobIconSprites.Length ? _jobIconSprites[j] : null;
            }

            int i = System.Array.IndexOf(TalentCatalog.All, def);
            return i >= 0 && i < _icons.Length ? _icons[i] : null;
        }

        /// <summary>Moves the pulsing selection frame onto <paramref name="frame"/>.</summary>
        private void Select(Image frame)
        {
            if (_selection == null || frame == null || _selection.parent == frame.transform) return;
            _selection.SetParent(frame.transform, false);
            _selection.SetAsLastSibling();
            _selection.anchorMin = Vector2.zero;
            _selection.anchorMax = Vector2.one;
            _selection.offsetMin = new Vector2(-10f, -10f);
            _selection.offsetMax = new Vector2(10f, 10f);
        }

        private static Color RankColor(int rank, int max, bool learnable) =>
            rank >= max ? new Color(1f, 0.85f, 0.35f, 1f) : learnable ? new Color(0.55f, 1f, 0.45f, 1f) : Color.white;

        private static string LineKey(JobLine line)
        {
            switch (line)
            {
                case JobLine.Warrior: return "job.line.warrior";
                case JobLine.Mage: return "job.line.mage";
                case JobLine.Archer: return "job.line.archer";
                default: return "talent.job_none";
            }
        }

        /// <summary>Display number for a rank: percent for fraction stats, points for crit rate.</summary>
        private static string Amount(TalentDef def, int rank)
        {
            double value = def.PerRank * rank;
            if (def.Stat != TalentStat.CritPoints) value *= 100d;
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private void Play(SfxId id)
        {
            if (_audio != null) _audio.Play(id);
        }
    }
}
