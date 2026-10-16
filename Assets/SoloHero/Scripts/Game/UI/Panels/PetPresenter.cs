using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-114 pet popup (right rail; the D-102 companions): the selected pet on a pedestal with its grade, level, enhance,
    /// attack and owned effect, equip and level-up buttons (D-177: at max level the button awakens the pet for stones),
    /// then a 5 x 4 collection grid (unowned pets as silhouettes) and a shortcut to the pet summon. Changes refresh the
    /// combat loadout.
    /// </summary>
    public sealed class PetPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;
        [SerializeField] private PanelHost _panels;
        [SerializeField] private GachaPanelPresenter _gacha;
        [SerializeField] private int _summonTab = 2;

        [Tooltip("Index-aligned with PetCatalog.")]
        [SerializeField] private CharacterArt[] _arts = new CharacterArt[0];
        [SerializeField] private GradeFrameSet _frames;

        [Header("Grid (index-aligned with PetCatalog)")]
        [SerializeField] private Image[] _cellFrames = new Image[0];
        [SerializeField] private Image[] _cellIcons = new Image[0];
        [SerializeField] private Text[] _cellLevels = new Text[0];
        [SerializeField] private GameObject[] _cellTags = new GameObject[0];
        [SerializeField] private RectTransform _selection;

        [Header("Detail")]
        [SerializeField] private UiFlipbook _detailArt;
        [SerializeField] private Image _detailGradeChip;
        [SerializeField] private Text _detailGrade;
        [SerializeField] private Text _detailName;
        [SerializeField] private Text _detailInfo;
        [SerializeField] private Text _summary;
        [SerializeField] private TapGuardButton _equipButton;
        [SerializeField] private Text _equipLabel;
        [SerializeField] private TapGuardButton _levelButton;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Image _levelIcon;
        [SerializeField] private Sprite _goldIcon;
        [SerializeField] private Sprite _awakenIcon;
        [Tooltip("D-178: the summon shortcut's label; before the first boss it names the stage that opens the pet summon.")]
        [SerializeField] private Text _summonLabel;

        private PetService _pets;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private double _shownGold = -1d;
        private int _shownOwned = -1;
        private int _shownAwaken = -1;
        private int _selected = -1;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _pets = PanelServices.TryGet<PetService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
        }

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            _selected = _pets != null ? _pets.EquippedIndex : 0;
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private string StageLabel(int global)
        {
            StageIndex.FromGlobal(global, _balance.STAGES_PER_CHAPTER, out int chapter, out int stage);
            return chapter + "-" + stage;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= PetCatalog.Count) return;
            _selected = index;
            Refresh();
        }

        public void Equip()
        {
            if (_pets == null) return;
            Result result = _pets.TryEquip(_selected);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            _session?.RefreshLoadout();
            Refresh();
        }

        public void LevelUp()
        {
            if (_pets == null) return;
            // D-177: a max-level pet's button awakens it instead.
            if (_pets.IsOwned(_selected) && _pets.IsMax(_selected))
            {
                Awaken();
                return;
            }

            Result result = _pets.TryLevelUp(_selected);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            _session?.RefreshLoadout();
            // D-171: the level up chimes and sparkles in the pet's grade colour.
            PanelServices.TryGet<SoloHero.Game.Audio.AudioService>()?.Play(SoloHero.Game.Audio.SfxId.Upgrade);
            if (_selected >= 0 && _selected < PetCatalog.Count)
                _rewardFx?.Sparkle(PanelServices.GradeColor(PetCatalog.All[_selected].Grade), Strings.Get("fx.level_up"));
            Refresh();
        }

        /// <summary>D-177: one awakening star for stones (genre "pet awakening"): the epic chime and a bigger sparkle.</summary>
        private void Awaken()
        {
            Result result = _pets.TryAwaken(_selected);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            _session?.RefreshLoadout();
            int stars = _pets.Stars(_selected);
            GameAnalytics.Log(AnalyticsEvents.PetAwaken,
                AnalyticsParam.Of(AnalyticsEvents.PPet, PetCatalog.All[_selected].Id),
                AnalyticsParam.Of(AnalyticsEvents.PLevel, stars));
            PanelServices.TryGet<SoloHero.Game.Audio.AudioService>()?.Play(SoloHero.Game.Audio.SfxId.GradeEpic);
            _rewardFx?.Sparkle(PanelServices.GradeColor(PetCatalog.All[_selected].Grade), Strings.Format("fx.awaken", stars));
            Refresh();
        }

        /// <summary>The pet summon: closes the popup and opens the summon tab on its pet page.</summary>
        public void OpenSummon()
        {
            Close();
            if (_panels != null) _panels.Open(_summonTab);
            if (_gacha != null) _gacha.SetMode(GachaPanelPresenter.ModePet);
        }

        private void LateUpdate()
        {
            if (!IsOpen || _save == null) return;
            int owned = _save.petOwned != null ? _save.petOwned.Count : 0;
            if (_save.gold != _shownGold || owned != _shownOwned || _save.awakenStones != _shownAwaken) Refresh();
        }

        private void Refresh()
        {
            if (_pets == null || _save == null || _balance == null) return;
            _shownGold = _save.gold;
            _shownOwned = _pets.OwnedCount;
            _shownAwaken = _save.awakenStones;
            if (_selected < 0 || _selected >= PetCatalog.Count) _selected = 0;
            int equipped = _pets.EquippedIndex;

            for (int i = 0; i < PetCatalog.Count; i++)
            {
                PetDef def = PetCatalog.All[i];
                bool owned = _pets.IsOwned(i);
                if (i < _cellFrames.Length && _cellFrames[i] != null && _frames != null)
                    _cellFrames[i].sprite = owned ? _frames.Get(def.Grade) : _frames.empty;
                if (i < _cellIcons.Length && _cellIcons[i] != null)
                {
                    CharacterArt art = i < _arts.Length ? _arts[i] : null;
                    _cellIcons[i].sprite = art != null && art.idle.Length > 0 ? art.idle[0] : null;
                    _cellIcons[i].color = owned ? Color.white : UiPalette.Silhouette;
                }

                if (i < _cellLevels.Length && _cellLevels[i] != null)
                {
                    int enhance = _pets.Enhance(i);
                    int stars = _pets.Stars(i);
                    // D-177: an awakened pet shows its stars where the level was (it is at max level).
                    _cellLevels[i].text = owned
                        ? (stars > 0 ? Strings.Format("pet.stars", stars) : Strings.Format("pet.cell", _pets.Level(i))) + (enhance > 0 ? " +" + enhance : "")
                        : "?";
                }

                if (i < _cellTags.Length && _cellTags[i] != null) _cellTags[i].SetActive(i == equipped);
            }

            if (_selection != null && _selected < _cellFrames.Length && _cellFrames[_selected] != null)
            {
                var cell = (RectTransform)_cellFrames[_selected].transform;
                _selection.SetParent(cell, false);
                _selection.anchorMin = Vector2.zero;
                _selection.anchorMax = Vector2.one;
                _selection.offsetMin = new Vector2(-8f, -8f);
                _selection.offsetMax = new Vector2(8f, 8f);
                _selection.SetAsLastSibling();
            }

            DrawDetail(_selected, equipped);
            if (_summary != null)
            {
                double owned = PetService.OwnedAtkBonus(_balance, _save) * 100d;
                _summary.text = Strings.Format("pet.summary", Percent(owned), _pets.OwnedCount, PetCatalog.Count);
            }
        }

        private void DrawDetail(int index, int equipped)
        {
            PetDef def = PetCatalog.All[index];
            bool owned = _pets.IsOwned(index);
            int level = _pets.Level(index);
            int enhance = _pets.Enhance(index);
            int stars = _pets.Stars(index);
            if (_detailArt != null && index < _arts.Length) _detailArt.SetArt(_arts[index]);
            if (_detailArt != null) _detailArt.GetComponent<Image>().color = owned ? Color.white : UiPalette.Silhouette;
            if (_detailGradeChip != null) _detailGradeChip.color = PanelServices.GradeColor(def.Grade);
            if (_detailGrade != null) _detailGrade.text = PanelServices.GradeName(def.Grade);
            if (_detailName != null)
            {
                string name = Strings.Get(def.NameKey);
                _detailName.text = owned
                    ? Strings.Format("pet.row", name, level) + (stars > 0 ? " " + Strings.Format("pet.stars", stars) : "") + (enhance > 0 ? "  +" + enhance : "")
                    : name;
                _detailName.color = UiPalette.GradeInk(def.Grade);
            }

            if (_detailInfo != null)
            {
                double scale = PetService.AttackScale(_balance, _save, index);
                string attack = Strings.Format("pet.attack", (def.AttackMult * scale * 100d).ToString("0", CultureInfo.InvariantCulture),
                    def.Interval.ToString("0.#", CultureInfo.InvariantCulture));
                string effect = def.DotPercent > 0d ? Strings.Get(def.Vfx == "poison" ? "pet.effect_poison" : "pet.effect_burn")
                    : def.StunSeconds > 0f ? Strings.Get("pet.effect_stun") : "";
                double ownedPercent = PetService.OwnedAtkPercent(_balance, def.Grade, enhance, level, stars);
                bool awakening = owned && _pets.IsMax(index);
                // D-178: before the first boss the unowned line names the stage that opens the pet summon.
                string unowned = PetSummonService.IsUnlocked(_balance, _save)
                    ? Strings.Get("pet.unowned")
                    : Strings.Format("gacha.pet_locked", StageLabel(_balance.PET_UNLOCK_STAGE));
                _detailInfo.text = (effect.Length > 0 ? Strings.Format("pet.with_effect", attack, effect) : attack) + "\n"
                    + Strings.Format("pet.owned_effect", Percent(ownedPercent))
                    + (awakening ? "\n" + Strings.Format("pet.awaken_info", stars, _balance.PET_AWAKEN_MAX_STARS, _save.awakenStones) : "")
                    + (owned ? "" : "\n" + unowned);
            }

            if (_summonLabel != null)
            {
                _summonLabel.text = PetSummonService.IsUnlocked(_balance, _save)
                    ? Strings.Get("pet.summon")
                    : Strings.Format("pet.summon_locked", StageLabel(_balance.PET_UNLOCK_STAGE));
            }

            if (_equipButton != null) _equipButton.SetAvailable(owned && index != equipped);
            if (_equipLabel != null) _equipLabel.text = Strings.Get(index == equipped ? "pet.equipped" : "pet.equip");
            bool max = owned && _pets.IsMax(index);
            if (max)
            {
                // D-177: the button turns into awakening (stones; done at PET_AWAKEN_MAX_STARS).
                int awaken = _pets.AwakenCost(index);
                bool done = awaken <= 0;
                if (_levelLabel != null)
                    _levelLabel.text = done ? Strings.Get("pet.awaken_done") : Strings.Format("pet.awaken_cost", stars + 1, awaken);
                if (_levelIcon != null && _awakenIcon != null) _levelIcon.sprite = _awakenIcon;
                if (_levelButton != null) _levelButton.SetAvailable(!done && _save.awakenStones >= awaken);
            }
            else
            {
                double cost = owned ? _pets.LevelCost(index) : 0d;
                if (_levelIcon != null && _goldIcon != null) _levelIcon.sprite = _goldIcon;
                if (_levelButton != null) _levelButton.SetAvailable(owned && _save.gold >= cost);
                if (_levelLabel != null) _levelLabel.text = !owned ? Strings.Get("pet.level_up") : BigNumberFormat.Format(cost);
            }
        }

        private static string Percent(double percent) =>
            percent.ToString(percent < 10d ? "0.#" : "0", CultureInfo.InvariantCulture);
    }
}
