using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
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

        private BalanceValues _balance;
        private SaveDataV2 _save;
        private bool _goldShown;
        private double _shownGold;
        private int _shownGlobalStage = -1;
        private int _shownKills = -1;
        private int _shownKillTarget = -1;
        private int _shownBossSeconds = -1;
        private bool _bossTimerVisible;
        private bool _failPanelVisible;
        private bool _retreatVisible;
        private bool _challengeVisible;

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

        public void ChallengeBoss()
        {
            StageRunner runner = CurrentRunner();
            if (runner == null) return;
            runner.ChallengeBoss();
        }

        private StageRunner CurrentRunner()
        {
            return _session != null ? _session.Runner : null;
        }

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

            if (_goldShown && _shownGold == _save.gold) return;
            _shownGold = _save.gold;
            _goldShown = true;
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
        }

        private static void SetShown(GameObject target, bool show, ref bool shown)
        {
            if (target == null || shown == show) return;
            shown = show;
            target.SetActive(show);
        }
    }
}
