using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using SoloHero.Core.Save;
using SoloHero.Core.Security;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// D-184 anti-tamper: masked values in memory (and their decoy), the save seal (fixed vectors - the key must never
    /// change), and what SaveService lets an unsealed or edited local backup do.
    /// </summary>
    public sealed class AntiTamperTests
    {
        [SetUp]
        public void ResetGuard() => TamperGuard.ResetForTests();

        // ---- Masked values ----

        [Test]
        public void SecureDouble_RoundTripsEveryValue_AndActsLikeADouble()
        {
            foreach (double v in new[] { 0d, -0d, 1d, -1d, 0.1, 12345.678, 1e300, -1e-300, double.MaxValue, double.Epsilon,
                         double.PositiveInfinity, double.NegativeInfinity })
            {
                SecureDouble s = v;
                Assert.AreEqual(BitConverter.DoubleToInt64Bits(v), BitConverter.DoubleToInt64Bits(s), v.ToString("R", CultureInfo.InvariantCulture));
            }

            SecureDouble nan = double.NaN;
            Assert.IsTrue(double.IsNaN(nan));

            SecureDouble gold = 100d;
            gold += 50;
            gold -= 0.5;
            gold++;
            Assert.AreEqual(150.5d, (double)gold, 1e-12);
            Assert.IsTrue(gold > 150 && gold < 151 && gold >= 150.5 && gold == 150.5);
            Assert.AreEqual(301d, gold * 2, 1e-12);
            Assert.AreEqual(150.5d, Math.Max(gold, 3d), 1e-12);
            Assert.AreEqual("150.5", gold.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual("151", string.Format(CultureInfo.InvariantCulture, "{0:0}", gold));
            Assert.AreEqual(gold, (SecureDouble)150.5);
            Assert.IsTrue(gold.Equals(150.5));
            Assert.AreEqual(150.5, gold, "NUnit compares a double with it");
            Assert.AreEqual(0d, default(SecureDouble), "default is zero");
            Assert.IsFalse(TamperGuard.Tripped);
        }

        [Test]
        public void SecureInt_AndSecureLong_RoundTrip_AndCount()
        {
            foreach (int v in new[] { 0, 1, -1, 7, int.MaxValue, int.MinValue })
            {
                SecureInt s = v;
                Assert.AreEqual(v, (int)s);
            }

            foreach (long v in new[] { 0L, 1L, -1L, 1_800_000_000L, long.MaxValue, long.MinValue })
            {
                SecureLong s = v;
                Assert.AreEqual(v, (long)s);
            }

            SecureInt tickets = 0;
            tickets += 10;
            tickets++;
            tickets--;
            tickets -= 3;
            Assert.AreEqual(7, tickets);
            Assert.AreEqual(7L, (long)tickets);
            Assert.AreEqual(7, Math.Max(tickets, 2));
            var list = new List<int> { 4, 5, 6, 7, 8, 9, 10, 11 };
            Assert.AreEqual(11, list[tickets], "indexes like an int");
            Assert.AreEqual("7", tickets.ToString());
            Assert.AreEqual("x7", "x" + tickets);
            SecureLong vip = 100L;
            vip += 86400L;
            Assert.AreEqual(86500L, vip);
            Assert.IsFalse(TamperGuard.Tripped);
        }

        [Test]
        public void SecureDouble_ValueIsNotInMemoryAsIs_AndEachWriteMasksAnew()
        {
            SecureDouble a = 123456d;
            SecureDouble b = 123456d;
            long plain = BitConverter.DoubleToInt64Bits(123456d);
            Assert.AreNotEqual(plain, Field<long>(a, "_hidden"), "the stored bits are not the number's bits");
            Assert.AreNotEqual(Field<long>(a, "_hidden"), Field<long>(b, "_hidden"), "the same value is stored differently each time");

            SecureInt t = 999;
            Assert.AreNotEqual(999, Field<int>(t, "_hidden"));
        }

        [Test]
        public void MemoryEditOfTheVisibleNumber_IsIgnored_AndReported()
        {
            int seen = 0;
            string kind = null;
            Action<string> listener = k =>
            {
                seen++;
                kind = k;
            };
            TamperGuard.Detected += listener;
            try
            {
                object gems = (SecureDouble)500d;
                // A memory editor finds the plain number (the decoy) and writes 999999 over it.
                typeof(SecureDouble).GetField("_decoy", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gems, 999999d);
                Assert.AreEqual(500d, (double)(SecureDouble)gems, "the real value is kept");
                Assert.AreEqual(500d, (double)(SecureDouble)gems);
                Assert.IsTrue(TamperGuard.Tripped);
                Assert.AreEqual(1, seen, "reported once per session");
                Assert.AreEqual("memory", kind);

                object stones = (SecureInt)30;
                typeof(SecureInt).GetField("_decoy", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stones, 9999);
                Assert.AreEqual(30, (int)(SecureInt)stones);
                Assert.AreEqual(1, seen);
            }
            finally
            {
                TamperGuard.Detected -= listener;
            }
        }

        [Test]
        public void SaveData_ProtectedFields_StartAtZero_WithoutTamperSigns()
        {
            SaveDataV2 data = SaveDataV2.CreateNew();
            Assert.AreEqual(0d, (double)data.gold + data.gem);
            Assert.AreEqual(0, data.gearTickets + data.skillTickets + data.petTickets + data.breakStones + data.awakenStones + data.raidMedals);
            Assert.AreEqual(0, data.pityCount + data.skillPityCount + data.petPityCount);
            Assert.AreEqual(0L, (long)data.vipUntilUtc);
            Assert.IsFalse(data.untrustedLocal);
            Assert.IsFalse(TamperGuard.Tripped);
        }

        private static T Field<T>(object boxed, string name) =>
            (T)boxed.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(boxed);

        // ---- Save seal ----

        [Test]
        public void Seal_FixedVectors_TheKeyNeverChanges()
        {
            // If this fails the seal key changed: every sealed backup on every device would read as edited.
            Assert.AreEqual("SH1:krEicwChXADTJVwPbgHCYG1qA2YYev9nYSl6d8JsUEA=:{}", SaveSeal.Seal("{}"));
            Assert.AreEqual("SH1:lJoRJgGCsjepxBNCcV7QVgfI33rVqkz05n0eIBOIFLw=:{\"gold\":12.5,\"\uC774\uB984\":\"\uC601\uC6C5\"}",
                SaveSeal.Seal("{\"gold\":12.5,\"\uC774\uB984\":\"\uC601\uC6C5\"}"));
        }

        [Test]
        public void Seal_Open_TellsSealedLegacyBrokenAndMissingApart()
        {
            const string json = "{\"gold\":1500.0,\"gem\":30.0,\"saveRevision\":42}";
            string sealedText = SaveSeal.Seal(json);

            Assert.AreEqual(LocalSaveTrust.Sealed, SaveSeal.Open(sealedText, out string read));
            Assert.AreEqual(json, read);

            Assert.AreEqual(LocalSaveTrust.Legacy, SaveSeal.Open(json, out read), "a plain save from before the seal");
            Assert.AreEqual(json, read);

            string edited = sealedText.Replace("30.0", "99999.0");
            Assert.AreEqual(LocalSaveTrust.Broken, SaveSeal.Open(edited, out read), "an edited number");
            Assert.AreEqual(json.Replace("30.0", "99999.0"), read, "the text is still handed back (for an offline start)");

            string badMac = "SH1:" + new string('A', 43) + "=:" + json;
            Assert.AreEqual(LocalSaveTrust.Broken, SaveSeal.Open(badMac, out _));
            Assert.AreEqual(LocalSaveTrust.Broken, SaveSeal.Open("SH1:short:{}", out _));
            Assert.AreEqual(LocalSaveTrust.Broken, SaveSeal.Open("garbage", out read));
            Assert.IsNull(read);
            Assert.AreEqual(LocalSaveTrust.Missing, SaveSeal.Open(null, out _));
            Assert.AreEqual(LocalSaveTrust.Missing, SaveSeal.Open("", out _));
        }

        // ---- What an unsealed or edited local backup may do ----

        [Test]
        public void Online_EditedLocalWithHigherRevision_LosesToTheServer_AndIsReported()
        {
            var raw = new Store { Json = SaveSeal.Seal(Json(100, 5)).Replace("\"g\":100", "\"g\":999999").Replace("\"r\":5", "\"r\":99") };
            var remote = new Store { Json = Json(100, 5) };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual(100d, data.gold, "the server's copy wins");
            Assert.AreEqual(5L, data.saveRevision);
            Assert.IsTrue(TamperGuard.Tripped, "the edited backup is reported");
            Assert.IsFalse(service.UploadsBlocked);
            Assert.AreEqual(LocalSaveTrust.Sealed, SaveSeal.Open(raw.Json, out string kept), "the backup is replaced by the sealed server copy");
            Assert.AreEqual(Json(100, 5), kept);
        }

        [Test]
        public void Online_UnsealedLocalWithHigherRevision_LosesToTheServer()
        {
            var raw = new Store { Json = Json(999999, 99) };
            var remote = new Store { Json = Json(100, 5) };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual(100d, data.gold);
            Assert.IsFalse(TamperGuard.Tripped, "an old plain backup is not a tamper sign by itself");
            Assert.AreEqual(0, remote.Saved.Count, "nothing uploaded");
        }

        [Test]
        public void Online_SealedLocalWithHigherRevision_StillBeatsTheServer_AndUploads()
        {
            var raw = new Store { Json = SaveSeal.Seal(Json(300, 9)) };
            var remote = new Store { Json = Json(100, 5) };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();

            Assert.AreEqual(300d, data.gold, "the app stopped before the upload: the device's copy is newer (D-073)");
            Assert.AreEqual(9L, data.saveRevision);
            WaitFor(() => remote.Saved.Count == 1);
            Assert.AreEqual(Json(300, 9), remote.Saved[0]);
        }

        [Test]
        public void Offline_EditedLocal_PlaysFlagged_NeverUploads_AndTheServerWinsNextTime()
        {
            var raw = new Store { Json = Json(999999, 99) };
            var remote = new Store { LoadError = new TimeoutException("no network") };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(999999d, data.gold, "it plays (the device cannot tell an old backup from an edited one alone)");
            Assert.IsTrue(data.untrustedLocal);
            Assert.IsTrue(service.UploadsBlocked);

            data.gold += 1;
            remote.LoadError = null;
            remote.Json = Json(100, 5);
            service.FlushAsync(data).GetAwaiter().GetResult();
            Assert.AreEqual(0, remote.Saved.Count, "the flagged session never uploads");
            Assert.AreEqual(LocalSaveTrust.Sealed, SaveSeal.Open(raw.Json, out string local));
            StringAssert.Contains("\"u\":1", local, "the flag rides in the sealed backup");

            // Next start with the network: the flagged copy has the higher revision but the server's copy wins.
            var next = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);
            SaveDataV2 again = next.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(100d, again.gold);
            Assert.IsFalse(again.untrustedLocal);
            Assert.IsFalse(next.UploadsBlocked);
        }

        [Test]
        public void Offline_SealedLocal_IsTrusted_AndUploadsLater()
        {
            var raw = new Store { Json = SaveSeal.Seal(Json(250, 7)) };
            var remote = new Store { LoadError = new TimeoutException("no network") };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(250d, data.gold);
            Assert.IsFalse(data.untrustedLocal);
            Assert.IsFalse(service.UploadsBlocked);
            service.FlushAsync(data).GetAwaiter().GetResult();
            Assert.AreEqual(1, remote.Saved.Count);
        }

        [Test]
        public void ServerHasNoCopy_EditedLocal_StaysOnTheDevice_AndIsNeverAdopted()
        {
            // A rooted device drops its Firebase session (a fresh, empty account) and keeps the edited backup.
            var raw = new Store { Json = SaveSeal.Seal(Json(100, 3)).Replace("\"g\":100", "\"g\":999999") };
            var remote = new Store { Json = null };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(999999d, data.gold, "nothing is deleted on the device");
            Assert.IsTrue(data.untrustedLocal);
            Assert.IsTrue(service.UploadsBlocked, "the empty server never adopts it");
            service.FlushAsync(data).GetAwaiter().GetResult();
            Assert.AreEqual(0, remote.Saved.Count);

            // The flag rides in the re-sealed backup: the next start with the same empty server still does not upload.
            var next = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);
            SaveDataV2 again = next.LoadAsync().GetAwaiter().GetResult();
            Assert.IsTrue(again.untrustedLocal);
            Assert.IsTrue(next.UploadsBlocked);
        }

        [Test]
        public void ServerHasNoCopy_SealedLocal_IsTrusted_AndUploads()
        {
            var raw = new Store { Json = SaveSeal.Seal(Json(4200, 3)) };
            var remote = new Store { Json = null };
            var service = new SaveService(new SealedSaveStore(raw), new Serializer(), remote);

            SaveDataV2 data = service.LoadAsync().GetAwaiter().GetResult();
            Assert.AreEqual(4200d, data.gold, "a first online start after playing offline keeps its progress");
            Assert.IsFalse(data.untrustedLocal);
            Assert.IsFalse(service.UploadsBlocked);
            service.FlushAsync(data).GetAwaiter().GetResult();
            Assert.AreEqual(1, remote.Saved.Count);
        }

        [Test]
        public void EverySaveIsSealed()
        {
            var raw = new Store();
            var service = new SaveService(new SealedSaveStore(raw), new Serializer());
            SaveDataV2 data = SaveDataV2.CreateNew();
            data.gold = 12;
            service.RequestSave(data);
            Assert.AreEqual(LocalSaveTrust.Sealed, SaveSeal.Open(raw.Json, out string json));
            Assert.AreEqual(Json(12, 1), json);
        }

        private static void WaitFor(Func<bool> condition)
        {
            for (int i = 0; i < 200 && !condition(); i++) System.Threading.Thread.Sleep(10);
            Assert.IsTrue(condition());
        }

        private static string Json(double gold, long revision, bool untrusted = false) =>
            "{\"g\":" + gold.ToString("R", CultureInfo.InvariantCulture) + ",\"r\":" + revision + ",\"u\":" + (untrusted ? 1 : 0) + "}";

        private sealed class Store : ISaveStore
        {
            public string Json;
            public Exception LoadError;
            public readonly List<string> Saved = new List<string>();

            public Task<string> LoadJsonAsync()
            {
                if (LoadError != null) throw LoadError;
                return Task.FromResult(Json);
            }

            public Task SaveJsonAsync(string json)
            {
                lock (Saved) Saved.Add(json);
                Json = json;
                return Task.CompletedTask;
            }
        }

        /// <summary>{"g":gold,"r":revision,"u":0|1} - just enough of a save to see who won.</summary>
        private sealed class Serializer : ISaveSerializer
        {
            public string ToJson(SaveDataV2 data) => Json(data.gold, data.saveRevision, data.untrustedLocal);

            public SaveDataV2 FromV2Json(string json)
            {
                var data = new SaveDataV2();
                foreach (string part in json.Trim('{', '}').Split(','))
                {
                    string[] kv = part.Split(':');
                    string key = kv[0].Trim('"');
                    if (key == "g") data.gold = double.Parse(kv[1], CultureInfo.InvariantCulture);
                    else if (key == "r") data.saveRevision = long.Parse(kv[1], CultureInfo.InvariantCulture);
                    else if (key == "u") data.untrustedLocal = kv[1] == "1";
                }

                return data;
            }

            public PlayerDataV1 FromV1Json(string json) => null;
        }
    }
}
