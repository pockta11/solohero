using System;
using System.Security.Cryptography;
using System.Text;

namespace SoloHero.Core.Security
{
    /// <summary>What <see cref="SaveSeal.Open"/> found in a stored save.</summary>
    public enum LocalSaveTrust
    {
        /// <summary>Nothing stored.</summary>
        Missing = 0,

        /// <summary>Sealed by this game and unchanged since.</summary>
        Sealed = 1,

        /// <summary>A plain JSON save from before the seal (D-184) - or one with its seal cut off.</summary>
        Legacy = 2,

        /// <summary>A seal that does not match the text: the file was edited (or damaged).</summary>
        Broken = 3
    }

    /// <summary>
    /// D-184: an HMAC-SHA256 seal on the save text kept on the device (the local backup, a transfer copy), so a save
    /// edited by hand (rooted device, PlayerPrefs XML) is told apart from one the game wrote. Format:
    /// "SH1:" + base64(mac, 44 characters) + ":" + json. The key is assembled at run time and MUST NEVER CHANGE: every
    /// sealed save on every device would read as Broken.
    /// </summary>
    public static class SaveSeal
    {
        public const string Prefix = "SH1:";
        private const int MacChars = 44;

        private static readonly byte[] PartA = { 0x3E, 0x0B, 0xDD, 0x7D, 0x71, 0x1C, 0x26, 0xAF, 0xD3, 0xBC, 0xE5, 0x5A, 0xD1, 0x69, 0xFF, 0x15 };

        public static string Seal(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return Prefix + Mac(json) + ":" + json;
        }

        /// <summary>Reads a stored save: <paramref name="json"/> is its text (null when there is none to read).</summary>
        public static LocalSaveTrust Open(string stored, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(stored)) return LocalSaveTrust.Missing;
            if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            {
                string trimmed = stored.TrimStart();
                if (trimmed.StartsWith("{", StringComparison.Ordinal))
                {
                    json = stored;
                    return LocalSaveTrust.Legacy;
                }

                return LocalSaveTrust.Broken;
            }

            int split = Prefix.Length + MacChars;
            if (stored.Length <= split || stored[split] != ':') return LocalSaveTrust.Broken;
            string mac = stored.Substring(Prefix.Length, MacChars);
            json = stored.Substring(split + 1);
            return FixedTimeEquals(mac, Mac(json)) ? LocalSaveTrust.Sealed : LocalSaveTrust.Broken;
        }

        private static string Mac(string json)
        {
            using (var hmac = new HMACSHA256(Key()))
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(json)));
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        /// <summary>The 32-byte key, put together from two halves kept apart (never change either).</summary>
        private static byte[] Key()
        {
            byte[] partB = SaveSealPart.B();
            var key = new byte[32];
            for (int i = 0; i < 16; i++)
            {
                key[i] = (byte)(PartA[i] ^ (0x5A + i * 7));
                key[16 + i] = (byte)(partB[i] ^ (0xA5 - i * 11));
            }

            return key;
        }
    }

    /// <summary>D-184: the second half of the save seal key (apart from the first on purpose; never change it).</summary>
    internal static class SaveSealPart
    {
        internal static byte[] B() => new byte[] { 0xFB, 0xF2, 0x3F, 0x5B, 0x3E, 0xA3, 0xEC, 0xCC, 0x08, 0x23, 0xD0, 0x93, 0x54, 0x52, 0xFC, 0x22 };
    }
}
