using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    public sealed class CombatWorld : ICombatWorld
    {
        /// <summary>D-110: ranged shots in flight at once.</summary>
        public const int ShotCapacity = 16;

        private readonly BalanceValues _balance;
        private readonly EnemyBrain[] _slots;
        private HeroBrain _hero;
        private readonly bool[] _shotActive = new bool[ShotCapacity];
        private readonly double[] _shotFromX = new double[ShotCapacity];
        private readonly float[] _shotTimer = new float[ShotCapacity];
        private readonly float[] _shotDuration = new float[ShotCapacity];
        private readonly double[] _shotDamage = new double[ShotCapacity];
        private readonly int[] _shotLane = new int[ShotCapacity];

        public CombatWorld(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            int cap = _balance.SPAWN_MAX_ALIVE;
            if (cap < 1) cap = 1;
            _slots = new EnemyBrain[cap];
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = new EnemyBrain();
        }

        public double HeroX => _hero != null ? _hero.X : 0d;

        public int SlotCount => _slots.Length;

        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsAlive) n++;
                }

                return n;
            }
        }

        public int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsActive) n++;
                }

                return n;
            }
        }

        /// <summary>Every hero hit on an enemy (basic, crit, skill, burn tick): target, damage, kind.</summary>
        public event Action<EnemyBrain, double, HitKind> HitLanded;

        public void BindHero(HeroBrain hero) => _hero = hero;

        public void ReportHit(EnemyBrain target, double amount, HitKind kind)
        {
            if (target == null) return;
            HitLanded?.Invoke(target, amount, kind);
        }

        public EnemyBrain GetSlot(int index) => _slots[index];

        public void ClearAll()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Deactivate();
            for (int i = 0; i < ShotCapacity; i++)
                _shotActive[i] = false;
        }

        /// <summary>D-110: a ranged shot in flight - its current X and the shooter's spawn index (the view's depth lane).</summary>
        public bool TryGetShot(int index, out double x, out int lane)
        {
            x = 0d;
            lane = 0;
            if (index < 0 || index >= ShotCapacity || !_shotActive[index]) return false;
            float t = _shotDuration[index] > 0f ? 1f - _shotTimer[index] / _shotDuration[index] : 1f;
            if (t < 0f) t = 0f;
            x = _shotFromX[index] + (HeroX - _shotFromX[index]) * t;
            lane = _shotLane[index];
            return true;
        }

        public bool TryActivateSlot(out EnemyBrain brain)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsActive) continue;
                brain = _slots[i];
                return true;
            }

            brain = null;
            return false;
        }

        public double SpawnXAheadOfHero()
        {
            double viewWidth = (double)_balance.PIXEL_REF_WIDTH / _balance.PPU;
            double heroFromLeft = viewWidth * _balance.HERO_SCREEN_X / 100d;
            double distToRight = viewWidth - heroFromLeft;
            return HeroX + distToRight + _balance.SPAWN_OFFSET_X;
        }

        public bool HasEnemyInRange(double range) => NearestEnemyInRange(range) != null;

        public double NearestEnemyDistance()
        {
            EnemyBrain e = NearestEnemyToTheRight();
            if (e == null) return double.PositiveInfinity;
            return e.X - HeroX;
        }

        public EnemyBrain NearestEnemyInRange(double range) =>
            Targeting.FindNearestInRange(this, HeroX, range);

        public EnemyBrain NearestEnemyToTheRight() =>
            Targeting.FindNearestToTheRight(this, HeroX);

        public int CountEnemiesInRange(double range) =>
            Targeting.CountInRange(this, HeroX, range);

        public void TickEnemies(float dt)
        {
            if (_hero == null) return;
            AssignStands();
            for (int i = 0; i < _slots.Length; i++)
            {
                EnemyBrain e = _slots[i];
                double dot = e.TickStatus(dt);
                if (dot > 0d) ReportHit(e, dot, HitKind.Dot);
                double shot = e.Tick(dt, _hero);
                if (shot > 0d) Launch(e, shot);
            }

            TickShots(dt);
        }

        /// <summary>
        /// D-110: the nearest ENEMY_FRONT_SLOTS walkers get the fighting spots in front of the hero, the rest queue
        /// behind them in order; shooters keep their range and the boss its own spot. Allocation free.
        /// </summary>
        private void AssignStands()
        {
            int front = _balance.ENEMY_FRONT_SLOTS < 1 ? 1 : _balance.ENEMY_FRONT_SLOTS;
            double lastFront = _balance.ENEMY_STAND_MIN + (front - 1) * _balance.ENEMY_STAND_STEP;
            for (int i = 0; i < _slots.Length; i++)
            {
                EnemyBrain e = _slots[i];
                if (!e.IsAlive) continue;
                if (e.IsBoss)
                {
                    e.Stand = _balance.BOSS_STAND;
                    continue;
                }

                if (e.Role == EnemyRole.Ranged)
                {
                    e.Stand = _balance.ENEMY_RANGED_STAND;
                    continue;
                }

                int rank = 0;
                for (int j = 0; j < _slots.Length; j++)
                {
                    if (j == i) continue;
                    EnemyBrain o = _slots[j];
                    if (!o.IsAlive || o.IsBoss || o.Role == EnemyRole.Ranged) continue;
                    if (o.X < e.X || (o.X == e.X && j < i)) rank++;
                }

                e.Stand = rank < front
                    ? _balance.ENEMY_STAND_MIN + rank * _balance.ENEMY_STAND_STEP
                    : lastFront + (rank - front + 1) * _balance.ENEMY_QUEUE_SPACING;
            }
        }

        private void Launch(EnemyBrain shooter, double damage)
        {
            double dist = shooter.X - HeroX;
            if (dist < 0d) dist = -dist;
            float duration = _balance.ENEMY_PROJECTILE_SPEED > 0d ? (float)(dist / _balance.ENEMY_PROJECTILE_SPEED) : 0f;
            if (duration < 0.05f)
            {
                _hero.ApplyDamage(damage);
                return;
            }

            for (int i = 0; i < ShotCapacity; i++)
            {
                if (_shotActive[i]) continue;
                _shotActive[i] = true;
                _shotFromX[i] = shooter.X;
                _shotTimer[i] = duration;
                _shotDuration[i] = duration;
                _shotDamage[i] = damage;
                _shotLane[i] = shooter.SpawnIndex;
                return;
            }

            _hero.ApplyDamage(damage);
        }

        /// <summary>Shots land when their flight time is up; a shot lands even if its shooter fell meanwhile.</summary>
        private void TickShots(float dt)
        {
            bool heroDown = _hero.State == HeroState.Dead;
            for (int i = 0; i < ShotCapacity; i++)
            {
                if (!_shotActive[i]) continue;
                if (heroDown)
                {
                    _shotActive[i] = false;
                    continue;
                }

                _shotTimer[i] -= dt;
                if (_shotTimer[i] > 0f) continue;
                _shotActive[i] = false;
                _hero.ApplyDamage(_shotDamage[i]);
            }
        }

        public int ResolveDeaths()
        {
            int kills = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                EnemyBrain e = _slots[i];
                if (!e.IsActive) continue;
                if (e.State != EnemyState.Dead && e.Hp > 0d) continue;
                e.Deactivate();
                kills++;
            }

            return kills;
        }
    }
}
