using System;
using System.Collections.Generic;
using System.Globalization;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
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
    /// D-170 adds the long-run signals of the 1-year roadmap (section 6): a daily stage_stall while the frontier does
    /// not move, axis_cap once per capped growth axis, and tier_enter when the frontier enters a new difficulty tier.
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
        private readonly IClock _clock;
        private float _checkIn;

        /// <summary>Seconds between the stall / cap checks (both are cheap, but neither needs every frame).</summary>
        private const float CheckInterval = 10f;

        private const int CapGearSummon = 1;
        private const int CapSkillSummon = 2;
        private const int CapPetSummon = 4;
        private const int CapFinalJob = 8;

        /// <param name="clock">Local days for stage_stall; without one no stall is reported.</param>
        public GameplayTelemetry(StageRunner runner, SaveDataV2 save, BalanceValues balance, IAnalytics analytics, IClock clock = null)
        {
            _clock = clock;
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

            _checkIn -= dt;
            if (_checkIn > 0f) return;
            _checkIn = CheckInterval;
            CheckStall();
            CheckCaps();
        }

        /// <summary>
        /// D-170: once per local day while the frontier has not moved since an earlier day. The day the frontier
        /// moves (or is first seen) starts the count again.
        /// </summary>
        private void CheckStall()
        {
            if (_clock == null) return;
            string today = DayKey.Today(_clock);
            if (_save.highestStage != _save.telemetryFrontier || string.IsNullOrEmpty(_save.telemetryFrontierDate))
            {
                _save.telemetryFrontier = _save.highestStage;
                _save.telemetryFrontierDate = today;
                return;
            }

            if (!DayKey.IsNewDay(_save.telemetryStallDate, today)) return;
            int days = DaysBetween(_save.telemetryFrontierDate, today);
            if (days < 1) return;
            _save.telemetryStallDate = today;
            int next = _save.highestStage + 1;
            StageIndex.FromGlobal(next, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            Send(AnalyticsEvents.StageStall,
                AnalyticsParam.Of(AnalyticsEvents.PStage, next),
                AnalyticsParam.Of(AnalyticsEvents.PChapter, chapter),
                AnalyticsParam.Of(AnalyticsEvents.PTier, DifficultyTier.Of(_balance, next)),
                AnalyticsParam.Of(AnalyticsEvents.PBoss, StageIndex.IsBoss(next, _balance.STAGES_PER_CHAPTER)),
                AnalyticsParam.Of(AnalyticsEvents.PDays, days));
        }

        private static int DaysBetween(string from, string to)
        {
            const string format = "yyyy-MM-dd";
            if (!DateTime.TryParseExact(from, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime a)) return 0;
            if (!DateTime.TryParseExact(to, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime b)) return 0;
            return (int)(b - a).TotalDays;
        }

        /// <summary>D-170: growth axes with an upper limit, each reported once per save when it is reached.</summary>
        private void CheckCaps()
        {
            int max = _balance.SUMMON_LV_MAX;
            Cap(CapGearSummon, AnalyticsEvents.AxisGearSummon, SummonLevel.Of(_balance, _save, SummonKind.Gear) >= max);
            Cap(CapSkillSummon, AnalyticsEvents.AxisSkillSummon, SummonLevel.Of(_balance, _save, SummonKind.Skill) >= max);
            Cap(CapPetSummon, AnalyticsEvents.AxisPetSummon, SummonLevel.Of(_balance, _save, SummonKind.Pet) >= max);
            JobDef job = JobCatalog.Find(_save.jobId);
            Cap(CapFinalJob, AnalyticsEvents.AxisFinalJob, job != null && job.Tier >= JobCatalog.MaxTier);
        }

        private void Cap(int bit, string axis, bool reached)
        {
            if (!reached || (_save.telemetryCaps & bit) != 0) return;
            _save.telemetryCaps |= bit;
            Send(AnalyticsEvents.AxisCap,
                AnalyticsParam.Of(AnalyticsEvents.PAxis, axis),
                AnalyticsParam.Of(AnalyticsEvents.PLevel, _save.heroLevel),
                AnalyticsParam.Of(AnalyticsEvents.PStage, _save.highestStage));
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
            int previous = _reachedStage;
            _reachedStage = _attemptStage;
            StageIndex.FromGlobal(_attemptStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            Send(AnalyticsEvents.StageReach,
                AnalyticsParam.Of(AnalyticsEvents.PStage, _attemptStage),
                AnalyticsParam.Of(AnalyticsEvents.PChapter, chapter),
                AnalyticsParam.Of(AnalyticsEvents.PBoss, _runner.IsBoss));
            if (chapter > _reportedChapter)
            {
                _reportedChapter = chapter;
                _analytics.SetUserProperty(AnalyticsEvents.UserHighestChapter, chapter.ToString(CultureInfo.InvariantCulture));
            }

            int tier = DifficultyTier.Of(_balance, _attemptStage);
            if (tier > DifficultyTier.Of(_balance, previous))
            {
                Send(AnalyticsEvents.TierEnter,
                    AnalyticsParam.Of(AnalyticsEvents.PTier, tier),
                    AnalyticsParam.Of(AnalyticsEvents.PStage, _attemptStage));
                _analytics.SetUserProperty(AnalyticsEvents.UserDifficultyTier, tier.ToString(CultureInfo.InvariantCulture));
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
