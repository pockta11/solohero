using System;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.UI.Panels;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI
{
    public sealed class BattleHud : MonoBehaviour
    {
        [SerializeField] private CombatSession _session;
        [SerializeField] private Text _goldText;

        /// <summary>Where kill coins fly to (D-096).</summary>
        public RectTransform GoldAnchor => _goldText != null ? _goldText.rectTransform : null;
        [SerializeField] private Text _stageText;
        [SerializeField] private Text _killsText;
        [SerializeField] private Text _bossTimerText;
        [SerializeField] private GameObject _failPanel;
        [SerializeField] private GameObject _retreatButton;
        [SerializeField] private GameObject _challengeButton;
        [SerializeField] private Text _retreatPrompt;
        [SerializeField] private UiPunch _goldPunch;
        [SerializeField] private Text _gemText;

        [Header("Hero status in the top bar: name + level, HP and EXP gauges")]
        [SerializeField] private Text _heroNameText;
        [SerializeField] private RectTransform _heroHpFill;
        [SerializeField] private Text _heroHpText;
        [SerializeField] private RectTransform _heroExpFill;
        [SerializeField] private Text _heroExpText;

        [Header("Skill bar (D-078): one entry per slot")]
        [SerializeField] private SkillIconSet _skillIconSet;
        [SerializeField] private GradeFrameSet _gradeFrames;
        [SerializeField] private Image[] _skillIcons = new Image[0];
        [SerializeField] private Image[] _skillFrames = new Image[0];
        [SerializeField] private Image[] _skillCooldowns = new Image[0];
        [SerializeField] private Text[] _skillTimes = new Text[0];
        [SerializeField] private Text[] _skillLocks = new Text[0];
        [SerializeField] private UiPunch[] _skillPunches = new UiPunch[0];
        [Header("Skill auto / manual toggle (D-085)")]
        [SerializeField] private Image _autoImage;
        [SerializeField] private Text _autoLabel;
        [SerializeField] private Sprite _autoOnSprite;
        [SerializeField] private Sprite _autoOffSprite;
        [SerializeField] private GameObject[] _skillReadyMarks = new GameObject[0];
        [Tooltip("D-093 basic skill: cooldown sweep over its icon (the attack-speed timer).")]
        [SerializeField] private Image _basicCooldown;
        [Tooltip("D-104: the basic skill's name, which is the job's main attack.")]
        [SerializeField] private Text _basicTag;
        [Tooltip("D-104: the basic skill's icon; jobs show their main attack icon, the beginner keeps the flash slash.")]
        [SerializeField] private Image _basicIcon;
        [SerializeField] private GameObject _bossBar;
        [SerializeField] private RectTransform _bossFill;
        [SerializeField] private Text _bossName;

        private const string FarmingKey = "hud.farming";
        private const string AutoRetreatKey = "hud.auto_retreat";
        private const float GoldCountSeconds = 0.4f;

        private BalanceValues _balance;
        private SaveDataV2 _save;
        private bool _goldShown;
        private double _shownGold;
        private static readonly string[] BossNameKeys = { "boss.name.1", "boss.name.2", "boss.name.3", "boss.name.4", "boss.name.5" };
        private double _shownGem = -1d;
        private bool _bossBarVisible;
        private int _bossBarStage = -1;
        private float _bossRatio = -1f;
        private double _goldFrom;
        private double _goldTo;
        private float _goldT = 1f;
        private int _shownGlobalStage = -1;
        private int _shownKills = -1;
        private int _shownKillTarget = -1;
        private int _shownBossSeconds = -1;
        private bool _bossTimerVisible;
        private bool _failPanelVisible;
        private bool _retreatVisible;
        private bool _challengeVisible;
        private bool _retreatPromptVisible;
        private SkillDef[] _shownDefs = new SkillDef[0];
        private int[] _shownSeconds = new int[0];
        private bool[] _shownLocked = new bool[0];
        private int _shownSkillLevel = -1;
        private StageRunner _skillRunner;
        private SettingsService _settings;
        private int _shownAuto = -1;
        private string _shownJob;
        private bool[] _shownReady = new bool[0];

        private void Awake()
        {
            if (_session == null)
                _session = GetComponent<CombatSession>();
            if (_session == null)
                _session = FindObjectOfType<CombatSession>();

            try
            {
                _balance = Services.Get<BalanceValues>();
            }
            catch (Exception)
            {
                _balance = new BalanceValues();
            }

            try
            {
                _save = Services.Get<SaveDataV2>();
            }
            catch (Exception)
            {
                _save = null;
            }

            try
            {
                _settings = Services.Get<SettingsService>();
            }
            catch (Exception)
            {
                _settings = null;
            }
        }

        private void LateUpdate()
        {
            RefreshGold();
            RefreshGem();

            StageRunner runner = CurrentRunner();
            if (runner == null)
            {
                HideChoices();
                return;
            }

            RefreshStage(runner);
            RefreshHero(runner);
            RefreshKills(runner);
            RefreshBossTimer(runner);
            RefreshBossBar(runner);
            RefreshChoices(runner);
            RefreshSkills(runner);
            RefreshRetreatPrompt(runner);
        }

        private int _shownHeroLevel = -1;
        private string _shownHeroJob;
        private int _shownHpPermille = -1;
        private double _shownHp = -1d;
        private int _shownExpPermille = -1;

        /// <summary>Top bar: "Lv 30 Pyromancer", HP gauge with the current HP, EXP gauge with its percentage.</summary>
        private void RefreshHero(StageRunner runner)
        {
            if (_save == null || _balance == null) return;
            if (_heroNameText != null && (_save.heroLevel != _shownHeroLevel || _save.jobId != _shownHeroJob))
            {
                _shownHeroLevel = _save.heroLevel;
                _shownHeroJob = _save.jobId;
                _heroNameText.text = Strings.Format("hud.hero_name", _save.heroLevel,
                    Strings.Get(SoloHero.Core.Jobs.JobCatalog.Find(_save.jobId).NameKey));
            }

            HeroBrain hero = runner.Hero;
            double maxHp = hero.MaxHp;
            int hp = maxHp > 0d ? Mathf.Clamp(Mathf.RoundToInt((float)(hero.Hp / maxHp * 1000d)), 0, 1000) : 0;
            if (hp != _shownHpPermille && _heroHpFill != null)
            {
                _shownHpPermille = hp;
                _heroHpFill.anchorMax = new Vector2(hp / 1000f, 1f);
            }

            if (_heroHpText != null && Math.Abs(hero.Hp - _shownHp) >= 1d)
            {
                _shownHp = hero.Hp;
                _heroHpText.text = BigNumberFormat.Format(Math.Max(0d, Math.Ceiling(hero.Hp)));
            }

            double need = HeroLevelService.ExpRequired(_balance, _save.heroLevel);
            int exp = need > 0d ? Mathf.Clamp(Mathf.FloorToInt((float)(_save.heroExp / need * 1000d)), 0, 1000) : 0;
            if (exp == _shownExpPermille) return;
            _shownExpPermille = exp;
            if (_heroExpFill != null) _heroExpFill.anchorMax = new Vector2(exp / 1000f, 1f);
            if (_heroExpText != null)
                _heroExpText.text = Strings.Format("char.percent", (exp / 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        public void ChooseRetry()
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            runner.ChooseRetry();
        }

        public void ChooseRetreat()
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            runner.ChooseRetreat();
        }

        public void ChooseStepDown()
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            runner.StepDown();
        }

        public void ChallengeBoss()
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            runner.ChallengeBoss();
        }

        /// <summary>D-085: AUTO button. Manual mode leaves casting to the skill bar taps.</summary>
        public void ToggleSkillAuto()
        {
            if (_settings == null) return;
            _settings.SetSkillManual(!_settings.SkillManual);
        }

        /// <summary>Skill bar tap: casts that slot now if it is ready (auto-cast keeps running either way).</summary>
        public void CastSlot(int slot)
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            if (runner.Skills.TryCast(slot, runner.Hero, runner.World).Ok && slot < _skillPunches.Length && _skillPunches[slot] != null)
                _skillPunches[slot].Play();
        }

        private StageRunner CurrentRunner()
        {
            return _session != null ? _session.Runner : null;
        }

        /// <summary>E8-09: gold counts toward its new value over 0.4 s; gains also punch the label.</summary>
        private void RefreshGold()
        {
            if (_goldText == null) return;

            if (_save == null)
            {
                if (_goldShown) return;
                _goldText.text = "0";
                _goldShown = true;
                return;
            }

            if (!_goldShown)
            {
                _goldShown = true;
                _goldFrom = _goldTo = _shownGold = _save.gold;
                _goldT = 1f;
                _goldText.text = BigNumberFormat.Format(_shownGold);
                return;
            }

            if (_save.gold != _goldTo)
            {
                if (_save.gold > _goldTo && _goldPunch != null) _goldPunch.Play();
                _goldFrom = _shownGold;
                _goldTo = _save.gold;
                _goldT = 0f;
            }

            if (_goldT >= 1f) return;
            _goldT = Mathf.Min(1f, _goldT + Time.unscaledDeltaTime / GoldCountSeconds);
            float eased = 1f - (1f - _goldT) * (1f - _goldT);
            double value = _goldT >= 1f ? _goldTo : _goldFrom + (_goldTo - _goldFrom) * eased;
            if (value == _shownGold) return;
            _shownGold = value;
            _goldText.text = BigNumberFormat.Format(_shownGold);
        }

        /// <summary>Boss fight: name and a big HP bar under the ad row (GDD boss rule 12).</summary>
        private void RefreshBossBar(StageRunner runner)
        {
            bool show = runner.IsBoss && (runner.State == StageState.BossIntro || runner.State == StageState.BossTimer);
            SetShown(_bossBar, show, ref _bossBarVisible);
            if (!show) return;

            if (_bossBarStage != runner.GlobalStage && _bossName != null && _balance != null)
            {
                _bossBarStage = runner.GlobalStage;
                StageIndex.FromGlobal(runner.GlobalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
                _bossName.text = Strings.Get(BossNameKeys[(chapter - 1) % BossNameKeys.Length]);
            }

            float ratio = 1f;
            for (int i = 0; i < runner.World.SlotCount; i++)
            {
                Core.Combat.EnemyBrain e = runner.World.GetSlot(i);
                if (e == null || !e.IsActive || !e.IsBoss) continue;
                ratio = e.MaxHp > 0d ? Mathf.Clamp01((float)(e.Hp / e.MaxHp)) : 0f;
                break;
            }

            if (_bossFill == null || Mathf.Abs(ratio - _bossRatio) < 0.002f) return;
            _bossRatio = ratio;
            _bossFill.anchorMax = new Vector2(ratio, 1f);
        }

        private void RefreshGem()
        {
            if (_gemText == null || _save == null || _save.gem == _shownGem) return;
            _shownGem = _save.gem;
            _gemText.text = BigNumberFormat.Format(_shownGem);
        }

        private void RefreshStage(StageRunner runner)
        {
            if (_stageText == null || _balance == null) return;
            if (_shownGlobalStage == runner.GlobalStage) return;

            int perChapter = _balance.STAGES_PER_CHAPTER;
            if (perChapter < 1) perChapter = 1;
            StageIndex.FromGlobal(runner.GlobalStage, perChapter, out int chapter, out int stageNumber);
            _shownGlobalStage = runner.GlobalStage;
            _stageText.text = chapter.ToString() + "-" + stageNumber.ToString();
        }

        private void RefreshKills(StageRunner runner)
        {
            if (_killsText == null) return;
            if (_shownKills == runner.Kills && _shownKillTarget == runner.KillTarget) return;

            _shownKills = runner.Kills;
            _shownKillTarget = runner.KillTarget;
            // D-100: a dungeon has no kill target, only a running count.
            _killsText.text = runner.InDungeon ? _shownKills.ToString() : _shownKills.ToString() + " / " + _shownKillTarget.ToString();
        }

        private void RefreshBossTimer(StageRunner runner)
        {
            bool show = runner.State == StageState.BossTimer;
            GameObject timerObject = _bossTimerText != null ? _bossTimerText.gameObject : null;
            SetShown(timerObject, show, ref _bossTimerVisible);
            if (!show || _bossTimerText == null) return;

            int seconds = Mathf.CeilToInt(runner.BossTimerRemaining);
            if (seconds < 0) seconds = 0;
            if (_shownBossSeconds == seconds) return;
            _shownBossSeconds = seconds;
            _bossTimerText.text = seconds.ToString();
        }

        private void RefreshChoices(StageRunner runner)
        {
            bool failed = runner.State == StageState.Failed;
            SetShown(_failPanel, failed, ref _failPanelVisible);
            SetShown(_retreatButton, failed && runner.IsBoss, ref _retreatVisible);
            SetShown(_challengeButton, runner.RetreatMode && !runner.InDungeon, ref _challengeVisible);
        }

        private void HideChoices()
        {
            GameObject timerObject = _bossTimerText != null ? _bossTimerText.gameObject : null;
            SetShown(timerObject, false, ref _bossTimerVisible);
            SetShown(_failPanel, false, ref _failPanelVisible);
            SetShown(_retreatButton, false, ref _retreatVisible);
            SetShown(_challengeButton, false, ref _challengeVisible);
            SetShown(_retreatPrompt != null ? _retreatPrompt.gameObject : null, false, ref _retreatPromptVisible);
        }

        /// <summary>
        /// D-078 skill bar: each slot shows its skill icon in a grade-coloured frame, a radial cooldown with seconds,
        /// or a lock with the hero level that opens it. Texts and sprites change only when their value changes.
        /// </summary>
        private void RefreshBasic(StageRunner runner)
        {
            if (_basicTag != null && _save != null && _save.jobId != _shownJob)
            {
                _shownJob = _save.jobId;
                SoloHero.Core.Jobs.JobDef job = SoloHero.Core.Jobs.JobCatalog.Find(_shownJob);
                _basicTag.text = Strings.Get(job.MainKey);
                ShowBasicIcon(job);
            }

            if (_basicCooldown == null) return;
            float interval = runner.Hero.SwingInterval;
            float fill = interval > 0f ? Mathf.Clamp01(runner.Hero.SwingCooldown / interval) : 0f;
            if (Mathf.Abs(_basicCooldown.fillAmount - fill) > 0.01f) _basicCooldown.fillAmount = fill;
        }

        private Sprite _basicDefault;
        private Vector2 _basicDefaultSize;

        private void ShowBasicIcon(SoloHero.Core.Jobs.JobDef job)
        {
            if (_basicIcon == null) return;
            if (_basicDefault == null)
            {
                _basicDefault = _basicIcon.sprite;
                _basicDefaultSize = _basicIcon.rectTransform.sizeDelta;
            }

            Sprite icon = job.Main != null && _skillIconSet != null ? _skillIconSet.Get(SoloHero.Core.Jobs.JobCatalog.MainIconId(job)) : null;
            _basicIcon.sprite = icon != null ? icon : _basicDefault;
            // Skill icons are 24 px tiles at 3 canvas units per pixel; the flash slash is a 16 px UI icon at 4.
            _basicIcon.rectTransform.sizeDelta = icon != null ? new Vector2(72f, 72f) : _basicDefaultSize;
        }

        private void RefreshAuto(StageRunner runner)
        {
            bool auto = _settings == null || !_settings.SkillManual;
            runner.Skills.AutoEnabled = auto;
            int state = auto ? 1 : 0;
            if (state == _shownAuto) return;
            _shownAuto = state;
            if (_autoImage != null) _autoImage.sprite = auto ? _autoOnSprite : _autoOffSprite;
            if (_autoLabel != null) _autoLabel.color = auto ? Color.white : new Color(0.75f, 0.75f, 0.8f, 1f);
        }

        /// <summary>Manual mode: a pulsing mark on every slot that can be tapped now.</summary>
        private void RefreshReadyMarks(StageRunner runner)
        {
            int count = _skillReadyMarks.Length;
            if (_shownReady.Length != count) _shownReady = new bool[count];
            bool manual = !runner.Skills.AutoEnabled;
            for (int i = 0; i < count; i++)
            {
                bool locked = i < _shownLocked.Length && _shownLocked[i];
                bool ready = manual && !locked && i != runner.Skills.UltimateSlot && runner.Skills.IsReady(i);
                if (ready == _shownReady[i] || _skillReadyMarks[i] == null) continue;
                _shownReady[i] = ready;
                _skillReadyMarks[i].SetActive(ready);
            }
        }

        private void RefreshSkills(StageRunner runner)
        {
            RefreshAuto(runner);
            RefreshBasic(runner);
            int count = _skillIcons.Length;
            if (_shownDefs.Length != count || _skillRunner != runner)
            {
                _skillRunner = runner;
                _shownDefs = new SkillDef[count];
                _shownSeconds = new int[count];
                _shownLocked = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    _shownSeconds[i] = -1;
                    _shownLocked[i] = true;
                }

                _shownSkillLevel = -1;
                for (int i = 0; i < count; i++) DrawSkillSlot(i, null, true);
            }

            bool levelChanged = _save != null && _save.heroLevel != _shownSkillLevel;
            if (levelChanged) _shownSkillLevel = _save.heroLevel;
            for (int i = 0; i < count; i++)
            {
                SkillDef def = runner.Skills.DefAt(i);
                // D-104: the ultimate slot is never level-locked; it shows only while the job has an ultimate.
                bool locked = i != runner.Skills.UltimateSlot && _save != null && _balance != null && !SkillService.IsSlotUnlocked(_balance, i, _save.heroLevel);
                if (def != _shownDefs[i] || locked != _shownLocked[i] || levelChanged)
                {
                    _shownDefs[i] = def;
                    _shownLocked[i] = locked;
                    DrawSkillSlot(i, def, locked);
                    _shownSeconds[i] = -1;
                }

                float remaining = def != null ? runner.Skills.CooldownRemaining(i) : 0f;
                float total = def != null ? runner.Skills.CooldownTotal(i) : 0f;
                Image overlay = i < _skillCooldowns.Length ? _skillCooldowns[i] : null;
                if (overlay != null)
                {
                    float fill = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
                    if (Mathf.Abs(overlay.fillAmount - fill) > 0.005f || (fill == 0f && overlay.fillAmount != 0f)) overlay.fillAmount = fill;
                }

                int seconds = remaining > 0f ? Mathf.CeilToInt(remaining) : 0;
                if (seconds == _shownSeconds[i]) continue;
                _shownSeconds[i] = seconds;
                Text time = i < _skillTimes.Length ? _skillTimes[i] : null;
                if (time != null) time.text = seconds > 0 ? seconds.ToString() : "";
            }

            RefreshReadyMarks(runner);
        }

        private void DrawSkillSlot(int i, SkillDef def, bool locked)
        {
            Image icon = i < _skillIcons.Length ? _skillIcons[i] : null;
            Image frame = i < _skillFrames.Length ? _skillFrames[i] : null;
            Text lockText = i < _skillLocks.Length ? _skillLocks[i] : null;
            Sprite sprite = def != null && _skillIconSet != null ? _skillIconSet.Get(def.IconId) : null;
            if (_balance != null && i >= SkillService.SlotCount(_balance) && frame != null && frame.gameObject.activeSelf != (def != null))
                frame.gameObject.SetActive(def != null);
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null && !locked;
            }

            if (frame != null && _gradeFrames != null)
            {
                frame.sprite = def != null && !locked ? _gradeFrames.Get(def.Grade) : _gradeFrames.empty;
                frame.color = Color.white;
            }
            else if (frame != null)
            {
                frame.color = def != null && !locked ? PanelServices.GradeColor(def.Grade) : new Color(0.35f, 0.35f, 0.4f, 1f);
            }
            if (lockText == null) return;
            lockText.gameObject.SetActive(locked);
            if (locked && _balance != null && i < SkillService.SlotCount(_balance))
                lockText.text = Strings.Format("skill.slot_locked", SkillService.UnlockHeroLevel(_balance, i));
        }

        private void RefreshRetreatPrompt(StageRunner runner)
        {
            // D-058: a normal-stage death drops to farming by itself after the retry delay; say so while waiting.
            // D-077: a boss fail offers retry / retreat and retreats by itself after a countdown.
            bool bossFail = runner.IsBoss && runner.State == StageState.Failed;
            bool show = bossFail || (runner.PromptRetreat && !runner.IsBoss && runner.State == StageState.Failed && runner.GlobalStage > 1);
            GameObject promptObject = _retreatPrompt != null ? _retreatPrompt.gameObject : null;
            SetShown(promptObject, show, ref _retreatPromptVisible);
            if (!show || _retreatPrompt == null) return;
            string text = bossFail
                ? Strings.Format(AutoRetreatKey, Mathf.CeilToInt(runner.BossAutoRetreatRemaining))
                : Strings.Get(FarmingKey);
            if (_retreatPrompt.text == text) return;
            _retreatPrompt.text = text;
        }

        private static void SetShown(GameObject target, bool show, ref bool shown)
        {
            if (target == null || shown == show) return;
            shown = show;
            target.SetActive(show);
        }
    }
}
