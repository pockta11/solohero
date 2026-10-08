using System;
using System.Globalization;
using Firebase.Database;
using SoloHero.Core.Common;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-133: feeds the server's clock into the <see cref="TrustedClock"/>. Firebase measures the offset between the
    /// server and the device clock at every connection (.info/serverTimeOffset, updated just before .info/connected
    /// turns true), so the clock re-anchors each time a connection is up. Callbacks are posted to the main thread.
    /// </summary>
    public static class ServerTimeSync
    {
        private static TrustedClock _clock;
        private static bool _connected;
        private static bool _hasOffset;
        private static double _offsetMs;

        public static void Start(TrustedClock clock)
        {
            if (clock == null || _clock != null) return;
            _clock = clock;
            try
            {
                FirebaseDatabase database = GameDatabase.Instance;
                database.GetReference(".info/serverTimeOffset").ValueChanged += OnOffsetChanged;
                database.GetReference(".info/connected").ValueChanged += OnConnectedChanged;
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "server time unavailable: " + e.Message);
            }
        }

        private static void OnOffsetChanged(object sender, ValueChangedEventArgs e)
        {
            if (e == null || e.DatabaseError != null || e.Snapshot == null || e.Snapshot.Value == null) return;
            double offset;
            try
            {
                offset = Convert.ToDouble(e.Snapshot.Value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return;
            }

            MainThreadDispatcher.Post(() =>
            {
                _offsetMs = offset;
                _hasOffset = true;
                Apply();
            });
        }

        private static void OnConnectedChanged(object sender, ValueChangedEventArgs e)
        {
            bool connected = e != null && e.DatabaseError == null && e.Snapshot != null && e.Snapshot.Value is bool up && up;
            MainThreadDispatcher.Post(() =>
            {
                _connected = connected;
                Apply();
            });
        }

        private static void Apply()
        {
            if (!_connected || !_hasOffset || _clock == null) return;
            bool first = !_clock.IsServerTime;
            _clock.SyncServer(_offsetMs);
            if (first) Log.Info(LogTag.Boot, "server time known, device offset " + Math.Round(_offsetMs / 1000d) + " s");
        }
    }
}
