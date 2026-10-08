using System;

namespace SoloHero.Core.Analytics
{
    /// <summary>
    /// P1-2: the short form of a logged exception for the "app_exception" event - the first line of the message and the
    /// first stack frame, each cut to Firebase's 100-character parameter limit - and the key that de-duplicates repeats.
    /// </summary>
    public readonly struct ExceptionSummary
    {
        public const int MaxText = 100;

        public readonly string Message;
        public readonly string Where;

        private ExceptionSummary(string message, string where)
        {
            Message = message;
            Where = where;
        }

        /// <summary>Same message and frame = same problem: reported once per session.</summary>
        public string Key => Message + "|" + Where;

        public static ExceptionSummary From(string condition, string stackTrace)
        {
            return new ExceptionSummary(Cut(FirstLine(condition)), Cut(FirstLine(stackTrace)));
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string trimmed = text.Trim();
            int end = trimmed.IndexOfAny(new[] { '\r', '\n' });
            return end < 0 ? trimmed : trimmed.Substring(0, end).Trim();
        }

        private static string Cut(string text) => text.Length <= MaxText ? text : text.Substring(0, MaxText);
    }
}
