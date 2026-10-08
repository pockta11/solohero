using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core.Analytics;
using SoloHero.Core.Save;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-134 account tools: transfer codes, adopting a transferred save, and holding saves back meanwhile.</summary>
    public sealed class AccountToolsTests
    {
        [Test]
        public void Generate_DrawsTwelveAlphabetCharacters()
        {
            string code = TransferCode.Generate();

            Assert.AreEqual(TransferCode.Length, code.Length);
            foreach (char c in code) StringAssert.Contains(c.ToString(), TransferCode.Alphabet);
        }

        [Test]
        public void Generate_MapsBytesOntoTheAlphabetEvenly()
        {
            string code = TransferCode.Generate(bytes =>
            {
                for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i * 32 + i);
            });

            Assert.AreEqual(TransferCode.Alphabet.Substring(0, TransferCode.Length), code);
        }

        [Test]
        public void Normalize_IgnoresCaseSpacesAndHyphens()
        {
            Assert.AreEqual("ABCDEFGH2345", TransferCode.Normalize(" abcd-efgh 2345 "));
            Assert.AreEqual("ABCDEFGH2345", TransferCode.Normalize(TransferCode.Format("ABCDEFGH2345")));
        }

        [Test]
        public void Normalize_WrongLengthOrLookAlikes_IsNull()
        {
            Assert.IsNull(TransferCode.Normalize(""));
            Assert.IsNull(TransferCode.Normalize("ABCD-EFGH-234"));
            Assert.IsNull(TransferCode.Normalize("ABCD-EFGH-23456"));
            Assert.IsNull(TransferCode.Normalize("ABCD-EFGH-2O45"));
            Assert.IsNull(TransferCode.Normalize("ABCD-EFGH-2145"));
        }

        [Test]
        public void Format_GroupsByFour()
        {
            Assert.AreEqual("ABCD-EFGH-2345", TransferCode.Format("ABCDEFGH2345"));
        }

        [Test]
        public void Adopt_RevisionAboveBothAndOfflineClockRestarted()
        {
            var incoming = new SaveDataV2 { saveRevision = 40, lastQuitTimeUtc = 100, notifyAsked = true, gold = 7 };
            var current = new SaveDataV2 { saveRevision = 90 };

            SaveDataV2 adopted = TransferCode.Adopt(incoming, current, 5000);

            Assert.AreEqual(91L, adopted.saveRevision);
            Assert.AreEqual(5000L, adopted.lastQuitTimeUtc);
            Assert.IsFalse(adopted.notifyAsked);
            Assert.AreEqual(7d, adopted.gold);
        }

        [Test]
        public void Suspend_DropsLaterSavesUntilResume()
        {
            var local = new RecordingStore();
            var remote = new RecordingStore();
            var service = new SaveService(local, new RevisionSerializer(), remote, debounceMs: 5000);
            var data = SaveDataV2.CreateNew();

            service.Suspend();
            service.RequestSave(data);
            service.FlushAsync(data).GetAwaiter().GetResult();

            Assert.IsTrue(service.IsSuspended);
            Assert.AreEqual(0, local.Saved.Count);
            Assert.AreEqual(0, remote.Saved.Count);
            Assert.AreEqual(0L, data.saveRevision);

            service.Resume();
            service.FlushAsync(data).GetAwaiter().GetResult();

            Assert.AreEqual(1, remote.Saved.Count);
        }

        [Test]
        public void WriteThroughAsync_WhileSuspended_WritesBothStores()
        {
            var local = new RecordingStore();
            var remote = new RecordingStore();
            var service = new SaveService(local, new RevisionSerializer(), remote, debounceMs: 5000);
            service.Suspend();

            service.WriteThroughAsync(new SaveDataV2 { saveRevision = 12 }).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "12" }, local.Saved);
            CollectionAssert.AreEqual(new[] { "12" }, remote.Saved);
        }

        [Test]
        public void ExceptionSummary_FirstLinesCutToTheParameterLimit()
        {
            string longMessage = "NullReferenceException: " + new string('x', 200) + "\nsecond line";
            ExceptionSummary summary = ExceptionSummary.From(longMessage, "  SoloHero.Game.Foo.Bar () (at Foo.cs:10)\nnext frame");

            Assert.AreEqual(ExceptionSummary.MaxText, summary.Message.Length);
            StringAssert.StartsWith("NullReferenceException: ", summary.Message);
            Assert.AreEqual("SoloHero.Game.Foo.Bar () (at Foo.cs:10)", summary.Where);
            Assert.AreEqual(summary.Key, ExceptionSummary.From(longMessage, "SoloHero.Game.Foo.Bar () (at Foo.cs:10)").Key);
        }

        private sealed class RecordingStore : ISaveStore
        {
            public readonly List<string> Saved = new List<string>();

            public Task<string> LoadJsonAsync() => Task.FromResult<string>(null);

            public Task SaveJsonAsync(string json)
            {
                Saved.Add(json);
                return Task.CompletedTask;
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
