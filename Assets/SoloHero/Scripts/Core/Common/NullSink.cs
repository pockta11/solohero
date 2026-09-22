namespace SoloHero.Core.Common
{
    public sealed class NullSink : ILogSink
    {
        public static readonly NullSink Instance = new NullSink();

        private NullSink() { }

        public void Write(LogLevel level, LogTag tag, string message) { }
    }
}
