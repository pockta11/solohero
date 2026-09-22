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

        public EnemyState State { get; private set; }
        public double Hp { get; private set; }
        public double MaxHp { get; private set; }
        public double Atk { get; private set; }
        public double X { get; private set; }
        public bool IsBoss { get; private set; }

        public bool IsActive => _active;
        public bool IsAlive => _active && State != EnemyState.Dead && Hp > 0d;

        public void Reset(BalanceValues balance, double maxHp, double atk, float atkInterval, double x, bool isBoss)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            MaxHp = maxHp;
            Hp = maxHp;
            Atk = atk;
            _atkInterval = atkInterval;
            _atkTimer = atkInterval;
            X = x;
            IsBoss = isBoss;
            State = EnemyState.Idle;
            _active = true;
        }

        public void Deactivate()
        {
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

        public void Tick(float dt, HeroBrain hero)
        {
            if (!IsAlive || hero == null || hero.State == HeroState.Dead) return;
            if (_balance == null) return;

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
            double damage = DamageCalc.EnemyHit(_balance, Atk, hero.Def);
            hero.ApplyDamage(damage);
        }
    }
}
