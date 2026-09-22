using SoloHero.Core.Economy;
using SoloHero.Core.Save;

namespace SoloHero.Core.Boot
{
    public readonly struct BootReport
    {
        public readonly string UserId;
        public readonly bool UsedLocalMode;
        public readonly bool LoadFailed;
        public readonly SaveDataV2 Data;
        public readonly SaveService Save;
        public readonly OfflineReward Offline;

        public BootReport(string userId, bool usedLocalMode, bool loadFailed, SaveDataV2 data, SaveService save, OfflineReward offline)
        {
            UserId = userId;
            UsedLocalMode = usedLocalMode;
            LoadFailed = loadFailed;
            Data = data;
            Save = save;
            Offline = offline;
        }
    }
}
