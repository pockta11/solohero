using System.Threading.Tasks;
using SoloHero.Core.Boot;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Game.Config;
using SoloHero.Game.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoloHero.Game.Boot
{
    public sealed class BootSequence : MonoBehaviour
    {
        public const string GameSceneName = "Game";

        [SerializeField] private BalanceConfig _balance;

        private SaveService _save;
        private SaveDataV2 _data;
        private bool _offlinePopupPending;

        private async void Start()
        {
            DontDestroyOnLoad(gameObject);
            try
            {
                await RunAsync();
            }
            catch (System.Exception e)
            {
                Log.Error(LogTag.Boot, "boot stopped unexpectedly: " + e.Message);
            }
        }

        public async Task RunAsync()
        {
            BalanceValues balance = _balance != null ? _balance.ToValues() : new BalanceValues();
            var clock = new SystemClock();
            BootReport report = await BootFlow.RunAsync(new AuthService(), CreateSave, balance, clock);

            _save = report.Save;
            _data = report.Data;
            _offlinePopupPending = report.Offline.ShowPopup;

            Services.Register(balance);
            Services.Register<IClock>(clock);
            Services.Register<IRandom>(new SystemRandom());
            if (_save != null) Services.Register(_save);
            Services.Register(_data);

            if (report.LoadFailed)
                Log.Warn(LogTag.Boot, "load failed, started a new user");

            EnterGame();
        }

        private SaveService CreateSave(string userId)
        {
            var serializer = new NewtonsoftSaveSerializer();
            var localV2 = new LocalBackupStore(LocalBackupStore.V2Key);
            var localV1 = new LocalBackupStore(LocalBackupStore.V1Key);
            if (userId == AuthService.LocalUserId)
                return new SaveService(localV2, serializer, null, null, localV1);

            return new SaveService(
                localV2,
                serializer,
                new FirebaseSaveStore(FirebaseSaveStore.V2Node(userId)),
                new FirebaseSaveStore(FirebaseSaveStore.V1Node(userId)),
                localV1);
        }

        private void EnterGame()
        {
            try
            {
                if (!Application.CanStreamedLevelBeLoaded(GameSceneName))
                {
                    Log.Warn(LogTag.Boot, "game scene missing, staying on boot");
                    return;
                }

                SceneManager.LoadScene(GameSceneName);
            }
            catch (System.Exception e)
            {
                Log.Warn(LogTag.Boot, "game scene load failed, staying on boot: " + e.Message);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveOnQuit();
        }

        private void OnApplicationQuit()
        {
            SaveOnQuit();
        }

        private async void SaveOnQuit()
        {
            if (_data == null || _save == null) return;
            try
            {
                if (!_offlinePopupPending)
                    _data.lastQuitTimeUtc = new SystemClock().UtcNowSeconds;
                await _save.FlushAsync(_data);
            }
            catch (System.Exception e)
            {
                Log.Warn(LogTag.Boot, "quit save failed: " + e.Message);
            }
        }
    }
}
