using SoloHero.Core.Config;

namespace SoloHero.Core.Stage
{
    /// <summary>D-175 (L-14): the raid boss's moves, in this order.</summary>
    public enum RaidPattern
    {
        None = 0,

        /// <summary>A ground slam: RAID_SLAM_MULT x its hit to a hero within RAID_SLAM_RANGE (ranged heroes stand clear).</summary>
        Slam = 1,

        /// <summary>A charge: RAID_CHARGE_MULT x its hit wherever the hero stands.</summary>
        Charge = 2,

        /// <summary>Calls RAID_SUMMON_COUNT minions of its stage.</summary>
        Summon = 3
    }

    /// <summary>
    /// D-175 (L-14) the raid boss's pattern clock (genre "boss patterns", X-axis rules only): every RAID_PATTERN_INTERVAL
    /// seconds it starts the next move with a RAID_TELEGRAPH-second warning the view draws, then the move lands. A stunned
    /// boss's clock waits. The runner applies what lands.
    /// </summary>
    public sealed class RaidPatterns
    {
        private static readonly RaidPattern[] Cycle = { RaidPattern.Slam, RaidPattern.Charge, RaidPattern.Summon };

        private BalanceValues _balance;
        private float _timer;
        private float _warn;
        private int _next;

        /// <summary>The move being warned of, or None.</summary>
        public RaidPattern Warning { get; private set; }

        /// <summary>0 when the warning starts, 1 when the move lands.</summary>
        public float WarningProgress => Warning == RaidPattern.None || _balance == null || _balance.RAID_TELEGRAPH <= 0f
            ? 0f : 1f - _warn / _balance.RAID_TELEGRAPH;

        public void Reset(BalanceValues balance)
        {
            _balance = balance;
            _timer = balance.RAID_PATTERN_INTERVAL * 0.6f;
            _warn = 0f;
            _next = 0;
            Warning = RaidPattern.None;
        }

        /// <summary>
        /// Advances the clock. Returns the move that lands this tick (or None); <paramref name="warned"/> is the move whose
        /// warning started this tick.
        /// </summary>
        public RaidPattern Tick(float dt, bool canAct, out RaidPattern warned)
        {
            warned = RaidPattern.None;
            if (_balance == null) return RaidPattern.None;
            if (Warning != RaidPattern.None)
            {
                _warn -= dt;
                if (_warn > 0f) return RaidPattern.None;
                RaidPattern landed = Warning;
                Warning = RaidPattern.None;
                _timer = _balance.RAID_PATTERN_INTERVAL;
                return landed;
            }

            if (!canAct) return RaidPattern.None;
            _timer -= dt;
            if (_timer > 0f) return RaidPattern.None;
            Warning = Cycle[_next % Cycle.Length];
            _next++;
            _warn = _balance.RAID_TELEGRAPH;
            warned = Warning;
            return RaidPattern.None;
        }
    }
}
