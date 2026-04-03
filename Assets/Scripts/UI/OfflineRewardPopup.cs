using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오프라인 보상 팝업. GameScene 진입 시 PendingOfflineGold 있으면 자동 표시.
/// </summary>
public class OfflineRewardPopup : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private Button _claimButton;
    [SerializeField] private Button _doubleButton; // 광고 시청 2배

    void Start()
    {
        _panel.SetActive(false);
        _claimButton.onClick.AddListener(Claim);
        _doubleButton.onClick.AddListener(ClaimDouble);

        // 오프라인 보상이 있으면 팝업 표시
        if (GameManager.Instance != null && GameManager.Instance.PendingOfflineGold > 0)
            Show();
    }

    private void Show()
    {
        long gold    = GameManager.Instance.PendingOfflineGold;
        long elapsed = OfflineRewardSystem.GetElapsedSeconds(
            GameManager.Instance.PlayerData.lastQuitTimeUtc);

        _timeText.text = FormatTime(elapsed);
        _goldText.text = $"+{FormatGold(gold)}";

        _panel.SetActive(true);
        _panel.transform.localScale = Vector3.zero;
        _panel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void Claim()
    {
        var gm = GameManager.Instance;
        if (gm?.PlayerData != null && gm.PendingOfflineGold > 0)
        {
            gm.PlayerData.gold += gm.PendingOfflineGold;
            HUDManager.Instance?.RefreshGold();
            SaveManager.Instance?.RequestSave(gm.PlayerData);
        }
        GameManager.Instance.ConsumeOfflineGold();
        Hide();
    }

    private void ClaimDouble()
    {
        // Disable buttons immediately to prevent double-tap while ad loads.
        _claimButton.interactable  = false;
        _doubleButton.interactable = false;

        if (AdMobService.Instance != null)
            AdMobService.Instance.ShowRewardedAd(OnRewardEarned);
        else
            FallbackDouble();
    }

    private void OnRewardEarned()
    {
        var gm = GameManager.Instance;
        if (gm?.PlayerData != null && gm.PendingOfflineGold > 0)
        {
            gm.PlayerData.gold += gm.PendingOfflineGold * 2;
            HUDManager.Instance?.RefreshGold();
            SaveManager.Instance?.RequestSave(gm.PlayerData);
        }
        gm?.ConsumeOfflineGold();
        Debug.Log("[OfflineReward] 2x reward granted after ad.");
        Hide();
    }

    // Called when AdMobService is absent (shouldn't happen in production).
    private void FallbackDouble()
    {
        var gm = GameManager.Instance;
        if (gm?.PlayerData != null && gm.PendingOfflineGold > 0)
        {
            gm.PlayerData.gold += gm.PendingOfflineGold * 2;
            HUDManager.Instance?.RefreshGold();
            SaveManager.Instance?.RequestSave(gm.PlayerData);
        }
        gm?.ConsumeOfflineGold();
        Hide();
    }

    private void Hide()
    {
        _panel.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => _panel.SetActive(false));
    }

    private static string FormatTime(long seconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m {ts.Seconds}s";
    }

    private static string FormatGold(long gold)
    {
        if (gold >= 1_000_000) return $"{gold / 1_000_000f:0.#}M";
        if (gold >= 1_000)     return $"{gold / 1_000f:0.#}K";
        return gold.ToString();
    }
}
