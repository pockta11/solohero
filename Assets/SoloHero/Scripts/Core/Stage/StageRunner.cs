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

        public StageRunner(BalanceValues balance, IRandom random, HeroStats stats, SaveDataV2 save, ISaveRequester saveRequester = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            if (random == null) throw new ArgumentNullException(nameof(random));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _stats = stats;
            _stageReward = new StageReward(_save, _balance, saveRequester);
            _world = new CombatWorld(_balance);
            _spawner = new SpawnScheduler(_balance);
            _skills = new SkillAutoCaster(_balance);
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

        public event Action<StageState> StateChanged;
        public event Action<int> StageCleared;
        public event Action BossFailed;

        public void Begin(int g)
        {
            if (g < 1) g = 1;
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
            _hero.Reset(_stats);

            if (_isBoss)
                SetState(StageState.BossIntro);
            else if (_retreatMode)
                SetState(StageState.Retreat);
            else
                SetState(StageState.Running);
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
            if (!PromptRetreat || _isBoss || _g <= 1) return false;
            if (State != StageState.Failed && State != StageState.Running) return false;
            FailStreak = 0;
            _retreatMode = true;
            _challenging = false;
            Begin(_g - 1);
            return true;
        }

        /// <summary>The stage a challenge from farming goes to: the first stage not yet cleared.</summary>
        public int FrontierStage => _save.highestStage + 1;

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
            if (!_retreatMode) return;
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
            _hero.SetAtkBuffFraction(_skills.AtkBuffSum);
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

        private void TickFailed(float dt)
        {
            if (_failedWasBoss) return;
            _retryTimer += dt;
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

            double x = _world.SpawnXAheadOfHero();
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
