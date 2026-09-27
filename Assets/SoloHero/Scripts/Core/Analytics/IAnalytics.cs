using System.Collections.Generic;

namespace SoloHero.Core.Analytics
{
    /// <summary>
    /// Event sink for gameplay metrics (E6-15). Firebase Analytics in the game, a recorder in tests, nothing in the
    /// editor without Firebase. Implementations must never throw: analytics may not break play. The parameter list is
    /// reused by the caller after the call returns; copy what you keep.
    /// </summary>
    public interface IAnalytics
    {
        void Log(string eventName, IReadOnlyList<AnalyticsParam> parameters);

        void SetUserProperty(string name, string value);
    }

    public sealed class NullAnalytics : IAnalytics
    {
        public void Log(string eventName, IReadOnlyList<AnalyticsParam> parameters)
        {
        }

        public void SetUserProperty(string name, string value)
        {
        }
    }
}
