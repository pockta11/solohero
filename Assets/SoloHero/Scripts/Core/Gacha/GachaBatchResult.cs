using SoloHero.Core.Common;

namespace SoloHero.Core.Gacha
{
    public readonly struct GachaBatchResult
    {
        public readonly Result Status;
        public readonly bool RequestSave;
        public readonly GachaPullItem[] Items;

        public GachaBatchResult(Result status, bool requestSave, GachaPullItem[] items)
        {
            Status = status;
            RequestSave = requestSave;
            Items = items;
        }

        public static GachaBatchResult Fail(FailReason reason)
            => new GachaBatchResult(Result.Fail(reason), false, System.Array.Empty<GachaPullItem>());

        public static GachaBatchResult Ok(GachaPullItem[] items)
            => new GachaBatchResult(Result.Success, true, items);
    }
}
