using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Skill panel (D-078, genre skill book): the equipped slots on top (locked ones show their hero level), every
    /// catalog skill in a 6-column grid (grade frame, level badge, slot number when equipped, dark with a lock when not owned), and
    /// the selected skill's detail with level-up and equip / unequip. Equipping into full slots enters a pick mode:
    /// tap the slot to replace. Auto-equip fills the slots with the strongest skills.
    /// D-104: skills of another job line (or any line before the first job) are tinted and say which job uses them.
    /// D-107: the grid shows only the hero's own skills (the starters for the beginner, else the job's line of 12),
    /// and the collection count is out of those.
    /// </summary>
    public sealed class SkillPanelPresenter : MonoBehaviour
    {
        private static readonly Color PickPulse = new Color(0.4f, 1f, 0.5f, 1f);
        // D-108: unowned skills sit on the cream panel as a soft frame with the icon's silhouette.
        private static readonly Color UnownedFrame = new Color(0.82f, 0.78f, 0.74f, 1f);
        private static readonly Color UnownedIcon = new Color(0.42f, 0.4f, 0.48f, 0.8f);
        private static readonly Color OtherLineIcon = new Color(0.55f, 0.5f, 0.6f, 1f);

        /// <summary>D-104 line names by JobLine value.</summary>
        private static readonly string[] LineKeys = { "", "job.line.warrior", "job.line.mage", "job.line.archer" };

        [Header("Equipped slots")]
        [SerializeField] private Image[] _slotFrames = new Image[0];
        [SerializeField] private Image[] _slotIcons = new Image[0];
        [SerializeField] private Text[] _slotLevels = new Text[0];
        [SerializeField] private GameObject[] _slotBadges = new GameObject[0];
        [SerializeField] private GameObject[] _slotLocks = new GameObject[0];
        [SerializeField] private Text[] _slotLockTexts = new Text[0];

        [Header("Skill grid, catalog order")]
        [SerializeField] private Image[] _cellFrames = new Image[0];
        [SerializeField] private Image[] _cellIcons = new Image[0];
        [SerializeField] private Text[] _cellLevels = new Text[0];
        [SerializeField] private GameObject[] _cellBadges = new GameObject[0];
        [SerializeField] private GameObject[] _cellMarks = new GameObject[0];
        [SerializeField] private Text[] _cellMarkTexts = new Text[0];
        [SerializeField] private GameObject[] _cellLocks = new GameObject[0];
        [SerializeField] private RectTransform _selection;

        [Header("Detail")]
        [SerializeField] private Image _detailIcon;
        [SerializeField] private Image _detailFrame;
        [SerializeField] private Image _detailGradeChip;
        [SerializeField] private Text _detailGrade;
        [SerializeField] private Text _detailName;
        [SerializeField] private Text _detailInfo;
        [SerializeField] private Text _detailDesc;
        [SerializeField] private Text _ownedBonus;
        [SerializeField] private Text _ownedCount;
        [SerializeField] private TapGuardButton _levelButton;
        [SerializeField] private Text _levelCost;
        [SerializeField] private TapGuardButton _equipButton;
        [SerializeField] private Text _equipLabel;
        [SerializeField] private UiPunch _detailPunch;

        [SerializeField] private SkillIconSet _icons;
        [SerializeField] private GradeFrameSet _frames;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;

        private SkillService _skills;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private AudioService _audio;
        private string _selected = SkillCatalog.PowerStrike;
        private bool _picking;
        private double _shownGold = -1d;
        private int _shownHeroLevel = -1;
        private int _shownPulls = -1;
        private readonly object[] _descArgs = new object[14];

        private void OnEnable()
        {
            _skills = PanelServices.TryGet<SkillService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
            _picking = false;
            if (_skills != null) _skills.LoadoutChanged += OnLoadoutChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_skills != null) _skills.LoadoutChanged -= OnLoadoutChanged;
            _picking = false;
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_picking) PulseSlots();
            if (_save.gold == _shownGold && _save.heroLevel == _shownHeroLevel && _save.skillPullCount == _shownPulls) return;
            Refresh();
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= SkillCatalog.All.Length) return;
            _selected = SkillCatalog.All[index].Id;
            _picking = false;
            Play(SfxId.Tap);
            Refresh();
        }

        public void TapSlot(int slot)
        {
            if (_skills == null || _save == null) return;
            if (_picking)
            {
                _picking = false;
                Result r = _skills.TryEquip(_selected, slot);
                if (!r.Ok) Fail(r.Reason);
                else Equipped();
                Refresh();
                return;
            }

            string id = SkillBook.EquippedAt(_save, slot);
            if (!string.IsNullOrEmpty(id)) _selected = id;
            Play(SfxId.Tap);
            Refresh();
        }

        public void LevelUp()
        {
            if (_skills == null) return;
            Result r = _skills.TryLevelUp(_selected);
            if (!r.Ok)
            {
                Fail(r.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            GameAnalytics.Log(AnalyticsEvents.SkillLevel,
                AnalyticsParam.Of(AnalyticsEvents.PSkill, _selected),
                AnalyticsParam.Of(AnalyticsEvents.PLevel, _skills.Level(_selected)));
            if (_detailPunch != null) _detailPunch.Play(0.4f);
            Play(SfxId.Upgrade);
            Refresh();
        }

        public void ToggleEquip()
        {
            if (_skills == null || _save == null) return;
            int slot = SkillBook.SlotOf(_save, _selected);
            if (slot >= 0 && SkillService.IsSlotUnlocked(_balance, slot, _save.heroLevel))
            {
                Result off = _skills.TryUnequip(slot);
                if (!off.Ok) Fail(off.Reason);
                else if (_session != null) _session.RefreshLoadout();
                Refresh();
                return;
            }

            Result r = _skills.TryEquip(_selected);
            if (r.Ok)
            {
                Equipped();
            }
            else if (r.Reason == FailReason.SlotsFull)
            {
                _picking = true;
                if (_toast != null) _toast.Show(Strings.Get("skill.pick_slot"));
            }
            else
            {
                Fail(r.Reason);
            }

            Refresh();
        }

        public void AutoEquip()
        {
            if (_skills == null) return;
            _picking = false;
            _skills.AutoEquip();
            if (_session != null) _session.RefreshLoadout();
            Play(SfxId.Upgrade);
            Refresh();
        }

        private void OnLoadoutChanged() => Refresh();

        private void Equipped()
        {
            if (_session != null) _session.RefreshLoadout();
            Play(SfxId.Upgrade);
            if (_toast != null) _toast.Show(Strings.Format("toast.skill_equipped", Strings.Get(SkillCatalog.Find(_selected).NameKey)));
        }

        private void Fail(FailReason reason)
        {
            if (_toast != null) _toast.ShowFailure(reason);
        }

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownGold = _save.gold;
            _shownHeroLevel = _save.heroLevel;
            _shownPulls = _save.skillPullCount;
            DrawSlots();
            DrawGrid();
            DrawDetail();
        }

        private void DrawSlots()
        {
            for (int i = 0; i < _slotFrames.Length; i++)
            {
                bool unlocked = SkillService.IsSlotUnlocked(_balance, i, _save.heroLevel);
                SkillDef def = unlocked ? SkillCatalog.Find(SkillBook.EquippedAt(_save, i)) : null;
                Sprite sprite = def != null && _icons != null ? _icons.Get(def.IconId) : null;
                if (i < _slotIcons.Length && _slotIcons[i] != null)
                {
                    _slotIcons[i].sprite = sprite;
                    _slotIcons[i].enabled = sprite != null;
                    // D-104: an equipped skill of another line stays in its slot but does not fight.
                    _slotIcons[i].color = def == null || JobService.CanUse(_save, def) ? Color.white : OtherLineIcon;
                }

                SetFrame(_slotFrames[i], def, true);
                Set(_slotBadges, i, def != null);
                Set(_slotLocks, i, !unlocked);
                if (def != null && i < _slotLevels.Length && _slotLevels[i] != null)
                    _slotLevels[i].text = Strings.Format("skill.lv", SkillBook.GetLevel(_save, def.Id));
                if (!unlocked && i < _slotLockTexts.Length && _slotLockTexts[i] != null)
                    _slotLockTexts[i].text = Strings.Format("skill.slot_locked", SkillService.UnlockHeroLevel(_balance, i));
            }
        }

        private void DrawGrid()
        {
            int owned = 0;
            int shown = 0;
            SkillDef selected = SkillCatalog.Find(_selected);
            if (selected == null || !JobService.CanUse(_save, selected))
            {
                // The selection follows the job: start on the first skill of the hero's own list.
                for (int i = 0; i < SkillCatalog.All.Length; i++)
                {
                    if (!JobService.CanUse(_save, SkillCatalog.All[i])) continue;
                    _selected = SkillCatalog.All[i].Id;
                    break;
                }
            }

            for (int i = 0; i < SkillCatalog.All.Length && i < _cellFrames.Length; i++)
            {
                SkillDef def = SkillCatalog.All[i];
                bool visible = JobService.CanUse(_save, def);
                if (_cellFrames[i] != null && _cellFrames[i].gameObject.activeSelf != visible) _cellFrames[i].gameObject.SetActive(visible);
                if (!visible) continue;
                shown++;
                int level = SkillBook.GetLevel(_save, def.Id);
                bool has = level > 0;
                if (has) owned++;
                SetFrame(_cellFrames[i], def, has);
                if (i < _cellIcons.Length && _cellIcons[i] != null)
                {
                    _cellIcons[i].sprite = _icons != null ? _icons.Get(def.IconId) : null;
                    _cellIcons[i].color = !has ? UnownedIcon : JobService.CanUse(_save, def) ? Color.white : OtherLineIcon;
                }

                Set(_cellBadges, i, has);
                Set(_cellLocks, i, !has);
                if (has && i < _cellLevels.Length && _cellLevels[i] != null)
                    _cellLevels[i].text = Strings.Format("skill.lv", level);

                int slot = has ? SkillBook.SlotOf(_save, def.Id) : -1;
                bool equipped = slot >= 0 && SkillService.IsSlotUnlocked(_balance, slot, _save.heroLevel);
                Set(_cellMarks, i, equipped);
                if (equipped && i < _cellMarkTexts.Length && _cellMarkTexts[i] != null)
                    _cellMarkTexts[i].text = (slot + 1).ToString(CultureInfo.InvariantCulture);

                if (_selection != null && def.Id == _selected && _cellFrames[i] != null && _selection.parent != _cellFrames[i].transform)
                {
                    _selection.SetParent(_cellFrames[i].transform, false);
                    _selection.SetAsLastSibling();
                    _selection.anchorMin = Vector2.zero;
                    _selection.anchorMax = Vector2.one;
                    _selection.offsetMin = new Vector2(-10f, -10f);
                    _selection.offsetMax = new Vector2(10f, 10f);
                }
            }

            if (_ownedBonus != null)
            {
                double bonus = SkillService.OwnedAtkBonus(_balance, _save) * 100d;
                _ownedBonus.text = Strings.Format("skill.owned_bonus", bonus.ToString("0.#", CultureInfo.InvariantCulture));
            }

            if (_ownedCount != null) _ownedCount.text = Strings.Format("skill.collection", owned, shown);
        }

        private void DrawDetail()
        {
            SkillDef def = SkillCatalog.Find(_selected) ?? SkillCatalog.All[0];
            int level = SkillBook.GetLevel(_save, def.Id);
            bool owned = level > 0;
            int shownLevel = owned ? level : 1;
            Color grade = PanelServices.GradeColor(def.Grade);

            if (_detailIcon != null)
            {
                _detailIcon.sprite = _icons != null ? _icons.Get(def.IconId) : null;
                _detailIcon.color = owned ? Color.white : new Color(0.55f, 0.5f, 0.5f, 0.75f);
            }

            SetFrame(_detailFrame, def, true);
            if (_detailGradeChip != null) _detailGradeChip.color = grade;
            if (_detailGrade != null) _detailGrade.text = PanelServices.GradeName(def.Grade);
            if (_detailName != null)
            {
                _detailName.text = Strings.Get(def.NameKey);
                _detailName.color = UiPalette.GradeInk(def.Grade);
            }

            if (_detailInfo != null)
            {
                _detailInfo.text = owned
                    ? Strings.Format("skill.info", level, _balance.SKILL_MAX_LEVEL, Num(def.Cooldown))
                    : Strings.Format("skill.info_unowned", Num(def.Cooldown));
                if (def.Line != JobLine.None)
                {
                    string line = Strings.Format("skill.line_only", Strings.Get(LineKeys[(int)def.Line]));
                    _detailInfo.text += JobService.CanUse(_save, def) ? "  " + line : "  <color=#D8423E>" + line + "</color>";
                }
            }

            if (_detailDesc != null) _detailDesc.text = Strings.Format(def.DescKey, DescArgs(def, shownLevel));

            bool max = level >= _balance.SKILL_MAX_LEVEL;
            double cost = SkillService.UpgradeCost(_balance, def, shownLevel);
            if (_levelCost != null) _levelCost.text = !owned ? Strings.Get("skill.not_owned") : max ? Strings.Get("char.max") : BigNumberFormat.Format(cost);
            if (_levelButton != null) _levelButton.SetAvailable(owned && !max && _save.gold >= cost);

            int slot = SkillBook.SlotOf(_save, def.Id);
            bool equipped = slot >= 0 && SkillService.IsSlotUnlocked(_balance, slot, _save.heroLevel);
            if (_equipLabel != null) _equipLabel.text = Strings.Get(equipped ? "skill.unequip" : "skill.equip");
            if (_equipButton != null) _equipButton.SetAvailable(owned);
        }

        /// <summary>Grade frame sprite for a skill (empty frame when none); <paramref name="bright"/> false dims it.</summary>
        private void SetFrame(Image frame, SkillDef def, bool bright)
        {
            if (frame == null) return;
            if (_frames != null)
            {
                frame.sprite = def != null ? _frames.Get(def.Grade) : _frames.empty;
                frame.color = bright ? Color.white : UnownedFrame;
                return;
            }

            frame.color = def != null ? PanelServices.GradeColor(def.Grade) : UnownedFrame;
        }

        private static void Set(GameObject[] items, int index, bool active)
        {
            if (index < items.Length && items[index] != null && items[index].activeSelf != active) items[index].SetActive(active);
        }

        /// <summary>Description arguments at <paramref name="level"/>; see the arg list in strings_ko.txt.</summary>
        private object[] DescArgs(SkillDef def, int level)
        {
            double scale = Formulas.SkillLevelScale(_balance, level);
            _descArgs[0] = Num(def.DamageMult * 100d * scale);
            _descArgs[1] = def.Waves;
            _descArgs[2] = Num(def.DotPercent * scale);
            _descArgs[3] = Num(def.DotSeconds);
            _descArgs[4] = Num(def.StunSeconds);
            _descArgs[5] = Num(def.BuffAmount * scale);
            _descArgs[6] = Num(def.BuffSeconds);
            _descArgs[7] = Num(def.HealPercent * scale);
            _descArgs[8] = Num(def.ShieldPercent * scale);
            _descArgs[9] = Num(def.ShieldSeconds);
            _descArgs[10] = Num(def.HpThreshold);
            _descArgs[11] = Num(def.Cooldown);
            _descArgs[12] = Num(def.MarkPercent * scale);
            _descArgs[13] = Num(def.MarkSeconds);
            return _descArgs;
        }

        private static string Num(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        private void PulseSlots()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
            Color pulse = Color.Lerp(Color.white, PickPulse, k);
            for (int i = 0; i < _slotFrames.Length; i++)
            {
                if (_slotFrames[i] == null || !SkillService.IsSlotUnlocked(_balance, i, _save.heroLevel)) continue;
                _slotFrames[i].color = pulse;
            }
        }

        private void Play(SfxId id)
        {
            if (_audio != null) _audio.Play(id);
        }
    }
}
