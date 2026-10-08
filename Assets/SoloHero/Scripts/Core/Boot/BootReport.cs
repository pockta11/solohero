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

        /// <summary>D-133: the offline reward waits for the server's clock (signed in, but not connected yet).</summary>
        public readonly bool OfflineDeferred;

        public BootReport(string userId, bool usedLocalMode, bool loadFailed, SaveDataV2 data, SaveService save, OfflineReward offline, bool offlineDeferred = false)
        {
            UserId = userId;
            UsedLocalMode = usedLocalMode;
            LoadFailed = loadFailed;
            Data = data;
            Save = save;
            Offline = offline;
            OfflineDeferred = offlineDeferred;
        }
    }
}
