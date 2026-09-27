using System;
using System.Collections.Generic;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Core.Analytics
{
    /// <summary>
    /// Turns stage flow and save changes into analytics events (E6-15). Watches, never changes, the game:
    /// frontier reach / first clear / fail with play seconds, retreat farming and the challenge back (retry cadence,
    /// V-7), tutorial steps, hero levels, and gold earned / spent per session (24 h spend rate). Farming clears are not
    /// logged - they carry no metric and would be most of the volume. A new save starts at highestStage = 1, so the
    /// first frontier (and the first stage_reach) is stage 2.
    /// </summary>
    public sealed class GameplayTelemetry
    {
        private readonly StageRunner _runner;
        private readonly SaveDataV2 _save;
        private readonly IAnalytics _analytics;
        private readonly BalanceValues _balance;
        private readonly Dictionary<int, int> _attempts = new Dictionary<int, int>();
        private readonly List<AnalyticsParam> _params = new List<AnalyticsParam>(6);

        private double _attemptSeconds;
        private double _retreatSeconds;
        private double _sessionSeconds;
        private int _attemptStage;
        private bool _attemptIsFrontier;
        private bool _lastRetreat;
        private int _lastFailStage;
        private bool _lastFailBoss;
        private int _reachedStage;
        private int _shownLevel;
        private int _shownTutorial;
        private double _lastGold;
        private double _earned;
        private double _spent;
        private int _reportedChapter;

        public GameplayTelemetry(StageRunner runner, SaveDataV2 save, BalanceValues balance, IAnalytics analytics)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _analytics = analytics ?? new NullAnalytics();
            _shownLevel = save.heroLevel;
            _shownTutorial = save.tutorialStep;
            _lastGold = save.gold;
            _reachedStage = save.highestStage;
            // A retreat restored from the save on Resume is not a new retreat.
            _lastRetreat = save.retreatMode;
            _runner.StateChanged += OnStateChanged;
            _runner.StageStarted += OnStageStarted;
            _runner.StageCleared += OnStageCleared;
        }

        public void Dispose()
        {
            _runner.StateChanged -= OnStateChanged;
            _runner.StageStarted -= OnStageStarted;
            _runner.StageCleared -= OnStageCleared;
        }

        /// <summary>Call once per frame after the runner ticked.</summary>
        public void Tick(float dt)
        {
            _attemptSeconds += dt;
            _sessionSeconds += dt;
            if (_lastRetreat) _retreatSeconds += dt;

            double gold = _save.gold;
            if (gold > _lastGold) _earned += gold - _lastGold;
            else if (gold < _lastGold) _spent += _lastGold - gold;
            _lastGold = gold;

            if (_save.heroLevel > _shownLevel)
            {
                _shownLevel = _save.heroLevel;
                Send(AnalyticsEvents.LevelUp, AnalyticsParam.Of(AnalyticsEvents.PLevel, _shownLevel));
            }

            if (_save.tutorialStep != _shownTutorial)
            {
                _shownTutorial = _save.tutorialStep;
                Send(AnalyticsEvents.TutorialStep, AnalyticsParam.Of(AnalyticsEvents.PStep, _shownTutorial));
            }
        }

        /// <summary>Call when the app pauses or quits: reports the session's gold flow and starts a new tally.</summary>
        public void Flush()
        {
            if (_earned <= 0d && _spent <= 0d) return;
            Send(AnalyticsEvents.GoldSession,
                AnalyticsParam.Of(AnalyticsEvents.PEarned, Math.Round(_earned)),
                AnalyticsParam.Of(AnalyticsEvents.PSpent, Math.Round(_spent)),
                AnalyticsParam.Of(AnalyticsEvents.PSeconds, (long)_sessionSeconds));
            _earned = 0d;
            _spent = 0d;
            _sessionSeconds = 0d;
        }

        private void OnStateChanged(StageState state)
        {
            if (state == StageState.Failed)
            {
                _lastFailStage = _runner.GlobalStage;
                _lastFailBoss = _runner.IsBoss;
                Send(AnalyticsEvents.StageFail,
                    AnalyticsParam.Of(AnalyticsEvents.PStage, _runner.GlobalStage),
                    AnalyticsParam.Of(AnalyticsEvents.PBoss, _runner.IsBoss),
                    AnalyticsParam.Of(AnalyticsEvents.PSeconds, (long)_attemptSeconds),
                    AnalyticsParam.Of(AnalyticsEvents.PAttempt, AttemptCount(_runner.GlobalStage)));
            }
        }

        private void OnStageStarted(int g)
        {
            TrackRetreat();
            _attemptSeconds = 0d;
            _attemptStage = _runner.GlobalStage;
            _attemptIsFrontier = !_runner.RetreatMode && _attemptStage > _save.highestStage;
            if (!_attemptIsFrontier) return;

            _attempts[_attemptStage] = AttemptCount(_attemptStage) + 1;
            if (_attemptStage <= _reachedStage) return;
            _reachedStage = _attemptStage;
            StageIndex.FromGlobal(_attemptStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            Send(AnalyticsEvents.StageReach,
                AnalyticsParam.Of(AnalyticsEvents.PStage, _attemptStage),
                AnalyticsParam.Of(AnalyticsEvents.PChapter, chapter),
                AnalyticsParam.Of(AnalyticsEvents.PBoss, _runner.IsBoss));
            if (chapter > _reportedChapter)
            {
                _reportedChapter = chapter;
                _analytics.SetUserProperty(AnalyticsEvents.UserHighestChapter, chapter.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private void TrackRetreat()
        {
            bool retreat = _runner.RetreatMode;
            if (retreat == _lastRetreat) return;
            _lastRetreat = retreat;
            if (retreat)
            {
                _retreatSeconds = 0d;
                Send(AnalyticsEvents.RetreatEnter,
                    AnalyticsParam.Of(AnalyticsEvents.PStage, _lastFailStage),
                    AnalyticsParam.Of(AnalyticsEvents.PBoss, _lastFailBoss));
                return;
            }

            Send(AnalyticsEvents.Challenge,
                AnalyticsParam.Of(AnalyticsEvents.PStage, _runner.GlobalStage),
                AnalyticsParam.Of(AnalyticsEvents.PSeconds, (long)_retreatSeconds));
        }

        private void OnStageCleared(int g)
        {
            if (!_attemptIsFrontier || g != _attemptStage) return;
            _attemptIsFrontier = false;
            Send(AnalyticsEvents.StageClear,
                AnalyticsParam.Of(AnalyticsEvents.PStage, g),
                AnalyticsParam.Of(AnalyticsEvents.PBoss, _runner.IsBoss),
                AnalyticsParam.Of(AnalyticsEvents.PSeconds, (long)_attemptSeconds),
                AnalyticsParam.Of(AnalyticsEvents.PAttempt, AttemptCount(g)));
        }

        private int AttemptCount(int g) => _attempts.TryGetValue(g, out int n) ? n : 0;

        private void Send(string name, params AnalyticsParam[] values)
        {
            _params.Clear();
            _params.AddRange(values);
            _analytics.Log(name, _params);
        }
    }
}
