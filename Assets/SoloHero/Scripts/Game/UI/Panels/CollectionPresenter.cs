using System.Globalization;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-164 (L-18) collection codex popup (right menu): the total bonus of the complete sets on top, then gear / pet /
    /// skill tabs, each a list of grade sets with how many are owned and the stat the set adds (complete sets glow).
    /// </summary>
    public sealed class CollectionPresenter : MonoBehaviour
    {
        private const int Rows = 7;
        private static readonly Color TabIdle = new Color(0.72f, 0.72f, 0.76f, 1f);
        private static readonly Color Done = new Color(1f, 0.93f, 0.62f, 1f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private Text _summary;
        [SerializeField] private Image[] _tabs = new Image[3];
        [SerializeField] private GameObject[] _rows = new GameObject[Rows];
        [SerializeField] private Image[] _rowCards = new Image[Rows];
        [SerializeField] private Text[] _names = new Text[Rows];
        [SerializeField] private Text[] _counts = new Text[Rows];
        [SerializeField] private RectTransform[] _fills = new RectTransform[Rows];
        [SerializeField] private Text[] _bonuses = new Text[Rows];
        [SerializeField] private GameObject[] _marks = new GameObject[Rows];

        private SaveDataV2 _save;
        private CollectionKind _kind = CollectionKind.Gear;
        private int _shownOwned = -1;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable() => _save = PanelServices.TryGet<SaveDataV2>();

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void ShowTab(int tab)
        {
            if (tab < 0 || tab > (int)CollectionKind.Skill) return;
            _kind = (CollectionKind)tab;
            PanelServices.TryGet<AudioService>()?.Play(SfxId.Tap);
            Refresh();
        }

        private void LateUpdate()
        {
            if (!IsOpen || _save == null) return;
            // Anything newly owned may complete a set.
            int owned = _save.ownedEquipment.Count + _save.ownedSkills.Count + _save.petOwned.Count + _save.jobId.Length;
            if (owned != _shownOwned) Refresh();
        }

        private void Refresh()
        {
            if (_save == null) return;
            _shownOwned = _save.ownedEquipment.Count + _save.ownedSkills.Count + _save.petOwned.Count + _save.jobId.Length;
            if (_summary != null)
            {
                _summary.text = Strings.Format("collection.summary", CollectionCatalog.CompleteCount(_save), CollectionCatalog.Count,
                    Percent(CollectionCatalog.Total(_save, TalentStat.AtkPct)), Percent(CollectionCatalog.Total(_save, TalentStat.HpPct)),
                    Percent(CollectionCatalog.Total(_save, TalentStat.SkillDamagePct)));
            }

            for (int t = 0; t < _tabs.Length; t++)
                if (_tabs[t] != null) _tabs[t].color = t == (int)_kind ? Color.white : TabIdle;

            int row = 0;
            foreach (CollectionSet set in CollectionCatalog.All)
            {
                if (set.Kind != _kind || row >= Rows) continue;
                DrawRow(row++, set);
            }

            for (; row < Rows; row++)
                if (row < _rows.Length && _rows[row] != null && _rows[row].activeSelf) _rows[row].SetActive(false);
        }

        private void DrawRow(int row, CollectionSet set)
        {
            if (row < _rows.Length && _rows[row] != null && !_rows[row].activeSelf) _rows[row].SetActive(true);
            CollectionCatalog.Progress(_save, set, out int owned, out int total);
            bool complete = total > 0 && owned >= total;
            GearGrade ladder = set.Kind == CollectionKind.Skill ? ((Grade)set.Grade).ToGearGrade() : (GearGrade)set.Grade;
            if (row < _names.Length && _names[row] != null)
            {
                _names[row].text = Strings.Format(KindKey(set.Kind), PanelServices.GradeName(ladder), total);
                _names[row].color = UiPalette.GradeInk(ladder);
            }

            if (row < _counts.Length && _counts[row] != null)
                _counts[row].text = total > 0 ? Strings.Format("collection.progress", owned, total) : Strings.Get("collection.skill_locked");
            if (row < _fills.Length && _fills[row] != null)
                _fills[row].anchorMax = new Vector2(total > 0 ? Mathf.Clamp01((float)owned / total) : 0f, 1f);
            if (row < _bonuses.Length && _bonuses[row] != null) _bonuses[row].text = Strings.Format(BonusKey(set.Stat), Percent(set.Amount));
            if (row < _rowCards.Length && _rowCards[row] != null) _rowCards[row].color = complete ? Done : Color.white;
            if (row < _marks.Length && _marks[row] != null && _marks[row].activeSelf != complete) _marks[row].SetActive(complete);
        }

        private static string KindKey(CollectionKind kind) =>
            kind == CollectionKind.Gear ? "collection.set_gear" : kind == CollectionKind.Pet ? "collection.set_pet" : "collection.set_skill";

        private static string BonusKey(TalentStat stat) =>
            stat == TalentStat.HpPct ? "collection.bonus_hp" : stat == TalentStat.SkillDamagePct ? "collection.bonus_skill" : "collection.bonus_atk";

        private static string Percent(double fraction) => (fraction * 100d).ToString("0.#", CultureInfo.InvariantCulture);
    }
}
