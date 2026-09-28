using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    /// <summary>
    /// Wave spawning (D-081, genre "mob pack"): enemies come in waves of SPAWN_WAVE_SIZE that appear together, lined
    /// up SPAWN_WAVE_SPACING apart. The next wave waits until the field is clear, then SPAWN_WAVE_GAP seconds.
    /// The first wave of a stage comes at once. Never more than SPAWN_MAX_ALIVE alive.
    /// </summary>
    public sealed class SpawnScheduler
    {
        private readonly BalanceValues _balance;
        private float _timer;
        private int _spawned;
        private int _killTarget;
        private bool _isBoss;
        private int _waveLeft;
        private int _waveSize;
        private bool _gapArmed;

        public SpawnScheduler(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        public int Spawned => _spawned;
        public int KillTarget => _killTarget;
        public int Remaining => _killTarget - _spawned;
        public bool IsBoss => _isBoss;

        /// <summary>Position of the last spawned enemy within its wave (0 = front).</summary>
        public int WaveSlot => _waveSize - _waveLeft - 1;

        public void Reset(int killTarget, bool isBoss)
        {
            _killTarget = killTarget < 0 ? 0 : killTarget;
            _isBoss = isBoss;
            _spawned = 0;
            _timer = 0f;
            _waveLeft = 0;
            _waveSize = 0;
            _gapArmed = false;
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

            if (_waveLeft == 0)
            {
                if (aliveCount > 0)
                {
                    _gapArmed = false;
                    return false;
                }

                if (!_gapArmed)
                {
                    _gapArmed = true;
                    _timer = _spawned == 0 ? 0f : _balance.SPAWN_WAVE_GAP;
                }

                if (_timer > 0f) return false;
                _gapArmed = false;
                int size = _balance.SPAWN_WAVE_SIZE < 1 ? 1 : _balance.SPAWN_WAVE_SIZE;
                _waveSize = Math.Min(size, _killTarget - _spawned);
                _waveLeft = _waveSize;
            }

            _waveLeft--;
            _spawned++;
            return true;
        }
    }
}
