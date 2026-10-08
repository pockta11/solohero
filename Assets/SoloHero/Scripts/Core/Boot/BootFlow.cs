using System;
using System.Threading.Tasks;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;

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

            // D-078: every save carries the skill collection (starters for new and pre-D-078 saves).
            if (SkillBook.EnsureStarters(data, balance)) save?.RequestSave(data);

            // D-141 / D-143: AP for every past level (auto split) and limit breaks covering the levels a save has.
            bool grown = SoloHero.Core.Growth.HeroAp.Settle(balance, data);
            if (SoloHero.Core.Growth.LaneRules.EnsureBreaks(balance, data)) grown = true;
            if (grown) save?.RequestSave(data);

            // D-133: offline gold is measured on trusted time. Signed in but the server's clock not known yet (no
            // network): the reward waits for it - BootSequence pays it when the clock syncs. Local mode has no server
            // and uses the device clock with the quit time never moving back.
            var trusted = clock as TrustedClock;
            if (trusted != null) trusted.ServerExpected = !usedLocal;
            OfflineReward offline;
            bool deferred = false;
            try
            {
                if (trusted != null && !trusted.IsTrusted)
                {
                    deferred = true;
                    offline = new OfflineReward(0d, false, false, false);
                }
                else
                {
                    offline = OfflineReturn.Apply(balance, data, clock.UtcNowSeconds, trusted == null || trusted.IsServerTime);
                    if (offline.GrantNow || offline.ResetQuitTime) save?.RequestSave(data);
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "offline calc failed: " + e.Message);
                offline = new OfflineReward(0d, false, false, false);
            }

            return new BootReport(userId, usedLocal, loadFailed, data, save, offline, deferred);
        }
    }
}
