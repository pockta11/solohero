using System;
using System.Globalization;
using System.Text;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Jobs;
using SoloHero.Core.Pets;
using SoloHero.Core.Skills;

namespace SoloHero.Core.Gacha
{
    /// <summary>
    /// D-183: the public probability page (Korean game law: paid probability items are disclosed in the game, on the
    /// store page and on the website). Built from the very tables the draws use - <see cref="GearTableValues.AtLevel"/>,
    /// <see cref="GachaTableValues.AtLevel"/>, the pet and skill pools - so it cannot drift from the game:
    /// RateDisclosureTests fails when the committed page differs from what this builds. Every per-item rate follows from
    /// the rule the draws follow: a grade first, then a uniform pick among that grade's items (gear: the slot is uniform
    /// over 8). The words come from the Strings table (disclosure.*).
    /// </summary>
    public static class RateDisclosure
    {
        public const string PagePath = "_bmad-output/implementation-artifacts/store/probability-ko.html";

        public static string BuildHtml(BalanceValues balance)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            var html = new StringBuilder(64 * 1024);
            html.Append("<!doctype html>\n<html lang=\"ko\">\n<head>\n<meta charset=\"utf-8\">\n")
                .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n")
                .Append("<title>").Append(H(Strings.Get("disclosure.title"))).Append("</title>\n")
                .Append("<style>\n")
                .Append("body{font-family:system-ui,-apple-system,'Malgun Gothic',sans-serif;margin:0 auto;max-width:960px;padding:16px;line-height:1.55;color:#222;background:#fff}\n")
                .Append("h1{font-size:1.5em}h2{font-size:1.25em;margin-top:2em;border-bottom:2px solid #333}h3{font-size:1.05em;margin-top:1.4em}\n")
                .Append("table{border-collapse:collapse;margin:8px 0 16px;font-size:.88em;display:block;overflow-x:auto}\n")
                .Append("th,td{border:1px solid #bbb;padding:3px 7px;text-align:right;white-space:nowrap}th{background:#f1f1f1}\n")
                .Append("td.l,th.l{text-align:left;white-space:normal}.n{color:#999}ul{padding-left:1.2em}\n")
                .Append("</style>\n</head>\n<body>\n");

            html.Append("<h1>").Append(H(Strings.Get("disclosure.title"))).Append("</h1>\n");
            html.Append("<p>").Append(H(Strings.Get("disclosure.intro"))).Append("</p>\n<ul>\n");
            Li(html, Strings.Get("disclosure.rule_order"));
            Li(html, Strings.Format("disclosure.rule_level", balance.SUMMON_LV_MAX));
            Li(html, Strings.Format("disclosure.rule_pity", N(balance.GEAR_PITY), N(balance.GACHA_PITY)));
            Li(html, Strings.Format("disclosure.rule_pickup", Pct(balance.PICKUP_SHARE)));
            Li(html, Strings.Get("disclosure.rule_dup"));
            Li(html, Strings.Format("disclosure.rule_bulk", N(balance.SUMMON_BULK_PULLS)));
            html.Append("</ul>\n");

            GearTableValues gear = GearTableValues.FromBalance(balance);
            GachaTableValues skill = GachaTableValues.FromBalance(balance);

            // Gear: 7 grades x 8 slots, the slot uniform.
            html.Append("<h2>").Append(H(Strings.Get("gacha.mode_gear"))).Append("</h2>\n");
            Cost(html, balance.GACHA_COST_SINGLE, balance.GACHA_COST_TEN, balance.GACHA_COST_TEN_GEM);
            html.Append("<p>").Append(H(Strings.Format("disclosure.gear_items", SlotNames()))).Append("</p>\n");
            GearLevels(html, balance, SummonKind.Gear, gear, level => GachaCatalog.SlotCount);

            // Pets: 7 grades, uniform within the grade's pets.
            html.Append("<h2>").Append(H(Strings.Get("gacha.mode_pet"))).Append("</h2>\n");
            Cost(html, balance.PET_SUMMON_COST_SINGLE, balance.PET_SUMMON_COST_TEN, balance.PET_SUMMON_COST_TEN_GEM);
            html.Append("<p>").Append(H(Strings.Get("disclosure.pet_items"))).Append("</p>\n<ul>\n");
            for (int g = 0; g < GearGrades.Count; g++)
            {
                PetDef[] pets = PetCatalog.OfGrade((GearGrade)g);
                var names = new string[pets.Length];
                for (int i = 0; i < pets.Length; i++) names[i] = Strings.Get(pets[i].NameKey);
                Li(html, GradeName((GearGrade)g) + " (" + Strings.Format("disclosure.unit_count", pets.Length) + "): " + string.Join(", ", names));
            }

            html.Append("</ul>\n");
            GearLevels(html, balance, SummonKind.Pet, gear, g => PetCatalog.OfGrade((GearGrade)g).Length);

            // Skills: 4 grades, uniform within the hero's line and job tier.
            html.Append("<h2>").Append(H(Strings.Get("gacha.mode_skill"))).Append("</h2>\n");
            Cost(html, balance.SKILL_SUMMON_COST_SINGLE, balance.SKILL_SUMMON_COST_TEN, balance.SKILL_SUMMON_COST_TEN_GEM);
            html.Append("<p>").Append(H(Strings.Get("disclosure.skill_items"))).Append("</p>\n");
            SkillGradeTable(html, balance, skill);
            foreach (JobLine line in new[] { JobLine.Warrior, JobLine.Mage, JobLine.Archer })
            {
                for (int tier = 1; tier <= JobCatalog.MaxTier; tier++)
                {
                    html.Append("<h3>").Append(H(Strings.Format("disclosure.skill_pool", LineName(line), tier))).Append("</h3>\n<ul>\n");
                    var counts = new int[SkillCatalog.GradeCount];
                    for (int g = 0; g < counts.Length; g++)
                    {
                        SkillDef[] pool = SkillCatalog.OfLine(line, (Grade)g, tier);
                        counts[g] = pool.Length;
                        var names = new string[pool.Length];
                        for (int i = 0; i < pool.Length; i++) names[i] = Strings.Get(pool[i].NameKey);
                        Li(html, GradeName(((Grade)g).ToGearGrade()) + " (" + Strings.Format("disclosure.unit_count", pool.Length) + "): " + string.Join(", ", names));
                    }

                    html.Append("</ul>\n");
                    SkillItemTable(html, balance, skill, counts);
                }
            }

            html.Append("<p class=\"n\">").Append(H(Strings.Get("disclosure.footer"))).Append("</p>\n</body>\n</html>\n");
            return html.ToString();
        }

        /// <summary>Gear and pets: per level the grade's rate and the rate of each item in it.</summary>
        private static void GearLevels(StringBuilder html, BalanceValues balance, SummonKind kind, GearTableValues table, Func<int, int> itemsOfGrade)
        {
            html.Append("<table>\n<tr><th>").Append(H(Strings.Get("disclosure.level_col"))).Append("</th><th>")
                .Append(H(Strings.Get("disclosure.pulls_col"))).Append("</th>");
            for (int g = 0; g < GearGrades.Count; g++) html.Append("<th>").Append(H(GradeName((GearGrade)g))).Append("</th>");
            html.Append("</tr>\n");
            for (int level = 1; level <= balance.SUMMON_LV_MAX; level++)
            {
                GearTableValues at = table.AtLevel(level);
                html.Append("<tr><td>Lv ").Append(level).Append("</td><td>").Append(N(SummonLevel.PullsFor(balance, kind, level))).Append("</td>");
                for (int g = 0; g < GearGrades.Count; g++) Cell(html, at.Rates[g], itemsOfGrade(g));
                html.Append("</tr>\n");
            }

            html.Append("</table>\n");
        }

        private static void SkillGradeTable(StringBuilder html, BalanceValues balance, GachaTableValues table)
        {
            html.Append("<table>\n<tr><th>").Append(H(Strings.Get("disclosure.level_col"))).Append("</th><th>")
                .Append(H(Strings.Get("disclosure.pulls_col"))).Append("</th>");
            for (int g = 0; g < SkillCatalog.GradeCount; g++) html.Append("<th>").Append(H(GradeName(((Grade)g).ToGearGrade()))).Append("</th>");
            html.Append("</tr>\n");
            for (int level = 1; level <= balance.SUMMON_LV_MAX; level++)
            {
                double[] rates = table.AtLevel(level).Rates;
                html.Append("<tr><td>Lv ").Append(level).Append("</td><td>").Append(N(SummonLevel.PullsFor(balance, SummonKind.Skill, level))).Append("</td>");
                for (int g = 0; g < rates.Length; g++) Cell(html, rates[g], 0);
                html.Append("</tr>\n");
            }

            html.Append("</table>\n");
        }

        /// <summary>One line and tier: each skill's rate per level (the grade's rate over the grade's pool).</summary>
        private static void SkillItemTable(StringBuilder html, BalanceValues balance, GachaTableValues table, int[] counts)
        {
            html.Append("<table>\n<tr><th>").Append(H(Strings.Get("disclosure.level_col"))).Append("</th>");
            for (int g = 0; g < counts.Length; g++)
                html.Append("<th>").Append(H(GradeName(((Grade)g).ToGearGrade()) + " " + Strings.Get("disclosure.per_item"))).Append("</th>");
            html.Append("</tr>\n");
            for (int level = 1; level <= balance.SUMMON_LV_MAX; level++)
            {
                double[] rates = table.AtLevel(level).Rates;
                html.Append("<tr><td>Lv ").Append(level).Append("</td>");
                for (int g = 0; g < rates.Length; g++)
                {
                    html.Append("<td>");
                    html.Append(rates[g] > 0d && counts[g] > 0 ? Pct(rates[g] / counts[g]) + "%" : "-");
                    html.Append("</td>");
                }

                html.Append("</tr>\n");
            }

            html.Append("</table>\n");
        }

        /// <summary>"rate% (each x%)" or "-" while the grade is not open yet.</summary>
        private static void Cell(StringBuilder html, double rate, int items)
        {
            html.Append("<td>");
            if (rate <= 0d)
            {
                html.Append("-</td>");
                return;
            }

            html.Append(Pct(rate)).Append('%');
            if (items > 0) html.Append("<br><span class=\"n\">").Append(H(Strings.Get("disclosure.per_item"))).Append(' ')
                .Append(Pct(rate / items)).Append("%</span>");
            html.Append("</td>");
        }

        private static void Cost(StringBuilder html, double single, double ten, int gemTen)
        {
            html.Append("<p>").Append(H(Strings.Format("disclosure.cost", N(single), N(ten), N(gemTen)))).Append("</p>\n");
        }

        private static string SlotNames()
        {
            var names = new string[GachaCatalog.SlotCount];
            for (int s = 0; s < names.Length; s++)
                names[s] = s == (int)EquipmentSlot.Sword ? Strings.Get("disclosure.weapon") : Strings.Get(SlotKey((EquipmentSlot)s));
            return string.Join(", ", names);
        }

        private static string SlotKey(EquipmentSlot slot) => "slot." + slot.ToString().ToLowerInvariant();

        private static string GradeName(GearGrade grade) => Strings.Get("grade." + grade.ToString().ToLowerInvariant());

        private static string LineName(JobLine line) => Strings.Get("job.line." + line.ToString().ToLowerInvariant());

        private static void Li(StringBuilder html, string text) => html.Append("<li>").Append(H(text)).Append("</li>\n");

        /// <summary>A percent with up to six decimals, trailing zeros dropped (the draws use the exact value).</summary>
        public static string Pct(double percent) => Math.Round(percent, 6).ToString("0.######", CultureInfo.InvariantCulture);

        private static string N(double value) => value.ToString("#,0", CultureInfo.InvariantCulture);

        private static string H(string text) =>
            (text ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
