using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    public sealed class EnemyBrain
    {
        private BalanceValues _balance;
        private float _atkTimer;
        private float _atkInterval;
        private bool _active;
        private float _stunTimer;
        private float _dotTimer;
        private float _dotTickTimer;
        private double _dotDps;
        private float _markTimer;
        private double _markFraction;

        public EnemyState State { get; private set; }
        public double Hp { get; private set; }
        public double MaxHp { get; private set; }
        public double Atk { get; private set; }
        public double X { get; private set; }
        public bool IsBoss { get; private set; }

        /// <summary>Order of this enemy within its stage (0-based). Views use it to pick one of the chapter's looks.</summary>
        public int SpawnIndex { get; private set; }

        /// <summary>Increments on every <see cref="Reset"/>; views use it to tell a reused slot from the same enemy.</summary>
        public int Generation { get; private set; }

        /// <summary>Increments on every attack swing; views watch it to play the attack animation.</summary>
        public int AttackCount { get; private set; }

        public bool IsStunned => _stunTimer > 0f && IsAlive;

        /// <summary>Burning or poisoned (a skill damage-over-time is running).</summary>
        public bool HasDot => _dotTimer > 0f && IsAlive;

        /// <summary>D-109: a skill mark is running; every hit on this enemy is multiplied by <see cref="DamageTakenMult"/>.</summary>
        public bool IsMarked => _markTimer > 0f && IsAlive;

        public double DamageTakenMult => IsMarked ? 1d + _markFraction : 1d;

        public bool IsActive => _active;
        public bool IsAlive => _active && State != EnemyState.Dead && Hp > 0d;

        public void Reset(BalanceValues balance, double maxHp, double atk, float atkInterval, double x, bool isBoss, int spawnIndex = 0)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            MaxHp = maxHp;
            Hp = maxHp;
            Atk = atk;
            _atkInterval = atkInterval;
            _atkTimer = atkInterval;
            X = x;
            IsBoss = isBoss;
            SpawnIndex = spawnIndex < 0 ? 0 : spawnIndex;
            Generation++;
            State = EnemyState.Idle;
            _active = true;
            ClearStatus();
        }

        public void Deactivate()
        {
            ClearStatus();
            _active = false;
            State = EnemyState.Dead;
            Hp = 0d;
        }

        public void TakeDamage(double amount)
        {
            if (!IsAlive) return;
            if (amount < 0d) amount = 0d;
            Hp -= amount;
            if (Hp <= 0d)
            {
                Hp = 0d;
                State = EnemyState.Dead;
            }
        }

        /// <summary>Stops attacks for <paramref name="seconds"/> (bosses: x SKILL_BOSS_STUN_MULT); never shortens a stun.</summary>
        public void Stun(float seconds)
        {
            if (!IsAlive || seconds <= 0f || _balance == null) return;
            if (IsBoss) seconds *= (float)_balance.SKILL_BOSS_STUN_MULT;
            if (seconds > _stunTimer) _stunTimer = seconds;
            if (State == EnemyState.Attacking) State = EnemyState.Idle;
        }

        /// <summary>Burn / poison: the stronger damage wins, the longer time wins.</summary>
        public void ApplyDot(double damagePerSecond, float seconds)
        {
            if (!IsAlive || damagePerSecond <= 0d || seconds <= 0f || _balance == null) return;
            if (_dotTimer <= 0f)
            {
                _dotTickTimer = _balance.SKILL_DOT_TICK;
                _dotDps = damagePerSecond;
            }
            else if (damagePerSecond > _dotDps)
            {
                _dotDps = damagePerSecond;
            }

            if (seconds > _dotTimer) _dotTimer = seconds;
        }

        /// <summary>D-109 mark: hits on this enemy deal (1 + fraction) times; the stronger mark wins, the longer time wins.</summary>
        public void Mark(double fraction, float seconds)
        {
            if (!IsAlive || fraction <= 0d || seconds <= 0f) return;
            if (_markTimer <= 0f || fraction > _markFraction) _markFraction = fraction;
            if (seconds > _markTimer) _markTimer = seconds;
        }

        /// <summary>Runs stun, mark and burn timers; returns the burn damage dealt this tick (0 when none).</summary>
        public double TickStatus(float dt)
        {
            if (!IsAlive)
            {
                if (_stunTimer > 0f || _dotTimer > 0f || _markTimer > 0f) ClearStatus();
                return 0d;
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                if (_stunTimer < 0f) _stunTimer = 0f;
            }

            if (_markTimer > 0f)
            {
                _markTimer -= dt;
                if (_markTimer <= 0f)
                {
                    _markTimer = 0f;
                    _markFraction = 0d;
                }
            }

            if (_dotTimer <= 0f) return 0d;
            _dotTickTimer -= dt;
            _dotTimer -= dt;
            double dealt = 0d;
            if (_dotTickTimer <= 0f)
            {
                float tick = _balance.SKILL_DOT_TICK > 0f ? _balance.SKILL_DOT_TICK : 0.5f;
                _dotTickTimer += tick;
                dealt = _dotDps * tick * DamageTakenMult;
                TakeDamage(dealt);
            }

            if (_dotTimer <= 0f)
            {
                _dotTimer = 0f;
                _dotDps = 0d;
            }

            return dealt;
        }

        private void ClearStatus()
        {
            _stunTimer = 0f;
            _dotTimer = 0f;
            _dotTickTimer = 0f;
            _dotDps = 0d;
            _markTimer = 0f;
            _markFraction = 0d;
        }

        public void Tick(float dt, HeroBrain hero)
        {
            if (!IsAlive || hero == null || hero.State == HeroState.Dead) return;
            if (_balance == null) return;
            if (_stunTimer > 0f) return;

            double dist = X - hero.X;
            if (dist < 0d) dist = -dist;
            if (dist > _balance.ATTACK_RANGE)
            {
                if (State == EnemyState.Attacking) State = EnemyState.Idle;
                return;
            }

            State = EnemyState.Attacking;
            _atkTimer -= dt;
            if (_atkTimer > 0f) return;

            _atkTimer = _atkInterval;
            AttackCount++;
            double damage = DamageCalc.EnemyHit(_balance, Atk, hero.Def);
            hero.ApplyDamage(damage);
        }
    }
}
