using System.Globalization;
using System.Text;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-104 job advancement popup (character panel): the current job on top, then one card per job the next
    /// advancement can pick (portrait, name, main attack, mastery, description). Advancing is permanent, so the
    /// pick button asks once more ("confirm") before it calls <see cref="JobService.TryAdvance"/>.
    /// </summary>
    public sealed class JobPresenter : MonoBehaviour
    {
        private const int Cards = 3;

        /// <summary>Mastery lines by TalentStat value; "" for stats no job uses.</summary>
        private static readonly string[] MasteryKeys =
        {
            "job.stat.atk", "job.stat.crit", "job.stat.crit_damage", "job.stat.atkspd", "", "",
            "job.stat.hp", "job.stat.def", "", "", "", "job.stat.skill_damage", "job.stat.cooldown", "", "job.stat.dot", "",
        };

        [SerializeField] private GameObject _popup;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [Tooltip("Job looks, index-aligned with JobCatalog.All.")]
        [SerializeField] private CharacterArt[] _arts = new CharacterArt[0];
        [SerializeField] private Text _current;
        [SerializeField] private GameObject[] _cards = new GameObject[Cards];
        [SerializeField] private Image[] _portraits = new Image[Cards];
        [SerializeField] private Text[] _names = new Text[Cards];
        [SerializeField] private Text[] _mains = new Text[Cards];
        [SerializeField] private Text[] _descs = new Text[Cards];
        [SerializeField] private TapGuardButton[] _picks = new TapGuardButton[Cards];
        [SerializeField] private Text[] _pickLabels = new Text[Cards];

        private JobService _jobs;
        private SaveDataV2 _save;
        private AudioService _audio;
        private JobDef[] _choices = new JobDef[0];
        private int _pending = -1;
        private int _shownLevel = -1;
        private readonly StringBuilder _text = new StringBuilder(128);

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _jobs = PanelServices.TryGet<JobService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
        }

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            _pending = -1;
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void Pick(int index)
        {
            if (_jobs == null || index < 0 || index >= _choices.Length) return;
            if (_pending != index)
            {
                _pending = index;
                _audio?.Play(SfxId.Tap);
                Refresh();
                return;
            }

            JobDef job = _choices[index];
            Result result = _jobs.TryAdvance(job.Id);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            // The new job may make equipped skills unusable (or open its line): refill the slots with the best usable ones.
            PanelServices.TryGet<SkillService>()?.AutoEquip();
            _session?.RefreshLoadout();
            _audio?.Play(SfxId.LevelUp);
            _toast?.Show(Strings.Format("job.advanced", Strings.Get(job.NameKey)));
            Close();
        }

        private void LateUpdate()
        {
            if (IsOpen && _save != null && _save.heroLevel != _shownLevel) Refresh();
        }

        private void Refresh()
        {
            if (_jobs == null || _save == null) return;
            _shownLevel = _save.heroLevel;
            JobDef current = _jobs.Current;
            bool max = _jobs.IsMax;
            if (_current != null)
            {
                _current.text = max
                    ? Strings.Format("job.current_max", Strings.Get(current.NameKey))
                    : Strings.Format("job.current", Strings.Get(current.NameKey), _jobs.NextLevel, _jobs.Tier + 1);
            }

            // At the last job the popup shows that job alone, without a pick button.
            _choices = max ? new[] { current } : _jobs.Choices();
            bool levelOk = _jobs.CanAdvance;
            for (int i = 0; i < Cards; i++)
            {
                bool shown = i < _choices.Length;
                if (i < _cards.Length && _cards[i] != null && _cards[i].activeSelf != shown) _cards[i].SetActive(shown);
                if (!shown) continue;
                JobDef job = _choices[i];
                CharacterArt art = Art(job);
                if (i < _portraits.Length && _portraits[i] != null)
                    _portraits[i].sprite = art != null && art.idle.Length > 0 ? art.idle[0] : null;
                if (i < _names.Length && _names[i] != null) _names[i].text = Strings.Get(job.NameKey);
                if (i < _mains.Length && _mains[i] != null) _mains[i].text = MainLine(job);
                if (i < _descs.Length && _descs[i] != null) _descs[i].text = Strings.Get(job.DescKey);
                if (i < _picks.Length && _picks[i] != null)
                {
                    _picks[i].gameObject.SetActive(!max);
                    _picks[i].SetAvailable(levelOk);
                }

                if (i < _pickLabels.Length && _pickLabels[i] != null)
                {
                    _pickLabels[i].text = !levelOk
                        ? Strings.Format("job.need_lv", _jobs.NextLevel)
                        : Strings.Get(_pending == i ? "job.confirm" : "job.pick");
                }
            }
        }

        private CharacterArt Art(JobDef job)
        {
            int index = JobCatalog.IndexOf(job.Id);
            if (index < _arts.Length && _arts[index] != null) return _arts[index];
            return _arts.Length > 0 ? _arts[0] : null;
        }

        /// <summary>"Main: name (targets x hits, ATK %)" plus the mastery lines.</summary>
        private string MainLine(JobDef job)
        {
            _text.Clear();
            _text.Append(Strings.Get(job.MainKey));
            if (job.Main != null)
            {
                _text.Append("  ");
                _text.Append(Strings.Format(job.Main.Hits > 1 ? "job.main_hits" : "job.main_info", job.Main.Targets,
                    (job.Main.Mult * 100d).ToString("0", CultureInfo.InvariantCulture), job.Main.Hits));
            }

            for (int m = 0; m < job.Mastery.Length; m++)
            {
                Mastery mastery = job.Mastery[m];
                int stat = (int)mastery.Stat;
                if (stat >= MasteryKeys.Length || MasteryKeys[stat] == "") continue;
                double value = mastery.Stat == TalentStat.CritPoints ? mastery.Amount : mastery.Amount * 100d;
                _text.Append(m == 0 ? "\n" : "  ");
                _text.Append(Strings.Format(MasteryKeys[stat], value.ToString("0.#", CultureInfo.InvariantCulture)));
            }

            return _text.ToString();
        }
    }
}
