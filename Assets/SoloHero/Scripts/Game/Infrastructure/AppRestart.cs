using System;
using SoloHero.Core.Common;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-134: starts the app over on fresh data (a transferred save, or a new player after deleting everything). The
    /// launch activity is started as a new task and this process ends, so every service, listener and static starts
    /// from scratch - the same path as a cold start. The editor leaves play mode instead.
    /// </summary>
    public static class AppRestart
    {
        public static void Now()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_ANDROID
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (AndroidJavaObject launch = manager.Call<AndroidJavaObject>("getLaunchIntentForPackage", activity.Call<string>("getPackageName")))
                using (AndroidJavaObject component = launch.Call<AndroidJavaObject>("getComponent"))
                using (var intents = new AndroidJavaClass("android.content.Intent"))
                using (AndroidJavaObject restart = intents.CallStatic<AndroidJavaObject>("makeRestartActivityTask", component))
                {
                    activity.Call("startActivity", restart);
                }

                using (var process = new AndroidJavaClass("android.os.Process"))
                    process.CallStatic("killProcess", process.CallStatic<int>("myPid"));
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "restart failed, quitting: " + e.Message);
                Application.Quit();
            }
#else
            Application.Quit();
#endif
        }
    }
}
