using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Analytics;
using Firebase.Auth;
using Firebase.Database;
using SoloHero.Core.Common;
using SoloHero.Core.Save;
using UnityEngine;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-134 account tools behind the settings: the account id, device transfer codes and deleting every piece of the
    /// player's data. A transfer copies the save under transfers/{code} (readable for 24 h from the server's createdAt
    /// stamp, database rules in tools/firebase/database.rules.json); the new device takes it with a revision above its
    /// own and restarts on it.
    /// Deleting removes users/{uid}, the anonymous account and the local backup, then restarts as a new player. Both
    /// suspend the game's own saves first so nothing writes the old data back.
    /// </summary>
    public sealed class AccountService
    {
        public const int TimeoutMs = 12000;
        private const string IssuedCodeKey = "transfer_code";
        private const string IssuedUntilKey = "transfer_code_until";

        private readonly string _userId;
        private readonly SaveService _save;
        private readonly SaveDataV2 _data;
        private readonly ISaveSerializer _serializer;
        private readonly IClock _clock;

        public AccountService(string userId, SaveService save, SaveDataV2 data, ISaveSerializer serializer, IClock clock)
        {
            _userId = string.IsNullOrEmpty(userId) ? AuthService.LocalUserId : userId;
            _save = save;
            _data = data;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>The Firebase user id, or "local" when the game runs without the server.</summary>
        public string UserId => _userId;

        /// <summary>Signed in with Firebase: transfers need it, deleting also clears the server copy.</summary>
        public bool IsOnline => _userId != AuthService.LocalUserId;

        /// <summary>A code this device issued that has not expired yet (shown again when the window reopens).</summary>
        public string IssuedCode
        {
            get
            {
                string code = PlayerPrefs.GetString(IssuedCodeKey, "");
                long until = long.TryParse(PlayerPrefs.GetString(IssuedUntilKey, "0"), out long parsed) ? parsed : 0L;
                return code.Length == TransferCode.Length && until > _clock.UtcNowSeconds ? code : null;
            }
        }

        /// <summary>Hours left on <see cref="IssuedCode"/> (rounded up).</summary>
        public int IssuedHoursLeft
        {
            get
            {
                long until = long.TryParse(PlayerPrefs.GetString(IssuedUntilKey, "0"), out long parsed) ? parsed : 0L;
                long left = until - _clock.UtcNowSeconds;
                return left <= 0 ? 0 : (int)((left + 3599) / 3600);
            }
        }

        /// <summary>Puts the current save under a new code for 24 hours; an earlier code of this device is withdrawn.</summary>
        public async Task<(Result Result, string Code)> IssueTransferCodeAsync()
        {
            if (!IsOnline || _save == null || _data == null) return (Result.Fail(FailReason.NetworkUnavailable), null);
            try
            {
                string previous = PlayerPrefs.GetString(IssuedCodeKey, "");
                long expiresUtc = _clock.UtcNowSeconds + TransferCode.LifetimeSeconds;
                // The server stamps createdAt; the rules let the code be read for 24 hours from it.
                var record = new Dictionary<string, object>
                {
                    ["uid"] = _userId,
                    ["save"] = _serializer.ToJson(_data),
                    ["createdAt"] = ServerValue.Timestamp
                };

                // 2^60 codes: a clash with a live code is not a real case, so a refusal means the rules are missing.
                string code = TransferCode.Generate();
                await WithTimeout(Node(code).SetValueAsync(record));
                PlayerPrefs.SetString(IssuedCodeKey, code);
                PlayerPrefs.SetString(IssuedUntilKey, expiresUtc.ToString(System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
                if (previous.Length == TransferCode.Length && previous != code) _ = TryRemove(Node(previous));
                return (Result.Success, code);
            }
            catch (Exception e)
            {
                if (IsPermissionDenied(e)) Log.Warn(LogTag.Save, "the database rules refused the transfer (apply tools/firebase/database.rules.json)");
                Log.Warn(LogTag.Save, "transfer code not issued: " + Describe(e));
                return (Result.Fail(FailReason.NetworkUnavailable), null);
            }
        }

        /// <summary>Reads the save waiting under <paramref name="code"/> (already normalized).</summary>
        public async Task<(Result Result, SaveDataV2 Save)> FindTransferAsync(string code)
        {
            if (string.IsNullOrEmpty(code)) return (Result.Fail(FailReason.CodeInvalid), null);
            if (!IsOnline) return (Result.Fail(FailReason.NetworkUnavailable), null);
            try
            {
                DataSnapshot snapshot = await WithTimeout(Node(code).GetValueAsync());
                string json = snapshot != null && snapshot.Exists ? snapshot.Child("save").Value as string : null;
                SaveDataV2 incoming = string.IsNullOrEmpty(json) ? null : _serializer.FromV2Json(json);
                if (incoming == null) return (Result.Fail(FailReason.CodeNotFound), null);
                return (Result.Success, incoming);
            }
            catch (Exception e)
            {
                // The rules refuse a missing or expired code: both read as "not found".
                if (IsPermissionDenied(e)) return (Result.Fail(FailReason.CodeNotFound), null);
                Log.Warn(LogTag.Save, "transfer lookup failed: " + Describe(e));
                return (Result.Fail(FailReason.NetworkUnavailable), null);
            }
        }

        /// <summary>
        /// Replaces this device's progress with <paramref name="incoming"/>: saves stop, the adopted copy goes to both
        /// stores, the code is used up and the app restarts on it. Once the local copy is written the restart goes
        /// ahead even if the upload is slow - its higher revision wins and uploads on the next start (D-073).
        /// </summary>
        public async Task<Result> AcceptTransferAsync(string code, SaveDataV2 incoming)
        {
            if (incoming == null || _save == null) return Result.Fail(FailReason.CodeNotFound);
            _save.Suspend();
            Task write;
            try
            {
                SaveDataV2 adopted = TransferCode.Adopt(incoming, _data, _clock.UtcNowSeconds);
                write = _save.WriteThroughAsync(adopted);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "transfer not taken: " + e.Message);
                _save.Resume();
                return Result.Fail(FailReason.Busy);
            }

            if (write.IsFaulted)
            {
                Log.Warn(LogTag.Save, "transfer not taken: " + write.Exception?.GetBaseException().Message);
                _save.Resume();
                return Result.Fail(FailReason.Busy);
            }

            if (await Task.WhenAny(write, Task.Delay(TimeoutMs)) == write) await TryRemove(Node(code));
            Log.Info(LogTag.Save, "transferred save adopted, restarting");
            AppRestart.Now();
            return Result.Success;
        }

        /// <summary>
        /// Deletes the server copy (users/{uid}), the anonymous account and every local value, then restarts as a new
        /// player. Without the server it fails and nothing is lost; in local mode only the device holds data.
        /// </summary>
        public async Task<Result> DeleteAllDataAsync()
        {
            _save?.Suspend();
            if (IsOnline)
            {
                try
                {
                    await WithTimeout(GameDatabase.Instance.RootReference.Child("users").Child(_userId).RemoveValueAsync());
                }
                catch (Exception e)
                {
                    Log.Warn(LogTag.Save, "server data not deleted: " + Describe(e));
                    _save?.Resume();
                    return Result.Fail(FailReason.NetworkUnavailable);
                }

                // A transfer code holds a copy of the save; it goes too, while this account may still remove it.
                string issued = PlayerPrefs.GetString(IssuedCodeKey, "");
                if (issued.Length == TransferCode.Length) await TryRemove(Node(issued));
                await DeleteAccount();
            }

            try
            {
                FirebaseAnalytics.ResetAnalyticsData();
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "analytics id not reset: " + e.Message);
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Log.Info(LogTag.Save, "all data deleted, restarting");
            AppRestart.Now();
            return Result.Success;
        }

        /// <summary>
        /// The anonymous account goes with its data. Firebase may ask for a recent sign-in first, which an anonymous
        /// account cannot give; then it is signed out instead - the account holds nothing any more.
        /// </summary>
        private static async Task DeleteAccount()
        {
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            FirebaseUser user = auth.CurrentUser;
            if (user == null) return;
            try
            {
                await WithTimeout(user.DeleteAsync());
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "anonymous account not deleted, signing out: " + Describe(e));
                auth.SignOut();
            }
        }

        private static DatabaseReference Node(string code) =>
            GameDatabase.Instance.RootReference.Child("transfers").Child(code);

        private static async Task TryRemove(DatabaseReference node)
        {
            try
            {
                await WithTimeout(node.RemoveValueAsync());
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Save, "transfer code not removed (it expires on its own): " + Describe(e));
            }
        }

        /// <summary>The database refused the request (rules): Firebase hides it as "Internal task faulted" around the cause.</summary>
        private static bool IsPermissionDenied(Exception e) =>
            Describe(e).IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Every message down the exception chain - Firebase wraps the useful one in an AggregateException.</summary>
        private static string Describe(Exception e)
        {
            var text = new System.Text.StringBuilder();
            for (Exception current = e; current != null; current = current.InnerException)
            {
                if (current is AggregateException aggregate)
                {
                    foreach (Exception inner in aggregate.Flatten().InnerExceptions)
                        if (inner != null) text.Append(text.Length > 0 ? " / " : "").Append(inner.Message);
                    break;
                }

                text.Append(text.Length > 0 ? " / " : "").Append(current.Message);
            }

            return text.ToString();
        }

        private static async Task WithTimeout(Task task)
        {
            if (await Task.WhenAny(task, Task.Delay(TimeoutMs)) != task) throw new TimeoutException("no answer from the server");
            await task;
        }

        private static async Task<T> WithTimeout<T>(Task<T> task)
        {
            if (await Task.WhenAny(task, Task.Delay(TimeoutMs)) != task) throw new TimeoutException("no answer from the server");
            return await task;
        }
    }
}
