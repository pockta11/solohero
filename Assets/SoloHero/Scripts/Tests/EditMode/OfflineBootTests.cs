using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// D-136: without a network the server never answers; boot must still start from the local backup. The loads run
    /// on the thread pool: blocking Unity's main thread on a continuation that needs it would deadlock the test runner.
    /// </summary>
    public sealed class OfflineBootTests
    {
        [Test]
        public void LoadAsync_ServerNeverAnswers_StartsFromLocalBackupAfterTheWait()
        {
            var local = new Store { Json = "7" };
            var remote = new Store { Hang = true };
            var service = new SaveService(local, new RevisionSerializer(), remote, remoteLoadTimeoutMs: 50);
            var watch = Stopwatch.StartNew();

            SaveDataV2 data = Task.Run(() => service.LoadAsync()).GetAwaiter().GetResult();

            Assert.AreEqual(7L, data.saveRevision);
            Assert.Less(watch.ElapsedMilliseconds, 5000L);
        }

        [Test]
        public void LoadAsync_LocalNewerAndUploadHangs_DoesNotWaitForTheUpload()
        {
            var local = new Store { Json = "9" };
            var remote = new Store { Json = "4", HangSaves = true };
            var service = new SaveService(local, new RevisionSerializer(), remote, remoteLoadTimeoutMs: 50);

            Task<SaveDataV2> load = Task.Run(() => service.LoadAsync());

            Assert.IsTrue(load.Wait(5000));
            Assert.AreEqual(9L, load.Result.saveRevision);
            CollectionAssert.AreEqual(new[] { "9" }, remote.Saved);
        }

        private sealed class Store : ISaveStore
        {
            public string Json;
            public bool Hang;
            public bool HangSaves;
            public readonly List<string> Saved = new List<string>();

            public Task<string> LoadJsonAsync() => Hang ? new TaskCompletionSource<string>().Task : Task.FromResult(Json);

            public Task SaveJsonAsync(string json)
            {
                Saved.Add(json);
                return HangSaves ? new TaskCompletionSource<bool>().Task : Task.CompletedTask;
            }
        }

        /// <summary>The JSON of a save is just its revision.</summary>
        private sealed class RevisionSerializer : ISaveSerializer
        {
            public string ToJson(SaveDataV2 data) => data.saveRevision.ToString(CultureInfo.InvariantCulture);

            public SaveDataV2 FromV2Json(string json) => new SaveDataV2 { saveRevision = long.Parse(json, CultureInfo.InvariantCulture) };

            public PlayerDataV1 FromV1Json(string json) => null;
        }
    }
}
