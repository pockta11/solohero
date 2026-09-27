using System.Text;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
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
    /// then the result text is drawn. The card flip animation (E5-11) will slot in after the save.
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

        private readonly StringBuilder _sb = new StringBuilder();
        private GachaService _gacha;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private ISaveRequester _requester;
        private double _shownGold = -1d;
        private double _shownGem = -1d;

        private void OnEnable()
        {
            _gacha = PanelServices.TryGet<GachaService>();
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
            if (_save.gold == _shownGold && _save.gem == _shownGem) return;
            Refresh();
        }

        public void PullSingle() => Apply(_gacha != null && _save != null ? _gacha.TryPull(_save) : default);

        public void PullTen() => Apply(_gacha != null && _save != null ? _gacha.TryPullTen(_save) : default);

        public void PullTenWithGem() => Apply(_gacha != null && _save != null ? _gacha.TryPullTenWithGem(_save) : default);

        private void Apply(GachaBatchResult result)
        {
            if (!result.Status.Ok || result.Items == null)
            {
                if (_toast != null && result.Status.Reason != FailReason.None) _toast.ShowFailure(result.Status.Reason);
                return;
            }

            if (_requester != null) _requester.RequestSave();
            if (_session != null) _session.RefreshLoadout();
            DrawResult(result.Items);
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
        }

        private void DrawResult(GachaPullItem[] items)
        {
            if (_resultText == null) return;
            _sb.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                GachaPullItem item = items[i];
                if (i > 0) _sb.Append('\n');
                _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(PanelServices.GradeColor(item.Grade))).Append('>')
                    .Append(PanelServices.GradeName(item.Grade)).Append(' ').Append(PanelServices.SlotName(item.Slot)).Append("</color>  ");
                if (item.EnhancedLevel > 0) _sb.Append(Strings.Format("gacha.enhanced", item.EnhancedLevel));
                else if (item.WasDuplicate) _sb.Append(Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold)));
                else if (item.AutoEquipped) _sb.Append(Strings.Get("gacha.equipped"));
                else _sb.Append(Strings.Get("gacha.stored"));
            }

            _resultText.text = _sb.ToString();
        }
    }
}
