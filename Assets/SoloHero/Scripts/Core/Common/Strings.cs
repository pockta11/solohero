using System;
using System.Collections.Generic;
using System.Globalization;

namespace SoloHero.Core.Common
{
    /// <summary>
    /// Player-facing text (E7-17). Code only holds keys ("{area}.{snake_case}"); the Korean values live in the data
    /// file Data/Strings/strings_ko.tsv, loaded once at boot. A missing key shows the key itself so gaps are visible.
    /// D-140 terms: a row "term.NAME" defines a word that any value can use as "{NAME}" (the job's attack stat, main stat
    /// and weapon); <see cref="UseTermSet"/> swaps in the "term.NAME.SET" rows for a job line.
    /// </summary>
    public static class Strings
    {
        private const string TermPrefix = "term.";

        private static readonly Dictionary<string, string> Table = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> Terms = new Dictionary<string, string>();
        private static readonly List<string> TermNames = new List<string>();
        private static string _termSet = "";

        public static int Count => Table.Count;

        /// <summary>D-140: raised when the terms change, so fixed labels read their text again.</summary>
        public static event Action TermsChanged;

        public static void Load(IDictionary<string, string> entries)
        {
            Table.Clear();
            TermNames.Clear();
            if (entries != null)
            {
                foreach (KeyValuePair<string, string> pair in entries)
                    Table[pair.Key] = pair.Value;
            }

            foreach (string key in Table.Keys)
            {
                if (key.StartsWith(TermPrefix, StringComparison.Ordinal) && key.IndexOf('.', TermPrefix.Length) < 0)
                    TermNames.Add(key.Substring(TermPrefix.Length));
            }

            ApplyTerms(_termSet);
        }

        public static bool Has(string key) => key != null && Table.ContainsKey(key);

        public static string Get(string key)
        {
            if (key == null) return "";
            return Table.TryGetValue(key, out string value) ? Expand(value) : key;
        }

        /// <summary>D-140: the current value of a term ("atk" is the spell power word for a mage), or the name itself.</summary>
        public static string Term(string name) => name != null && Terms.TryGetValue(name, out string value) ? value : name;

        /// <summary>
        /// D-140: uses the "term.NAME.<paramref name="set"/>" rows where they exist and the plain "term.NAME" rows
        /// elsewhere ("" = the plain rows only).
        /// </summary>
        public static void UseTermSet(string set)
        {
            set = set ?? "";
            if (set == _termSet && Terms.Count == TermNames.Count) return;
            ApplyTerms(set);
            TermsChanged?.Invoke();
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, pattern, args);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        private static void ApplyTerms(string set)
        {
            _termSet = set;
            Terms.Clear();
            for (int i = 0; i < TermNames.Count; i++)
            {
                string name = TermNames[i];
                if (set.Length == 0 || !Table.TryGetValue(TermPrefix + name + "." + set, out string value))
                    value = Table[TermPrefix + name];
                Terms[name] = value;
            }
        }

        private static string Expand(string value)
        {
            if (Terms.Count == 0 || value.IndexOf('{') < 0) return value;
            foreach (KeyValuePair<string, string> term in Terms)
            {
                string token = "{" + term.Key + "}";
                if (value.IndexOf(token, StringComparison.Ordinal) >= 0) value = value.Replace(token, term.Value);
            }

            return value;
        }

        /// <summary>
        /// "key&lt;TAB&gt;value" per line. Blank lines and lines starting with '#' are skipped; "\n" in a value becomes
        /// a line break. A later duplicate key wins.
        /// </summary>
        public static Dictionary<string, string> ParseTsv(string text)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(text)) return result;

            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0 || line[0] == '#') continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                string key = line.Substring(0, tab).Trim();
                string value = line.Substring(tab + 1).Replace("\\n", "\n");
                if (key.Length > 0) result[key] = value;
            }

            return result;
        }
    }
}
