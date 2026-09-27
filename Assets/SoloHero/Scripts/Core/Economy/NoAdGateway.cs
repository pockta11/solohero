using System;

namespace SoloHero.Core.Economy
{
    /// <summary>Stand-in when no ad SDK is available: never ready, every show fails, so normal paths are used.</summary>
    public sealed class NoAdGateway : IAdGateway
    {
        public bool IsReady => false;

        public void Show(Action<AdOutcome> onDone) => onDone?.Invoke(AdOutcome.Failed);
    }
}
