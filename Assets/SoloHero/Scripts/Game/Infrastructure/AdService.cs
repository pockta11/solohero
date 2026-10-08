using System;
using GoogleMobileAds.Api;
using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using SoloHero.Game.Config;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// AdMob rewarded ads behind <see cref="IAdGateway"/> (E6-09..E6-13, ported from Legacy AdMobService).
    /// Every SDK callback is posted to <see cref="MainThreadDispatcher"/>. A failed load or show is a normal
    /// outcome (<see cref="AdOutcome.Failed"/>), never an error state. In the editor an ad is an instant reward.
    /// </summary>
    public sealed class AdService : MonoBehaviour, IAdGateway
    {
        private const float RetrySeconds = 30f;

        [SerializeField] private BuildConfig _build;

        private RewardedAd _ad;
        private bool _initialized;
        private bool _loading;
        private bool _showing;
        private bool _rewarded;
        private float _retryAt = -1f;
        private Action<AdOutcome> _pending;

        public bool IsReady => Application.isEditor || (_ad != null && _ad.CanShowAd());

        private void Start()
        {
            if (Application.isEditor) return;
            if (_build != null && !_build.adsEnabled)
            {
                Log.Info(LogTag.Ad, "ads disabled by build config");
                return;
            }

            // D-135: consent first (UMP); the SDK starts once ads may be requested.
            AdConsent.Gather(StartSdk);
        }

        private void StartSdk()
        {
            try
            {
                MobileAds.Initialize(_ => MainThreadDispatcher.Post(() =>
                {
                    _initialized = true;
                    Load();
                }));
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Ad, "ad sdk init failed, ads disabled: " + e.Message);
            }
        }

        private void Update()
        {
            if (_retryAt >= 0f && Time.unscaledTime >= _retryAt)
            {
                _retryAt = -1f;
                Load();
            }
        }

        public void Show(Action<AdOutcome> onDone)
        {
            if (Application.isEditor)
            {
                onDone?.Invoke(AdOutcome.Rewarded);
                return;
            }

            if (_showing || !IsReady)
            {
                onDone?.Invoke(AdOutcome.Failed);
                Load();
                return;
            }

            Log.Info(LogTag.Ad, "showing rewarded ad");
            _pending = onDone;
            _rewarded = false;
            _showing = true;
            try
            {
                _ad.Show(_ => MainThreadDispatcher.Post(() =>
                {
                    Log.Info(LogTag.Ad, "reward earned");
                    _rewarded = true;
                }));
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Ad, "ad show failed: " + e.Message);
                Finish(AdOutcome.Failed);
            }
        }

        private void Load()
        {
            if (!_initialized || _loading || _ad != null) return;
            string unitId = _build != null ? _build.RewardedAdUnitId : "";
            if (string.IsNullOrEmpty(unitId))
            {
                Log.Warn(LogTag.Ad, "no rewarded ad unit id, ads disabled");
                return;
            }

            _loading = true;
            try
            {
                RewardedAd.Load(unitId, new AdRequest(), (ad, error) => MainThreadDispatcher.Post(() => OnLoaded(ad, error)));
            }
            catch (Exception e)
            {
                _loading = false;
                Log.Warn(LogTag.Ad, "ad load threw: " + e.Message);
                _retryAt = Time.unscaledTime + RetrySeconds;
            }
        }

        private void OnLoaded(RewardedAd ad, LoadAdError error)
        {
            _loading = false;
            if (error != null || ad == null)
            {
                Log.Warn(LogTag.Ad, "ad load failed: " + (error != null ? error.GetMessage() : "no ad"));
                _retryAt = Time.unscaledTime + RetrySeconds;
                return;
            }

            _ad = ad;
            Log.Info(LogTag.Ad, "rewarded ad loaded");
            _ad.OnAdFullScreenContentClosed += () =>
                MainThreadDispatcher.Post(() => Finish(_rewarded ? AdOutcome.Rewarded : AdOutcome.Closed));
            _ad.OnAdFullScreenContentFailed += _ => MainThreadDispatcher.Post(() => Finish(AdOutcome.Failed));
        }

        private void Finish(AdOutcome outcome)
        {
            Log.Info(LogTag.Ad, "ad finished: " + outcome + " showing=" + _showing);
            if (!_showing) return;
            _showing = false;
            Action<AdOutcome> callback = _pending;
            _pending = null;
            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            Load();
            callback?.Invoke(outcome);
        }
    }
}
