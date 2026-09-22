using System;

namespace SoloHero.Core.Common
{
    public sealed class SystemClock : IClock
    {
        public long UtcNowSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public DateTime LocalNow => DateTime.Now;
    }
}
