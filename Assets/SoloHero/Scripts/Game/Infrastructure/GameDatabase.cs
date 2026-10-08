using Firebase.Database;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using SoloHero.Core.Common;
#endif

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// The Realtime Database the game talks to - saves, the server clock and the account tools all go through here.
    /// Release builds always use the project's database. QA (development builds only): a db_emulator.txt holding
    /// "host:port" in the app's files folder (adb push to /sdcard/Android/data/com.SoloSoft.solohero/files/; 10.0.2.2 is
    /// the PC from the Android emulator) switches to the Firebase emulator running tools/firebase/database.rules.json,
    /// so device transfer and data deletion can be tried without touching the live data. The file survives the wipe.
    /// </summary>
    public static class GameDatabase
    {
        private static FirebaseDatabase _instance;

        public static FirebaseDatabase Instance => _instance ?? (_instance = Resolve());

        private static FirebaseDatabase Resolve()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            try
            {
                string path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "db_emulator.txt");
                if (System.IO.File.Exists(path))
                {
                    string host = System.IO.File.ReadAllText(path).Trim();
                    // The default database's name is the namespace the emulator serves the project's rules on.
                    var live = new Uri(FirebaseDatabase.DefaultInstance.App.Options.DatabaseUrl.ToString());
                    string ns = live.Host.Split('.')[0];
                    string url = "http://" + host + "?ns=" + ns;
                    Log.Warn(LogTag.Boot, "QA: database emulator " + url);
                    return FirebaseDatabase.GetInstance(url);
                }
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "QA: database emulator not used: " + e.Message);
            }
#endif
            return FirebaseDatabase.DefaultInstance;
        }
    }
}
