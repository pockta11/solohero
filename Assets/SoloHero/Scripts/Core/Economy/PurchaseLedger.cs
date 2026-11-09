using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SoloHero.Core.Economy
{
    /// <summary>D-183: what the purchase ledger says about a purchase token.</summary>
    public enum LedgerClaim
    {
        /// <summary>First time anyone reported it: it is now recorded for this account, not granted yet.</summary>
        Fresh = 0,

        /// <summary>Recorded for this account earlier but not marked granted (a crash between record and grant).</summary>
        Mine = 1,

        /// <summary>Recorded for this account and already granted: pay nothing.</summary>
        MineGranted = 2,

        /// <summary>Recorded for another account: a receipt replayed from somewhere else. Never pay it.</summary>
        Taken = 3,

        /// <summary>The ledger could not be asked (no network, timed out): try again later.</summary>
        Unavailable = 4
    }

    /// <summary>
    /// D-183: the server-side record that makes each store purchase payable once, to one account
    /// (purchases/{key} in the Realtime Database: create-only, owner-only, granted false then true - see
    /// tools/firebase/database.rules.json). The local token history guards a restore on the same device; the ledger
    /// guards a receipt replayed into another install or account.
    /// </summary>
    public interface IPurchaseLedger
    {
        /// <summary>False when purchases cannot be recorded at all (no server account); the shop then stays closed.</summary>
        bool Online { get; }

        Task<LedgerClaim> ClaimAsync(string key, string productId);

        /// <summary>Marks a claimed purchase as paid (best effort: a failure leaves it claimed by this account).</summary>
        Task MarkGrantedAsync(string key);
    }

    /// <summary>
    /// The ledger of development builds and tests (the fake store has no real receipts): remembers claims in memory
    /// for the session.
    /// </summary>
    public sealed class LocalPurchaseLedger : IPurchaseLedger
    {
        private readonly Dictionary<string, bool> _claims = new Dictionary<string, bool>();

        public bool Online { get; set; } = true;

        /// <summary>Tests: what the next claim answers instead of the bookkeeping (null = normal).</summary>
        public LedgerClaim? Override { get; set; }

        public Task<LedgerClaim> ClaimAsync(string key, string productId)
        {
            if (Override.HasValue) return Task.FromResult(Override.Value);
            if (!Online) return Task.FromResult(LedgerClaim.Unavailable);
            if (_claims.TryGetValue(key, out bool granted)) return Task.FromResult(granted ? LedgerClaim.MineGranted : LedgerClaim.Mine);
            _claims[key] = false;
            return Task.FromResult(LedgerClaim.Fresh);
        }

        public Task MarkGrantedAsync(string key)
        {
            if (_claims.ContainsKey(key)) _claims[key] = true;
            return Task.CompletedTask;
        }

        public bool IsGranted(string key) => _claims.TryGetValue(key, out bool granted) && granted;
    }

    /// <summary>D-183: ledger keys - the token's hash, so the shared ledger never holds a purchase token itself.</summary>
    public static class PurchaseTokens
    {
        /// <summary>Lower-case hex SHA-256 of the token (64 characters, a valid database key).</summary>
        public static string Key(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentException("empty purchase token", nameof(token));
            // The managed class directly: SHA256.Create() looks it up by name, which code stripping can break on IL2CPP.
            using (SHA256 sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
                var text = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) text.Append(b.ToString("x2"));
                return text.ToString();
            }
        }
    }
}
