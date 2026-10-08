using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>D-128 shop items, index-aligned with SaveDataV2.shopBought (append only).</summary>
    public enum ShopItem
    {
        /// <summary>Gear tickets for free, once a day.</summary>
        FreeGear = 0,

        /// <summary>Discounted ticket bundles, once a day each.</summary>
        DealGear = 1,
        DealSkill = 2,
        DealPet = 3,

        /// <summary>E6-02 gold package (gems for gold), no daily limit.</summary>
        GoldPack = 4
    }

    /// <summary>
    /// D-128 daily shop (genre: a free bundle and discounted bundles every day). A ticket is one pull of its summon;
    /// daily items reset at device-local midnight like the ad counters, the gold package has no limit.
    /// </summary>
    public sealed class ShopService
    {
        public static readonly ShopItem[] Items = { ShopItem.FreeGear, ShopItem.DealGear, ShopItem.DealSkill, ShopItem.DealPet, ShopItem.GoldPack };

        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly GemShop _gemShop;
        private readonly ISaveRequester _save;

        public ShopService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
            _gemShop = new GemShop(balance, save);
        }

        public static bool IsDaily(ShopItem item) => item != ShopItem.GoldPack;

        public static SummonKind TicketKind(ShopItem item) =>
            item == ShopItem.DealSkill ? SummonKind.Skill : item == ShopItem.DealPet ? SummonKind.Pet : SummonKind.Gear;

        public int Tickets(ShopItem item)
        {
            switch (item)
            {
                case ShopItem.FreeGear: return _balance.SHOP_FREE_GEAR_TICKETS;
                case ShopItem.GoldPack: return 0;
                default: return _balance.SHOP_DEAL_TICKETS;
            }
        }

        public int GemPrice(ShopItem item)
        {
            switch (item)
            {
                case ShopItem.DealGear: return _balance.SHOP_DEAL_GEAR_GEM;
                case ShopItem.DealSkill: return _balance.SHOP_DEAL_SKILL_GEM;
                case ShopItem.DealPet: return _balance.SHOP_DEAL_PET_GEM;
                case ShopItem.GoldPack: return _balance.GEM_GOLD_PACK_COST;
                default: return 0;
            }
        }

        /// <summary>Gold the package gives right now (it follows the farming stage).</summary>
        public double GoldPackAmount => _gemShop.GoldPackAmount(_data);

        public bool Bought(ShopItem item)
        {
            if (!IsDaily(item)) return false;
            RollDay();
            int i = (int)item;
            return i < _data.shopBought.Count && _data.shopBought[i];
        }

        /// <summary>The free bundle is waiting (the shop's red dot).</summary>
        public bool FreeReady => !Bought(ShopItem.FreeGear);

        public Result TryBuy(ShopItem item)
        {
            RollDay();
            if (item == ShopItem.GoldPack) return _gemShop.TryBuyGoldPack(_data);
            if (Bought(item)) return Result.Fail(FailReason.DailyLimit);
            int price = GemPrice(item);
            if (_data.gem < price) return Result.Fail(FailReason.NotEnoughGem);
            _data.gem -= price;
            AddTickets(_data, TicketKind(item), Tickets(item));
            while (_data.shopBought.Count <= (int)item) _data.shopBought.Add(false);
            _data.shopBought[(int)item] = true;
            _save?.RequestSave();
            return Result.Success;
        }

        public static int TicketsOf(SaveDataV2 data, SummonKind kind)
        {
            if (data == null) return 0;
            switch (kind)
            {
                case SummonKind.Skill: return data.skillTickets;
                case SummonKind.Pet: return data.petTickets;
                default: return data.gearTickets;
            }
        }

        public static void AddTickets(SaveDataV2 data, SummonKind kind, int count)
        {
            if (data == null || count <= 0) return;
            switch (kind)
            {
                case SummonKind.Skill: data.skillTickets += count; break;
                case SummonKind.Pet: data.petTickets += count; break;
                default: data.gearTickets += count; break;
            }
        }

        private void RollDay()
        {
            string today = DayKey.Today(_clock);
            if (!DayKey.IsNewDay(_data.shopDate, today)) return;
            _data.shopDate = today;
            _data.shopBought.Clear();
        }
    }
}
