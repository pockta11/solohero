using SoloHero.Core.Common;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Equipment panel (E7-06): 4 slots with the equipped grade in its color, owned grades per slot,
    /// and a swap button that steps to the next owned item in that slot (downgrades allowed, GDD).
    /// </summary>
    public sealed class EquipmentPanelPresenter : MonoBehaviour
    {
        [SerializeField] private Text[] _slotTexts = new Text[4];
        [SerializeField] private Text[] _ownedTexts = new Text[4];
        [SerializeField] private TapGuardButton[] _swapButtons = new TapGuardButton[4];
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private Image[] _slotIcons = new Image[4];
        [SerializeField] private EquipmentIconSet _icons;

        private EquipService _equip;
        private SaveDataV2 _save;
        private int _shownOwnedCount = -1;
        private string _shownKey = "";
        private int _shownPulls = -1;

        private void OnEnable()
        {
            _equip = PanelServices.TryGet<EquipService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _shownOwnedCount = -1;
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            string key = _save.equippedSword + _save.equippedHelm + _save.equippedArmor + _save.equippedBoots;
            if (_save.ownedEquipment.Count == _shownOwnedCount && key == _shownKey && _save.totalPullCount == _shownPulls) return;
            Refresh();
        }

        public void Swap(int slotIndex)
        {
            if (_equip == null || _save == null) return;
            var slot = (EquipmentSlot)slotIndex;
            int current = EquipmentBonus.GradeOrNone(Equipped(slot), slot);

            string next = null;
            for (int step = 1; step <= GachaCatalog.GradeCount; step++)
            {
                var grade = (Grade)((current + step + GachaCatalog.GradeCount) % GachaCatalog.GradeCount);
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

        private void Refresh()
        {
            if (_save == null) return;
            _shownOwnedCount = _save.ownedEquipment.Count;
            _shownPulls = _save.totalPullCount;
            _shownKey = _save.equippedSword + _save.equippedHelm + _save.equippedArmor + _save.equippedBoots;

            for (int s = 0; s < GachaCatalog.SlotCount; s++)
            {
                var slot = (EquipmentSlot)s;
                int grade = EquipmentBonus.GradeOrNone(Equipped(slot), slot);
                int owned = 0;
                string ownedList = "";
                for (int g = 0; g < GachaCatalog.GradeCount; g++)
                {
                    if (!_save.ownedEquipment.Contains(GachaCatalog.IdOf(slot, (Grade)g))) continue;
                    owned++;
                    int ownedLevel = EquipmentLevels.Get(_save, GachaCatalog.IdOf(slot, (Grade)g));
                    ownedList += (ownedList.Length > 0 ? " " : "") + PanelServices.GradeName((Grade)g)
                        + (ownedLevel > 0 ? "+" + ownedLevel : "");
                }

                if (s < _slotTexts.Length && _slotTexts[s] != null)
                {
                    int level = grade < 0 ? 0 : EquipmentLevels.Get(_save, Equipped(slot));
                    _slotTexts[s].text = grade < 0
                        ? Strings.Format("equip.empty", PanelServices.SlotName(slot))
                        : Strings.Format("equip.slot", PanelServices.SlotName(slot), PanelServices.GradeName((Grade)grade) + (level > 0 ? " +" + level : ""));
                    _slotTexts[s].color = grade < 0 ? Color.white : PanelServices.GradeColor((Grade)grade);
                }

                if (s < _slotIcons.Length && _slotIcons[s] != null && _icons != null)
                {
                    // Empty slot: the common icon as a dim silhouette, so the slot still reads at a glance (E8-07).
                    _slotIcons[s].sprite = _icons.Get(slot, grade < 0 ? Grade.Common : (Grade)grade);
                    _slotIcons[s].color = grade < 0 ? new Color(0f, 0f, 0f, 0.45f) : Color.white;
                }

                if (s < _ownedTexts.Length && _ownedTexts[s] != null)
                    _ownedTexts[s].text = owned == 0 ? Strings.Get("equip.none") : Strings.Format("equip.owned", ownedList);
                if (s < _swapButtons.Length && _swapButtons[s] != null)
                    _swapButtons[s].SetAvailable(owned > 1 || (owned == 1 && grade < 0));
            }
        }

        private string Equipped(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Sword: return _save.equippedSword;
                case EquipmentSlot.Helm: return _save.equippedHelm;
                case EquipmentSlot.Armor: return _save.equippedArmor;
                default: return _save.equippedBoots;
            }
        }
    }
}
