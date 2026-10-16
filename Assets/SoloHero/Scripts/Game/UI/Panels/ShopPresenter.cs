using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-128 daily shop popup (right menu): D-181 store rows on top (the VIP pack, three gem bundles, the gold bundle,
    /// each with the store's price), then the tickets owned and one row per ShopItem - the free gear bundle, the day's
    /// discounted bundles and the gold package - with its gem price. A red dot on the menu while the free bundle waits.
    /// </summary>
    public sealed class ShopPresenter : MonoBehaviour
    {
        private const float PollSeconds = 1f;

        private static readonly string[] NameKeys =
            { "shop.item.free_gear", "shop.item.deal_gear", "shop.item.deal_skill", "shop.item.deal_pet", "shop.item.gold" };

        [SerializeField] private GameObject _popup;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;

        /// <summary>D-171: seconds between two claims from this window (the reward animation's beat).</summary>
        private const float ClaimSeconds = 0.35f;

        private readonly ClaimGate _gate = new ClaimGate();
        [SerializeField] private Text _ticketsText;
        [SerializeField] private Text[] _names = new Text[0];
        [SerializeField] private Text[] _amounts = new Text[0];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[0];
        [SerializeField] private Text[] _buttonLabels = new Text[0];
        [SerializeField] private GameObject[] _badges = new GameObject[0];

        [Header("D-181 store rows, index-aligned with IapCatalog.All")]
        [SerializeField] private Text[] _iapNames = new Text[0];
        [SerializeField] private Text[] _iapAmounts = new Text[0];
        [SerializeField] private TapGuardButton[] _iapButtons = new TapGuardButton[0];
        [SerializeField] private Text[] _iapLabels = new Text[0];

        private ShopService _shop;
        private PurchaseService _purchases;
        private SaveDataV2 _save;
        private float _poll;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _shop = PanelServices.TryGet<ShopService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _purchases = PanelServices.TryGet<PurchaseService>();
            if (_purchases != null)
            {
                _purchases.Purchased += OnPurchased;
                _purchases.PurchaseEnded += OnPurchaseEnded;
            }
        }

        private void OnDisable()
        {
            if (_purchases == null) return;
            _purchases.Purchased -= OnPurchased;
            _purchases.PurchaseEnded -= OnPurchaseEnded;
        }

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        /// <summary>D-181: starts the store flow for the row; the grant arrives through <see cref="OnPurchased"/>.</summary>
        public void BuyIap(int index)
        {
            if (_purchases == null || index < 0 || index >= IapCatalog.Count || !_gate.Pass(ClaimSeconds)) return;
            Result result = _purchases.TryBuy(IapCatalog.All[index].Product);
            if (!result.Ok) _toast?.ShowFailure(result.Reason);
            Refresh();
        }

        private void OnPurchased(PurchaseGrant grant)
        {
            PanelServices.TryGet<AudioService>()?.Play(SfxId.GradeLegendary);
            GameAnalytics.Log(AnalyticsEvents.Purchase,
                AnalyticsParam.Of(AnalyticsEvents.PProduct, IapCatalog.Find(grant.Product)?.Id ?? ""),
                AnalyticsParam.Of(AnalyticsEvents.PCount, _save != null ? _save.iapCount : 0));
            if (_rewardFx != null)
            {
                Vector2 at = IsOpen ? _rewardFx.LastTap : RewardFx.BattleCenter;
                if (grant.Gems > 0d) _rewardFx.Play(RewardKind.Gem, grant.Gems, at);
                if (grant.Gold > 0d) _rewardFx.Play(RewardKind.Gold, grant.Gold, at);
                if (grant.VipDays > 0) _rewardFx.Sparkle(new Color(1f, 0.85f, 0.4f, 1f));
            }

            if (_toast != null)
            {
                if (grant.VipDays > 0) _toast.Show(Strings.Format("toast.vip_started", grant.VipDays));
                else if (grant.Gold > 0d) _toast.Show(Strings.Format("toast.gold_pack", BigNumberFormat.Format(grant.Gold)));
                else _toast.Show(Strings.Get("toast.purchase_ok"));
            }

            Refresh();
        }

        private void OnPurchaseEnded(IapProduct product, PurchaseOutcome outcome)
        {
            if (_toast != null)
            {
                _toast.Show(Strings.Get(outcome == PurchaseOutcome.Cancelled ? "toast.purchase_cancel"
                    : outcome == PurchaseOutcome.Pending ? "toast.purchase_pending" : "toast.purchase_fail"));
            }

            Refresh();
        }

        public void Buy(int index)
        {
            if (_shop == null || index < 0 || index >= ShopService.Items.Length || !_gate.Pass(ClaimSeconds)) return;
            ShopItem item = ShopService.Items[index];
            double gold = item == ShopItem.GoldPack ? _shop.GoldPackAmount : 0d;
            var before = new RewardSnapshot(_save);
            Result result = _shop.TryBuy(item);
            if (result.Ok)
            {
                // D-171: the goods fly to their counter with a chime (the gem price was already paid).
                PanelServices.TryGet<AudioService>()?.Play(SfxId.Gold);
                if (_rewardFx != null && _save != null)
                {
                    if (_save.gold > before.Gold) _rewardFx.Play(RewardKind.Gold, _save.gold - before.Gold, _rewardFx.LastTap);
                    int tickets = _save.gearTickets + _save.skillTickets + _save.petTickets - before.Tickets;
                    if (tickets > 0) _rewardFx.Play(RewardKind.Ticket, tickets, _rewardFx.LastTap);
                }
            }

            if (_toast != null)
            {
                if (!result.Ok) _toast.ShowFailure(result.Reason);
                else if (item == ShopItem.GoldPack) _toast.Show(Strings.Format("toast.gold_pack", BigNumberFormat.Format(gold)));
                else _toast.Show(Strings.Format("shop.bought", Strings.Get(KindKey(ShopService.TicketKind(item))), _shop.Tickets(item)));
            }

            Refresh();
        }

        private void LateUpdate()
        {
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f) return;
            _poll = PollSeconds;
            bool ready = _shop != null && _shop.FreeReady;
            for (int i = 0; i < _badges.Length; i++)
            {
                if (_badges[i] != null && _badges[i].activeSelf != ready) _badges[i].SetActive(ready);
            }

            if (IsOpen) Refresh();
        }

        private static string KindKey(SummonKind kind) =>
            kind == SummonKind.Skill ? "shop.ticket_skill" : kind == SummonKind.Pet ? "shop.ticket_pet" : "shop.ticket_gear";

        private void Refresh()
        {
            if (_shop == null || _save == null) return;
            if (_ticketsText != null)
                _ticketsText.text = Strings.Format("shop.tickets", BigNumberFormat.Format(_save.gearTickets),
                    BigNumberFormat.Format(_save.skillTickets), BigNumberFormat.Format(_save.petTickets));

            RefreshStore();
            for (int i = 0; i < ShopService.Items.Length; i++)
            {
                ShopItem item = ShopService.Items[i];
                if (i < _names.Length && _names[i] != null) _names[i].text = Strings.Get(NameKeys[i]);
                if (i < _amounts.Length && _amounts[i] != null)
                {
                    _amounts[i].text = item == ShopItem.GoldPack
                        ? Strings.Format("shop.amount_gold", BigNumberFormat.Format(_shop.GoldPackAmount))
                        : Strings.Format("shop.amount_tickets", Strings.Get(KindKey(ShopService.TicketKind(item))), _shop.Tickets(item));
                }

                bool bought = _shop.Bought(item);
                int price = _shop.GemPrice(item);
                if (i < _buttonLabels.Length && _buttonLabels[i] != null)
                    _buttonLabels[i].text = bought ? Strings.Get("shop.sold_out") : price <= 0 ? Strings.Get("shop.free") : Strings.Format("shop.price", price);
                if (i < _buttons.Length && _buttons[i] != null) _buttons[i].SetAvailable(!bought && _save.gem >= price);
            }
        }

        /// <summary>D-181 store rows: names, what each pays (first bonus, VIP days left), price labels and availability.</summary>
        private void RefreshStore()
        {
            bool ready = _purchases != null && _purchases.StoreReady;
            bool busy = _purchases != null && _purchases.Busy;
            for (int i = 0; i < IapCatalog.Count; i++)
            {
                IapProductDef def = IapCatalog.All[i];
                if (i < _iapNames.Length && _iapNames[i] != null)
                    _iapNames[i].text = def.Product == IapProduct.Vip ? Strings.Format(def.NameKey, VipDays()) : Strings.Get(def.NameKey);

                if (i < _iapAmounts.Length && _iapAmounts[i] != null) _iapAmounts[i].text = AmountText(def);
                string price = _purchases != null ? _purchases.PriceLabel(def.Product) : def.FallbackPrice;
                if (i < _iapLabels.Length && _iapLabels[i] != null)
                    _iapLabels[i].text = ready ? PriceText(price) : Strings.Get("shop.store_unavailable");
                if (i < _iapButtons.Length && _iapButtons[i] != null) _iapButtons[i].SetAvailable(ready && !busy);
            }
        }

        private int VipDays()
        {
            SoloHero.Core.Config.BalanceValues balance = PanelServices.TryGet<SoloHero.Core.Config.BalanceValues>();
            return balance != null ? balance.VIP_DAYS : 30;
        }

        private string AmountText(IapProductDef def)
        {
            if (_purchases == null) return "";
            switch (def.Product)
            {
                case IapProduct.Vip:
                    return _purchases.VipActive ? Strings.Format("shop.vip_active", _purchases.VipDaysLeft) : Strings.Get("shop.vip_desc");
                case IapProduct.GoldPack:
                    return Strings.Format("shop.amount_gold", BigNumberFormat.Format(_purchases.GoldPackAmount()));
                default:
                    return _purchases.FirstBonusAvailable(def.Product)
                        ? Strings.Format("shop.amount_first_bonus", BigNumberFormat.Format(_purchases.GemAmount(def.Product)))
                        : Strings.Format("shop.amount_gems", BigNumberFormat.Format(_purchases.GemAmount(def.Product)));
            }
        }

        /// <summary>The store's localized price as it came, or the catalog's won fallback.</summary>
        private static string PriceText(string price)
        {
            if (string.IsNullOrEmpty(price)) return Strings.Get("shop.store_unavailable");
            bool digitsOnly = true;
            for (int i = 0; i < price.Length && digitsOnly; i++) digitsOnly = char.IsDigit(price[i]) || price[i] == ',';
            return digitsOnly ? Strings.Format("shop.price_won", price) : price;
        }
    }
}
