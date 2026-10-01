using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;
using SoloHero.Game.UI.Panels;
using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-103 red dots on the bottom tabs for things waiting on the player: the character tab while a promotion can be
    /// bought, the talent tab while talent points are unspent. Checked twice a second, not every frame.
    /// </summary>
    public sealed class TabBadges : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;

        [SerializeField] private GameObject _heroBadge;
        [SerializeField] private GameObject _talentBadge;

        private PromotionService _promotion;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private float _timer;

        private void OnEnable()
        {
            _promotion = PanelServices.TryGet<PromotionService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _timer = 0f;
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;
            Set(_heroBadge, _promotion != null && _promotion.CanPromote().Ok);
            Set(_talentBadge, _save != null && _balance != null && TalentService.Available(_balance, _save) > 0);
        }

        private static void Set(GameObject badge, bool show)
        {
            if (badge != null && badge.activeSelf != show) badge.SetActive(show);
        }
    }
}
