using System;
using System.Collections.Generic;
using System.Globalization;

namespace SoloHero.Core.Common
{
    /// <summary>
    /// Player-facing text (E7-17). Code only holds keys ("{area}.{snake_case}"); the Korean values live in the data
    /// file Data/Strings/strings_ko.tsv, loaded once at boot. A missing key shows the key itself so gaps are visible.
    /// </summary>
    public static class Strings
    {
        private static readonly Dictionary<string, string> Table = new Dictionary<string, string>();

        public static int Count => Table.Count;

        public static void Load(IDictionary<string, string> entries)
        {
            Table.Clear();
            if (entries == null) return;
            foreach (KeyValuePair<string, string> pair in entries)
                Table[pair.Key] = pair.Value;
        }

        public static bool Has(string key) => key != null && Table.ContainsKey(key);

        public static string Get(string key)
        {
            if (key == null) return "";
            return Table.TryGetValue(key, out string value) ? value : key;
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
