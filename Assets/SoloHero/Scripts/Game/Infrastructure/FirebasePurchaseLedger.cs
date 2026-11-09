using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using SoloHero.Core.Common;
using SoloHero.Core.Economy;

namespace SoloHero.Game.Infrastructure
{
    /// <summary>
    /// D-183: purchase records in the Realtime Database, purchases/{sha256(token)} = {uid, product, granted, at}. The
    /// rules (tools/firebase/database.rules.json) let a record be created once, only by the signed-in account and only
    /// with granted = false, then flipped to granted = true by that same account, and read only by it - so a receipt
    /// replayed into another install or account finds the record taken and pays nothing.
    /// Writing first and reading only after a refusal keeps it to one round trip for a new purchase.
    /// </summary>
    public sealed class FirebasePurchaseLedger : IPurchaseLedger
    {
        public const int TimeoutMs = 12000;

        private readonly string _userId;

        public FirebasePurchaseLedger(string userId)
        {
            _userId = userId;
        }

        public bool Online => !string.IsNullOrEmpty(_userId) && _userId != AuthService.LocalUserId;

        public async Task<LedgerClaim> ClaimAsync(string key, string productId)
        {
            if (!Online || !SignedInAsOwner()) return LedgerClaim.Unavailable;
            var record = new Dictionary<string, object>
            {
                ["uid"] = _userId,
                ["product"] = productId ?? "",
                ["granted"] = false,
                ["at"] = ServerValue.Timestamp
            };

            try
            {
                await WithTimeout(Node(key).SetValueAsync(record));
                return LedgerClaim.Fresh;
            }
            catch (Exception e)
            {
                if (!IsPermissionDenied(e))
                {
                    Log.Warn(LogTag.Boot, "purchase record not written (retried later): " + Describe(e));
                    return LedgerClaim.Unavailable;
                }
            }

            // Refused: usually the record exists - this account's (a restore, a retry after a crash) or another's (a
            // replay). The rules let anyone signed in see that a key is free, but only the owner read a record.
            try
            {
                DataSnapshot snapshot = await WithTimeout(Node(key).GetValueAsync());
                // Free key yet the write was refused: not a replay (rules not applied, a signed-out moment) - retry later.
                if (snapshot == null || !snapshot.Exists)
                {
                    Log.Warn(LogTag.Boot, "purchase record refused on a free key (database rules?), retried later");
                    return LedgerClaim.Unavailable;
                }

                if (!(snapshot.Child("uid").Value is string owner) || owner != _userId) return LedgerClaim.Taken;
                return snapshot.Child("granted").Value is bool granted && granted ? LedgerClaim.MineGranted : LedgerClaim.Mine;
            }
            catch (Exception e)
            {
                // Only the owner may read an existing record, so a refused read is another account's purchase - if the
                // rules are the ones we wrote: a free key must then be readable. If it is not (rules not applied yet),
                // nothing can be told apart and the purchase waits instead of being refused for good.
                if (IsPermissionDenied(e) && SignedInAsOwner() && await RulesAnswerFreeKeys()) return LedgerClaim.Taken;
                Log.Warn(LogTag.Boot, "purchase record not read (retried later): " + Describe(e));
                return LedgerClaim.Unavailable;
            }
        }

        /// <summary>A read of a key nobody holds; refused means the database rules are not the game's yet.</summary>
        private static async Task<bool> RulesAnswerFreeKeys()
        {
            try
            {
                await WithTimeout(Node(ProbeKey).GetValueAsync());
                return true;
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Boot, "purchase rules probe refused (apply tools/firebase/database.rules.json): " + Describe(e));
                return false;
            }
        }

        /// <summary>A well-formed key no real token hashes to in practice (all zeros).</summary>
        private static readonly string ProbeKey = new string('0', 64);

        /// <summary>The database session is this account's (a signed-out moment would make every rule refuse).</summary>
        private bool SignedInAsOwner()
        {
            try
            {
                FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
                return user != null && user.UserId == _userId;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task MarkGrantedAsync(string key)
        {
            if (!Online) return;
            await WithTimeout(Node(key).Child("granted").SetValueAsync(true));
        }

        private static DatabaseReference Node(string key) =>
            GameDatabase.Instance.RootReference.Child("purchases").Child(key);

        /// <summary>The database refused the request (rules): Firebase hides it as "Internal task faulted" around the cause.</summary>
        private static bool IsPermissionDenied(Exception e) =>
            Describe(e).IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0;

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
            if (await Task.WhenAny(task, Task.Delay(TimeoutMs)) != task)
            {
                // Observe a late fault so it is not an unobserved task exception.
                _ = task.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("no answer from the server");
            }

            await task;
        }

        private static async Task<T> WithTimeout<T>(Task<T> task)
        {
            if (await Task.WhenAny(task, Task.Delay(TimeoutMs)) != task)
            {
                _ = task.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("no answer from the server");
            }

            return await task;
        }
    }
}
