using System;
using System.Collections.Generic;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using UnityEngine;
#if SOLOHERO_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace SoloHero.Game.Infrastructure
{
    /// <summary>D-181 / D-183: picks the store adapter and the purchase ledger for this build.</summary>
    public static class PurchaseGateways
    {
        /// <summary>
        /// The fake store in the editor, development builds and QA builds without the ad SDK (<paramref name="qaBuild"/>,
        /// never shipped - the emulator has no Play Store); the real store when the purchasing package is in; else none.
        /// </summary>
        public static IPurchaseGateway Create(GameObject host, bool qaBuild)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new FakePurchaseGateway();
#else
            if (qaBuild) return new FakePurchaseGateway();
#if SOLOHERO_IAP
            return host != null ? host.AddComponent<StorePurchaseGateway>() : (IPurchaseGateway)new NoPurchaseGateway();
#else
            return new NoPurchaseGateway();
#endif
#endif
        }

        /// <summary>
        /// D-183: the server's purchase records for the real store; the fake store's receipts are made up, so a memory
        /// ledger stands in for it.
        /// </summary>
        public static IPurchaseLedger CreateLedger(string userId, bool qaBuild)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new LocalPurchaseLedger();
#else
            if (qaBuild) return new LocalPurchaseLedger();
            return new FirebasePurchaseLedger(userId);
#endif
        }

        /// <summary>D-183: one analytics line for a purchase the store reported but the game did not pay (reason).</summary>
        internal static void ReportCheck(string product, string reason)
        {
            Log.Warn(LogTag.Boot, "purchase not delivered: " + product + " " + reason);
            try
            {
                SoloHero.Game.UI.Common.GameAnalytics.Log(AnalyticsEvents.PurchaseCheck,
                    AnalyticsParam.Of(AnalyticsEvents.PProduct, product ?? ""),
                    AnalyticsParam.Of(AnalyticsEvents.PReason, reason ?? ""));
            }
            catch (Exception)
            {
                // Analytics never stands in the way of a purchase.
            }
        }
    }

    /// <summary>
    /// Development builds and the editor: every purchase succeeds at once with a unique token and goes through the same
    /// delivery as a real one, so the shop flow, the grants and the VIP perks can be checked on the emulator (which has
    /// no Play Store). Never in a release build.
    /// </summary>
    public sealed class FakePurchaseGateway : IPurchaseGateway
    {
        private PurchaseDelivery _deliver;

        public bool IsReady => true;

        public string PriceLabel(IapProduct product) => null;

        public void Attach(PurchaseDelivery deliver) => _deliver = deliver;

        public async void Buy(IapProduct product, Action<PurchaseOutcome> onDone)
        {
            PurchaseOutcome outcome = PurchaseOutcome.Failed;
            try
            {
                if (_deliver != null)
                {
                    DeliveryResult result = await _deliver(product, "fake-" + Guid.NewGuid().ToString("N"));
                    outcome = result == DeliveryResult.Granted || result == DeliveryResult.AlreadyGranted ? PurchaseOutcome.Success
                        : result == DeliveryResult.RetryLater ? PurchaseOutcome.Pending : PurchaseOutcome.Failed;
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "fake purchase failed: " + e.Message);
            }

            onDone?.Invoke(outcome);
        }
    }

#if SOLOHERO_IAP
    /// <summary>
    /// Google Play through Unity IAP (consumable products from IapCatalog). D-183, in order for every purchase the store
    /// reports - the one bought now, one bought before a crash, a deferred one that cleared:
    /// (1) a deferred (pending) payment is left open and paid when Google says it cleared;
    /// (2) the receipt must carry Google Play's signature for this app (<see cref="ReceiptCheck"/>) - a fake billing app
    ///     cannot make one;
    /// (3) PurchaseService records the token in the purchase ledger (one account per purchase), pays and saves;
    /// (4) only then the purchase is confirmed (consumed). Anything not paid stays open: no network = tried again every
    ///     <see cref="RetrySeconds"/> and on the next start; not ours = never paid, and Google refunds it after three days.
    /// Store callbacks are run on the main thread.
    /// </summary>
    public sealed class StorePurchaseGateway : MonoBehaviour, IPurchaseGateway, IDetailedStoreListener
    {
        private const float RetrySeconds = 30f;

        /// <summary>
        /// Back from the purchase sheet with no word from the store this long (some failures arrive under a purchase
        /// token the store cannot match, and are dropped): the shop's flow ends as Pending so it never stays frozen.
        /// </summary>
        private const float SilentAfterResumeSeconds = 20f;

        /// <summary>A purchase flow never waits longer than this, resumed or not.</summary>
        private const float FlowCapSeconds = 600f;

        private IStoreController _controller;
        private IGooglePlayStoreExtensions _google;
        private PurchaseDelivery _deliver;
        private Action<PurchaseOutcome> _pending;
        private IapProduct _pendingProduct;
        private bool _initializing;
        private float _retryIn;
        private float _pendingSince;
        private float _resumedAt = -1f;
        private readonly List<Product> _retry = new List<Product>();
        private readonly HashSet<string> _working = new HashSet<string>();

        public bool IsReady => _controller != null && ReceiptCheck.HasKey;

        public void Attach(PurchaseDelivery deliver) => _deliver = deliver;

        private void Start() => InitializeAsync();

        private async void InitializeAsync()
        {
            if (_initializing || _controller != null) return;
            _initializing = true;
            if (!ReceiptCheck.HasKey) Log.Error(LogTag.Boot, "store: no Google Play license key in this build, purchases stay closed");
            try
            {
                await Unity.Services.Core.UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                // Without a linked Unity project the services core cannot start; the Google Play store still can.
                Log.Warn(LogTag.Boot, "unity services init skipped: " + e.Message);
            }

            try
            {
                ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (IapProductDef def in IapCatalog.All) builder.AddProduct(def.StoreId, ProductType.Consumable);
                // A pending payment (cash, parental approval) is announced here, not through ProcessPurchase: end the
                // shop's flow as Pending. Google calls ProcessPurchase once it clears, and only then is it paid.
                builder.Configure<IGooglePlayConfiguration>().SetDeferredPurchaseListener(OnDeferred);
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "store init failed: " + e.Message);
                _initializing = false;
            }
        }

        public string PriceLabel(IapProduct product)
        {
            IapProductDef def = IapCatalog.Find(product);
            if (_controller == null || def == null) return null;
            Product item = _controller.products.WithID(def.StoreId);
            return item != null && item.availableToPurchase ? item.metadata.localizedPriceString : null;
        }

        public void Buy(IapProduct product, Action<PurchaseOutcome> onDone)
        {
            IapProductDef def = IapCatalog.Find(product);
            Product item = _controller != null && def != null ? _controller.products.WithID(def.StoreId) : null;
            if (!IsReady || item == null || !item.availableToPurchase || _pending != null)
            {
                onDone?.Invoke(PurchaseOutcome.Failed);
                return;
            }

            _pending = onDone;
            _pendingProduct = product;
            _pendingSince = Time.realtimeSinceStartup;
            _resumedAt = -1f;
            try
            {
                _controller.InitiatePurchase(item);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "purchase not started: " + e.Message);
                Finish(product, PurchaseOutcome.Failed);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _initializing = false;
            try
            {
                _google = extensions.GetExtension<IGooglePlayStoreExtensions>();
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "google play extensions unavailable: " + e.Message);
            }

            Log.Info(LogTag.Boot, "store ready");
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            _initializing = false;
            Log.Warn(LogTag.Boot, "store init failed: " + error + " " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Product product = args?.purchasedProduct;
            if (product == null) return PurchaseProcessingResult.Pending;
            if (IsDeferred(product))
            {
                // Paid later (parental approval, cash payment): Google calls again once it clears. Nothing is paid now.
                OnDeferred(product);
                return PurchaseProcessingResult.Pending;
            }

            MainThreadDispatcher.Post(() => Handle(product));
            return PurchaseProcessingResult.Pending;
        }

        private void OnDeferred(Product product)
        {
            IapProductDef deferred = product != null ? IapCatalog.FindByStoreId(product.definition.id) : null;
            if (deferred != null) MainThreadDispatcher.Post(() => Finish(deferred.Product, PurchaseOutcome.Pending));
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && _pending != null) _resumedAt = Time.realtimeSinceStartup;
        }

        private bool IsDeferred(Product product)
        {
            try
            {
                return _google != null && _google.IsPurchasedProductDeferred(product);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "deferred check failed: " + e.Message);
                return false;
            }
        }

        private async void Handle(Product product)
        {
            IapProductDef def = IapCatalog.FindByStoreId(product.definition.id);
            if (def == null)
            {
                PurchaseGateways.ReportCheck(product.definition.id, "unknown_product");
                return;
            }

            string work = string.IsNullOrEmpty(product.transactionID) ? def.StoreId : product.transactionID;
            if (!_working.Add(work)) return;
            try
            {
                if (!ReceiptCheck.TryVerify(product, def, out string token, out string reason))
                {
                    PurchaseGateways.ReportCheck(def.Id, "receipt_" + reason);
                    Finish(def.Product, PurchaseOutcome.Failed);
                    return;
                }

                if (_deliver == null)
                {
                    Retry(product);
                    Finish(def.Product, PurchaseOutcome.Pending);
                    return;
                }

                DeliveryResult result = await _deliver(def.Product, token);
                switch (result)
                {
                    case DeliveryResult.Granted:
                    case DeliveryResult.AlreadyGranted:
                        Confirm(product);
                        Finish(def.Product, PurchaseOutcome.Success);
                        break;
                    case DeliveryResult.RetryLater:
                        Retry(product);
                        Finish(def.Product, PurchaseOutcome.Pending);
                        break;
                    case DeliveryResult.Rejected:
                        PurchaseGateways.ReportCheck(def.Id, "rejected");
                        Finish(def.Product, PurchaseOutcome.Failed);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "purchase delivery failed, retrying: " + e.Message);
                Retry(product);
                Finish(def.Product, PurchaseOutcome.Pending);
            }
            finally
            {
                _working.Remove(work);
            }
        }

        private void Confirm(Product product)
        {
            try
            {
                _controller?.ConfirmPendingPurchase(product);
            }
            catch (Exception e)
            {
                // Paid and saved already; the store reports it again on the next start and the token history pays nothing.
                Log.Warn(LogTag.Boot, "purchase not confirmed: " + e.Message);
            }
        }

        private void Retry(Product product)
        {
            if (!_retry.Contains(product)) _retry.Add(product);
            if (_retryIn <= 0f) _retryIn = RetrySeconds;
        }

        private void Update()
        {
            // The shop's flow never stays frozen: back from the sheet with silence (or far too long), it ends as Pending.
            if (_pending != null && _working.Count == 0)
            {
                float now = Time.realtimeSinceStartup;
                if ((_resumedAt >= 0f && now - _resumedAt > SilentAfterResumeSeconds) || now - _pendingSince > FlowCapSeconds)
                {
                    Log.Warn(LogTag.Boot, "purchase flow ended without a store answer, left pending");
                    Finish(_pendingProduct, PurchaseOutcome.Pending);
                }
            }

            if (_retry.Count == 0) return;
            _retryIn -= Time.unscaledDeltaTime;
            if (_retryIn > 0f) return;
            _retryIn = RetrySeconds;
            Product[] batch = _retry.ToArray();
            _retry.Clear();
            foreach (Product product in batch) Handle(product);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) =>
            FailFor(product, failureReason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) =>
            FailFor(product, failureDescription != null && failureDescription.reason == PurchaseFailureReason.UserCancelled
                ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);

        private void FailFor(Product product, PurchaseOutcome outcome)
        {
            IapProductDef def = product != null ? IapCatalog.FindByStoreId(product.definition.id) : null;
            MainThreadDispatcher.Post(() =>
            {
                if (def != null) Finish(def.Product, outcome);
                else if (_pending != null) Finish(_pendingProduct, outcome);
            });
        }

        /// <summary>Ends the purchase flow started from the shop, if this is its product.</summary>
        private void Finish(IapProduct product, PurchaseOutcome outcome)
        {
            if (_pending == null || product != _pendingProduct) return;
            Action<PurchaseOutcome> callback = _pending;
            _pending = null;
            callback(outcome);
        }
    }
#endif
}
