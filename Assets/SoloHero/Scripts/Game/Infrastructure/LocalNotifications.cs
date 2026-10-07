using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
#if SOLOHERO_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-119 local notifications (genre: bring the player back): when the app goes to the background it schedules "the
    /// offline reward is full" at the offline cap and "a new day" at the next morning; coming back cancels them. Off in
    /// the settings, the editor and builds without the Mobile Notifications package. Android 13+ asks for permission
    /// once, from the second session on, so the first minutes stay on the game.
    /// </summary>
    public static class LocalNotifications
    {
        public const string ChannelId = "solohero_default";

        /// <summary>Local hour of the "new day" reminder.</summary>
        public const int MorningHour = 9;

#if SOLOHERO_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
        private static bool _ready;
#endif

        public static void Init(SaveDataV2 data)
        {
#if SOLOHERO_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                {
                    Id = ChannelId,
                    Name = Strings.Get("notify.channel"),
                    Importance = Importance.Default,
                    Description = Strings.Get("notify.channel")
                });
                _ready = true;
                CancelAll();
                // The first session belongs to the game; ask from the second one on (Android 13+ shows a dialog).
                if (data != null && !data.notifyAsked && data.totalPullCount > 0)
                {
                    data.notifyAsked = true;
                    new PermissionRequest();
                }
            }
            catch (Exception e)
            {
                _ready = false;
                Log.Warn(LogTag.Boot, "notifications unavailable: " + e.Message);
            }
#endif
        }

        public static void CancelAll()
        {
#if SOLOHERO_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            if (!_ready) return;
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
        }

        /// <summary>Schedules the reminders for a player leaving now (nothing when switched off).</summary>
        public static void ScheduleOnLeave(SaveDataV2 data, BalanceValues balance)
        {
            if (data == null || balance == null || data.notificationsOff) return;
            DateTime now = DateTime.Now;
            DateTime morning = now.Date.AddDays(1).AddHours(MorningHour);
#if SOLOHERO_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            if (!_ready) return;
            CancelAll();
            Send(Strings.Get("notify.offline_title"), Strings.Format("notify.offline_body", balance.OFFLINE_CAP / 3600),
                now.AddSeconds(balance.OFFLINE_CAP));
            Send(Strings.Get("notify.daily_title"), Strings.Get("notify.daily_body"), morning);
#else
            Log.Info(LogTag.Boot, "notifications would fire at " + now.AddSeconds(balance.OFFLINE_CAP) + " and " + morning);
#endif
        }

#if SOLOHERO_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
        private static void Send(string title, string text, DateTime at)
        {
            AndroidNotificationCenter.SendNotification(new AndroidNotification
            {
                Title = title,
                Text = text,
                FireTime = at
            }, ChannelId);
        }
#endif
    }
}
