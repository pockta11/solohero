using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SoloHero.Core.Common;

namespace SoloHero.Tests.EditMode
{
    public sealed class StringsTests
    {
        private const string TablePath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const string ScriptsRoot = "Assets/SoloHero/Scripts";

        [TearDown]
        public void Reset() => Strings.Load(null);

        [Test]
        public void ParseTsv_SkipsCommentsAndBlankLines_ReadsTabSeparatedPairs()
        {
            string text = "# comment" + "\n" + "\n" + "a.one\tFirst" + "\n" + "a.two\tLine1\\nLine2" + "\n" + "broken line";

            Dictionary<string, string> table = Strings.ParseTsv(text);

            Assert.AreEqual(2, table.Count);
            Assert.AreEqual("First", table["a.one"]);
            Assert.AreEqual("Line1" + "\n" + "Line2", table["a.two"]);
        }

        [Test]
        public void Get_MissingKey_ReturnsKey()
        {
            Strings.Load(new Dictionary<string, string> { { "x.y", "value" } });

            Assert.AreEqual("value", Strings.Get("x.y"));
            Assert.AreEqual("no.such_key", Strings.Get("no.such_key"));
        }

        [Test]
        public void Format_FillsArgumentsWithInvariantCulture()
        {
            Strings.Load(new Dictionary<string, string> { { "p", "{0} of {1}" } });

            Assert.AreEqual("1.5 of 3", Strings.Format("p", 1.5, 3));
        }

        [Test]
        public void Format_BrokenPattern_ReturnsPatternInsteadOfThrowing()
        {
            Strings.Load(new Dictionary<string, string> { { "bad", "{0" } });

            Assert.AreEqual("{0", Strings.Format("bad", 1));
        }

        [Test]
        public void Table_ContainsEveryKeyUsedInCode()
        {
            Dictionary<string, string> table = Strings.ParseTsv(File.ReadAllText(TablePath));
            var used = new Regex("Strings\\.(?:Get|Format)\\(\"([a-z0-9_.]+)\"");
            var keyed = new Regex("\"((?:tab|hud|toast|ad|boot|offline|tutorial|stat|char|skill|grade|slot|equip|gacha|settings|quit|stage|boss)\\.[a-z0-9_.]+)\"");
            var missing = new List<string>();

            foreach (string file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Tests/") || file.Replace('\\', '/').Contains("/Legacy/")) continue;
                string code = File.ReadAllText(file);
                foreach (Regex rx in new[] { used, keyed })
                {
                    foreach (Match m in rx.Matches(code))
                    {
                        string key = m.Groups[1].Value;
                        // "skill.name." style prefixes get a data-driven suffix; Table_HasEverySkillNameAndDescription covers them.
                        if (key.EndsWith(".")) continue;
                        if (!table.ContainsKey(key) && !missing.Contains(key)) missing.Add(key + " (" + Path.GetFileName(file) + ")");
                    }
                }
            }

            Assert.IsEmpty(missing, "keys missing from " + TablePath + ": " + string.Join(", ", missing));
        }

        [Test]
        public void Table_HasEveryBossName()
        {
            Dictionary<string, string> table = Strings.ParseTsv(File.ReadAllText(TablePath));
            for (int i = 1; i <= 5; i++) Assert.IsTrue(table.ContainsKey("boss.name." + i), "boss.name." + i);
        }

        [Test]
        public void Table_HasEverySkillNameAndDescription()
        {
            Dictionary<string, string> table = Strings.ParseTsv(File.ReadAllText(TablePath));
            foreach (SoloHero.Core.Skills.SkillDef def in SoloHero.Core.Skills.SkillCatalog.All)
            {
                Assert.IsTrue(table.ContainsKey(def.NameKey), def.NameKey);
                Assert.IsTrue(table.ContainsKey(def.DescKey), def.DescKey);
            }
        }

        [Test]
        public void Code_HasNoNonAsciiText()
        {
            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Legacy/")) continue;
                foreach (char c in File.ReadAllText(file))
                {
                    if (c > 127)
                    {
                        offenders.Add(Path.GetFileName(file));
                        break;
                    }
                }
            }

            Assert.IsEmpty(offenders, "non-ASCII text belongs in the Strings table: " + string.Join(", ", offenders));
        }
    }
}
