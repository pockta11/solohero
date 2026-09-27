using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI
{
    public sealed class BattleHud : MonoBehaviour
    {
        [SerializeField] private CombatSession _session;
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _stageText;
        [SerializeField] private Text _killsText;
        [SerializeField] private Text _bossTimerText;
        [SerializeField] private GameObject _failPanel;
        [SerializeField] private GameObject _retreatButton;
        [SerializeField] private GameObject _challengeButton;
        [SerializeField] private Text _slot1Text;
        [SerializeField] private Text _slot2Text;
        [SerializeField] private Text _slot3Text;
        [SerializeField] private Text _retreatPrompt;
        [SerializeField] private UiPunch _goldPunch;

        private const string FarmingKey = "hud.farming";
        private const string AutoRetreatKey = "hud.auto_retreat";
        private const float GoldCountSeconds = 0.4f;

        private BalanceValues _balance;
        private SaveDataV2 _save;
        private bool _goldShown;
        private double _shownGold;
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
        private int _shownSlot1Seconds = -1;
        private int _shownSlot2Seconds = -1;
        private int _shownSlot3Seconds = -1;

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
        }

        private void LateUpdate()
        {
            RefreshGold();

            StageRunner runner = CurrentRunner();
            if (runner == null)
            {
                HideChoices();
                return;
            }

            RefreshStage(runner);
            RefreshKills(runner);
            RefreshBossTimer(runner);
            RefreshChoices(runner);
            RefreshSkills(runner);
            RefreshRetreatPrompt(runner);
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

        public void CastSlot1()
        {
            Cast(SkillSlot.Slot1);
        }

        public void CastSlot2()
        {
            Cast(SkillSlot.Slot2);
        }

        public void CastSlot3()
        {
            Cast(SkillSlot.Slot3);
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
            _killsText.text = _shownKills.ToString() + " / " + _shownKillTarget.ToString();
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
            SetShown(_challengeButton, runner.RetreatMode, ref _challengeVisible);
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

        private void Cast(SkillSlot slot)
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            runner.Skills.TryCast(slot, runner.Hero, runner.World);
        }

        private void RefreshSkills(StageRunner runner)
        {
            RefreshSkillLabel(_slot1Text, Strings.Get("skill.name.1"), runner.Skills.CooldownRemaining(SkillSlot.Slot1), ref _shownSlot1Seconds);
            RefreshSkillLabel(_slot2Text, Strings.Get("skill.name.2"), runner.Skills.CooldownRemaining(SkillSlot.Slot2), ref _shownSlot2Seconds);
            RefreshSkillLabel(_slot3Text, Strings.Get("skill.name.3"), runner.Skills.CooldownRemaining(SkillSlot.Slot3), ref _shownSlot3Seconds);
        }

        private static void RefreshSkillLabel(Text label, string readyName, float remaining, ref int shownSeconds)
        {
            if (label == null) return;

            int seconds = remaining > 0f ? Mathf.CeilToInt(remaining) : 0;
            if (seconds < 0) seconds = 0;
            if (shownSeconds == seconds) return;

            shownSeconds = seconds;
            label.text = seconds > 0 ? Strings.Format("hud.skill_cooldown", readyName, seconds) : readyName;
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
