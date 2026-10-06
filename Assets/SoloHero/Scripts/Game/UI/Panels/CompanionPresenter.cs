using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Companions;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-102 companions popup (right rail): one row per companion with its portrait, name, level and attack, then
    /// either the unlock condition or equip / level-up buttons. Changes refresh the combat loadout.
    /// </summary>
    public sealed class CompanionPresenter : MonoBehaviour
    {
        private static readonly Color LockedTint = UiPalette.Silhouette;

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private CharacterArt[] _arts = new CharacterArt[0];
        [SerializeField] private Image[] _portraits = new Image[4];
        [SerializeField] private Text[] _names = new Text[4];
        [SerializeField] private Text[] _infos = new Text[4];
        [SerializeField] private Button[] _equipButtons = new Button[4];
        [SerializeField] private Text[] _equipLabels = new Text[4];
        [SerializeField] private Button[] _levelButtons = new Button[4];
        [SerializeField] private Text[] _levelLabels = new Text[4];

        private CompanionService _companions;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private double _shownGold = -1d;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _companions = PanelServices.TryGet<CompanionService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
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

        public void Equip(int index)
        {
            if (_companions == null) return;
            Result result = _companions.TryEquip(index);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            _session?.RefreshLoadout();
            Refresh();
        }

        public void LevelUp(int index)
        {
            if (_companions == null) return;
            Result result = _companions.TryLevelUp(index);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            _session?.RefreshLoadout();
            Refresh();
        }

        private void LateUpdate()
        {
            if (IsOpen && _save != null && _save.gold != _shownGold) Refresh();
        }

        private void Refresh()
        {
            if (_companions == null || _save == null || _balance == null) return;
            _shownGold = _save.gold;
            int equipped = _companions.EquippedIndex;
            for (int i = 0; i < CompanionCatalog.Count && i < _names.Length; i++)
            {
                CompanionDef def = CompanionCatalog.All[i];
                bool unlocked = _companions.IsUnlocked(i);
                int level = _companions.Level(i);
                CharacterArt art = i < _arts.Length ? _arts[i] : null;
                if (_portraits[i] != null)
                {
                    _portraits[i].sprite = art != null && art.idle.Length > 0 ? art.idle[0] : null;
                    _portraits[i].color = unlocked ? Color.white : LockedTint;
                }

                if (_names[i] != null)
                {
                    _names[i].text = Strings.Format("companion.row", Strings.Get(def.NameKey), level);
                    _names[i].color = UiPalette.GradeInk(def.Grade);
                }

                if (_infos[i] != null)
                {
                    double percent = def.AttackMult * Formulas.CompanionLevelScale(_balance, level) * 100d;
                    _infos[i].text = unlocked
                        ? Strings.Format("companion.attack", percent.ToString("0", CultureInfo.InvariantCulture), def.Interval.ToString("0.#", CultureInfo.InvariantCulture))
                        : Strings.Format("companion.unlock", StageLabel(def.UnlockStage));
                }

                if (_equipButtons[i] != null)
                {
                    _equipButtons[i].gameObject.SetActive(unlocked);
                    _equipButtons[i].interactable = unlocked && i != equipped;
                }

                if (_equipLabels[i] != null) _equipLabels[i].text = Strings.Get(i == equipped ? "companion.equipped" : "companion.equip");
                bool max = _companions.IsMax(i);
                double cost = max ? 0d : _companions.LevelCost(i);
                if (_levelButtons[i] != null)
                {
                    _levelButtons[i].gameObject.SetActive(unlocked);
                    _levelButtons[i].interactable = unlocked && !max && _save.gold >= cost;
                }

                if (_levelLabels[i] != null) _levelLabels[i].text = max ? Strings.Get("char.max") : BigNumberFormat.Format(cost);
            }
        }

        /// <summary>"c-s" label of the boss stage that unlocks a companion.</summary>
        private string StageLabel(int g)
        {
            int per = _balance.STAGES_PER_CHAPTER;
            return ((g - 1) / per + 1) + "-" + ((g - 1) % per + 1);
        }
    }
}
