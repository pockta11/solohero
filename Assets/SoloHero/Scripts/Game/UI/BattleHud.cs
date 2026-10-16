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
        [Tooltip("D-108: level badge under the portrait; when set, the name text shows only the job name.")]
        [SerializeField] private Text _heroLevelText;
        [Tooltip("D-108: kill progress bar under the stage number (anchorMax.x = kills / target).")]
        [SerializeField] private RectTransform _killFill;
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
        [Tooltip("D-146: a burst of light over a slot as its skill is cast, auto or tapped (grade coloured).")]
        [SerializeField] private Image[] _skillCastGlows = new Image[0];
        [Header("Skill auto / manual toggle (D-085)")]
        [SerializeField] private Image _autoImage;
        [SerializeField] private Text _autoLabel;
        [SerializeField] private Sprite _autoOnSprite;
        [SerializeField] private Sprite _autoOffSprite;
        [SerializeField] private GameObject[] _skillReadyMarks = new GameObject[0];
        [Tooltip("D-108: faint plus shown in empty unlocked slots (tap opens the skill tab).")]
        [SerializeField] private Sprite _emptySlotIcon;
        [Tooltip("D-178: says what a locked slot waits for when it is tapped.")]
        [SerializeField] private ToastQueue _toast;
        [Tooltip("D-108: tapping an empty skill slot opens the skill tab of this host.")]
        [SerializeField] private PanelHost _panels;
        [SerializeField] private GameObject _bossBar;
        [SerializeField] private RectTransform _bossFill;
        [SerializeField] private Text _bossName;

        private const string FarmingKey = "hud.farming";
        private const string AutoRetreatKey = "hud.auto_retreat";
        private const string TimeUpRetreatKey = "hud.time_up_retreat";
        private const float GoldCountSeconds = 0.4f;

        private BalanceValues _balance;
        private SaveDataV2 _save;
        private bool _goldShown;
        private double _shownGold;
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
        private float[] _shownRemaining = new float[0];
        private float[] _castGlowAge = new float[0];
        private const float CastGlowSeconds = 0.5f;
        private bool[] _shownLocked = new bool[0];
        private int _shownSkillLevel = -1;
        private StageRunner _skillRunner;
        private SettingsService _settings;
        private int _shownAuto = -1;
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
                string job = Strings.Get(SoloHero.Core.Jobs.JobCatalog.Find(_save.jobId).NameKey);
                if (_heroLevelText != null)
                {
                    _heroLevelText.text = Strings.Format("char.lv", _save.heroLevel);
                    _heroNameText.text = job;
                }
                else
                {
                    _heroNameText.text = Strings.Format("hud.hero_name", _save.heroLevel, job);
                }
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

        /// <summary>
        /// Skill bar tap: casts that slot now if it is ready (auto-cast keeps running either way). D-108: an empty
        /// unlocked slot opens the skill tab instead, so the player finds where skills are equipped.
        /// </summary>
        public void CastSlot(int slot)
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            if (runner.Skills.DefAt(slot) == null)
            {
                bool locked = slot < _shownLocked.Length && _shownLocked[slot];
                if (!locked && _panels != null) _panels.Toggle(SkillTabIndex);
                // D-178: a locked slot says what opens it (the ultimate waits for the second job).
                else if (locked && _toast != null && _balance != null)
                    _toast.Show(slot == runner.Skills.UltimateSlot
                        ? Strings.Format("hud.ultimate_need", _balance.JOB_LV_2)
                        : Strings.Format("skill.slot_opens", slot + 1, SkillService.UnlockHeroLevel(_balance, slot)));
                return;
            }

            // The slot punches when its cooldown restarts (OnSlotCast), for taps and auto casts alike.
            runner.Skills.TryCast(slot, runner.Hero, runner.World);
        }

        private void OnSlotCast(int slot, SkillDef def)
        {
            if (slot < _skillPunches.Length && _skillPunches[slot] != null) _skillPunches[slot].Play();
            if (slot >= _skillCastGlows.Length || _skillCastGlows[slot] == null) return;
            _castGlowAge[slot] = 0f;
            Color color = Color.Lerp(SoloHero.Game.UI.Panels.PanelServices.GradeColor(def.Grade), Color.white, 0.3f);
            _skillCastGlows[slot].color = color;
        }

        private void DrawCastGlow(int slot)
        {
            if (slot >= _skillCastGlows.Length || _skillCastGlows[slot] == null || _castGlowAge[slot] < 0f) return;
            Image glow = _skillCastGlows[slot];
            _castGlowAge[slot] += Time.unscaledDeltaTime;
            float t = _castGlowAge[slot] / CastGlowSeconds;
            if (t >= 1f)
            {
                _castGlowAge[slot] = -1f;
                glow.enabled = false;
                return;
            }

            glow.enabled = true;
            Color c = glow.color;
            c.a = (1f - t) * (1f - t);
            glow.color = c;
            float scale = 0.8f + 0.6f * t;
            glow.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Index of the skill tab in the bottom tab bar (character, equipment, summon, skill, talent).</summary>
        private const int SkillTabIndex = 3;

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
            // D-130: a tower floor is a boss fight too.
            bool show = runner.IsBoss && (runner.State == StageState.BossIntro || runner.State == StageState.BossTimer
                || (runner.InTower && runner.State == StageState.Dungeon));
            SetShown(_bossBar, show, ref _bossBarVisible);
            if (!show) return;

            if (_bossBarStage != runner.GlobalStage && _bossName != null && _balance != null)
            {
                _bossBarStage = runner.GlobalStage;
                StageIndex.FromGlobal(runner.GlobalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
                _bossName.text = BossNames.NameFor(_balance, chapter);
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
            // D-130: the plate reads "tower N" while climbing (a negative key keeps it apart from stage numbers).
            int key = runner.InTower ? -runner.TowerFloor : runner.GlobalStage;
            if (_shownGlobalStage == key) return;
            _shownGlobalStage = key;
            if (runner.InTower)
            {
                _stageText.text = Strings.Format("tower.plate", runner.TowerFloor);
                return;
            }

            // D-153: "3-7" in the normal tier, "Hard 3-7" past it.
            _stageText.text = DifficultyTier.StageLabel(_balance, runner.GlobalStage);
        }

        private void RefreshKills(StageRunner runner)
        {
            if (_killsText == null) return;
            int kills = runner.InTower ? runner.TowerCleared : runner.Kills;
            if (_shownKills == kills && _shownKillTarget == runner.KillTarget) return;

            _shownKills = kills;
            _shownKillTarget = runner.KillTarget;
            // D-100: a dungeon has no kill target, only a running count.
            _killsText.text = runner.InDungeon ? _shownKills.ToString() : _shownKills.ToString() + " / " + _shownKillTarget.ToString();
            if (_killFill != null)
            {
                float ratio = runner.InDungeon || _shownKillTarget <= 0 ? 1f : Mathf.Clamp01((float)_shownKills / _shownKillTarget);
                _killFill.anchorMax = new Vector2(ratio, 1f);
            }
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

        /// <summary>
        /// D-078 skill bar: each slot shows its skill icon in a grade-coloured frame, a radial cooldown with seconds,
        /// or a lock with the hero level that opens it. Texts and sprites change only when their value changes.
        /// D-178: the job's main attack is not a slot any more (it always fires; the character panel describes it).
        /// </summary>
        private void RefreshSkills(StageRunner runner)
        {
            RefreshAuto(runner);
            int count = _skillIcons.Length;
            if (_shownDefs.Length != count || _skillRunner != runner)
            {
                _skillRunner = runner;
                _shownDefs = new SkillDef[count];
                _shownSeconds = new int[count];
                _shownLocked = new bool[count];
                _shownRemaining = new float[count];
                _castGlowAge = new float[count];
                for (int i = 0; i < count; i++)
                {
                    // A slot already cooling down when the bar (re)builds is not a fresh cast.
                    _shownRemaining[i] = float.MaxValue;
                    _castGlowAge[i] = -1f;
                }
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
                // D-104: the ultimate slot is never level-locked; D-178: it stays visible and reads as locked until the
                // job has an ultimate, so the player sees what the second job adds.
                bool locked = i == runner.Skills.UltimateSlot
                    ? def == null
                    : _save != null && _balance != null && !SkillService.IsSlotUnlocked(_balance, i, _save.heroLevel);
                if (def != _shownDefs[i] || locked != _shownLocked[i] || levelChanged)
                {
                    // A newly equipped skill's cooldown is not a cast.
                    if (def != _shownDefs[i]) _shownRemaining[i] = float.MaxValue;
                    _shownDefs[i] = def;
                    _shownLocked[i] = locked;
                    DrawSkillSlot(i, def, locked);
                    _shownSeconds[i] = -1;
                }

                float remaining = def != null ? runner.Skills.CooldownRemaining(i) : 0f;
                float total = def != null ? runner.Skills.CooldownTotal(i) : 0f;
                // D-146: a cooldown that jumps back up means the skill just went off (auto or tapped).
                if (def != null && remaining > _shownRemaining[i] + 0.5f) OnSlotCast(i, def);
                _shownRemaining[i] = remaining;
                DrawCastGlow(i);
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
            bool ultimate = _balance != null && i >= SkillService.SlotCount(_balance);
            // D-178: the ultimate slot stays on screen (locked until the second job) instead of appearing from nowhere.
            if (ultimate && frame != null && !frame.gameObject.activeSelf) frame.gameObject.SetActive(true);
            if (icon != null)
            {
                bool empty = def == null && !locked && _balance != null && i < SkillService.SlotCount(_balance) && _emptySlotIcon != null;
                icon.sprite = empty ? _emptySlotIcon : sprite;
                icon.enabled = empty || (sprite != null && !locked);
                icon.color = empty ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            }

            if (frame != null && _gradeFrames != null)
            {
                frame.sprite = def != null && !locked ? _gradeFrames.Get(def.Grade) : _gradeFrames.EmptyDark;
                frame.color = Color.white;
            }
            else if (frame != null)
            {
                frame.color = def != null && !locked ? PanelServices.GradeColor(def.Grade) : new Color(0.35f, 0.35f, 0.4f, 1f);
            }
            if (lockText == null) return;
            lockText.gameObject.SetActive(locked);
            if (locked && _balance != null)
                lockText.text = ultimate ? Strings.Get("hud.ultimate_locked") : Strings.Format("skill.slot_locked", SkillService.UnlockHeroLevel(_balance, i));
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
            // D-124: a boss mostly ends by the clock; say so when the hero is still standing.
            string text = bossFail
                ? Strings.Format(runner.Hero.Hp > 0d ? TimeUpRetreatKey : AutoRetreatKey, Mathf.CeilToInt(runner.BossAutoRetreatRemaining))
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
