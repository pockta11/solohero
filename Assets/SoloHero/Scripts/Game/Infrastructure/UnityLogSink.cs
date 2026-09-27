using SoloHero.Core.Common;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>Routes Core <see cref="Log"/> to the Unity console / logcat as "[Tag] message".</summary>
    public sealed class UnityLogSink : ILogSink
    {
        public void Write(LogLevel level, LogTag tag, string message)
        {
            string line = "[" + tag + "] " + message;
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(line);
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning(line);
                    break;
                default:
                    Debug.Log(line);
                    break;
            }
        }
    }
}
