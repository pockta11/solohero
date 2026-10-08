using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Equipment panel (E7-06): one slot per EquipmentSlot with the equipped grade in its color and a swap on tap that
    /// steps to the next owned item in that slot (downgrades allowed, GDD).
    /// D-106: the slots stand around the hero, with ATK / HP / DEF and "equip best" below.
    /// D-109: 8 slots (gear on the left, accessories on the right) and the collection's owned bonus.
    /// D-116: "promote all" turns every max-level item into its next grade (up to Epic) while gold lasts.
    /// </summary>
    public sealed class EquipmentPanelPresenter : MonoBehaviour
    {
        [SerializeField] private Text[] _slotTexts = new Text[GachaCatalog.SlotCount];
        [SerializeField] private Text[] _ownedTexts = new Text[GachaCatalog.SlotCount];
        [SerializeField] private TapGuardButton[] _swapButtons = new TapGuardButton[GachaCatalog.SlotCount];
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private Image[] _slotIcons = new Image[GachaCatalog.SlotCount];
        [SerializeField] private Image[] _slotFrames = new Image[GachaCatalog.SlotCount];
        [SerializeField] private EquipmentIconSet _icons;
        [SerializeField] private GradeFrameSet _frames;
        [Tooltip("D-106: ATK, HP, DEF of the current loadout.")]
        [SerializeField] private Text[] _statTexts = new Text[3];
        [Tooltip("D-140: the ATK chip icon, spell power for mages.")]
        [SerializeField] private Image _atkStatIcon;
        [SerializeField] private Sprite _atkSprite;
        [SerializeField] private Sprite _spellSprite;
        [Tooltip("D-109: the owned bonus of the whole collection.")]
        [SerializeField] private Text _ownedBonusText;
        [SerializeField] private TapGuardButton _promoteButton;
        [SerializeField] private Text _promoteLabel;

        private EquipService _equip;
        private SaveDataV2 _save;
        private SoloHero.Core.Config.BalanceValues _balance;
        private int _shownOwnedCount = -1;
        private readonly string[] _shownIds = new string[GachaCatalog.SlotCount];
        private int _shownPulls = -1;
        private double _shownGold = -1d;
        private string _shownJob;

        private void OnEnable()
        {
            _equip = PanelServices.TryGet<EquipService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<SoloHero.Core.Config.BalanceValues>();
            _shownOwnedCount = -1;
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_save.gold != _shownGold) DrawPromote();
            // D-140: the weapon icon follows the job line.
            if (_save.ownedEquipment.Count == _shownOwnedCount && _save.totalPullCount == _shownPulls && _save.jobId == _shownJob
                && !EquippedChanged()) return;
            Refresh();
        }

        private bool EquippedChanged()
        {
            for (int s = 0; s < _shownIds.Length; s++)
            {
                if (!ReferenceEquals(_shownIds[s], Equipped((EquipmentSlot)s))) return true;
            }

            return false;
        }

        public void Swap(int slotIndex)
        {
            if (_equip == null || _save == null) return;
            var slot = (EquipmentSlot)slotIndex;
            int current = EquipmentBonus.GradeOrNone(Equipped(slot), slot);

            string next = null;
            for (int step = 1; step <= GachaCatalog.GradeCount; step++)
            {
                var grade = (GearGrade)((current + step + GachaCatalog.GradeCount) % GachaCatalog.GradeCount);
                string id = GachaCatalog.IdOf(slot, grade);
                if (_save.ownedEquipment.Contains(id))
                {
                    next = id;
                    break;
                }
            }

            if (next == null)
            {
                if (_toast != null) _toast.ShowFailure(FailReason.Locked);
                return;
            }

            Result result = _equip.TryEquip(_save, slot, next);
            if (!result.Ok)
            {
                if (_toast != null) _toast.ShowFailure(result.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            Refresh();
        }

        /// <summary>D-116: promotes every max-level item it can, lowest grade first, while gold lasts.</summary>
        public void PromoteAll()
        {
            if (_save == null || _balance == null) return;
            EquipPromotion.TotalCost(_balance, _save, out int eligible);
            int made = EquipPromotion.PromoteAll(_balance, _save, out double spent);
            if (made > 0)
            {
                PanelServices.TryGet<SoloHero.Core.Growth.ISaveRequester>()?.RequestSave();
                if (_session != null) _session.RefreshLoadout();
                PanelServices.TryGet<AudioService>()?.Play(SfxId.Upgrade);
                if (_toast != null) _toast.Show(Strings.Format("equip.promoted", made, BigNumberFormat.Format(spent)));
            }
            else if (_toast != null)
            {
                if (eligible > 0) _toast.ShowFailure(FailReason.NotEnoughGold);
                else _toast.Show(Strings.Get("equip.promote_none"));
            }

            Refresh();
        }

        private void DrawPromote()
        {
            if (_save == null || _balance == null) return;
            _shownGold = _save.gold;
            EquipPromotion.TotalCost(_balance, _save, out int count);
            if (_promoteLabel != null)
                _promoteLabel.text = count > 0 ? Strings.Format("equip.promote_n", count) : Strings.Get("equip.promote");
            if (_promoteButton != null) _promoteButton.SetAvailable(true);
        }

        /// <summary>D-106: equips the highest owned grade in every slot.</summary>
        public void EquipBest()
        {
            if (_equip == null || _save == null) return;
            bool changed = false;
            for (int s = 0; s < GachaCatalog.SlotCount; s++)
            {
                var slot = (EquipmentSlot)s;
                for (int g = GachaCatalog.GradeCount - 1; g >= 0; g--)
                {
                    string id = GachaCatalog.IdOf(slot, (GearGrade)g);
                    if (!_save.ownedEquipment.Contains(id)) continue;
                    if (id != Equipped(slot) && _equip.TryEquip(_save, slot, id).Ok) changed = true;
                    break;
                }
            }

            if (changed)
            {
                if (_session != null) _session.RefreshLoadout();
                PanelServices.TryGet<AudioService>()?.Play(SfxId.Upgrade);
            }
            else if (_toast != null)
            {
                _toast.Show(Strings.Get("equip.best_already"));
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_save == null) return;
            _shownOwnedCount = _save.ownedEquipment.Count;
            _shownPulls = _save.totalPullCount;
            _shownJob = _save.jobId;
            JobLine line = JobService.LineOf(_save);
            WeaponKind weapon = JobTerms.WeaponOf(line);
            Sprite atkSprite = line == JobLine.Mage && _spellSprite != null ? _spellSprite : _atkSprite;
            if (_atkStatIcon != null && atkSprite != null) _atkStatIcon.sprite = atkSprite;
            for (int s = 0; s < _shownIds.Length; s++) _shownIds[s] = Equipped((EquipmentSlot)s);
            EquipmentBonus bonus = _balance != null ? EquipmentBonus.Resolve(_balance, _save) : default;

            for (int s = 0; s < GachaCatalog.SlotCount; s++)
            {
                var slot = (EquipmentSlot)s;
                int grade = EquipmentBonus.GradeOrNone(Equipped(slot), slot);
                int owned = 0;
                for (int g = 0; g < GachaCatalog.GradeCount; g++)
                {
                    if (!_save.ownedEquipment.Contains(GachaCatalog.IdOf(slot, (GearGrade)g))) continue;
                    owned++;
                }

                if (s < _slotTexts.Length && _slotTexts[s] != null)
                {
                    int level = grade < 0 ? 0 : EquipmentLevels.Get(_save, Equipped(slot));
                    _slotTexts[s].text = grade < 0
                        ? PanelServices.SlotName(slot)
                        : PanelServices.GradeName((GearGrade)grade) + (level > 0 ? " +" + level : "");
                    _slotTexts[s].color = grade < 0 ? UiPalette.InkMuted : UiPalette.GradeInk((GearGrade)grade);
                }

                if (s < _slotIcons.Length && _slotIcons[s] != null && _icons != null)
                {
                    // Empty slot: the common icon as a dim silhouette, so the slot still reads at a glance (E8-07).
                    _slotIcons[s].sprite = _icons.Get(slot, grade < 0 ? GearGrade.Common : (GearGrade)grade, weapon);
                    _slotIcons[s].color = grade < 0 ? UiPalette.Silhouette : Color.white;
                }

                if (s < _slotFrames.Length && _slotFrames[s] != null && _frames != null)
                    _slotFrames[s].sprite = grade < 0 ? _frames.empty : _frames.Get((GearGrade)grade);

                if (s < _ownedTexts.Length && _ownedTexts[s] != null)
                    _ownedTexts[s].text = grade < 0 ? Strings.Get("equip.none") : Effect(slot, bonus);
                if (s < _swapButtons.Length && _swapButtons[s] != null)
                    _swapButtons[s].SetAvailable(owned > 1 || (owned == 1 && grade < 0));
            }

            if (_balance == null) return;
            HeroStats stats = CombatLoadout.ComputeStats(_balance, _save);
            SetStat(0, stats.Atk);
            SetStat(1, stats.Hp);
            SetStat(2, stats.Def);
            if (_ownedBonusText != null)
                _ownedBonusText.text = Strings.Format("equip.owned_bonus", Percent(bonus.OwnedAtk));
            DrawPromote();
        }

        private static string Percent(double fraction) =>
            (fraction * 100d).ToString(fraction * 100d < 10d ? "0.#" : "0", System.Globalization.CultureInfo.InvariantCulture);

        private void SetStat(int index, double value)
        {
            if (index < _statTexts.Length && _statTexts[index] != null) _statTexts[index].text = BigNumberFormat.Format(value);
        }

        /// <summary>D-103: what the equipped item does, e.g. "ATK x1.80" (boots: attack speed and crit).</summary>
        private static string Effect(EquipmentSlot slot, EquipmentBonus bonus)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            switch (slot)
            {
                case EquipmentSlot.Sword: return Strings.Format("equip.effect_atk", bonus.SwordMult.ToString("0.00", inv));
                case EquipmentSlot.Helm: return Strings.Format("equip.effect_def", bonus.HelmMult.ToString("0.00", inv));
                case EquipmentSlot.Armor: return Strings.Format("equip.effect_hp", bonus.ArmorMult.ToString("0.00", inv));
                case EquipmentSlot.Gloves: return Strings.Format("equip.effect_atk", bonus.GlovesMult.ToString("0.00", inv));
                case EquipmentSlot.Necklace: return Strings.Format("equip.effect_hp", bonus.NecklaceMult.ToString("0.00", inv));
                case EquipmentSlot.Ring: return Strings.Format("equip.effect_ring", Percent(bonus.RingCritDamage));
                case EquipmentSlot.Earring: return Strings.Format("equip.effect_earring", Percent(bonus.EarringSkillDamage));
                default:
                    return Strings.Format("equip.effect_boots",
                        (bonus.BootsSpeedBonus * 100d).ToString("0", inv),
                        bonus.BootsCritBonus.ToString("0.#", inv));
            }
        }

        private string Equipped(EquipmentSlot slot) => EquippedSlots.Get(_save, slot);
    }
}
