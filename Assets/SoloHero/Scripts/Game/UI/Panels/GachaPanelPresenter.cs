using System.Text;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Summon panel (E7-07, D-078): an equipment / skill tab on top (genre summon screen), then single / 10 / gem 10
    /// pulls, the pity gauge of the shown tab, rate disclosure and the last result. Order is confirm -> save -> show:
    /// the service settles gold and items, then the save is requested, then the card reveal (E5-11) plays.
    /// </summary>
    public sealed class GachaPanelPresenter : MonoBehaviour
    {
        public const int ModeGear = 0;
        public const int ModeSkill = 1;

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

        [Header("Tabs (D-078)")]
        [SerializeField] private Image[] _tabImages = new Image[2];
        [SerializeField] private Sprite _tabIdle;
        [SerializeField] private Sprite _tabActive;
        [SerializeField] private EquipmentIconSet _equipmentIcons;
        [SerializeField] private SkillIconSet _skillIcons;

        private readonly StringBuilder _sb = new StringBuilder();
        private GachaService _gacha;
        private SkillSummonService _skillSummon;
        private GemShop _shop;
        private int _shownFarmingStage = -1;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private ISaveRequester _requester;
        private double _shownGold = -1d;
        private double _shownGem = -1d;
        private int _mode = ModeGear;

        private void OnEnable()
        {
            _gacha = PanelServices.TryGet<GachaService>();
            _skillSummon = PanelServices.TryGet<SkillSummonService>();
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

        /// <summary>D-111: plays the card reveal for gear pulled elsewhere (the welcome gift's free summon).</summary>
        public void ShowGearReveal(GachaPullItem[] items)
        {
            if (_reveal != null && items != null && items.Length > 0) _reveal.Show(Cards(items));
        }

        public void SetMode(int mode)
        {
            if (mode != ModeGear && mode != ModeSkill) return;
            if (_mode != mode && _resultText != null) _resultText.text = "";
            _mode = mode;
            DrawStatic();
            Refresh();
        }

        public void PullSingle()
        {
            if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPull(_save) : default, "skill_gold_single");
            else Apply(_gacha != null && _save != null ? _gacha.TryPull(_save) : default, "gold_single");
        }

        public void PullTen()
        {
            if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPullTen(_save) : default, "skill_gold_ten");
            else Apply(_gacha != null && _save != null ? _gacha.TryPullTen(_save) : default, "gold_ten");
        }

        public void PullTenWithGem()
        {
            if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPullTenWithGem(_save) : default, "skill_gem_ten");
            else Apply(_gacha != null && _save != null ? _gacha.TryPullTenWithGem(_save) : default, "gem_ten");
        }

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
            LogPull(AnalyticsEvents.GachaPull, BestGrade(result.Items), result.Items.Length, kind, _save.pityCount);
            DrawResult(result.Items);
            if (_reveal != null) _reveal.Show(Cards(result.Items));
            Refresh();
        }

        private void ApplySkill(SkillSummonResult result, string kind)
        {
            if (!result.Status.Ok || result.Items == null)
            {
                if (_toast != null && result.Status.Reason != FailReason.None) _toast.ShowFailure(result.Status.Reason);
                return;
            }

            if (_requester != null) _requester.RequestSave();
            if (_session != null) _session.RefreshLoadout();
            Grade best = Grade.Common;
            for (int i = 0; i < result.Items.Length; i++)
            {
                if (result.Items[i].Grade > best) best = result.Items[i].Grade;
            }

            LogPull(AnalyticsEvents.SkillSummon, best, result.Items.Length, kind, _save.skillPityCount);
            DrawSkillResult(result.Items);
            if (_reveal != null) _reveal.Show(Cards(result.Items));
            Refresh();
        }

        private RevealCard[] Cards(GachaPullItem[] items)
        {
            var cards = new RevealCard[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                GachaPullItem item = items[i];
                Sprite icon = _equipmentIcons != null ? _equipmentIcons.Get(item.Slot, item.Grade) : null;
                cards[i] = new RevealCard(item.Grade, PanelServices.SlotName(item.Slot), PanelServices.PullNote(item), icon);
            }

            return cards;
        }

        private RevealCard[] Cards(SkillPullItem[] items)
        {
            var cards = new RevealCard[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                SkillPullItem item = items[i];
                SkillDef def = SkillCatalog.Find(item.Id);
                Sprite icon = _skillIcons != null ? _skillIcons.Get(item.Id) : null;
                cards[i] = new RevealCard(item.Grade, def != null ? Strings.Get(def.NameKey) : item.Id, SkillNote(item), icon);
            }

            return cards;
        }

        private static string SkillNote(SkillPullItem item)
        {
            if (item.WasNew) return Strings.Get("gacha.skill_new");
            if (item.RefundGold > 0d) return Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold));
            return Strings.Format("gacha.skill_up", item.Level);
        }

        private void DrawStatic()
        {
            if (_balance == null) return;
            if (_rateText != null)
            {
                // The disclosure is drawn from the same table the draw uses (GDD rate disclosure rule); skills share it.
                _rateText.text = Strings.Format("gacha.rates",
                    _balance.GACHA_RATE_C, _balance.GACHA_RATE_R, _balance.GACHA_RATE_E, _balance.GACHA_RATE_L, _balance.GACHA_PITY);
            }

            bool skill = _mode == ModeSkill;
            if (_singleCostText != null)
                _singleCostText.text = Strings.Format("gacha.cost_single", BigNumberFormat.Format(skill ? _balance.SKILL_SUMMON_COST_SINGLE : _balance.GACHA_COST_SINGLE));
            if (_tenCostText != null)
                _tenCostText.text = Strings.Format("gacha.cost_ten", BigNumberFormat.Format(skill ? _balance.SKILL_SUMMON_COST_TEN : _balance.GACHA_COST_TEN));
            if (_gemCostText != null)
                _gemCostText.text = Strings.Format("gacha.cost_gem", skill ? _balance.SKILL_SUMMON_COST_TEN_GEM : _balance.GACHA_COST_TEN_GEM);
            for (int i = 0; i < _tabImages.Length; i++)
            {
                if (_tabImages[i] == null) continue;
                Sprite sprite = i == _mode ? _tabActive : _tabIdle;
                if (sprite != null) _tabImages[i].sprite = sprite;
                // D-108: the idle tab has its own gray plate; only dim when there is no separate sprite.
                _tabImages[i].color = i == _mode || (_tabIdle != null && _tabIdle != _tabActive) ? Color.white : new Color(0.55f, 0.55f, 0.62f, 1f);
            }
        }

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownGold = _save.gold;
            _shownGem = _save.gem;
            bool skill = _mode == ModeSkill;

            int pity = _balance.GACHA_PITY;
            int count = skill ? _save.skillPityCount : _save.pityCount;
            int left = pity - count;
            if (_pityText != null) _pityText.text = Strings.Format("gacha.pity", left, count, pity);
            if (_pityFill != null)
            {
                // Width by anchor: a Filled image without a sprite ignores fillAmount.
                float ratio = pity > 0 ? Mathf.Clamp01((float)count / pity) : 0f;
                _pityFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            }

            double single = skill ? _balance.SKILL_SUMMON_COST_SINGLE : _balance.GACHA_COST_SINGLE;
            double ten = skill ? _balance.SKILL_SUMMON_COST_TEN : _balance.GACHA_COST_TEN;
            int gem = skill ? _balance.SKILL_SUMMON_COST_TEN_GEM : _balance.GACHA_COST_TEN_GEM;
            if (_singleButton != null) _singleButton.SetAvailable(_save.gold >= single);
            if (_tenButton != null) _tenButton.SetAvailable(_save.gold >= ten);
            if (_gemButton != null) _gemButton.SetAvailable(_save.gem >= gem);
            _shownFarmingStage = _save.farmingStage;
            if (_shop != null && _goldPackText != null)
                _goldPackText.text = Strings.Format("gacha.gold_pack", _balance.GEM_GOLD_PACK_COST, BigNumberFormat.Format(_shop.GoldPackAmount(_save)));
            if (_goldPackButton != null) _goldPackButton.SetAvailable(_save.gem >= _balance.GEM_GOLD_PACK_COST);
        }

        private static Grade BestGrade(GachaPullItem[] items)
        {
            Grade best = Grade.Common;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Grade > best) best = items[i].Grade;
            }

            return best;
        }

        private static void LogPull(string eventName, Grade best, int count, string kind, int pity)
        {
            GameAnalytics.Log(eventName,
                AnalyticsParam.Of(AnalyticsEvents.PKind, kind),
                AnalyticsParam.Of(AnalyticsEvents.PCount, count),
                AnalyticsParam.Of(AnalyticsEvents.PBestGrade, (long)best),
                AnalyticsParam.Of(AnalyticsEvents.PPity, pity));
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

            AppendGradeCounts(counts);
            _sb.Append('\n').Append(Strings.Format("gacha.summary", equipped, enhanced, stored, refunded));
            _resultText.text = _sb.ToString();
        }

        private void DrawSkillResult(SkillPullItem[] items)
        {
            if (_resultText == null) return;
            var counts = new int[GachaCatalog.GradeCount];
            int fresh = 0, up = 0, refunded = 0;
            for (int i = 0; i < items.Length; i++)
            {
                counts[(int)items[i].Grade]++;
                if (items[i].WasNew) fresh++;
                else if (items[i].RefundGold > 0d) refunded++;
                else up++;
            }

            AppendGradeCounts(counts);
            _sb.Append('\n').Append(Strings.Format("gacha.skill_summary", fresh, up, refunded));
            _resultText.text = _sb.ToString();
        }

        private void AppendGradeCounts(int[] counts)
        {
            _sb.Clear();
            for (int g = GachaCatalog.GradeCount - 1; g >= 0; g--)
            {
                if (counts[g] == 0) continue;
                if (_sb.Length > 0) _sb.Append("   ");
                _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiPalette.GradeInk((Grade)g))).Append('>')
                    .Append(PanelServices.GradeName((Grade)g)).Append(' ').Append(counts[g]).Append("</color>");
            }
        }
    }
}
