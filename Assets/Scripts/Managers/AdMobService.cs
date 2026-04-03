using System;
using UnityEngine;
using GoogleMobileAds.Api;

/// <summary>
/// AdMob rewarded interstitial ad loader and presenter.
/// Call ShowRewardedAd(onRewarded) to show an ad; onRewarded fires only if the user earns the reward.
/// </summary>
public class AdMobService : MonoBehaviour
{
    public static AdMobService Instance { get; private set; }

    // Replace with real ad unit ID from AdMob console before release.
    // Test ID: ca-app-pub-3940256099942544/5354046379
    private const string AdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-3940256099942544/5354046379";  // TODO: replace with real unit ID
#else
        "unused";
#endif

    private RewardedInterstitialAd _ad;
    private bool _isInitialized;
    private bool _isLoading;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        MobileAds.Initialize(_ =>
        {
            _isInitialized = true;
            Debug.Log("[AdMobService] MobileAds initialized.");
            LoadAd();
        });
    }

    // ── Load ─────────────────────────────────────────────────────

    private void LoadAd()
    {
        if (!_isInitialized || _isLoading) return;
        _isLoading = true;

        var request = new AdRequest();
        RewardedInterstitialAd.Load(AdUnitId, request, OnAdLoaded);
    }

    private void OnAdLoaded(RewardedInterstitialAd ad, LoadAdError error)
    {
        _isLoading = false;

        if (error != null || ad == null)
        {
            Debug.LogWarning($"[AdMobService] Load failed: {error?.GetMessage()}");
            return;
        }

        _ad = ad;
        _ad.OnAdFullScreenContentClosed += OnAdClosed;
        _ad.OnAdFullScreenContentFailed += OnAdFailed;
        Debug.Log("[AdMobService] Rewarded interstitial loaded.");
    }

    // ── Show ─────────────────────────────────────────────────────

    private Action _pendingReward;

    /// <summary>
    /// Show the rewarded interstitial ad.
    /// <paramref name="onRewarded"/> is called on the main thread only if the user earns the reward.
    /// Falls back to calling <paramref name="onRewarded"/> immediately in editor or when no ad is ready.
    /// </summary>
    public void ShowRewardedAd(Action onRewarded)
    {
#if UNITY_EDITOR
        Debug.Log("[AdMobService] Editor: skipping ad, rewarding immediately.");
        onRewarded?.Invoke();
#else
        if (_ad == null || !_ad.CanShowAd())
        {
            Debug.LogWarning("[AdMobService] Ad not ready — rewarding without ad.");
            onRewarded?.Invoke();
            LoadAd();
            return;
        }

        _pendingReward = onRewarded;
        _ad.Show(reward =>
        {
            Debug.Log($"[AdMobService] Reward earned: {reward.Type} x{reward.Amount}");
            MainThreadDispatcher.Post(_pendingReward);
            _pendingReward = null;
        });
#endif
    }

    // ── Ad lifecycle ─────────────────────────────────────────────

    private void OnAdClosed()
    {
        Debug.Log("[AdMobService] Ad closed. Pre-loading next ad.");
        _ad?.Destroy();
        _ad = null;
        LoadAd();
    }

    private void OnAdFailed(AdError error)
    {
        Debug.LogWarning($"[AdMobService] Ad show failed: {error?.GetMessage()}");
        _ad?.Destroy();
        _ad = null;
        LoadAd();
    }
}
