using System;
using System.Threading.Tasks;

namespace SoloHero.Core.Economy
{
    public enum PurchaseOutcome
    {
        /// <summary>The purchase was verified and delivered (paid now, or paid before).</summary>
        Success = 0,
        Cancelled = 1,
        Failed = 2,

        /// <summary>
        /// Not delivered yet: deferred by the store (parental approval, slow payment) or the purchase record could not be
        /// written (no network). The store keeps the purchase open and it is delivered later - never lost.
        /// </summary>
        Pending = 3
    }

    /// <summary>D-183: what the game did with a verified store purchase - which tells the store whether to close it.</summary>
    public enum DeliveryResult
    {
        /// <summary>Paid now and saved: the store may close (consume) the purchase.</summary>
        Granted = 0,

        /// <summary>Paid before (a restore, or our own receipt again): close it without paying.</summary>
        AlreadyGranted = 1,

        /// <summary>The purchase record could not be reached: keep the purchase open and try again later.</summary>
        RetryLater = 2,

        /// <summary>
        /// Not this player's to receive (the receipt is recorded for another account - a replayed receipt) or not one of
        /// our products: keep it open and never pay. Google refunds a purchase nobody closes within three days.
        /// </summary>
        Rejected = 3,

        /// <summary>The same purchase is being delivered right now; this report changes nothing.</summary>
        InProgress = 4
    }

    /// <summary>D-183: delivers one verified purchase (product and the store's purchase token).</summary>
    public delegate Task<DeliveryResult> PurchaseDelivery(IapProduct product, string token);

    /// <summary>
    /// D-181 / D-183: the native store behind the shop (Google Play through Unity IAP in release, an instant fake in
    /// development builds). The store verifies each purchase's receipt (release: the Google Play signature) and hands
    /// it to the delivery attached by <see cref="PurchaseService"/>; it closes the purchase only when that delivery
    /// says so. Every callback arrives on the main thread. <see cref="PriceLabel"/> is the store's localized price, or
    /// null before the store answered.
    /// </summary>
    public interface IPurchaseGateway
    {
        bool IsReady { get; }

        string PriceLabel(IapProduct product);

        /// <summary>The delivery every verified purchase goes to - the one started here and any the store restores.</summary>
        void Attach(PurchaseDelivery deliver);

        /// <summary>Starts a purchase; <paramref name="onDone"/> tells how this purchase flow ended.</summary>
        void Buy(IapProduct product, Action<PurchaseOutcome> onDone);
    }

    /// <summary>A store that is never ready: release builds without the purchasing package, or a failed init.</summary>
    public sealed class NoPurchaseGateway : IPurchaseGateway
    {
        public bool IsReady => false;

        public string PriceLabel(IapProduct product) => null;

        public void Attach(PurchaseDelivery deliver)
        {
        }

        public void Buy(IapProduct product, Action<PurchaseOutcome> onDone) => onDone?.Invoke(PurchaseOutcome.Failed);
    }
}
