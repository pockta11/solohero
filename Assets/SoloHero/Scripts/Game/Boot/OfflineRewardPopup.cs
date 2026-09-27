using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.Boot
{
    public sealed class OfflineRewardPopup : MonoBehaviour
    {
        [SerializeField] private Button _doubleButton;
        [SerializeField] private Text _doubleLabel;

        private double _gold;
        private bool _claimed;
        private bool _adBusy;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Show(double gold)
        {
            _gold = gold;
            _claimed = false;

            Text text = FindGoldText();
            if (text != null)
                text.text = BigNumberFormat.Format(gold);

            GameObject panel = FindPanel();
            if (panel != null)
                panel.SetActive(true);
            RefreshDoubleButton();
        }

        public void Claim() => ClaimWith(adDoubled: false);

        /// <summary>A-1: watch an ad to claim x2. Ad failure or early close keeps the popup and the normal claim.</summary>
        public void ClaimDoubled()
        {
            if (_claimed || _adBusy) return;
            AdSlotPolicy policy = TryGet<AdSlotPolicy>();
            IAdGateway ads = TryGet<IAdGateway>();
            if (policy == null || ads == null || !policy.CanUse(AdSlot.OfflineDouble).Ok)
            {
                RefreshDoubleButton();
                return;
            }

            _adBusy = true;
            ads.Show(outcome =>
            {
                _adBusy = false;
                if (policy.Complete(AdSlot.OfflineDouble, outcome).Ok)
                    ClaimWith(adDoubled: true);
                else
                    RefreshDoubleButton(failed: true);
            });
        }

        private void ClaimWith(bool adDoubled)
        {
            if (_claimed)
                return;

            GameObject panel = FindPanel();
            if (panel != null && !panel.activeSelf)
                return;

            BalanceValues balance = TryGet<BalanceValues>() ?? new BalanceValues();
            ISaveRequester requester = TryGet<ISaveRequester>();
            IClock clock = TryGet<IClock>();
            long now = clock != null ? clock.UtcNowSeconds : 0L;
            SaveDataV2 save = TryGet<SaveDataV2>();
            if (save == null)
                return;

            var reward = new OfflineReward(_gold, showPopup: true, grantNow: false, resetQuitTime: false);
            Result result = new OfflineClaim(balance, requester).Apply(save, reward, now, adDoubled);
            if (!result.Ok)
                return;

            _claimed = true;
            if (panel != null)
                panel.SetActive(false);

            BootSequence boot = FindObjectOfType<BootSequence>();
            if (boot != null)
                boot.NotifyOfflineClaimed();
        }

        private void RefreshDoubleButton(bool failed = false)
        {
            if (_doubleButton == null) return;
            AdSlotPolicy policy = TryGet<AdSlotPolicy>();
            int left = policy != null ? policy.Remaining(AdSlot.OfflineDouble) : 0;
            _doubleButton.interactable = left > 0;
            if (_doubleLabel != null)
                _doubleLabel.text = failed ? "Ad not available" : "Ad x2  (" + left + " left)";
        }

        private Text FindGoldText()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (IsButtonLabel(texts[i]))
                    continue;
                return texts[i];
            }

            return null;
        }

        private static bool IsButtonLabel(Text text)
        {
            Transform node = text.transform;
            while (node != null)
            {
                if (node.GetComponent<Button>() != null)
                    return true;
                node = node.parent;
            }

            return false;
        }

        private GameObject FindPanel()
        {
            Transform panel = transform.Find("Panel");
            return panel != null ? panel.gameObject : null;
        }

        private static T TryGet<T>() where T : class
        {
            try
            {
                return Services.Get<T>();
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
