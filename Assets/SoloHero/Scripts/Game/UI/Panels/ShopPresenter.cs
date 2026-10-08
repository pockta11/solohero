using SoloHero.Core.Common;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-128 daily shop popup (right menu): the tickets owned on top, then one row per ShopItem - the free gear
    /// bundle, the day's discounted bundles and the gold package - with its price button. A red dot on the menu while
    /// the free bundle waits.
    /// </summary>
    public sealed class ShopPresenter : MonoBehaviour
    {
        private const float PollSeconds = 1f;

        private static readonly string[] NameKeys =
            { "shop.item.free_gear", "shop.item.deal_gear", "shop.item.deal_skill", "shop.item.deal_pet", "shop.item.gold" };

        [SerializeField] private GameObject _popup;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private Text _ticketsText;
        [SerializeField] private Text[] _names = new Text[0];
        [SerializeField] private Text[] _amounts = new Text[0];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[0];
        [SerializeField] private Text[] _buttonLabels = new Text[0];
        [SerializeField] private GameObject[] _badges = new GameObject[0];

        private ShopService _shop;
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

        public void Buy(int index)
        {
            if (_shop == null || index < 0 || index >= ShopService.Items.Length) return;
            ShopItem item = ShopService.Items[index];
            double gold = item == ShopItem.GoldPack ? _shop.GoldPackAmount : 0d;
            Result result = _shop.TryBuy(item);
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
    }
}
