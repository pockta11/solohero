using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Core.Balance
{
    /// <summary>
    /// Plays the real <see cref="StageRunner"/> headless at a fixed step, day by day, with a scripted player
    /// (<see cref="SimSettings"/> + <see cref="SimSpender"/>). Game rules come only from Core; the player model
    /// is the only thing this class adds. Output feeds <see cref="BalanceChecks"/> and <see cref="SimCsv"/>.
    /// </summary>
    public static class BalanceSimulator
    {
        public static SimReport Run(BalanceValues balance, SimSettings settings)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new SimRun(balance, settings).Execute();
        }

        private sealed class StageTrack
        {
            public double FirstStartPlay = -1d;
            public int Attempts;
            public int Fails;
        }

        private sealed class SimRun
        {
            private const int SecondsPerDay = 86400;
            private const int AdOfflineDailyLimit = 5;
            private const int AdGemDailyLimit = 5;
            private const int AdGemReward = 5;
            private const int AdBoosterDailyLimit = 3;
            private const double AdBoosterSeconds = 600d;
            private const double FirstSessionCheckSeconds = 300d;

            private readonly BalanceValues _b;
            private readonly SimSettings _s;
            private readonly SaveDataV2 _save;
            private readonly StageRunner _runner;
            private readonly SimSpender _spender;
            private readonly SimReport _report;
            private readonly Dictionary<int, StageTrack> _tracks = new Dictionary<int, StageTrack>();
            private readonly HashSet<int> _clearedOnce = new HashSet<int>();

            private SimDayRow _day;
            private int _sessionIndex;
            private long _sessionStartUtc;
            private double _sessionStartPlay;
            private double _play;
            private double _earnedTotal;
            private double _sessionStageGold;
            private double _lastSessionGoldPerMinute;
            private int _lastHeroLevel;
            private bool _attemptStarted;
            private double _attemptStartPlay;
            private int _pendingClearG;
            private bool _pendingFail;
            private int _retreatFarmClears;
            private double _boosterRemaining;
            private bool _firstFiveRecorded;
            private bool _tutorialDone;

            public SimRun(BalanceValues balance, SimSettings settings)
            {
                _b = balance;
                _s = settings;
                _save = SaveDataV2.CreateNew();
                _report = new SimReport { Name = settings.Name, Seed = settings.Seed };

                IRandom combatRng = new SystemRandom(new Random(settings.Seed));
                IRandom gachaRng = new SystemRandom(new Random(unchecked(settings.Seed * 7919 + 17)));
                var gacha = new GachaService(balance, GachaTableValues.FromBalance(balance), gachaRng, GachaCatalog.Standard(balance));
                _spender = new SimSpender(balance, _save, gacha);
                _spender.GradeObtained += OnGradeObtained;

                _runner = new StageRunner(balance, combatRng, CombatLoadout.ComputeStats(balance, _save), _save);
                // No animation in the simulator: the hit frame lands on the same step the attack starts.
                _runner.Hero.AttackRequested += () => _runner.Hero.OnHitFrame(_runner.World);
                _runner.StateChanged += OnStateChanged;
                _runner.StageCleared += g => _pendingClearG = g;
                CombatLoadout.Apply(_runner, balance, _save);
                _lastHeroLevel = _save.heroLevel;
            }

            public SimReport Execute()
            {
                for (int d = 1; d <= _s.Days; d++)
                {
                    _day = new SimDayRow { Day = d };
                    double spentUpgrade0 = _spender.SpentUpgrade;
                    double spentGacha0 = _spender.SpentGacha;
                    double spentSkill0 = _spender.SpentSkill;
                    double refund0 = _spender.EarnedRefund;
                    int goldPulls0 = _spender.GoldPulls;
                    int gemPulls0 = _spender.GemPulls;
                    int adOffline = 0;
                    int adGem = 0;
                    int adBooster = 0;

                    for (int i = 0; i < _s.DailySessions.Length; i++)
                    {
                        SimSession session = _s.DailySessions[i];
                        _sessionIndex = i + 1;
                        _sessionStartUtc = _s.StartUtc + (long)(d - 1) * SecondsPerDay + (long)(session.StartHour * 3600d);
                        _sessionStartPlay = _play;

                        ClaimOffline(d, ref adOffline);
                        if (_s.UseAds)
                        {
                            while (adGem < AdGemDailyLimit)
                            {
                                _save.gem += AdGemReward;
                                adGem++;
                            }

                            if (adBooster < AdBoosterDailyLimit)
                            {
                                _boosterRemaining = AdBoosterSeconds;
                                adBooster++;
                            }
                        }

                        Spend();
                        // Resume rule (E2-12): coming back restarts the current farming stage.
                        _runner.Resume(_save.farmingStage < 1 ? 1 : _save.farmingStage, _save.retreatMode);
                        _attemptStarted = true;
                        ProcessAttemptStart();

                        _sessionStageGold = 0d;
                        int steps = (int)Math.Round(session.Minutes * 60d / _s.DeltaTime);
                        for (int t = 0; t < steps; t++)
                            Step();

                        if (session.Minutes > 0d)
                            _lastSessionGoldPerMinute = _sessionStageGold / session.Minutes;
                        _save.lastQuitTimeUtc = _sessionStartUtc + (long)Math.Round(session.Minutes * 60d);
                    }

                    _day.SpentUpgrade = _spender.SpentUpgrade - spentUpgrade0;
                    _day.SpentGacha = _spender.SpentGacha - spentGacha0;
                    _day.SpentSkill = _spender.SpentSkill - spentSkill0;
                    _day.EarnedRefund = _spender.EarnedRefund - refund0;
                    _day.GoldPulls = _spender.GoldPulls - goldPulls0;
                    _day.GemPulls = _spender.GemPulls - gemPulls0;
                    _day.HighestStage = _save.highestStage;
                    _day.HeroLevel = _save.heroLevel;
                    _day.GoldEnd = _save.gold;
                    _day.GemEnd = _save.gem;
                    _report.Days.Add(_day);
                }

                _report.TotalPlaySeconds = _play;
                _report.FinalHighestStage = _save.highestStage;
                return _report;
            }

            private void Step()
            {
                float dt = _s.DeltaTime;
                double goldBefore = _save.gold;
                _runner.Tick(dt);
                _play += dt;
                _day.PlaySeconds += dt;
                if (_boosterRemaining > 0d) _boosterRemaining -= dt;

                double gained = _save.gold - goldBefore;
                if (gained > 0d)
                {
                    _day.EarnedStage += gained;
                    _earnedTotal += gained;
                    _sessionStageGold += gained;
                }

                if (_save.heroLevel != _lastHeroLevel)
                {
                    _lastHeroLevel = _save.heroLevel;
                    CombatLoadout.Apply(_runner, _b, _save);
                }

                if (!_tutorialDone && _play >= _s.TutorialPullAtSeconds)
                {
                    _tutorialDone = true;
                    _spender.FreePulls(_b.TUTORIAL_FREE_PULLS);
                    CombatLoadout.Apply(_runner, _b, _save);
                }

                if (!_firstFiveRecorded && _play >= FirstSessionCheckSeconds)
                {
                    _firstFiveRecorded = true;
                    _report.FirstFiveMinutesStage = _runner.GlobalStage;
                    _report.FirstFiveMinutesUpgrades = _spender.Upgrades;
                    _report.FirstFiveMinutesPulls = _save.totalPullCount;
                }

                if (_pendingClearG > 0)
                {
                    int g = _pendingClearG;
                    _pendingClearG = 0;
                    HandleClear(g);
                }

                if (_pendingFail)
                {
                    _pendingFail = false;
                    HandleFail();
                }

                ProcessAttemptStart();
            }

            private void OnStateChanged(StageState state)
            {
                switch (state)
                {
                    case StageState.Running:
                    case StageState.Retreat:
                    case StageState.BossIntro:
                        _attemptStarted = true;
                        break;
                    case StageState.Failed:
                        _pendingFail = true;
                        break;
                }
            }

            private void ProcessAttemptStart()
            {
                if (!_attemptStarted) return;
                _attemptStarted = false;

                int g = _runner.GlobalStage;
                _save.farmingStage = g;
                _save.retreatMode = _runner.RetreatMode;
                _attemptStartPlay = _play;
                StageTrack track = Track(g);
                if (track.FirstStartPlay < 0d) track.FirstStartPlay = _play;
                track.Attempts++;
                if (_runner.IsBoss) _day.BossAttempts++;
                else _day.NormalAttempts++;
            }

            private void HandleClear(int g)
            {
                bool isBoss = StageIndex.IsBoss(g, _b.STAGES_PER_CHAPTER);
                if (_boosterRemaining > 0d)
                {
                    double extra = Formulas.StageClearGold(_b, g, isBoss);
                    _save.gold += extra;
                    _day.EarnedBooster += extra;
                    _earnedTotal += extra;
                }

                double duration = _play - _attemptStartPlay;
                if (!isBoss)
                {
                    if (!_report.ClearStats.TryGetValue(g, out SimClearStat stat))
                    {
                        stat = new SimClearStat { G = g };
                        _report.ClearStats.Add(g, stat);
                    }

                    stat.Count++;
                    stat.Sum += duration;
                    if (duration < stat.Min) stat.Min = duration;
                    if (duration > stat.Max) stat.Max = duration;
                }

                if (_clearedOnce.Add(g))
                    _report.Stages.Add(BuildRow(g, isBoss, duration));

                if (_runner.RetreatMode) _retreatFarmClears++;

                Spend();

                if (_runner.RetreatMode && _retreatFarmClears >= _s.BossRetryAfterFarmClears)
                {
                    _retreatFarmClears = 0;
                    _runner.ChallengeBoss();
                    _attemptStarted = true;
                }
            }

            private void HandleFail()
            {
                int g = _runner.GlobalStage;
                Track(g).Fails++;
                bool isBoss = _runner.IsBoss;
                if (isBoss) _day.BossFails++;
                else _day.NormalFails++;

                Spend();

                if (isBoss)
                {
                    _runner.ChooseRetreat();
                    _retreatFarmClears = 0;
                    _attemptStarted = true;
                }
                else if (_runner.StepDown())
                {
                    // GDD fail-streak prompt: after 3 straight fails on a normal stage, farm one stage lower.
                    _attemptStarted = true;
                }
            }

            private void Spend()
            {
                if (_spender.Spend(_save.highestStage + 1, _s.MaxPurchasesPerDecision))
                    CombatLoadout.Apply(_runner, _b, _save);
            }

            private void ClaimOffline(int day, ref int adOffline)
            {
                if (_save.lastQuitTimeUtc <= 0) return;

                OfflineReward reward = OfflineReward.Compute(_b, _save.farmingStage, _save.lastQuitTimeUtc, _sessionStartUtc);
                if (reward.ResetQuitTime)
                {
                    _save.lastQuitTimeUtc = _sessionStartUtc;
                    return;
                }

                if (reward.Gold <= 0d) return;

                bool doubled = _s.UseAds && reward.ShowPopup && adOffline < AdOfflineDailyLimit;
                if (doubled) adOffline++;
                double gold = reward.Gold * (doubled ? _b.OFFLINE_AD_MULT : 1d);

                _report.OfflineClaims.Add(new SimOfflineClaim
                {
                    Day = day,
                    ElapsedSeconds = _sessionStartUtc - _save.lastQuitTimeUtc,
                    Gold = gold,
                    Doubled = doubled,
                    EarnedBefore = _earnedTotal + _spender.EarnedRefund,
                    OnlineGoldPerMinute = _lastSessionGoldPerMinute
                });

                if (reward.ShowPopup)
                    new OfflineClaim(_b).Apply(_save, reward, _sessionStartUtc, doubled);
                else
                    _save.gold += gold;

                _day.EarnedOffline += gold;
                _earnedTotal += gold;
            }

            private void OnGradeObtained(Grade grade)
            {
                if (grade >= Grade.Epic && _report.FirstEpicPlaySeconds < 0d)
                {
                    _report.FirstEpicPlaySeconds = _play;
                    _report.FirstEpicDay = _day != null ? _day.Day : 1;
                }

                if (grade == Grade.Legendary && _report.FirstLegendaryPlaySeconds < 0d)
                {
                    _report.FirstLegendaryPlaySeconds = _play;
                    _report.FirstLegendaryDay = _day != null ? _day.Day : 1;
                }
            }

            private SimStageRow BuildRow(int g, bool isBoss, double duration)
            {
                StageTrack track = Track(g);
                double wallSeconds = _sessionStartUtc - _s.StartUtc + (_play - _sessionStartPlay);
                return new SimStageRow
                {
                    G = g,
                    IsBoss = isBoss,
                    Day = _day.Day,
                    PlaySeconds = _play,
                    WallHours = wallSeconds / 3600d,
                    Attempts = track.Attempts,
                    Fails = track.Fails,
                    ClearSeconds = duration,
                    StuckPlaySeconds = _play - track.FirstStartPlay,
                    HeroLevel = _save.heroLevel,
                    UpgradeHp = _save.upgradeHp,
                    UpgradeAtk = _save.upgradeAtk,
                    UpgradeDef = _save.upgradeDef,
                    UpgradeSpd = _save.upgradeSpd,
                    SkillLevel1 = _save.skillLevel1,
                    SkillLevel2 = _save.skillLevel2,
                    SkillLevel3 = _save.skillLevel3,
                    SwordGrade = EquipmentBonus.GradeOrNone(_save.equippedSword, EquipmentSlot.Sword),
                    HelmGrade = EquipmentBonus.GradeOrNone(_save.equippedHelm, EquipmentSlot.Helm),
                    ArmorGrade = EquipmentBonus.GradeOrNone(_save.equippedArmor, EquipmentSlot.Armor),
                    BootsGrade = EquipmentBonus.GradeOrNone(_save.equippedBoots, EquipmentSlot.Boots),
                    Pulls = _save.totalPullCount,
                    Gold = _save.gold,
                    EarnedTotal = _earnedTotal + _spender.EarnedRefund,
                    SpentTotal = _spender.SpentUpgrade + _spender.SpentGacha + _spender.SpentSkill
                };
            }

            private StageTrack Track(int g)
            {
                if (!_tracks.TryGetValue(g, out StageTrack track))
                {
                    track = new StageTrack();
                    _tracks.Add(g, track);
                }

                return track;
            }
        }
    }
}
