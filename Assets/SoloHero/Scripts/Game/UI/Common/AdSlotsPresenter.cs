using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// A-2 gem ad and A-3 gold booster buttons (E6-10, E6-11): remaining daily count, booster countdown,
    /// and one message for every failure (E6-13). English until the Strings table (E7-17).
    /// </summary>
    public sealed class AdSlotsPresenter : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;

        [SerializeField] private TapGuardButton _gemButton;
        [SerializeField] private Text _gemLabel;
        [SerializeField] private TapGuardButton _boosterButton;
        [SerializeField] private Text _boosterLabel;
        [SerializeField] private ToastQueue _toast;

        private AdSlotPolicy _policy;
        private IAdGateway _ads;
        private float _refresh;
        private bool _busy;

        private void OnEnable()
        {
            _policy = PanelServices.TryGet<AdSlotPolicy>();
            _ads = PanelServices.TryGet<IAdGateway>();
            Refresh();
        }

        private void LateUpdate()
        {
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = RefreshSeconds;
            Refresh();
        }

        public void WatchGem() => Watch(AdSlot.Gem, "Gems received");

        public void WatchBooster() => Watch(AdSlot.GoldBooster, "Gold x2 for 10 minutes");

        private void Watch(AdSlot slot, string successText)
        {
            if (_policy == null || _ads == null || _busy) return;
            Result can = _policy.CanUse(slot);
            if (!can.Ok)
            {
                if (_toast != null) _toast.ShowFailure(can.Reason);
                return;
            }

            _busy = true;
            _ads.Show(outcome =>
            {
                _busy = false;
                Result r = _policy.Complete(slot, outcome);
                if (_toast != null)
                {
                    if (r.Ok) _toast.Show(successText);
                    else _toast.ShowFailure(r.Reason);
                }

                Refresh();
            });
        }

        private void Refresh()
        {
            if (_policy == null) return;

            int gemLeft = _policy.Remaining(AdSlot.Gem);
            if (_gemLabel != null) _gemLabel.text = "Ad: Gem +5  (" + gemLeft + ")";
            if (_gemButton != null) _gemButton.SetAvailable(!_busy && gemLeft > 0);

            long left = _policy.BoosterSecondsLeft;
            int boosterLeft = _policy.Remaining(AdSlot.GoldBooster);
            if (_boosterLabel != null)
            {
                _boosterLabel.text = left > 0
                    ? "Gold x2  " + (left / 60) + ":" + (left % 60).ToString("00")
                    : "Ad: Gold x2  (" + boosterLeft + ")";
            }

            if (_boosterButton != null) _boosterButton.SetAvailable(!_busy && left == 0 && boosterLeft > 0);
        }
    }
}
