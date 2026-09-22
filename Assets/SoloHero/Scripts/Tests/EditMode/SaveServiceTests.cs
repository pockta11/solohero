using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    public sealed class SaveServiceTests
    {
        [SetUp]
        public void NoSyncContext()
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        [Test]
        public void RequestSave_TwiceBeforeDebounce_PersistsLastOnly()
        {
            var local = new MemoryStore();
            var remote = new MemoryStore();
            var service = new SaveService(local, new FakeSerializer(), remote, debounceMs: 40);

            service.RequestSave(Gold(1));
            service.RequestSave(Gold(2));
            Task.Delay(120).GetAwaiter().GetResult();

            Assert.AreEqual(1, remote.Saved.Count);
            Assert.AreEqual("2:1:2", remote.Saved[0]);
            Assert.AreEqual("2:1:2", local.Json);
        }

        [Test]
        public void FlushAsync_PersistsImmediately()
        {
            var local = new MemoryStore();
            var remote = new MemoryStore();
            var service = new SaveService(local, new FakeSerializer(), remote, debounceMs: 5000);

            service.FlushAsync(Gold(4)).GetAwaiter().GetResult();

            Assert.AreEqual(1, remote.Saved.Count);
            Assert.AreEqual("4:1:2", remote.Saved[0]);
        }

        [Test]
        public void RequestSave_DuringPersist_PersistsAgainWithLatest()
        {
            var local = new MemoryStore();
            var remote = new MemoryStore { SaveGate = new TaskCompletionSource<bool>() };
            var service = new SaveService(local, new FakeSerializer(), remote, debounceMs: 5000);

            Task flush = service.FlushAsync(Gold(1));
            Task.Delay(30).GetAwaiter().GetResult();
            service.RequestSave(Gold(2));
            remote.SaveGate.SetResult(true);
            flush.GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "1:1:2", "2:1:2" }, remote.Saved);
            Assert.AreEqual("2:1:2", local.Json);
        }

        [Test]
        public void RemoteSaveThrows_KeepsLocalAndDoesNotThrow()
        {
            var local = new MemoryStore();
            var remote = new MemoryStore { SaveError = new InvalidOperationException("offline") };
            var service = new SaveService(local, new FakeSerializer(), remote, debounceMs: 5000);

            service.FlushAsync(Gold(9)).GetAwaiter().GetResult();

            Assert.AreEqual("9:1:2", local.Json);
            Assert.AreEqual(1, remote.Saved.Count);
        }

        [Test]
        public void Load_RemoteFails_UsesLocal()
        {
            var local = new MemoryStore { Json = "8:1:2" };
            var remote = new MemoryStore { LoadError = new InvalidOperationException("down") };
            var service = new SaveService(local, new FakeSerializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual(8d, data.gold);
        }

        [Test]
        public void Load_RemoteMissing_MigratesV1()
        {
            var local = new MemoryStore();
            var remoteV2 = new MemoryStore();
            var remoteV1 = new MemoryStore { Json = "25" };
            var service = new SaveService(local, new FakeSerializer(), remoteV2, remoteV1);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual(25d, data.gold);
            Assert.AreEqual(SaveDataV2.CurrentVersion, data.dataVersion);
            Assert.AreEqual("25:1:2", local.Json);
        }

        [Test]
        public void Load_LocalMode_DoesNotTouchRemote()
        {
            var local = new MemoryStore { Json = "3:1:2" };
            var remote = new MemoryStore { LoadError = new InvalidOperationException("should not load") };
            var service = new SaveService(local, new FakeSerializer(), remoteV2: null);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual(3d, data.gold);
            Assert.AreEqual(0, remote.Loads);
        }

        private static SaveDataV2 Gold(double gold)
        {
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = gold;
            return data;
        }

        private sealed class MemoryStore : ISaveStore
        {
            public string Json;
            public int Loads;
            public Exception LoadError;
            public Exception SaveError;
            public TaskCompletionSource<bool> SaveGate;
            public readonly List<string> Saved = new List<string>();

            public Task<string> LoadJsonAsync()
            {
                Loads++;
                if (LoadError != null) throw LoadError;
                return Task.FromResult(Json);
            }

            public async Task SaveJsonAsync(string json)
            {
                Saved.Add(json);
                if (SaveGate != null && Saved.Count == 1) await SaveGate.Task;
                if (SaveError != null) throw SaveError;
                Json = json;
            }
        }

        private sealed class FakeSerializer : ISaveSerializer
        {
            public string ToJson(SaveDataV2 data)
            {
                return data.gold.ToString("R", CultureInfo.InvariantCulture)
                    + ":" + data.highestStage
                    + ":" + data.dataVersion;
            }

            public SaveDataV2 FromV2Json(string json)
            {
                string[] parts = json.Split(':');
                return new SaveDataV2
                {
                    gold = double.Parse(parts[0], CultureInfo.InvariantCulture),
                    highestStage = int.Parse(parts[1], CultureInfo.InvariantCulture),
                    farmingStage = int.Parse(parts[1], CultureInfo.InvariantCulture),
                    dataVersion = int.Parse(parts[2], CultureInfo.InvariantCulture)
                };
            }

            public PlayerDataV1 FromV1Json(string json)
            {
                return new PlayerDataV1
                {
                    gold = long.Parse(json, CultureInfo.InvariantCulture),
                    chapter = 1,
                    stageNumber = 1
                };
            }
        }
    }
}
