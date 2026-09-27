using System;
using System.Collections.Generic;
using Firebase.Analytics;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// Firebase Analytics adapter (E6-15). Only active when Firebase dependencies resolved at boot; otherwise every call
    /// is dropped (local mode, editor without google-services.json). Never throws into gameplay.
    /// </summary>
    public sealed class AnalyticsService : IAnalytics
    {
        private readonly bool _enabled;

        public AnalyticsService(bool firebaseReady)
        {
            _enabled = firebaseReady;
            if (!_enabled) return;
            try
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }
            catch (Exception e)
            {
                _enabled = false;
                Core.Common.Log.Warn(LogTag.Boot, "analytics disabled: " + e.Message);
            }
        }

        public void Log(string eventName, IReadOnlyList<AnalyticsParam> parameters)
        {
            if (!_enabled) return;
            try
            {
                var list = new Parameter[parameters != null ? parameters.Count : 0];
                for (int i = 0; i < list.Length; i++) list[i] = Convert(parameters[i]);
                FirebaseAnalytics.LogEvent(eventName, list);
            }
            catch (Exception e)
            {
                Core.Common.Log.Warn(LogTag.Boot, "analytics event " + eventName + " failed: " + e.Message);
            }
        }

        public void SetUserProperty(string name, string value)
        {
            if (!_enabled) return;
            try
            {
                FirebaseAnalytics.SetUserProperty(name, value);
            }
            catch (Exception e)
            {
                Core.Common.Log.Warn(LogTag.Boot, "analytics property " + name + " failed: " + e.Message);
            }
        }

        private static Parameter Convert(AnalyticsParam p)
        {
            switch (p.Kind)
            {
                case AnalyticsParam.ValueKind.Long: return new Parameter(p.Key, p.LongValue);
                case AnalyticsParam.ValueKind.Double: return new Parameter(p.Key, p.DoubleValue);
                default: return new Parameter(p.Key, p.TextValue);
            }
        }
    }
}
