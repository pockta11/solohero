using System;
using System.Diagnostics;
using SoloHero.Core.Common;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-133: seconds that only move forward and keep counting while the device sleeps - the monotonic source of the
    /// TrustedClock. On Android it is SystemClock.elapsedRealtime() through cached JNI ids (no allocation per call; the
    /// TrustedClock is read every frame by the boosters). The editor, and a device where binding fails, use a
    /// Stopwatch for the whole session (switching sources mid-session would make time jump).
    /// </summary>
    public static class MonotonicTime
    {
        private static readonly Stopwatch Watch = Stopwatch.StartNew();

#if UNITY_ANDROID && !UNITY_EDITOR
        private static readonly jvalue[] NoArgs = new jvalue[0];
        private static IntPtr _class;
        private static IntPtr _method;
        private static int _mode; // 0 not bound yet, 1 elapsedRealtime, 2 stopwatch
#endif

        public static double Seconds()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_mode == 0) Bind();
            if (_mode == 1) return AndroidJNI.CallStaticLongMethod(_class, _method, NoArgs) / 1000d;
#endif
            return Watch.Elapsed.TotalSeconds;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void Bind()
        {
            _mode = 2;
            try
            {
                IntPtr local = AndroidJNI.FindClass("android/os/SystemClock");
                if (local == IntPtr.Zero)
                {
                    AndroidJNI.ExceptionClear();
                    Log.Warn(LogTag.Boot, "SystemClock not found, time uses a stopwatch");
                    return;
                }

                _class = AndroidJNI.NewGlobalRef(local);
                AndroidJNI.DeleteLocalRef(local);
                _method = AndroidJNI.GetStaticMethodID(_class, "elapsedRealtime", "()J");
                if (_method == IntPtr.Zero)
                {
                    AndroidJNI.ExceptionClear();
                    Log.Warn(LogTag.Boot, "elapsedRealtime not found, time uses a stopwatch");
                    return;
                }

                _mode = 1;
            }
            catch (Exception e)
            {
                _mode = 2;
                Log.Warn(LogTag.Boot, "elapsedRealtime unavailable, time uses a stopwatch: " + e.Message);
            }
        }
#endif
    }
}
