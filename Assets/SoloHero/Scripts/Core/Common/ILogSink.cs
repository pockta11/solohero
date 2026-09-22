namespace SoloHero.Core.Common
{
    public interface ILogSink
    {
        void Write(LogLevel level, LogTag tag, string message);
    }
}
