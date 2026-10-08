using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;
using SoloHero.Game.UI.Panels;
using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-103 red dots on the bottom tabs for things waiting on the player: the character tab while a job advancement is
    /// ready, AP waits in manual mode (D-141) or a lane can be limit-broken (D-143); the talent tab while talent points
    /// are unspent. Checked twice a second, not every frame.
    /// </summary>
    public sealed class TabBadges : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;

        [SerializeField] private GameObject _heroBadge;
        [SerializeField] private GameObject _talentBadge;

        private JobService _jobs;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private float _timer;

        private void OnEnable()
        {
            _jobs = PanelServices.TryGet<JobService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _timer = 0f;
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;
            Set(_heroBadge, (_jobs != null && _jobs.CanAdvance) || GrowthWaiting());
            Set(_talentBadge, _save != null && _balance != null && TalentService.Available(_balance, _save) > 0);
        }

        private bool GrowthWaiting()
        {
            if (_save == null || _balance == null) return false;
            if (_save.apManual && HeroAp.Unspent(_balance, _save) > 0) return true;
            for (int i = 0; i < UpgradeLanes.Count; i++)
            {
                var lane = (UpgradeLane)i;
                if (LaneRules.State(_balance, _save, lane) == LaneState.NeedsBreak
                    && _save.breakStones >= LaneRules.BreakCost(_balance, LaneRules.Breaks(_save, lane))) return true;
            }

            return false;
        }

        private static void Set(GameObject badge, bool show)
        {
            if (badge != null && badge.activeSelf != show) badge.SetActive(show);
        }
    }
}
