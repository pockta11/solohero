using SoloHero.Core.Config;

namespace SoloHero.Core.Economy
{
    /// <summary>D-181 store products. Saved by id (first-bonus flags), so never rename one.</summary>
    public enum IapProduct
    {
        Vip = 0,
        GemSmall = 1,
        GemMedium = 2,
        GemLarge = 3,
        GoldPack = 4
    }

    public sealed class IapProductDef
    {
        public IapProduct Product { get; init; }

        /// <summary>The store's product id (Google Play Console); the save keys off <see cref="Id"/>.</summary>
        public string StoreId { get; init; } = "";

        /// <summary>Shown while the store has no localized price yet (Korean won).</summary>
        public string FallbackPrice { get; init; } = "";

        public string Id => Product.ToString().ToLowerInvariant();

        public string NameKey => "shop.item." + Suffix;

        private string Suffix
        {
            get
            {
                switch (Product)
                {
                    case IapProduct.Vip: return "vip";
                    case IapProduct.GemSmall: return "gem_small";
                    case IapProduct.GemMedium: return "gem_medium";
                    case IapProduct.GemLarge: return "gem_large";
                    default: return "gold_pack";
                }
            }
        }

        public bool IsGems => Product == IapProduct.GemSmall || Product == IapProduct.GemMedium || Product == IapProduct.GemLarge;
    }

    /// <summary>
    /// D-181 store catalog (genre standard "shop" top rows): the VIP pack first, then three gem bundles and a gold bundle.
    /// Amounts live in BalanceValues (IAP_GEM_*, IAP_GOLD_PACK_STAGES, VIP_DAYS); prices are the store's, with a won
    /// fallback for the label before the store answers. No probability item is sold directly (GDD): gems buy the same
    /// summons as gold.
    /// </summary>
    public static class IapCatalog
    {
        public static readonly IapProductDef[] All =
        {
            new IapProductDef { Product = IapProduct.Vip, StoreId = "solohero_vip_30", FallbackPrice = "5,900" },
            new IapProductDef { Product = IapProduct.GemSmall, StoreId = "solohero_gem_300", FallbackPrice = "1,200" },
            new IapProductDef { Product = IapProduct.GemMedium, StoreId = "solohero_gem_1000", FallbackPrice = "3,900" },
            new IapProductDef { Product = IapProduct.GemLarge, StoreId = "solohero_gem_3300", FallbackPrice = "12,000" },
            new IapProductDef { Product = IapProduct.GoldPack, StoreId = "solohero_gold_pack", FallbackPrice = "2,500" },
        };

        public static int Count => All.Length;

        public static IapProductDef Find(IapProduct product)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Product == product) return All[i];
            return null;
        }

        public static IapProductDef FindByStoreId(string storeId)
        {
            if (string.IsNullOrEmpty(storeId)) return null;
            for (int i = 0; i < All.Length; i++)
                if (All[i].StoreId == storeId) return All[i];
            return null;
        }

        /// <summary>Gems a gem bundle pays before the first-purchase bonus (0 for the others).</summary>
        public static int Gems(BalanceValues balance, IapProduct product)
        {
            switch (product)
            {
                case IapProduct.GemSmall: return balance.IAP_GEM_SMALL;
                case IapProduct.GemMedium: return balance.IAP_GEM_MEDIUM;
                case IapProduct.GemLarge: return balance.IAP_GEM_LARGE;
                default: return 0;
            }
        }
    }
}
