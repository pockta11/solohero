using System;
using System.Threading.Tasks;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Save;

namespace SoloHero.Core.Boot
{
    public static class BootFlow
    {
        public static async Task<BootReport> RunAsync(
            IAuthGateway auth,
            Func<string, SaveService> createSave,
            BalanceValues balance,
            IClock clock)
        {
            string userId = "local";
            try
            {
                string signedIn = await auth.SignInAsync();
                if (!string.IsNullOrEmpty(signedIn)) userId = signedIn;
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "auth failed, local mode: " + e.Message);
                userId = "local";
            }

            bool usedLocal = userId == "local";
            SaveService save = null;
            try
            {
                save = createSave(userId);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "save setup failed, local mode: " + e.Message);
                userId = "local";
                usedLocal = true;
                try { save = createSave("local"); }
                catch (Exception inner)
                {
                    Log.Warn(LogTag.Boot, "local save setup failed: " + inner.Message);
                }
            }

            SaveDataV2 data;
            bool loadFailed = false;
            if (save == null)
            {
                data = SaveDataV2.CreateNew();
                loadFailed = true;
            }
            else
            {
                try
                {
                    data = await save.LoadAsync() ?? SaveDataV2.CreateNew();
                }
                catch (Exception e)
                {
                    Log.Warn(LogTag.Boot, "load failed, new user: " + e.Message);
                    data = SaveDataV2.CreateNew();
                    loadFailed = true;
                }
            }

            OfflineReward offline;
            try
            {
                offline = OfflineReward.Compute(balance, data.farmingStage, data.lastQuitTimeUtc, clock.UtcNowSeconds);
                if (offline.GrantNow)
                {
                    data.gold += offline.Gold;
                    data.lastQuitTimeUtc = clock.UtcNowSeconds;
                    save?.RequestSave(data);
                }
                else if (offline.ResetQuitTime)
                {
                    data.lastQuitTimeUtc = clock.UtcNowSeconds;
                    save?.RequestSave(data);
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "offline calc failed: " + e.Message);
                offline = new OfflineReward(0d, false, false, false);
            }

            return new BootReport(userId, usedLocal, loadFailed, data, save, offline);
        }
    }
}
