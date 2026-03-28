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
        GameManager.Instance.ConsumeOfflineGold();
        Hide();
    }

    private void ClaimDouble()
    {
        // TODO: AdMob 광고 시청 후 2배 지급
        GameManager.Instance.PlayerData.gold += GameManager.Instance.PendingOfflineGold;
        GameManager.Instance.ConsumeOfflineGold();
        Debug.Log("[OfflineReward] 2배 지급 (광고 연결 예정)");
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
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}시간 {ts.Minutes}분";
        return $"{ts.Minutes}분 {ts.Seconds}초";
    }

    private static string FormatGold(long gold)
    {
        if (gold >= 1_000_000) return $"{gold / 1_000_000f:0.#}M";
        if (gold >= 1_000)     return $"{gold / 1_000f:0.#}K";
        return gold.ToString();
    }
}
