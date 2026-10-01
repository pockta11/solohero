using System;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;

namespace SoloHero.Core.Stage
{
    public sealed class StageRunner
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _save;
        private readonly StageReward _stageReward;
        private readonly CombatWorld _world;
        private readonly SpawnScheduler _spawner;
        private readonly SkillAutoCaster _skills;
        private readonly CompanionCaster _companion;
        private readonly HeroBrain _hero;
        private HeroStats _stats;

        private int _g;
        private bool _isBoss;
        private bool _retreatMode;
        private bool _challenging;
        private int _killTarget;
        private int _kills;
        private bool _cleared;
        private float _bossTimer;
        private float _introTimer;
        private float _clearTimer;
        private float _deathTimer;
        private float _retryTimer;
        private int _failedG;
        private bool _failedWasBoss;
        private readonly ISaveRequester _saveRequester;
        private DungeonKind _dungeon;
        private float _dungeonTimer;
        private float _resultTimer;
        private int _returnG;
        private bool _returnRetreat;

        public StageRunner(BalanceValues balance, IRandom random, HeroStats stats, SaveDataV2 save, ISaveRequester saveRequester = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            if (random == null) throw new ArgumentNullException(nameof(random));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _stats = stats;
            _stageReward = new StageReward(_save, _balance, saveRequester);
            _saveRequester = saveRequester;
            _world = new CombatWorld(_balance);
            _spawner = new SpawnScheduler(_balance);
            _skills = new SkillAutoCaster(_balance);
            _companion = new CompanionCaster(_balance);
            _hero = new HeroBrain(_balance, random, stats);
            _world.BindHero(_hero);
        }

        public StageState State { get; private set; }
        public int GlobalStage => _g;
        public int Kills => _kills;
        public int KillTarget => _killTarget;
        public bool IsBoss => _isBoss;
        public bool RetreatMode => _retreatMode;
        public float BossTimerRemaining => _bossTimer;
        public int FailStreak { get; private set; }

        /// <summary>Multiplier on stage-clear gold, e.g. the A-3 ad gold booster. Set by the owner every frame.</summary>
        public double ClearGoldMultiplier { get; set; } = 1d;
        public bool PromptRetreat => FailStreak >= _balance.FAIL_STREAK_STEP_DOWN;
        public HeroBrain Hero => _hero;
        public CombatWorld World => _world;
        public SkillAutoCaster Skills => _skills;
        public CompanionCaster Companion => _companion;

        public event Action<StageState> StateChanged;
        public event Action<int> StageCleared;
        public event Action BossFailed;

        /// <summary>Raised at the end of every <see cref="Begin"/>, even when the state enum does not change.</summary>
        public event Action<int> StageStarted;

        /// <summary>D-100: the dungeon run that is in progress (or showing its result), None otherwise.</summary>
        public DungeonKind Dungeon => _dungeon;
        public bool InDungeon => _dungeon != DungeonKind.None;
        public float DungeonTimeRemaining => _dungeonTimer > 0f ? _dungeonTimer : 0f;

        /// <summary>Gold or EXP earned in the current dungeon run (already added to the save, kill by kill).</summary>
        public double DungeonEarned { get; private set; }

        /// <summary>Raised once when a dungeon run ends (time up or hero down), with what it earned.</summary>
        public event Action<DungeonKind, double> DungeonEnded;

        /// <summary>
        /// D-100: leaves the current stage for a daily dungeon run at the farming stage's strength. Waves keep coming
        /// for DUNGEON_TIME seconds; every kill pays at once, so an interrupted run keeps what it earned. Afterwards
        /// the runner returns to the stage it left. Refused during a boss fight or another dungeon.
        /// </summary>
        public bool StartDungeon(DungeonKind kind)
        {
            if (kind == DungeonKind.None || InDungeon) return false;
            if (State == StageState.BossIntro || State == StageState.BossTimer) return false;
            _returnG = _isBoss ? StageIndex.PreviousNormalStage(_g) : _g;
            _returnRetreat = _retreatMode || _isBoss;
            int g = _save.farmingStage < 1 ? 1 : _save.farmingStage;
            if (StageIndex.IsBoss(g, _balance.STAGES_PER_CHAPTER)) g = StageIndex.PreviousNormalStage(g);
            _g = g < 1 ? 1 : g;
            _isBoss = false;
            _killTarget = int.MaxValue;
            _kills = 0;
            _cleared = false;
            _deathTimer = 0f;
            _dungeonTimer = _balance.DUNGEON_TIME;
            _resultTimer = 0f;
            DungeonEarned = 0d;
            _world.ClearAll();
            _spawner.Reset(int.MaxValue, false);
            _skills.ResetCooldowns();
            _companion.Reset();
            _hero.Reset(_stats);
            _dungeon = kind;
            // No StageStarted: a dungeon run is not a stage attempt (telemetry counts those).
            SetState(StageState.Dungeon);
            return true;
        }

        public void Begin(int g)
        {
            if (g < 1) g = 1;
            _dungeon = DungeonKind.None;
            _g = g;
            _isBoss = StageIndex.IsBoss(g, _balance.STAGES_PER_CHAPTER);
            _killTarget = _isBoss ? 1 : _balance.KILL_TARGET_NORMAL;
            _kills = 0;
            _cleared = false;
            _bossTimer = _balance.BOSS_TIME_LIMIT;
            _introTimer = _balance.BOSS_INTRO_TIME;
            _clearTimer = 0f;
            _deathTimer = 0f;
            _retryTimer = 0f;
            _world.ClearAll();
            _spawner.Reset(_killTarget, _isBoss);
            _skills.ResetCooldowns();
            _companion.Reset();
            _hero.Reset(_stats);

            if (_isBoss)
                SetState(StageState.BossIntro);
            else if (_retreatMode)
                SetState(StageState.Retreat);
            else
                SetState(StageState.Running);

            StageStarted?.Invoke(_g);
        }

        /// <summary>Starts at the saved farming stage and restores retreat farming after a restart.</summary>
        public void Resume(int g, bool retreatMode)
        {
            _retreatMode = retreatMode && !StageIndex.IsBoss(g < 1 ? 1 : g, _balance.STAGES_PER_CHAPTER);
            Begin(g);
        }

        /// <summary>Fail-streak prompt on a normal stage: farm one stage lower. Clearing it moves back up.</summary>
        public bool StepDown()
        {
            if (InDungeon || !PromptRetreat || _isBoss || _g <= 1) return false;
            if (State != StageState.Failed && State != StageState.Running) return false;
            FailStreak = 0;
            _retreatMode = true;
            _challenging = false;
            Begin(_g - 1);
            return true;
        }

        /// <summary>
        /// Stage select sheet (E3-09, D-072): farm any cleared normal stage. Boss stages are not farmable (30 s timer,
        /// one kill). Returns false and changes nothing for a locked, boss or out-of-range stage.
        /// </summary>
        public bool FarmAt(int g)
        {
            if (InDungeon || g < 1 || g > _save.highestStage) return false;
            if (StageIndex.IsBoss(g, _balance.STAGES_PER_CHAPTER)) return false;
            FailStreak = 0;
            _retreatMode = true;
            _challenging = false;
            Begin(g);
            return true;
        }

        /// <summary>The stage a challenge from farming goes to: the first stage not yet cleared.</summary>
        public int FrontierStage => _save.highestStage + 1;

        /// <summary>D-087: combat-side talent effects (boss / execute damage, damage taken, last stand, skill talents).</summary>
        public void SetTalents(SoloHero.Core.Talents.TalentEffects talents)
        {
            _hero.SetTalents(talents);
            _skills.SetTalents(talents);
        }

        public void SetHeroStats(HeroStats stats)
        {
            _stats = stats;
            _hero.SetStats(stats);
        }

        public void ChooseRetry()
        {
            if (State != StageState.Failed) return;
            _retreatMode = false;
            Begin(_failedG);
        }

        public void ChooseRetreat()
        {
            if (State != StageState.Failed || !_failedWasBoss) return;
            _retreatMode = true;
            _challenging = false;
            FailStreak = 0;
            Begin(StageIndex.PreviousNormalStage(_failedG));
        }

        /// <summary>Leaves farming for the frontier: the boss after a boss retreat, the wall stage after a step-down.</summary>
        public void ChallengeBoss()
        {
            if (!_retreatMode || InDungeon) return;
            _retreatMode = false;
            _challenging = true;
            Begin(FrontierStage);
        }

        public void Tick(float dt)
        {
            switch (State)
            {
                case StageState.BossIntro:
                    TickBossIntro(dt);
                    break;
                case StageState.Running:
                case StageState.Retreat:
                case StageState.BossTimer:
                    TickCombat(dt);
                    break;
                case StageState.Clearing:
                    TickClearing(dt);
                    break;
                case StageState.Failed:
                    TickFailed(dt);
                    break;
                case StageState.Dungeon:
                    TickDungeon(dt);
                    break;
                case StageState.DungeonResult:
                    _resultTimer += dt;
                    if (_resultTimer >= _balance.DUNGEON_RESULT_TIME) Resume(_returnG, _returnRetreat);
                    break;
            }
        }

        private void TickBossIntro(float dt)
        {
            _introTimer -= dt;
            if (_introTimer > 0f) return;
            SpawnOne(isBoss: true);
            SetState(StageState.BossTimer);
        }

        private void TickCombat(float dt)
        {
            if (State == StageState.Running || State == StageState.Retreat)
                TickSpawns(dt);

            _world.TickEnemies(dt);
            bool bossFight = State == StageState.BossTimer;
            _skills.Tick(dt, _hero, _world, bossFight);
            _companion.Tick(dt, _hero, _world);
            _hero.SetSkillBuffs(_skills.BuffAtk, _skills.BuffAtkSpd, _skills.BuffCrit, _skills.BuffGuard);
            _hero.Tick(dt, _world);

            // Simultaneous resolution order (GDD):
            // enemy deaths -> clear check -> boss timer -> hero death.
            // Clear beats hero death; boss kill beats timer expiry.
            int gained = _world.ResolveDeaths();
            if (gained > 0)
            {
                _kills += gained;
                for (int i = 0; i < gained; i++)
                    KillExp.Grant(_save, _balance, _g, _isBoss);
            }
            if (_kills >= _killTarget) _cleared = true;

            if (_cleared)
            {
                if (State != StageState.Clearing)
                {
                    SetState(StageState.Clearing);
                    _clearTimer = 0f;
                    FailStreak = 0;
                    _challenging = false;
                    _stageReward.ApplyClear(_g, ClearGoldMultiplier);
                    StageCleared?.Invoke(_g);
                }
                return;
            }

            if (State == StageState.BossTimer)
            {
                _bossTimer -= dt;
                if (_bossTimer <= 0f)
                {
                    Fail(wasBoss: true);
                    return;
                }
            }

            if (_hero.Hp <= 0d || _hero.State == HeroState.Dead)
            {
                if (_hero.State != HeroState.Dead) _hero.Kill();
                _deathTimer += dt;
                if (_deathTimer >= _balance.DEATH_ANIM_TIME)
                    Fail(wasBoss: _isBoss);
            }
        }

        /// <summary>Combat with endless waves; every kill pays; time up or the hero falling ends the run.</summary>
        private void TickDungeon(float dt)
        {
            TickSpawns(dt);
            _world.TickEnemies(dt);
            _skills.Tick(dt, _hero, _world, false);
            _companion.Tick(dt, _hero, _world);
            _hero.SetSkillBuffs(_skills.BuffAtk, _skills.BuffAtkSpd, _skills.BuffCrit, _skills.BuffGuard);
            _hero.Tick(dt, _world);

            int gained = _world.ResolveDeaths();
            for (int i = 0; i < gained; i++)
            {
                _kills++;
                if (_dungeon == DungeonKind.Gold)
                {
                    double gold = Formulas.DungeonGoldPerKill(_balance, _g);
                    _save.gold += gold;
                    DungeonEarned += gold;
                }
                else
                {
                    double exp = Formulas.DungeonExpPerKill(_balance, _g);
                    new HeroLevelService(_save, _balance).AddExp(exp);
                    DungeonEarned += exp;
                }
            }

            _dungeonTimer -= dt;
            bool down = _hero.Hp <= 0d || _hero.State == HeroState.Dead;
            if (down && _hero.State != HeroState.Dead) _hero.Kill();
            if (_dungeonTimer > 0f && !down) return;

            _dungeonTimer = 0f;
            _resultTimer = 0f;
            _world.ClearAll();
            SetState(StageState.DungeonResult);
            _saveRequester?.RequestSave();
            DungeonEnded?.Invoke(_dungeon, DungeonEarned);
        }

        private void TickClearing(float dt)
        {
            _clearTimer += dt;
            if (_clearTimer < _balance.STAGE_CLEAR_DELAY) return;

            if (_retreatMode)
            {
                Begin(_g);
                return;
            }

            Begin(_g + 1);
        }

        /// <summary>Seconds until a failed boss auto-retreats (D-077); 0 when not waiting on a boss fail.</summary>
        public float BossAutoRetreatRemaining =>
            State == StageState.Failed && _failedWasBoss ? Math.Max(0f, _balance.BOSS_FAIL_AUTO_RETREAT - _retryTimer) : 0f;

        private void TickFailed(float dt)
        {
            _retryTimer += dt;
            if (_failedWasBoss)
            {
                // D-077: an idle game must keep farming while nobody watches - the retry / retreat choice is offered
                // for a few seconds, then the runner retreats to farming by itself.
                if (_retryTimer >= _balance.BOSS_FAIL_AUTO_RETREAT) ChooseRetreat();
                return;
            }

            if (_retryTimer < _balance.STAGE_RETRY_DELAY) return;

            if ((_challenging || PromptRetreat) && _failedG > 1)
            {
                // Genre rule (D-058): a normal-stage death, or a failed challenge from farming, drops to
                // farming one stage lower instead of repeating the death. Challenge returns to the frontier.
                _challenging = false;
                _retreatMode = true;
                FailStreak = 0;
                Begin(_failedG - 1);
                return;
            }

            Begin(_failedG);
        }

        private void TickSpawns(float dt)
        {
            _spawner.Tick(dt);
            while (_spawner.TryConsumeSpawn(_world.ActiveCount))
            {
                if (!SpawnOne(isBoss: false)) break;
            }
        }

        private bool SpawnOne(bool isBoss)
        {
            EnemyBrain brain;
            if (!_world.TryActivateSlot(out brain)) return false;

            double hp = Formulas.EnemyHp(_balance, _g);
            double atk = Formulas.EnemyAtk(_balance, _g);
            float interval = _balance.ENEMY_ATK_INTERVAL;
            if (isBoss)
            {
                hp *= _balance.BOSS_HP_MULT;
                atk *= _balance.BOSS_ATK_MULT;
                interval = _balance.BOSS_ATK_INTERVAL;
            }

            // D-081: a wave stands in a line, front enemy first.
            double x = _world.SpawnXAheadOfHero() + (isBoss ? 0 : _spawner.WaveSlot) * _balance.SPAWN_WAVE_SPACING;
            // SpawnIndex only picks the look (E8-03); every enemy of a stage has the same stats.
            brain.Reset(_balance, hp, atk, interval, x, isBoss, isBoss ? 0 : _spawner.Spawned - 1);
            return true;
        }

        private void Fail(bool wasBoss)
        {
            _failedG = _g;
            _failedWasBoss = wasBoss;
            _retryTimer = 0f;
            FailStreak++;
            SetState(StageState.Failed);
            if (wasBoss) BossFailed?.Invoke();
        }

        private void SetState(StageState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
