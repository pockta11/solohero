using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameScene HUD. 골드 표시, 챕터/스테이지 정보.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] TextMeshProUGUI _goldTMP;
    [SerializeField] TextMeshProUGUI _stageTMP;
    [SerializeField] TextMeshProUGUI _killsTMP;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        var pd = GameManager.Instance?.PlayerData;
        if (pd == null) return;

        SetGold(pd.gold);
        SetStage(pd.chapter);
    }

    public void SetGold(long gold)
    {
        if (_goldTMP != null)
            _goldTMP.text = $"{gold:N0} G";
    }

    public void SetStage(int chapter)
    {
        if (_stageTMP != null)
            _stageTMP.text = $"Ch.{chapter}";
    }

    public void SetKills(int kills, int max)
    {
        if (_killsTMP != null)
            _killsTMP.text = $"{kills} / {max}";
    }
}
