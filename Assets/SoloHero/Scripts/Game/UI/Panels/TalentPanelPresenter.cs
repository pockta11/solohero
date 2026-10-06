using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
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
    /// </summary>
    public sealed class TalentPanelPresenter : MonoBehaviour
    {
        // D-108: nodes are light slot frames on the cream panel, tinted by state; branch names use the darker inks.
        private static readonly Color LockedIcon = UiPalette.Silhouette;
        private static readonly Color LockedFrame = new Color(0.8f, 0.76f, 0.72f, 1f);
        private static readonly Color OpenFrame = Color.white;
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

        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;

        private TalentService _talents;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private AudioService _audio;
        private int _selected;
        private int _shownHeroLevel = -1;
        private double _shownGem = -1d;

        private void OnEnable()
        {
            _talents = PanelServices.TryGet<TalentService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
            if (_talents != null) _talents.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_talents != null) _talents.Changed -= Refresh;
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_save.heroLevel == _shownHeroLevel && _save.gem == _shownGem) return;
            Refresh();
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= TalentCatalog.All.Length) return;
            _selected = index;
            Play(SfxId.Tap);
            Refresh();
        }

        public void Learn()
        {
            if (_talents == null) return;
            Result r = _talents.TryLearn(TalentCatalog.All[_selected].Id);
            if (!r.Ok)
            {
                if (_toast != null) _toast.ShowFailure(r.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            if (_detailPunch != null) _detailPunch.Play(0.4f);
            Play(SfxId.Upgrade);
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

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownHeroLevel = _save.heroLevel;
            _shownGem = _save.gem;
            int points = TalentService.Available(_balance, _save);
            if (_pointsText != null) _pointsText.text = Strings.Format("talent.points", points);
            if (_resetLabel != null) _resetLabel.text = Strings.Format("talent.reset", _balance.TALENT_RESET_GEM);
            if (_resetButton != null) _resetButton.SetAvailable(TalentService.Spent(_save) > 0 && _save.gem >= _balance.TALENT_RESET_GEM);

            for (int b = 0; b < _branchTexts.Length && b < TalentCatalog.BranchCount; b++)
            {
                if (_branchTexts[b] == null) continue;
                _branchTexts[b].text = Strings.Format("talent.branch_spent",
                    Strings.Get("talent.branch." + b), TalentService.BranchSpent(_save, (TalentBranch)b));
                _branchTexts[b].color = BranchInks[b];
            }

            for (int i = 0; i < TalentCatalog.All.Length && i < _cellFrames.Length; i++) DrawCell(i, points);
            DrawDetail(points);
        }

        private void DrawCell(int i, int points)
        {
            TalentDef def = TalentCatalog.All[i];
            int rank = TalentService.Rank(_save, def.Id);
            bool open = TalentService.IsTierOpen(_balance, _save, def);
            bool learnable = open && rank < def.MaxRank && points > 0;
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
                _cellRanks[i].color = rank >= def.MaxRank ? new Color(1f, 0.85f, 0.35f, 1f)
                    : learnable ? new Color(0.55f, 1f, 0.45f, 1f) : Color.white;
            }

            if (_selection == null || i != _selected || _cellFrames[i] == null || _selection.parent == _cellFrames[i].transform) return;
            _selection.SetParent(_cellFrames[i].transform, false);
            _selection.SetAsLastSibling();
            _selection.anchorMin = Vector2.zero;
            _selection.anchorMax = Vector2.one;
            _selection.offsetMin = new Vector2(-10f, -10f);
            _selection.offsetMax = new Vector2(10f, 10f);
        }

        private void DrawDetail(int points)
        {
            TalentDef def = TalentCatalog.All[_selected];
            int rank = TalentService.Rank(_save, def.Id);
            bool open = TalentService.IsTierOpen(_balance, _save, def);
            bool maxed = rank >= def.MaxRank;

            if (_detailIcon != null)
            {
                _detailIcon.sprite = _selected < _icons.Length ? _icons[_selected] : null;
                _detailIcon.color = open ? Color.white : LockedIcon;
            }

            if (_detailFrame != null) _detailFrame.color = BranchColors[(int)def.Branch];
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
                    desc += "\n" + Strings.Format("talent.need",
                        Strings.Get("talent.branch." + (int)def.Branch), TalentService.TierRequirement(_balance, def.Tier));
                }

                _detailDesc.text = desc;
            }

            if (_learnLabel != null) _learnLabel.text = Strings.Get(maxed ? "talent.maxed" : "talent.learn");
            if (_learnButton != null) _learnButton.SetAvailable(open && !maxed && points > 0);
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
