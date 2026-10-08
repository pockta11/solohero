using System.Threading.Tasks;
using SoloHero.Core.Analytics;
using SoloHero.Core.Boot;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Progression;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
using SoloHero.Core.Skills;
using SoloHero.Game.Audio;
using SoloHero.Game.Config;
using SoloHero.Game.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoloHero.Game.Boot
{
    public sealed class BootSequence : MonoBehaviour
    {
        public const string GameSceneName = "Game";
        private const int QuitSaveTimeoutMs = 1500;

        [SerializeField] private BalanceConfig _balance;
        [SerializeField] private TextAsset _strings;
        [Tooltip("D-108: the loading screen; when set, the game scene loads asynchronously behind it.")]
        [SerializeField] private LoadingScreen _loading;

        private SaveService _save;
        private SaveDataV2 _data;
        private BalanceValues _balanceValues;
        private TrustedClock _clock;
        private bool _offlinePopupPending;

        /// <summary>D-133: the player came back before the server's clock was known; paid when it is.</summary>
        private bool _offlineDeferred;

        /// <summary>Monotonic reading at the last return (start-up or back from the background).</summary>
        private double _returnMonotonic;

        private async void Start()
        {
            Log.Sink = new UnityLogSink();
            ExceptionReporter.Install();
            if (_strings != null) Strings.Load(Strings.ParseTsv(_strings.text));
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
            // SDK callbacks (the server clock among them) are posted here; it must run while sign-in and loading await.
            if (GetComponent<MainThreadDispatcher>() == null) gameObject.AddComponent<MainThreadDispatcher>();
            // D-133: trusted time - the device clock at start-up, then the server's clock once Firebase connects.
            _clock = new TrustedClock(new SystemClock(), MonotonicTime.Seconds);
            _returnMonotonic = _clock.MonotonicSeconds;
            var clock = _clock;
            var auth = new AuthService(_clock);
            if (_loading != null) _loading.SetProgress(0.15f);
            BootReport report = await BootFlow.RunAsync(auth, CreateSave, balance, clock);
            if (_loading != null) _loading.SetProgress(0.5f);

            _save = report.Save;
            _data = report.Data;
            _offlinePopupPending = report.Offline.ShowPopup;
            _offlineDeferred = report.OfflineDeferred;

            Services.Register(balance);
            Services.Register<IClock>(clock);
            Services.Register<IRandom>(new SystemRandom());
            if (_save != null) Services.Register(_save);
            Services.Register(_data);
            ISaveRequester requester = _save != null ? new SaveRequestBridge(_save) : null;
            if (requester != null) Services.Register(requester);
            RegisterGrowth(balance, requester);
            RegisterAds(balance, clock, requester);
            RegisterSettingsAndAudio();
            Services.Register<IAnalytics>(new AnalyticsService(auth.FirebaseReady));
            ExceptionReporter.Flush();
            // D-134 account tools (settings > account): transfer codes and data deletion.
            Services.Register(new AccountService(report.UserId, _save, _data, new NewtonsoftSaveSerializer(), clock));
            _balanceValues = balance;
            // D-119: the game is open, so pending reminders go away (and the channel exists for the next leave).
            SoloHero.Game.Infrastructure.LocalNotifications.Init(_data);

            if (report.LoadFailed)
            {
                LoadFailBanner banner = FindObjectOfType<LoadFailBanner>();
                if (banner != null)
                    banner.Show();
                else
                    Log.Warn(LogTag.Boot, "load failed, started a new user");
            }

            if (report.Offline.ShowPopup)
                ShowOfflinePopup(report.Offline);

            // D-133: no network at start-up - the offline gold waits for the server's clock (it may already be here).
            if (_offlineDeferred)
            {
                if (_clock.IsTrusted) PayDeferredOffline();
                else _clock.ServerTimeKnown += PayDeferredOffline;
            }

            EnterGame();
        }

        /// <summary>
        /// D-133 / GDD offline rule 1: coming back from the background pays the time away too (before, only a cold
        /// start did, and a process Android kept alive lost the hours). Untrusted time waits for the server's clock.
        /// </summary>
        private void ComeBack()
        {
            if (_data == null || _clock == null) return;
            _returnMonotonic = _clock.MonotonicSeconds;
            if (_offlinePopupPending || _offlineDeferred) return;
            if (!_clock.IsTrusted)
            {
                _offlineDeferred = true;
                _clock.ServerTimeKnown -= PayDeferredOffline;
                _clock.ServerTimeKnown += PayDeferredOffline;
                return;
            }

            PayOffline(_clock.UtcNowSeconds);
        }

        private void PayDeferredOffline()
        {
            _clock.ServerTimeKnown -= PayDeferredOffline;
            if (!_offlineDeferred || _data == null) return;
            _offlineDeferred = false;
            // Measured to the moment the player came back, not to now: the time played since is not offline time.
            PayOffline(_clock.UtcAt(_returnMonotonic));
        }

        private void PayOffline(long returnUtc)
        {
            OfflineReward offline = OfflineReturn.Apply(_balanceValues, _data, returnUtc, _clock.IsServerTime);
            if (offline.GrantNow || offline.ResetQuitTime) _save?.RequestSave(_data);
            if (!offline.ShowPopup) return;
            _offlinePopupPending = true;
            ShowOfflinePopup(offline);
        }

        private void ShowOfflinePopup(OfflineReward offline)
        {
            OfflineRewardPopup popup = FindObjectOfType<OfflineRewardPopup>();
            if (popup != null)
                popup.Show(offline.Gold, offline.CountedSeconds, _balanceValues != null ? _balanceValues.OFFLINE_CAP : 0);
        }

        private void RegisterAds(BalanceValues balance, IClock clock, ISaveRequester requester)
        {
            if (GetComponent<MainThreadDispatcher>() == null) gameObject.AddComponent<MainThreadDispatcher>();
            AdService ads = GetComponent<AdService>();
            Services.Register<IAdGateway>(ads != null ? ads : new NoAdGateway());
            Services.Register(new AdSlotPolicy(balance, _data, clock, requester));
            Services.Register(new GemShop(balance, requester));
            Services.Register(new SoloHero.Core.Daily.DailyService(balance, _data, clock, requester));
            Services.Register(new SoloHero.Core.Stage.DungeonService(balance, _data, clock, requester));
            // D-128 daily shop, D-130 infinite tower.
            Services.Register(new ShopService(balance, _data, clock, requester));
            Services.Register(new SoloHero.Core.Stage.TowerService(balance, _data, requester));
        }

        private void RegisterSettingsAndAudio()
        {
            var settings = new SettingsService(_data);
            Services.Register(settings);
            settings.Changed += () => Application.targetFrameRate = settings.TargetFrameRate;
            Application.targetFrameRate = settings.TargetFrameRate;

            AudioService audio = GetComponent<AudioService>();
            if (audio == null)
            {
                Log.Warn(LogTag.Audio, "no AudioService on the boot object, playing silent");
                return;
            }

            audio.Bind(settings);
            Services.Register(audio);
        }

        private void RegisterGrowth(BalanceValues balance, ISaveRequester requester)
        {
            Services.Register(new UpgradeService(_data, balance, requester));
            Services.Register(new ApService(_data, balance, requester));
            var jobs = new SoloHero.Core.Jobs.JobService(_data, balance, requester);
            Services.Register(jobs);
            // D-140: the strings name ATK, the AP main stat and the weapon after the hero's job line.
            SoloHero.Core.Jobs.JobTerms.Apply(SoloHero.Core.Jobs.JobService.LineOf(_data));
            jobs.Advanced += job => SoloHero.Core.Jobs.JobTerms.Apply(job.Line);
            // D-114: an older save gets the pets its cleared stages had unlocked (the D-102 companions).
            SoloHero.Core.Pets.PetService.EnsureOwned(_data);
            Services.Register(new SoloHero.Core.Pets.PetService(_data, balance, requester));
            var skills = new SkillService(_data, balance, requester);
            Services.Register(skills);
            // D-107: saves that advanced before the line-only rule get their line's commons and a usable loadout.
            SoloHero.Core.Jobs.JobService.EnsureLineSkills(_data, skills, balance);
            Services.Register(new SoloHero.Core.Talents.TalentService(_data, balance, requester));
            Services.Register(new EquipService(requester));
            var gacha = new GachaService(
                balance,
                GearTableValues.FromBalance(balance),
                Services.Get<IRandom>(),
                GachaCatalog.Standard(balance));
            Services.Register(gacha);
            Services.Register(new SkillSummonService(balance, GachaTableValues.FromBalance(balance), Services.Get<IRandom>()));
            Services.Register(new SoloHero.Core.Pets.PetSummonService(balance, GearTableValues.FromBalance(balance), Services.Get<IRandom>()));
            Services.Register(new TutorialService(balance, gacha, requester));
            Services.Register(new SoloHero.Core.Progression.GuideQuestService(balance, _data, requester));
            Services.Register(new SoloHero.Core.Progression.AchievementService(_data, requester));
        }

        public void NotifyOfflineClaimed()
        {
            _offlinePopupPending = false;
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

                if (_loading != null) StartCoroutine(LoadGameAsync());
                else SceneManager.LoadScene(GameSceneName);
            }
            catch (System.Exception e)
            {
                Log.Warn(LogTag.Boot, "game scene load failed, staying on boot: " + e.Message);
            }
        }

        /// <summary>
        /// D-108: loads the game scene behind the loading screen, reporting progress to its bar. The background
        /// loading priority goes to High for the load (Normal integrates only about 10 ms per frame, which made the
        /// asynchronous load several times slower than the old blocking one) and back to Normal afterwards.
        /// </summary>
        private System.Collections.IEnumerator LoadGameAsync()
        {
            ThreadPriority previous = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            AsyncOperation op = SceneManager.LoadSceneAsync(GameSceneName);
            if (op == null)
            {
                Application.backgroundLoadingPriority = previous;
                SceneManager.LoadScene(GameSceneName);
                yield break;
            }

            while (!op.isDone)
            {
                _loading.SetProgress(0.55f + op.progress * 0.4f);
                yield return null;
            }

            Application.backgroundLoadingPriority = previous;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SoloHero.Game.Infrastructure.LocalNotifications.ScheduleOnLeave(_data, _balanceValues);
                SaveOnQuit();
            }
            else
            {
                SoloHero.Game.Infrastructure.LocalNotifications.CancelAll();
                ComeBack();
            }
        }

        private void OnApplicationQuit()
        {
            SoloHero.Game.Infrastructure.LocalNotifications.ScheduleOnLeave(_data, _balanceValues);
            SaveOnQuit();
        }

        private async void SaveOnQuit()
        {
            await SaveBeforeQuitAsync();
        }

        /// <summary>Stamps the quit time and flushes the save; waits at most <see cref="QuitSaveTimeoutMs"/>.</summary>
        public async Task SaveBeforeQuitAsync()
        {
            if (_data == null || _save == null) return;
            try
            {
                // D-133: an unclaimed popup or untrusted time keeps the old stamp (the next trusted return measures from it).
                if (!_offlinePopupPending && _clock != null && _clock.IsTrusted)
                    OfflineReturn.StampLeave(_data, _clock.UtcNowSeconds, _clock.IsServerTime);
                Task flush = _save.FlushAsync(_data);
                await Task.WhenAny(flush, Task.Delay(QuitSaveTimeoutMs));
            }
            catch (System.Exception e)
            {
                Log.Warn(LogTag.Boot, "quit save failed: " + e.Message);
            }
        }
    }
}
