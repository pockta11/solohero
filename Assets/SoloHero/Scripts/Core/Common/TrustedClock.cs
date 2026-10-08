using System;

namespace SoloHero.Core.Common
{
    /// <summary>
    /// D-133: wall-clock time that moving the device clock does not change. Time is an anchor (a known UTC second) plus
    /// the monotonic seconds counted since it. The clock anchors to the device clock when it is created and to the
    /// server's clock each time <see cref="SyncServer"/> reports the server offset (Firebase .info/serverTimeOffset,
    /// measured at every connection); between anchors the device clock is not read, so changing it while the game runs
    /// or sits in the background has no effect. The monotonic source must keep counting while the device sleeps
    /// (Android SystemClock.elapsedRealtime), or time spent in the background would be lost.
    /// </summary>
    public sealed class TrustedClock : IClock
    {
        private const long MaxUnixSeconds = 253402300799L;

        private readonly IClock _device;
        private readonly Func<double> _monotonic;
        private long _anchorUtc;
        private double _anchorMonotonic;

        public TrustedClock(IClock device, Func<double> monotonicSeconds)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _monotonic = monotonicSeconds ?? throw new ArgumentNullException(nameof(monotonicSeconds));
            _anchorUtc = device.UtcNowSeconds;
            _anchorMonotonic = _monotonic();
        }

        /// <summary>Raised once, when the server's clock first becomes known.</summary>
        public event Action ServerTimeKnown;

        /// <summary>True once the server's clock is known; before that the time is the device clock at start-up.</summary>
        public bool IsServerTime { get; private set; }

        /// <summary>Whether a server clock is coming (signed in with Firebase). False in local mode, where the device clock is all there is.</summary>
        public bool ServerExpected { get; set; } = true;

        /// <summary>Rewards may be measured now: the server's clock is known, or there is no server to wait for.</summary>
        public bool IsTrusted => IsServerTime || !ServerExpected;

        public double MonotonicSeconds => _monotonic();

        public long UtcNowSeconds => UtcAt(_monotonic());

        public DateTime LocalNow => ToLocal(UtcNowSeconds);

        /// <summary>The time at another monotonic reading (for example when the app came back), from the current anchor.</summary>
        public long UtcAt(double monotonicSeconds) => _anchorUtc + (long)Math.Floor(monotonicSeconds - _anchorMonotonic);

        /// <summary>Anchors to the server: server time = device time + <paramref name="offsetMs"/>.</summary>
        public void SyncServer(double offsetMs)
        {
            if (double.IsNaN(offsetMs) || double.IsInfinity(offsetMs)) return;
            _anchorUtc = _device.UtcNowSeconds + (long)Math.Round(offsetMs / 1000d);
            _anchorMonotonic = _monotonic();
            if (IsServerTime) return;
            IsServerTime = true;
            ServerTimeKnown?.Invoke();
        }

        private static DateTime ToLocal(long utcSeconds)
        {
            long clamped = utcSeconds < 0L ? 0L : utcSeconds > MaxUnixSeconds ? MaxUnixSeconds : utcSeconds;
            return DateTimeOffset.FromUnixTimeSeconds(clamped).ToLocalTime().DateTime;
        }
    }
}
