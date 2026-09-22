using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    public sealed class SpawnScheduler
    {
        private readonly BalanceValues _balance;
        private float _timer;
        private int _spawned;
        private int _killTarget;
        private bool _isBoss;

        public SpawnScheduler(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        public int Spawned => _spawned;
        public int KillTarget => _killTarget;
        public int Remaining => _killTarget - _spawned;
        public bool IsBoss => _isBoss;

        public void Reset(int killTarget, bool isBoss)
        {
            _killTarget = killTarget < 0 ? 0 : killTarget;
            _isBoss = isBoss;
            _spawned = 0;
            _timer = 0f;
        }

        public void Tick(float dt)
        {
            if (_spawned >= _killTarget) return;
            _timer -= dt;
            if (_timer < 0f) _timer = 0f;
        }

        public bool TryConsumeSpawn(int aliveCount)
        {
            if (_spawned >= _killTarget) return false;
            if (aliveCount >= _balance.SPAWN_MAX_ALIVE) return false;
            if (_timer > 0f) return false;

            _spawned++;
            _timer = _balance.SPAWN_INTERVAL;
            return true;
        }
    }
}
