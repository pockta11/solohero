using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-right vertical icon menu (SoulStrike style).
/// Three buttons: Gift | Shop | Settings
/// </summary>
public class TopRightMenuUI : MonoBehaviour
{
    [SerializeField] Button _giftBtn;
    [SerializeField] Button _shopBtn;
    [SerializeField] Button _settingsBtn;

    [SerializeField] MainBottomNav _nav;

    void Start()
    {
        _giftBtn?.onClick.AddListener(    OnGift);
        _shopBtn?.onClick.AddListener(    OnShop);
        _settingsBtn?.onClick.AddListener(OnSettings);
    }

    void OnGift()     => Debug.Log("[TopMenu] Gift (not implemented)");
    void OnShop()     => Debug.Log("[TopMenu] Shop (not implemented)");
    void OnSettings() => Debug.Log("[TopMenu] Settings (not implemented)");
}
