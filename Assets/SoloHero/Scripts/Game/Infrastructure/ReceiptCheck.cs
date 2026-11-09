#if SOLOHERO_IAP
using System;
using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-183: a Google Play receipt is accepted only with Google's RSA signature made for this app's license key
    /// (<see cref="PlayLicenseKey"/>), for this package, for the product asked about, in the Purchased state, with a
    /// purchase token. Fake billing apps (purchase "bypass") cannot sign, so their receipts fail here and nothing is paid.
    /// </summary>
    internal static class ReceiptCheck
    {
        public static bool HasKey => PlayLicenseKey.Present;

        public static bool TryVerify(Product product, IapProductDef def, out string token, out string reason)
        {
            token = null;
            reason = "";
            if (product == null || def == null || !product.hasReceipt || string.IsNullOrEmpty(product.receipt))
            {
                reason = "missing";
                return false;
            }

            byte[] key = PlayLicenseKey.Get();
            if (key == null || key.Length == 0)
            {
                reason = "no_key";
                return false;
            }

            IPurchaseReceipt[] receipts;
            try
            {
                receipts = new CrossPlatformValidator(key, null, Application.identifier).Validate(product.receipt);
            }
            catch (IAPSecurityException e)
            {
                reason = "signature";
                Log.Warn(LogTag.Boot, "receipt refused: " + e.GetType().Name);
                return false;
            }
            catch (Exception e)
            {
                reason = "unreadable";
                Log.Warn(LogTag.Boot, "receipt unreadable: " + e.Message);
                return false;
            }

            if (receipts == null) receipts = new IPurchaseReceipt[0];
            foreach (IPurchaseReceipt receipt in receipts)
            {
                if (!(receipt is GooglePlayReceipt google) || google.productID != def.StoreId) continue;
                if (google.purchaseState != GooglePurchaseState.Purchased)
                {
                    reason = "state_" + (int)google.purchaseState;
                    return false;
                }

                if (!string.Equals(google.packageName, Application.identifier, StringComparison.Ordinal))
                {
                    reason = "package";
                    return false;
                }

                if (string.IsNullOrEmpty(google.purchaseToken))
                {
                    reason = "no_token";
                    return false;
                }

                token = google.purchaseToken;
                return true;
            }

            reason = "no_match";
            return false;
        }
    }
}
#endif
