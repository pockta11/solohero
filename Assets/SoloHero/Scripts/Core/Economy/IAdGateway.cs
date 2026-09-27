using System;

namespace SoloHero.Core.Economy
{
    /// <summary>Rewarded ad display. Implemented in Game (AdMob); <paramref name="onDone"/> runs on the main thread.</summary>
    public interface IAdGateway
    {
        bool IsReady { get; }

        void Show(Action<AdOutcome> onDone);
    }
}
