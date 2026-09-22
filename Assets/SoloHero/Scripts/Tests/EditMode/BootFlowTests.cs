using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core.Boot;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class BootFlowTests
    {
        [Test]
        public void Run_AuthThrows_UsesLocalSave()
        {
            var local = new MemoryStore { Json = "6:1:2" };
            BootReport report = BootFlow.RunAsync(
                new ThrowingAuth(),
                userId =>
                {
                    Assert.AreEqual("local", userId);
                    return new SaveService(local, new FakeSerializer(), debounceMs: 5000);
                },
                new BalanceValues(),
                new FixedClock(1000)).GetAwaiter().GetResult();

            Assert.IsTrue(report.UsedLocalMode);
            Assert.AreEqual(6d, report.Data.gold);
            Assert.IsFalse(report.LoadFailed);
        }

        [Test]
        public void Run_LoadThrows_StartsNewUser()
        {
            BootReport report = BootFlow.RunAsync(
                new FixedAuth("uid"),
                userId => new SaveService(new MemoryStore { LoadError = new InvalidOperationException("down") }, new FakeSerializer()),
                new BalanceValues(),
                new FixedClock(1000)).GetAwaiter().GetResult();

            Assert.IsTrue(report.LoadFailed);
            Assert.AreEqual(SaveDataV2.CurrentVersion, report.Data.dataVersion);
            Assert.AreEqual(0d, report.Data.gold);
            Assert.AreEqual("uid", report.UserId);
        }

        [Test]
        public void Run_ShortOffline_GrantsGold()
        {
            var stored = SaveDataV2.CreateNew();
            stored.lastQuitTimeUtc = 1000;
            var local = new MemoryStore { Json = new FakeSerializer().ToJson(stored) };
            var clock = new FixedClock(1030);

            BootReport report = BootFlow.RunAsync(
                new FixedAuth("local"),
                userId => new SaveService(local, new FakeSerializer(), debounceMs: 5000),
                new BalanceValues(),
                clock).GetAwaiter().GetResult();

            Assert.AreEqual(50d / 400d * 30d, report.Data.gold, 1e-9);
            Assert.AreEqual(1030L, report.Data.lastQuitTimeUtc);
            Assert.IsTrue(report.Offline.GrantNow);
        }

        private sealed class FixedClock : IClock
        {
            private readonly long _now;
            public FixedClock(long now) { _now = now; }
            public long UtcNowSeconds => _now;
            public DateTime LocalNow => DateTime.UnixEpoch;
        }

        private sealed class FixedAuth : IAuthGateway
        {
            private readonly string _userId;
            public FixedAuth(string userId) { _userId = userId; }
            public Task<string> SignInAsync() => Task.FromResult(_userId);
        }

        private sealed class ThrowingAuth : IAuthGateway
        {
            public Task<string> SignInAsync() => throw new InvalidOperationException("no network");
        }

        private sealed class MemoryStore : ISaveStore
        {
            public string Json;
            public Exception LoadError;

            public Task<string> LoadJsonAsync()
            {
                if (LoadError != null) throw LoadError;
                return Task.FromResult(Json);
            }

            public Task SaveJsonAsync(string json)
            {
                Json = json;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeSerializer : ISaveSerializer
        {
            public string ToJson(SaveDataV2 data)
            {
                return data.gold.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                    + ":" + data.highestStage
                    + ":" + data.dataVersion
                    + ":" + data.lastQuitTimeUtc;
            }

            public SaveDataV2 FromV2Json(string json)
            {
                string[] parts = json.Split(':');
                return new SaveDataV2
                {
                    gold = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    highestStage = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                    farmingStage = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                    dataVersion = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                    lastQuitTimeUtc = parts.Length > 3
                        ? long.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture)
                        : 0L
                };
            }

            public PlayerDataV1 FromV1Json(string json) => null;
        }
    }
}
