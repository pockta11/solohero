using System;
using System.Threading;

namespace SoloHero.Core.Security
{
    /// <summary>
    /// D-184 anti-tamper signals. The secure value types (<see cref="SecureDouble"/>, <see cref="SecureInt"/>,
    /// <see cref="SecureLong"/>) report here when the plain decoy they keep next to the masked value was changed in
    /// memory (a memory editor found the number and wrote it); the save seal reports a local backup whose seal does
    /// not match. The game keeps the real value either way - this only tells analytics once per kind.
    /// </summary>
    public static class TamperGuard
    {
        private static long _state = DateTime.UtcNow.Ticks ^ ((long)Environment.TickCount << 21);
        private static int _reported;

        /// <summary>Raised once per session with the first tamper kind seen ("memory", "save"...). Any thread.</summary>
        public static event Action<string> Detected;

        /// <summary>True once anything was reported this session.</summary>
        public static bool Tripped => Volatile.Read(ref _reported) != 0;

        public static void Report(string kind)
        {
            if (Interlocked.Exchange(ref _reported, 1) != 0) return;
            try
            {
                Detected?.Invoke(kind ?? "unknown");
            }
            catch (Exception)
            {
                // A listener must never break the value read that noticed the change.
            }
        }

        /// <summary>A fresh non-zero mask (splitmix64 over a shared counter; thread-safe).</summary>
        internal static long NextKey()
        {
            ulong z = unchecked((ulong)Interlocked.Add(ref _state, unchecked((long)0x9E3779B97F4A7C15UL)));
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return unchecked((long)(z | 1UL));
        }

        /// <summary>Tests only: forget the once-per-session report.</summary>
        public static void ResetForTests() => Interlocked.Exchange(ref _reported, 0);
    }
}
