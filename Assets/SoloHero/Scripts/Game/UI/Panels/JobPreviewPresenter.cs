using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-179 job line preview, opened from a job card before advancing: the branch this pick leads to (the first job on
    /// top, its two second jobs, their third jobs) with portraits, tier chips and ultimate icons, then the line's
    /// skill book (grade rows, the third-job skills badged) and a detail band for whatever is tapped - a job's main
    /// attack, mastery and ultimate, or a skill's description at level 1.
    /// </summary>
    public sealed class JobPreviewPresenter : MonoBehaviour
    {
        public const int JobSlots = 5;
        public const int Columns = 5;
        public const int Rows = 4;

        /// <summary>Mastery lines by TalentStat value (same table as the job popup); "" for stats no job uses.</summary>
        private static readonly string[] MasteryKeys =
        {
            "job.stat.atk", "job.stat.crit", "job.stat.crit_damage", "job.stat.atkspd", "job.stat.boss", "",
            "job.stat.hp", "job.stat.def", "job.stat.damage_taken", "", "", "job.stat.skill_damage", "job.stat.cooldown", "",
            "job.stat.dot", "",
        };

        private static readonly string[] LineKeys = { "", "job.line.warrior", "job.line.mage", "job.line.archer" };

        private static readonly Vector2[] Tree5 =
        {
            new Vector2(0f, 0f), new Vector2(-240f, -205f), new Vector2(240f, -205f), new Vector2(-240f, -410f), new Vector2(240f, -410f),
        };

        private static readonly Vector2[] Tree2 = { new Vector2(0f, 0f), new Vector2(0f, -205f) };
        private static readonly Vector2[] Tree1 = { new Vector2(0f, 0f) };
        private static readonly Color Selected = new Color(1f, 0.82f, 0.4f, 1f);
        private static readonly Color Unselected = Color.white;
        private static readonly Color LockedIcon = new Color(0.62f, 0.58f, 0.6f, 0.85f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private Text _title;
        [SerializeField] private Text _hint;
        [Tooltip("Job looks index-aligned with JobCatalog.All.")]
        [SerializeField] private CharacterArt[] _arts = new CharacterArt[0];
        [SerializeField] private SkillIconSet _icons;
        [SerializeField] private GradeFrameSet _frames;

        [Header("Job tree")]
        [SerializeField] private RectTransform[] _jobSlots = new RectTransform[0];
        [SerializeField] private Image[] _jobFrames = new Image[0];
        [SerializeField] private Image[] _jobPortraits = new Image[0];
        [SerializeField] private Text[] _jobNames = new Text[0];
        [SerializeField] private Text[] _jobTiers = new Text[0];
        [SerializeField] private Image[] _jobUltIcons = new Image[0];

        [Header("Skill book")]
        [SerializeField] private Text _skillsHeader;
        [SerializeField] private Image[] _cellFrames = new Image[0];
        [SerializeField] private Image[] _cellIcons = new Image[0];
        [SerializeField] private GameObject[] _cellBadges = new GameObject[0];
        [SerializeField] private Text[] _cellBadgeTexts = new Text[0];

        [Header("Detail")]
        [SerializeField] private Image _detailFrame;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private Image _detailPortrait;
        [SerializeField] private Text _detailName;
        [SerializeField] private Text _detailInfo;
        [SerializeField] private Text _detailDesc;

        private readonly List<JobDef> _jobs = new List<JobDef>(JobSlots);
        private readonly List<SkillDef> _skills = new List<SkillDef>(Columns * Rows);
        private readonly object[] _descArgs = new object[SkillText.ArgCount];
        private BalanceValues _balance;
        private AudioService _audio;
        private int _selectedJob = -1;
        private int _selectedSkill = -1;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _balance = PanelServices.TryGet<BalanceValues>();
            _audio = PanelServices.TryGet<AudioService>();
        }

        /// <summary>Shows the branch <paramref name="candidate"/> leads to and its line's skills.</summary>
        public void Open(JobDef candidate)
        {
            if (candidate == null || candidate.Tier < 1) return;
            _jobs.Clear();
            _jobs.Add(candidate);
            foreach (JobDef second in JobService.ChoicesFrom(candidate))
            {
                _jobs.Add(second);
            }

            // Third jobs under their second job: slots 3 and 4 follow slots 1 and 2.
            int seconds = _jobs.Count - 1;
            for (int s = 0; s < seconds; s++)
            {
                JobDef[] thirds = JobService.ChoicesFrom(_jobs[1 + s]);
                if (thirds.Length > 0) _jobs.Add(thirds[0]);
            }

            _skills.Clear();
            for (int grade = 0; grade < Rows; grade++)
            {
                foreach (SkillDef def in SkillCatalog.OfLine(candidate.Line, (Grade)grade, JobCatalog.MaxTier))
                    _skills.Add(def);
            }

            if (_popup != null) _popup.SetActive(true);
            if (_title != null) _title.text = Strings.Format("job.preview_title", Strings.Get(LineKeys[(int)candidate.Line]));
            if (_hint != null) _hint.text = Strings.Get("job.preview_hint");
            if (_skillsHeader != null) _skillsHeader.text = Strings.Format("job.preview_skills", _skills.Count);
            DrawTree();
            DrawSkills();
            SelectJob(0);
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void SelectJob(int index)
        {
            if (index < 0 || index >= _jobs.Count) return;
            _selectedJob = index;
            _selectedSkill = -1;
            _audio?.Play(SfxId.Tap);
            Highlight();
            DrawJobDetail(_jobs[index]);
        }

        public void SelectSkill(int index)
        {
            if (index < 0 || index >= _skills.Count) return;
            _selectedSkill = index;
            _selectedJob = -1;
            _audio?.Play(SfxId.Tap);
            Highlight();
            DrawSkillDetail(_skills[index]);
        }

        private void DrawTree()
        {
            Vector2[] layout = _jobs.Count >= JobSlots ? Tree5 : _jobs.Count >= 2 ? Tree2 : Tree1;
            for (int i = 0; i < _jobSlots.Length; i++)
            {
                bool shown = i < _jobs.Count;
                if (_jobSlots[i] == null) continue;
                if (_jobSlots[i].gameObject.activeSelf != shown) _jobSlots[i].gameObject.SetActive(shown);
                if (!shown) continue;
                _jobSlots[i].anchoredPosition = i < layout.Length ? layout[i] : Vector2.zero;
                JobDef job = _jobs[i];
                CharacterArt art = Art(job);
                if (i < _jobPortraits.Length && _jobPortraits[i] != null)
                    UiFlipbook.Fit(_jobPortraits[i], art != null && art.idle.Length > 0 ? art.idle[0] : null, 2.2f);
                if (i < _jobNames.Length && _jobNames[i] != null) _jobNames[i].text = Strings.Get(job.NameKey);
                if (i < _jobTiers.Length && _jobTiers[i] != null) _jobTiers[i].text = Strings.Format("job.preview_tier", job.Tier);
                if (i < _jobUltIcons.Length && _jobUltIcons[i] != null)
                {
                    Sprite ult = job.Ultimate != null && _icons != null ? _icons.Get(job.Ultimate.IconId) : null;
                    _jobUltIcons[i].sprite = ult;
                    _jobUltIcons[i].enabled = ult != null;
                }
            }
        }

        private void DrawSkills()
        {
            for (int i = 0; i < _cellFrames.Length; i++)
            {
                bool shown = i < _skills.Count;
                if (_cellFrames[i] != null && _cellFrames[i].gameObject.activeSelf != shown) _cellFrames[i].gameObject.SetActive(shown);
                if (!shown) continue;
                SkillDef def = _skills[i];
                bool third = def.JobTier >= JobCatalog.MaxTier;
                if (_cellFrames[i] != null)
                {
                    _cellFrames[i].sprite = _frames != null ? _frames.Get(def.Grade) : _cellFrames[i].sprite;
                    _cellFrames[i].color = Color.white;
                }

                if (i < _cellIcons.Length && _cellIcons[i] != null)
                {
                    _cellIcons[i].sprite = _icons != null ? _icons.Get(def.IconId) : null;
                    _cellIcons[i].color = third ? LockedIcon : Color.white;
                }

                if (i < _cellBadges.Length && _cellBadges[i] != null && _cellBadges[i].activeSelf != third) _cellBadges[i].SetActive(third);
                if (third && i < _cellBadgeTexts.Length && _cellBadgeTexts[i] != null)
                    _cellBadgeTexts[i].text = Strings.Format("skill.tier_badge", def.JobTier);
            }
        }

        private void Highlight()
        {
            for (int i = 0; i < _jobFrames.Length; i++)
                if (_jobFrames[i] != null) _jobFrames[i].color = i == _selectedJob ? Selected : Unselected;
            for (int i = 0; i < _cellFrames.Length && i < _skills.Count; i++)
                if (_cellFrames[i] != null) _cellFrames[i].color = i == _selectedSkill ? Selected : Unselected;
        }

        private void DrawJobDetail(JobDef job)
        {
            CharacterArt art = Art(job);
            if (_detailPortrait != null)
            {
                Sprite face = art != null && art.idle.Length > 0 ? art.idle[0] : null;
                UiFlipbook.Fit(_detailPortrait, face, 1.6f);
                _detailPortrait.enabled = face != null;
            }

            if (_detailIcon != null) _detailIcon.enabled = false;
            if (_detailFrame != null) _detailFrame.enabled = false;
            if (_detailName != null)
            {
                _detailName.text = Strings.Get(job.NameKey);
                _detailName.color = UiPalette.InkTitle;
            }

            int level = _balance != null ? JobService.NextLevelFor(_balance, job.Tier - 1) : 0;
            if (_detailInfo != null)
            {
                string info = Strings.Format("job.preview_job_info", job.Tier, level);
                if (job.Main != null)
                {
                    info += "  " + Strings.Format("job.preview_main", Strings.Get(job.MainKey)) + " " + Strings.Format(
                        job.Main.Hits > 1 ? "job.main_hits" : "job.main_info", job.Main.Targets, SkillText.Num(job.Main.Mult * 100d), job.Main.Hits);
                }

                for (int m = 0; m < job.Mastery.Length; m++)
                {
                    Mastery mastery = job.Mastery[m];
                    int stat = (int)mastery.Stat;
                    if (stat >= MasteryKeys.Length || MasteryKeys[stat] == "") continue;
                    double value = mastery.Stat == TalentStat.CritPoints ? mastery.Amount : mastery.Amount * 100d;
                    info += "  " + Strings.Format(MasteryKeys[stat], SkillText.Num(value));
                }

                _detailInfo.text = info;
            }

            if (_detailDesc != null)
            {
                string desc = Strings.Get(job.DescKey);
                if (job.Ultimate != null && _balance != null)
                {
                    desc += "\n" + Strings.Format("job.preview_ult", Strings.Get(job.Ultimate.NameKey)) + ": "
                        + Strings.Format(job.Ultimate.DescKey, SkillText.DescArgs(_balance, job.Ultimate, 1, 0, _descArgs));
                }

                _detailDesc.text = desc;
            }
        }

        private void DrawSkillDetail(SkillDef def)
        {
            if (_detailPortrait != null) _detailPortrait.enabled = false;
            if (_detailIcon != null)
            {
                _detailIcon.sprite = _icons != null ? _icons.Get(def.IconId) : null;
                _detailIcon.enabled = _detailIcon.sprite != null;
                _detailIcon.color = Color.white;
            }

            if (_detailFrame != null)
            {
                _detailFrame.sprite = _frames != null ? _frames.Get(def.Grade) : _detailFrame.sprite;
                _detailFrame.enabled = true;
            }

            if (_detailName != null)
            {
                _detailName.text = Strings.Get(def.NameKey);
                _detailName.color = UiPalette.GradeInk(def.Grade);
            }

            if (_detailInfo != null)
            {
                string info = Strings.Format("job.preview_skill_info", PanelServices.GradeName(def.Grade), SkillText.Num(def.Cooldown));
                if (def.JobTier >= JobCatalog.MaxTier && _balance != null)
                    info += "  <color=#D8423E>" + Strings.Format("job.preview_skill_third", JobService.NextLevelFor(_balance, def.JobTier - 1)) + "</color>";
                _detailInfo.text = info;
            }

            if (_detailDesc != null && _balance != null)
                _detailDesc.text = Strings.Format(def.DescKey, SkillText.DescArgs(_balance, def, 1, 0, _descArgs));
        }

        private CharacterArt Art(JobDef job)
        {
            int index = JobCatalog.IndexOf(job.Id);
            if (index < _arts.Length && _arts[index] != null) return _arts[index];
            return _arts.Length > 0 ? _arts[0] : null;
        }
    }
}
