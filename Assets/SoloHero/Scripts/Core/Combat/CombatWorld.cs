using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    public sealed class CombatWorld : ICombatWorld
    {
        private readonly BalanceValues _balance;
        private readonly EnemyBrain[] _slots;
        private HeroBrain _hero;

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

        public void BindHero(HeroBrain hero) => _hero = hero;

        public EnemyBrain GetSlot(int index) => _slots[index];

        public void ClearAll()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Deactivate();
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
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Tick(dt, _hero);
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
