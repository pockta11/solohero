using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;

namespace SoloHero.Core.Combat
{
    public sealed class HeroBrain
    {
        private readonly BalanceValues _balance;
        private readonly IRandom _random;
        private HeroStats _stats;
        private float _attackTimer;
        private bool _attackPending;

        public HeroBrain(BalanceValues balance, IRandom random, HeroStats stats)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Reset(stats);
        }

        public HeroState State { get; private set; }
        public double X { get; private set; }
        public double Hp { get; private set; }
        public double MaxHp => _stats.Hp;
        public double Def => _stats.Def;
        public HeroStats Stats => _stats;

        public event Action<HeroState> StateChanged;
        public event Action AttackRequested;
        public event Action<double, bool> DealtDamage;

        public void Reset(HeroStats stats)
        {
            _stats = stats;
            Hp = stats.Hp;
            X = 0d;
            _attackTimer = 0f;
            _attackPending = false;
            SetState(HeroState.Advance);
        }

        public void SetStats(HeroStats stats)
        {
            _stats = stats;
            if (Hp > stats.Hp) Hp = stats.Hp;
            if (Hp < 0d) Hp = 0d;
        }

        public void Tick(float dt, ICombatWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            switch (State)
            {
                case HeroState.Advance:
                    TickAdvance(dt, world);
                    break;
                case HeroState.Engage:
                    TickEngage(dt, world);
                    break;
                case HeroState.Hit:
                    SetState(world.HasEnemyInRange(_balance.ATTACK_RANGE) ? HeroState.Engage : HeroState.Advance);
                    break;
                case HeroState.Skill:
                    break;
                case HeroState.Dead:
                    break;
            }
        }

        public void OnHitFrame(ICombatWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (State != HeroState.Engage && State != HeroState.Advance) return;
            if (!_attackPending) return;

            _attackPending = false;
            EnemyBrain target = world.NearestEnemyInRange(_balance.ATTACK_RANGE);
            if (target == null) return;

            bool crit = DamageCalc.RollCrit(_stats, _random);
            double dmg = DamageCalc.HeroHit(_stats, crit, _balance);
            target.TakeDamage(dmg);
            DealtDamage?.Invoke(dmg, crit);
        }

        public void ApplyDamage(double amount)
        {
            if (State == HeroState.Dead) return;
            if (amount < 0d) amount = 0d;
            Hp -= amount;
            if (Hp <= 0d)
            {
                Hp = 0d;
                _attackPending = false;
                SetState(HeroState.Dead);
                return;
            }

            if (State != HeroState.Skill)
                SetState(HeroState.Hit);
        }

        public void Heal(double amount)
        {
            if (State == HeroState.Dead) return;
            if (amount < 0d) amount = 0d;
            Hp += amount;
            if (Hp > MaxHp) Hp = MaxHp;
        }

        public Result TryBeginSkill()
        {
            if (State == HeroState.Dead) return Result.Fail(FailReason.Busy);
            if (State == HeroState.Skill) return Result.Fail(FailReason.Busy);
            _attackPending = false;
            SetState(HeroState.Skill);
            return Result.Success;
        }

        public void EndSkill(ICombatWorld world)
        {
            if (State != HeroState.Skill) return;
            if (Hp <= 0d)
            {
                SetState(HeroState.Dead);
                return;
            }

            SetState(world != null && world.HasEnemyInRange(_balance.ATTACK_RANGE)
                ? HeroState.Engage
                : HeroState.Advance);
        }

        public void Kill()
        {
            Hp = 0d;
            _attackPending = false;
            SetState(HeroState.Dead);
        }

        private void TickAdvance(float dt, ICombatWorld world)
        {
            if (world.HasEnemyInRange(_balance.ATTACK_RANGE))
            {
                SetState(HeroState.Engage);
                return;
            }

            X += _balance.MOVE_SPEED * dt;
        }

        private void TickEngage(float dt, ICombatWorld world)
        {
            if (!world.HasEnemyInRange(_balance.ATTACK_RANGE))
            {
                SetState(HeroState.Advance);
                return;
            }

            _attackTimer -= dt;
            if (_attackTimer > 0f) return;

            double spd = _stats.AtkSpd;
            _attackTimer = spd > 0d ? (float)(1d / spd) : float.MaxValue;
            _attackPending = true;
            AttackRequested?.Invoke();
        }

        private void SetState(HeroState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
