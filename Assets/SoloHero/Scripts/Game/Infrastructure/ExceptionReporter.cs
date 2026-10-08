using System.Collections.Generic;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// P1-2: C# exceptions do not crash a Unity app, so Play's Android vitals never sees them. Every distinct exception,
    /// error or failed assert of the session goes to analytics as "app_exception" (message and first stack frame, see
    /// <see cref="ExceptionSummary"/>), at most <see cref="MaxPerSession"/>, so they show up in Firebase - without a new
    /// native SDK (the 16 KB page check stays as it is). Reports wait until analytics is registered at boot.
    /// </summary>
    public static class ExceptionReporter
    {
        public const int MaxPerSession = 10;

        private static readonly object Gate = new object();
        private static readonly Queue<(LogType Type, ExceptionSummary Summary)> Pending = new Queue<(LogType, ExceptionSummary)>();
        private static readonly HashSet<string> Seen = new HashSet<string>();
        private static bool _installed;
        private static int _accepted;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>Sends what waited for analytics (BootSequence calls it once analytics is registered).</summary>
        public static void Flush()
        {
            IAnalytics analytics;
            try
            {
                analytics = Services.Get<IAnalytics>();
            }
            catch (System.InvalidOperationException)
            {
                return;
            }

            while (true)
            {
                (LogType Type, ExceptionSummary Summary) next;
                lock (Gate)
                {
                    if (Pending.Count == 0) return;
                    next = Pending.Dequeue();
                }

                analytics.Log(AnalyticsEvents.AppException, new[]
                {
                    AnalyticsParam.Of(AnalyticsEvents.PKind, next.Type.ToString()),
                    AnalyticsParam.Of(AnalyticsEvents.PMessage, next.Summary.Message),
                    AnalyticsParam.Of(AnalyticsEvents.PWhere, next.Summary.Where)
                });
            }
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            ExceptionSummary summary = ExceptionSummary.From(condition, stackTrace);
            lock (Gate)
            {
                if (_accepted >= MaxPerSession || !Seen.Add(summary.Key)) return;
                _accepted++;
                Pending.Enqueue((type, summary));
            }

            MainThreadDispatcher.Post(Flush);
        }
    }
}
