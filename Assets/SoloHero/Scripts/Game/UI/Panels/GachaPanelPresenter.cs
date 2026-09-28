using System.Text;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Gacha panel (E7-07): single / 10 / gem 10 pulls, pity gauge, rate disclosure and the last result.
    /// Order is confirm -> save -> show: the service settles gold and items, then the save is requested,
    /// then the card reveal (E5-11) plays and the result list is drawn.
    /// </summary>
    public sealed class GachaPanelPresenter : MonoBehaviour
    {
        [SerializeField] private Text _pityText;
        [SerializeField] private Image _pityFill;
        [SerializeField] private Text _rateText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _singleCostText;
        [SerializeField] private Text _tenCostText;
        [SerializeField] private Text _gemCostText;
        [SerializeField] private TapGuardButton _singleButton;
        [SerializeField] private TapGuardButton _tenButton;
        [SerializeField] private TapGuardButton _gemButton;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private GachaRevealView _reveal;
        [SerializeField] private TapGuardButton _goldPackButton;
        [SerializeField] private Text _goldPackText;

        private readonly StringBuilder _sb = new StringBuilder();
        private GachaService _gacha;
        private GemShop _shop;
        private int _shownFarmingStage = -1;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private ISaveRequester _requester;
        private double _shownGold = -1d;
        private double _shownGem = -1d;

        private void OnEnable()
        {
            _gacha = PanelServices.TryGet<GachaService>();
            _shop = PanelServices.TryGet<GemShop>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _requester = PanelServices.TryGet<ISaveRequester>();
            _shownGold = -1d;
            DrawStatic();
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_save.gold == _shownGold && _save.gem == _shownGem && _save.farmingStage == _shownFarmingStage) return;
            Refresh();
        }

        public void PullSingle() => Apply(_gacha != null && _save != null ? _gacha.TryPull(_save) : default, "gold_single");

        public void PullTen() => Apply(_gacha != null && _save != null ? _gacha.TryPullTen(_save) : default, "gold_ten");

        public void PullTenWithGem() => Apply(_gacha != null && _save != null ? _gacha.TryPullTenWithGem(_save) : default, "gem_ten");

        /// <summary>E6-02: gems for an instant gold package worth 100 clears of the farming stage.</summary>
        public void BuyGoldPack()
        {
            if (_shop == null || _save == null) return;
            double gold = _shop.GoldPackAmount(_save);
            Result r = _shop.TryBuyGoldPack(_save);
            if (_toast != null)
            {
                if (r.Ok) _toast.Show(Strings.Format("toast.gold_pack", BigNumberFormat.Format(gold)));
                else _toast.ShowFailure(r.Reason);
            }

            Refresh();
        }

        private void Apply(GachaBatchResult result, string kind)
        {
            if (!result.Status.Ok || result.Items == null)
            {
                if (_toast != null && result.Status.Reason != FailReason.None) _toast.ShowFailure(result.Status.Reason);
                return;
            }

            if (_requester != null) _requester.RequestSave();
            if (_session != null) _session.RefreshLoadout();
            LogPull(result.Items, kind);
            DrawResult(result.Items);
            if (_reveal != null) _reveal.Show(result.Items);
            Refresh();
        }

        private void DrawStatic()
        {
            if (_balance == null) return;
            if (_rateText != null)
            {
                // The disclosure is drawn from the same table the draw uses (GDD rate disclosure rule).
                _rateText.text = Strings.Format("gacha.rates",
                    _balance.GACHA_RATE_C, _balance.GACHA_RATE_R, _balance.GACHA_RATE_E, _balance.GACHA_RATE_L, _balance.GACHA_PITY);
            }

            if (_singleCostText != null) _singleCostText.text = Strings.Format("gacha.cost_single", BigNumberFormat.Format(_balance.GACHA_COST_SINGLE));
            if (_tenCostText != null) _tenCostText.text = Strings.Format("gacha.cost_ten", BigNumberFormat.Format(_balance.GACHA_COST_TEN));
            if (_gemCostText != null) _gemCostText.text = Strings.Format("gacha.cost_gem", _balance.GACHA_COST_TEN_GEM);
        }

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownGold = _save.gold;
            _shownGem = _save.gem;

            int pity = _balance.GACHA_PITY;
            int left = pity - _save.pityCount;
            if (_pityText != null) _pityText.text = Strings.Format("gacha.pity", left, _save.pityCount, pity);
            if (_pityFill != null)
            {
                // Width by anchor: a Filled image without a sprite ignores fillAmount.
                float ratio = pity > 0 ? Mathf.Clamp01((float)_save.pityCount / pity) : 0f;
                _pityFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            }

            if (_singleButton != null) _singleButton.SetAvailable(_save.gold >= _balance.GACHA_COST_SINGLE);
            if (_tenButton != null) _tenButton.SetAvailable(_save.gold >= _balance.GACHA_COST_TEN);
            if (_gemButton != null) _gemButton.SetAvailable(_save.gem >= _balance.GACHA_COST_TEN_GEM);
            _shownFarmingStage = _save.farmingStage;
            if (_shop != null && _goldPackText != null)
                _goldPackText.text = Strings.Format("gacha.gold_pack", _balance.GEM_GOLD_PACK_COST, BigNumberFormat.Format(_shop.GoldPackAmount(_save)));
            if (_goldPackButton != null) _goldPackButton.SetAvailable(_save.gem >= _balance.GEM_GOLD_PACK_COST);
        }

        private void LogPull(GachaPullItem[] items, string kind)
        {
            Grade best = Grade.Common;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Grade > best) best = items[i].Grade;
            }

            GameAnalytics.Log(AnalyticsEvents.GachaPull,
                AnalyticsParam.Of(AnalyticsEvents.PKind, kind),
                AnalyticsParam.Of(AnalyticsEvents.PCount, items.Length),
                AnalyticsParam.Of(AnalyticsEvents.PBestGrade, (long)best),
                AnalyticsParam.Of(AnalyticsEvents.PPity, _save != null ? _save.pityCount : 0));
        }

        /// <summary>
        /// The last pull at a glance (the card reveal shows each item): grade counts in grade colours, then what
        /// happened to the items - equipped, enhanced, stored, refunded.
        /// </summary>
        private void DrawResult(GachaPullItem[] items)
        {
            if (_resultText == null) return;
            var counts = new int[GachaCatalog.GradeCount];
            int equipped = 0, enhanced = 0, stored = 0, refunded = 0;
            for (int i = 0; i < items.Length; i++)
            {
                GachaPullItem item = items[i];
                counts[(int)item.Grade]++;
                if (item.EnhancedLevel > 0) enhanced++;
                else if (item.WasDuplicate) refunded++;
                else if (item.AutoEquipped) equipped++;
                else stored++;
            }

            _sb.Clear();
            for (int g = GachaCatalog.GradeCount - 1; g >= 0; g--)
            {
                if (counts[g] == 0) continue;
                if (_sb.Length > 0) _sb.Append("   ");
                _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(PanelServices.GradeColor((Grade)g))).Append('>')
                    .Append(PanelServices.GradeName((Grade)g)).Append(' ').Append(counts[g]).Append("</color>");
            }

            _sb.Append('\n').Append(Strings.Format("gacha.summary", equipped, enhanced, stored, refunded));
            _resultText.text = _sb.ToString();
        }
    }
}
