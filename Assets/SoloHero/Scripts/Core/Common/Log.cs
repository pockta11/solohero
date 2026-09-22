using System.Diagnostics;

namespace SoloHero.Core.Common
{
    public static class Log
    {
        public static ILogSink Sink = NullSink.Instance;

        public static void Error(LogTag tag, string message) => Sink.Write(LogLevel.Error, tag, message);

        public static void Warn(LogTag tag, string message) => Sink.Write(LogLevel.Warn, tag, message);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(LogTag tag, string message) => Sink.Write(LogLevel.Info, tag, message);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(LogTag tag, string message) => Sink.Write(LogLevel.Debug, tag, message);
    }
}
