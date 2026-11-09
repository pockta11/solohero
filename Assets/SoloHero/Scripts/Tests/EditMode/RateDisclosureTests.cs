using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using SoloHero.Core.Pets;
using SoloHero.Core.Skills;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// D-183: the public probability page is the draw tables, word for word. When balance values change, regenerate it
    /// (Tools > Release > Export Rate Disclosure, or run the tests with SOLOHERO_WRITE_DISCLOSURE=1) and publish it again.
    /// </summary>
    public sealed class RateDisclosureTests
    {
        private const string TablePath = "Assets/SoloHero/Data/Strings/strings_ko.txt";

        [SetUp]
        public void LoadStrings()
        {
            Strings.Load(Strings.ParseTsv(File.ReadAllText(TablePath)));
            Strings.UseTermSet("");
        }

        [TearDown]
        public void ResetStrings()
        {
            Strings.Load(null);
            Strings.UseTermSet("");
        }

        [Test]
        public void CommittedPage_IsWhatTheDrawTablesBuild()
        {
            string built = RateDisclosure.BuildHtml(new BalanceValues());
            if (Environment.GetEnvironmentVariable("SOLOHERO_WRITE_DISCLOSURE") == "1")
                File.WriteAllText(RateDisclosure.PagePath, built, new System.Text.UTF8Encoding(false));
            Assert.IsTrue(File.Exists(RateDisclosure.PagePath), "missing " + RateDisclosure.PagePath);
            string committed = File.ReadAllText(RateDisclosure.PagePath).Replace("\r\n", "\n");
            Assert.AreEqual(committed, built.Replace("\r\n", "\n"),
                "the probability page is out of date: Tools > Release > Export Rate Disclosure, then publish it again");
        }

        [Test]
        public void Page_HasNoMissingStrings_AndNamesEveryItem()
        {
            string html = RateDisclosure.BuildHtml(new BalanceValues());
            StringAssert.DoesNotContain("disclosure.", html, "a disclosure key without a value");
            StringAssert.DoesNotContain("grade.", html);
            StringAssert.DoesNotContain("{weapon}", html);
            foreach (PetDef pet in PetCatalog.All) StringAssert.Contains(Strings.Get(pet.NameKey), html, pet.Id);
            foreach (JobLine line in new[] { JobLine.Warrior, JobLine.Mage, JobLine.Archer })
            for (int g = 0; g < SkillCatalog.GradeCount; g++)
            foreach (SkillDef def in SkillCatalog.OfLine(line, (Grade)g, JobCatalog.MaxTier))
                StringAssert.Contains(Strings.Get(def.NameKey), html, def.Id);
        }

        [Test]
        public void EveryLevelsTable_AddsUpTo100_AndOnlyOpensGradesItShould()
        {
            var b = new BalanceValues();
            GearTableValues gear = GearTableValues.FromBalance(b);
            GachaTableValues skill = GachaTableValues.FromBalance(b);
            for (int level = 1; level <= b.SUMMON_LV_MAX; level++)
            {
                double sum = 0d;
                foreach (double r in gear.AtLevel(level).Rates)
                {
                    Assert.GreaterOrEqual(r, 0d);
                    sum += r;
                }

                Assert.AreEqual(100d, sum, 1e-9, "gear Lv " + level);
                sum = 0d;
                foreach (double r in skill.AtLevel(level).Rates) sum += r;
                Assert.AreEqual(100d, sum, 1e-9, "skill Lv " + level);
            }

            Assert.AreEqual(0d, gear.AtLevel(1).Rates[(int)GearGrade.Uncommon], "level 1 drops Common only (D-123)");
            Assert.Greater(gear.AtLevel(b.SUMMON_OPEN_A).Rates[(int)GearGrade.Ancient], 0d);
        }

        /// <summary>
        /// The game reads BalanceConfig.asset (and BalanceConfig's own defaults for fields the asset has not saved yet);
        /// the page and the tests read BalanceValues. They must be the same numbers, or the published rates would not be
        /// the rates in the game.
        /// </summary>
        [Test]
        public void GameBalanceAsset_EqualsTheValuesThePageIsBuiltFrom()
        {
            var values = new BalanceValues();
            var assetFields = new Dictionary<string, string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                         File.ReadAllText("Assets/SoloHero/Data/Config/BalanceConfig.asset"), @"^  ([A-Z][A-Z0-9_]+): (\S+)\s*$",
                         System.Text.RegularExpressions.RegexOptions.Multiline))
                assetFields[m.Groups[1].Value] = m.Groups[2].Value;
            var configDefaults = new Dictionary<string, string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                         File.ReadAllText("Assets/SoloHero/Scripts/Game/Config/BalanceConfig.cs"),
                         @"public (?:double|int|long|bool|float) ([A-Z][A-Z0-9_]+) = ([^;]+);"))
                configDefaults[m.Groups[1].Value] = m.Groups[2].Value;

            int compared = 0;
            foreach (System.Reflection.FieldInfo field in typeof(BalanceValues).GetFields())
            {
                if (!char.IsUpper(field.Name[0]) || field.Name.ToUpperInvariant() != field.Name) continue;
                object expected = field.GetValue(values);
                if (!(expected is double || expected is int || expected is long || expected is bool || expected is float)) continue;
                string game = assetFields.TryGetValue(field.Name, out string inAsset) ? inAsset
                    : configDefaults.TryGetValue(field.Name, out string inConfig) ? inConfig : null;
                Assert.IsNotNull(game, field.Name + " is not in BalanceConfig");
                double relative = expected is float ? 1e-6 : 1e-9;
                Assert.AreEqual(Number(expected), Parse(game), Math.Abs(Number(expected)) * relative + 1e-12, field.Name);
                compared++;
            }

            Assert.Greater(compared, 300);
        }

        private static double Number(object value) => value is bool b ? (b ? 1d : 0d) : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);

        private static double Parse(string literal)
        {
            string text = literal.Trim().Replace("_", "");
            if (text == "true") return 1d;
            if (text == "false") return 0d;
            text = text.TrimEnd('f', 'F', 'd', 'D', 'L', 'l');
            return double.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        }

        [Test]
        public void Pct_KeepsSixDecimals()
        {
            Assert.AreEqual("0.000625", RateDisclosure.Pct(0.005 / 8));
            Assert.AreEqual("60", RateDisclosure.Pct(60));
            Assert.AreEqual("0.45", RateDisclosure.Pct(0.45));
        }
    }
}
