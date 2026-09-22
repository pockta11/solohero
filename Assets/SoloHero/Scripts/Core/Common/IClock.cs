using System;

namespace SoloHero.Core.Common
{
    public interface IClock
    {
        long UtcNowSeconds { get; }
        DateTime LocalNow { get; }
    }
}
