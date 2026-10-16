using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using UnityEngine;
#if SOLOHERO_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace SoloHero.Game.Infrastructure
{
    /// <summary>D-181: picks the store adapter for this build.</summary>
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
    }

    /// <summary>
    /// Development builds and the editor: every purchase succeeds at once with a unique token, so the shop flow, the
    /// grants and the VIP perks can be checked on the emulator (which has no Play Store). Never in a release build.
    /// </summary>
    public sealed class FakePurchaseGateway : IPurchaseGateway
    {
        public bool IsReady => true;

        public string PriceLabel(IapProduct product) => null;

        public void Buy(IapProduct product, Action<PurchaseOutcome, string> onDone) =>
            onDone?.Invoke(PurchaseOutcome.Success, "fake-" + Guid.NewGuid().ToString("N"));
    }

#if SOLOHERO_IAP
    /// <summary>
    /// Google Play through Unity IAP (consumable products from IapCatalog). Initializes on Start; store callbacks are
    /// routed to the main thread. A purchase that arrives without a pending Buy (a restore after a crash, a deferred
    /// payment) is granted straight to the PurchaseService, whose token history keeps it from paying twice.
    /// </summary>
    public sealed class StorePurchaseGateway : MonoBehaviour, IPurchaseGateway, IDetailedStoreListener
    {
        private IStoreController _controller;
        private Action<PurchaseOutcome, string> _pending;
        private IapProduct _pendingProduct;
        private bool _initializing;

        public bool IsReady => _controller != null;

        private void Start() => InitializeAsync();

        private async void InitializeAsync()
        {
            if (_initializing || _controller != null) return;
            _initializing = true;
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

        public void Buy(IapProduct product, Action<PurchaseOutcome, string> onDone)
        {
            IapProductDef def = IapCatalog.Find(product);
            if (_controller == null || def == null)
            {
                onDone?.Invoke(PurchaseOutcome.Failed, null);
                return;
            }

            Product item = _controller.products.WithID(def.StoreId);
            if (item == null || !item.availableToPurchase)
            {
                onDone?.Invoke(PurchaseOutcome.Failed, null);
                return;
            }

            _pending = onDone;
            _pendingProduct = product;
            _controller.InitiatePurchase(item);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _initializing = false;
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
            string storeId = args.purchasedProduct.definition.id;
            string token = args.purchasedProduct.transactionID;
            MainThreadDispatcher.Post(() => Deliver(storeId, token));
            return PurchaseProcessingResult.Complete;
        }

        private void Deliver(string storeId, string token)
        {
            IapProductDef def = IapCatalog.FindByStoreId(storeId);
            if (def == null) return;
            if (_pending != null && def.Product == _pendingProduct)
            {
                Action<PurchaseOutcome, string> callback = _pending;
                _pending = null;
                callback(PurchaseOutcome.Success, token);
                return;
            }

            // Not the purchase in flight (a restore or a deferred one): pay it directly; the token history dedupes.
            try
            {
                Services.Get<PurchaseService>()?.Grant(def.Product, token);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "store delivery failed: " + e.Message);
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) =>
            Fail(failureReason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) =>
            Fail(failureDescription != null && failureDescription.reason == PurchaseFailureReason.UserCancelled
                ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed);

        private void Fail(PurchaseOutcome outcome)
        {
            MainThreadDispatcher.Post(() =>
            {
                Action<PurchaseOutcome, string> callback = _pending;
                _pending = null;
                callback?.Invoke(outcome, null);
            });
        }
    }
#endif
}
