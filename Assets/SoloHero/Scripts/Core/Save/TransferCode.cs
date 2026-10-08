using System;
using System.Security.Cryptography;
using System.Text;

namespace SoloHero.Core.Save
{
    /// <summary>
    /// D-134 device transfer codes: 12 characters from an alphabet without look-alikes (no I, O, 0, 1), shown as
    /// XXXX-XXXX-XXXX. 32^12 = 2^60 codes, drawn from a cryptographic source, so a valid code cannot be guessed in the
    /// 24 hours it lives. The save travels under the code; the new device copies it and deletes the code.
    /// </summary>
    public static class TransferCode
    {
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public const int Length = 12;
        public const int GroupLength = 4;

        /// <summary>How long a code can be redeemed (the database rules allow at most this much).</summary>
        public const long LifetimeSeconds = 24L * 60L * 60L;

        /// <summary>A new code from <paramref name="randomBytes"/> (cryptographic by default; tests pass their own).</summary>
        public static string Generate(Action<byte[]> randomBytes = null)
        {
            var bytes = new byte[Length];
            if (randomBytes != null)
            {
                randomBytes(bytes);
            }
            else
            {
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                    rng.GetBytes(bytes);
            }

            var code = new StringBuilder(Length);
            // 256 is a multiple of 32, so taking the low five bits keeps every character equally likely.
            for (int i = 0; i < Length; i++) code.Append(Alphabet[bytes[i] % Alphabet.Length]);
            return code.ToString();
        }

        /// <summary>
        /// What the player typed, as a code: case and the separators (spaces, hyphens) do not matter. Null when it is
        /// not 12 characters of the alphabet.
        /// </summary>
        public static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var code = new StringBuilder(Length);
            foreach (char raw in input)
            {
                if (raw == '-' || raw == ' ' || raw == '\t') continue;
                char c = char.ToUpperInvariant(raw);
                if (Alphabet.IndexOf(c) < 0) return null;
                code.Append(c);
                if (code.Length > Length) return null;
            }

            return code.Length == Length ? code.ToString() : null;
        }

        /// <summary>XXXX-XXXX-XXXX for display.</summary>
        public static string Format(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            var text = new StringBuilder(code.Length + code.Length / GroupLength);
            for (int i = 0; i < code.Length; i++)
            {
                if (i > 0 && i % GroupLength == 0) text.Append('-');
                text.Append(code[i]);
            }

            return text.ToString();
        }

        /// <summary>
        /// The copy the new device keeps: a revision above both saves (so D-073's newest-copy rule keeps it on every
        /// store), the offline clock started now (the old device already earned the time before), and the
        /// notification permission asked again on this device.
        /// </summary>
        public static SaveDataV2 Adopt(SaveDataV2 incoming, SaveDataV2 current, long nowUtc)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            long revision = current != null ? current.saveRevision : 0L;
            incoming.saveRevision = Math.Max(incoming.saveRevision, revision) + 1L;
            incoming.lastQuitTimeUtc = nowUtc;
            incoming.notifyAsked = false;
            return incoming;
        }
    }
}
