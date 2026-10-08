using System;
using GoogleMobileAds.Ump.Api;
using SoloHero.Core.Common;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-135 (launch plan P1-3): Google's User Messaging Platform before any ad. Every start asks UMP for the consent
    /// status; where the law asks for consent (EEA, UK, Switzerland) it shows the form set up in the AdMob console
    /// (Privacy &amp; messaging) and the ad SDK starts only once UMP says ads may be requested. Elsewhere - the v1 launch
    /// is Korea only - nothing is shown and ads start as before. Consent from an earlier session starts ads at once.
    /// When UMP requires a way back to the choice, the account window shows a "privacy options" button.
    /// </summary>
    public static class AdConsent
    {
        private static bool _gathering;
        private static bool _adsStarted;

        /// <summary>Asks UMP and calls <paramref name="startAds"/> (once, on the main thread) when ads may be requested.</summary>
        public static void Gather(Action startAds)
        {
            if (_gathering) return;
            _gathering = true;
            TryStart(startAds);
            try
            {
                var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
                ConsentInformation.Update(request, updateError => MainThreadDispatcher.Post(() =>
                {
                    if (updateError != null)
                    {
                        Log.Warn(LogTag.Ad, "consent status not updated: " + updateError.Message);
                        TryStart(startAds);
                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired(formError => MainThreadDispatcher.Post(() =>
                    {
                        if (formError != null) Log.Warn(LogTag.Ad, "consent form not shown: " + formError.Message);
                        TryStart(startAds);
                    }));
                }));
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Ad, "consent unavailable: " + e.Message);
            }
        }

        /// <summary>UMP asks for an entry point where the player can change the choice (consent regions only).</summary>
        public static bool PrivacyOptionsRequired
        {
            get
            {
                if (Application.isEditor || !_gathering) return false;
                try
                {
                    return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>Shows Google's privacy options form; <paramref name="done"/> gets false when it could not open.</summary>
        public static void ShowPrivacyOptions(Action<bool> done)
        {
            try
            {
                ConsentForm.ShowPrivacyOptionsForm(error => MainThreadDispatcher.Post(() =>
                {
                    if (error != null) Log.Warn(LogTag.Ad, "privacy options not shown: " + error.Message);
                    done?.Invoke(error == null);
                }));
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Ad, "privacy options unavailable: " + e.Message);
                done?.Invoke(false);
            }
        }

        private static void TryStart(Action startAds)
        {
            if (_adsStarted) return;
            bool allowed;
            try
            {
                allowed = ConsentInformation.CanRequestAds();
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Ad, "consent check failed: " + e.Message);
                allowed = false;
            }

            if (!allowed) return;
            _adsStarted = true;
            startAds?.Invoke();
        }
    }
}
