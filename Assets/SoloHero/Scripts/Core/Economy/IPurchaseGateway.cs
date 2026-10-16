using System;

namespace SoloHero.Core.Economy
{
    public enum PurchaseOutcome
    {
        /// <summary>The store confirmed the payment; the token is its transaction id.</summary>
        Success = 0,
        Cancelled = 1,
        Failed = 2,

        /// <summary>Deferred by the store (parental approval, slow card); the grant comes later through a restore.</summary>
        Pending = 3
    }

    /// <summary>
    /// D-181: the native store behind the shop (Google Play through Unity IAP in release, an instant fake in development
    /// builds). The callback arrives on the main thread. <see cref="PriceLabel"/> is the store's localized price or null
    /// before the store answered.
    /// </summary>
    public interface IPurchaseGateway
    {
        bool IsReady { get; }

        string PriceLabel(IapProduct product);

        void Buy(IapProduct product, Action<PurchaseOutcome, string> onDone);
    }

    /// <summary>A store that is never ready: release builds without the purchasing package, or a failed init.</summary>
    public sealed class NoPurchaseGateway : IPurchaseGateway
    {
        public bool IsReady => false;

        public string PriceLabel(IapProduct product) => null;

        public void Buy(IapProduct product, Action<PurchaseOutcome, string> onDone) => onDone?.Invoke(PurchaseOutcome.Failed, null);
    }
}
