using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Jobs;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Tests.EditMode
{
    /// <summary>
    /// Release QA of the job content on disk (2026-10-16): every job look has its clips and art asset, the game
    /// scene wires the looks in catalog order, every main attack, ultimate and skill has an icon, a built VFX clip
    /// and a mapped sound, and the job popup can name every mastery stat. Reads repo files like StringsTests.
    /// </summary>
    public sealed class JobContentTests
    {
        private const string TablePath = "Assets/SoloHero/Data/Strings/strings_ko.txt";
        private const string HeroArt = "Assets/SoloHero/Art/Hero";
        private const string DataArt = "Assets/SoloHero/Data/Art";
        private const string HdSkillIcons = "Assets/SoloHero/Art/UI/Hd/Skills";
        private const string VfxArt = "Assets/SoloHero/Art/Vfx";
        private const string ScenePath = "Assets/SoloHero/Scenes/Game.unity";
        private const string ArtBuilderPath = "Assets/SoloHero/Scripts/Editor/ArtBuilder.cs";
        private const string SkillFxPath = "Assets/SoloHero/Scripts/Game/Combat/SkillFx.cs";
        private const string JobPresenterPath = "Assets/SoloHero/Scripts/Game/UI/Panels/JobPresenter.cs";

        private static readonly string[] Clips = { "idle", "run", "attack", "hit", "dead", "skill" };

        private static string AssetName(string look) => "Hero_" + char.ToUpperInvariant(look[0]) + look.Substring(1);

        private static string GuidOf(string metaPath)
        {
            Match m = Regex.Match(File.ReadAllText(metaPath), @"^guid: ([0-9a-f]{32})", RegexOptions.Multiline);
            Assert.IsTrue(m.Success, "no guid in " + metaPath);
            return m.Groups[1].Value;
        }

        private static IEnumerable<SkillDef> EverySkillAndUltimate()
        {
            foreach (SkillDef def in SkillCatalog.All) yield return def;
            foreach (JobDef job in JobCatalog.All)
                if (job.Ultimate != null) yield return job.Ultimate;
        }

        [Test]
        public void Art_EveryJobLook_HasSixClipSheetsAndItsCharacterAsset()
        {
            foreach (JobDef job in JobCatalog.All)
            {
                foreach (string clip in Clips)
                {
                    string[] sheets = Directory.GetFiles(HeroArt, job.Look + "_" + clip + "_*.png");
                    Assert.IsNotEmpty(sheets, job.Id + " look " + job.Look + " has no " + clip + " sheet");
                }

                Assert.IsTrue(File.Exists(DataArt + "/" + AssetName(job.Look) + ".asset"), job.Id + " has no " + AssetName(job.Look) + ".asset");
            }
        }

        [Test]
        public void Scene_Game_WiresJobLooksInCatalogOrder()
        {
            var expected = new List<string>();
            foreach (JobDef job in JobCatalog.All)
                expected.Add(GuidOf(DataArt + "/" + AssetName(job.Look) + ".asset.meta"));

            string scene = File.ReadAllText(ScenePath);
            var block = new Regex(@"^  (_jobArts|_arts):\r?\n((?:  - \{fileID: \d+, guid: [0-9a-f]{32}, type: 2\}\r?\n)+)", RegexOptions.Multiline);
            var guid = new Regex(@"guid: ([0-9a-f]{32})");
            int jobArtsBlocks = 0, jobPopupBlocks = 0;
            foreach (Match m in block.Matches(scene))
            {
                var guids = new List<string>();
                foreach (Match g in guid.Matches(m.Groups[2].Value)) guids.Add(g.Groups[1].Value);
                if (m.Groups[1].Value == "_jobArts")
                {
                    jobArtsBlocks++;
                    CollectionAssert.AreEqual(expected, guids, "CombatWorldView._jobArts is not JobCatalog order");
                }
                else if (guids.Count == expected.Count)
                {
                    jobPopupBlocks++;
                    CollectionAssert.AreEqual(expected, guids, "JobPresenter._arts is not JobCatalog order");
                }
            }

            Assert.GreaterOrEqual(jobArtsBlocks, 1, "no _jobArts in the game scene");
            Assert.GreaterOrEqual(jobPopupBlocks, 1, "no 16-look _arts (job popup) in the game scene");
        }

        [Test]
        public void Icons_EveryMainAttackUltimateAndSkill_HasAnHdIcon()
        {
            var missing = new List<string>();
            foreach (SkillDef def in EverySkillAndUltimate())
                if (!File.Exists(HdSkillIcons + "/skill_" + def.IconId + ".png")) missing.Add(def.IconId);
            foreach (JobDef job in JobCatalog.All)
                if (job.Main != null && !File.Exists(HdSkillIcons + "/skill_" + JobCatalog.MainIconId(job) + ".png")) missing.Add(JobCatalog.MainIconId(job));
            Assert.IsEmpty(missing, "icons missing under " + HdSkillIcons + ": " + string.Join(", ", missing));
        }

        [Test]
        public void Vfx_EveryMainAttackUltimateAndSkillClip_IsBuiltByArtBuilderAndOnDisk()
        {
            string builder = File.ReadAllText(ArtBuilderPath);
            int start = builder.IndexOf("SkillClips =", System.StringComparison.Ordinal);
            Assert.Greater(start, 0, "SkillClips not found in ArtBuilder");
            int end = builder.IndexOf("};", start, System.StringComparison.Ordinal);
            var built = new HashSet<string>();
            foreach (Match m in Regex.Matches(builder.Substring(start, end - start), "\\(\"([a-z0-9]+)\", (?:true|false), [0-9.]+f\\)"))
                built.Add(m.Groups[1].Value);
            Assert.Greater(built.Count, 20, "SkillClips parsed too few names");

            var missing = new List<string>();
            var names = new Dictionary<string, string>();
            foreach (SkillDef def in EverySkillAndUltimate()) names[def.Vfx] = def.Id;
            foreach (JobDef job in JobCatalog.All)
                if (job.Main != null) names[job.Main.Vfx] = "main of " + job.Id;
            foreach (KeyValuePair<string, string> pair in names)
            {
                if (!built.Contains(pair.Key)) missing.Add(pair.Key + " (not in SkillClips, used by " + pair.Value + ")");
                if (Directory.GetFiles(VfxArt, "vfx" + pair.Key + "_play*.png").Length == 0) missing.Add(pair.Key + " (no sheet, used by " + pair.Value + ")");
            }

            Assert.IsEmpty(missing, string.Join("; ", missing));
        }

        [Test]
        public void Sounds_EverySkillAndUltimateSound_IsMappedInSkillFx()
        {
            string fx = File.ReadAllText(SkillFxPath);
            int start = fx.IndexOf("SoundOf(SkillDef def)", System.StringComparison.Ordinal);
            Assert.Greater(start, 0, "SoundOf not found in SkillFx");
            int end = fx.IndexOf("default:", start, System.StringComparison.Ordinal);
            var mapped = new HashSet<string> { "strike" };
            foreach (Match m in Regex.Matches(fx.Substring(start, end - start), "case \"([a-z]+)\":"))
                mapped.Add(m.Groups[1].Value);
            Assert.Greater(mapped.Count, 4, "SoundOf parsed too few names");

            var unmapped = new List<string>();
            foreach (SkillDef def in EverySkillAndUltimate())
                if (!mapped.Contains(def.Sound)) unmapped.Add(def.Id + ":" + def.Sound);
            Assert.IsEmpty(unmapped, "sounds that fall to the default: " + string.Join(", ", unmapped));
        }

        [Test]
        public void Strings_EveryMasteryStat_HasAJobStatLineInThePopupAndTheTable()
        {
            Dictionary<string, string> table = Strings.ParseTsv(File.ReadAllText(TablePath));
            string presenter = File.ReadAllText(JobPresenterPath);
            int start = presenter.IndexOf("MasteryKeys =", System.StringComparison.Ordinal);
            Assert.Greater(start, 0, "MasteryKeys not found in JobPresenter");
            int end = presenter.IndexOf("};", start, System.StringComparison.Ordinal);
            var keys = new List<string>();
            foreach (Match m in Regex.Matches(presenter.Substring(start, end - start), "\"([a-z_.]*)\""))
                keys.Add(m.Groups[1].Value);
            int stats = System.Enum.GetValues(typeof(TalentStat)).Length;
            Assert.AreEqual(stats, keys.Count, "MasteryKeys must have one entry per TalentStat value");

            foreach (JobDef job in JobCatalog.All)
            {
                foreach (Mastery mastery in job.Mastery)
                {
                    string key = keys[(int)mastery.Stat];
                    Assert.AreNotEqual("", key, job.Id + " mastery " + mastery.Stat + " has no popup line");
                    Assert.IsTrue(table.ContainsKey(key), job.Id + " mastery key " + key + " missing from the table");
                }
            }

            foreach (TalentDef def in TalentCatalog.Job)
            {
                Assert.IsTrue(table.ContainsKey(def.NameKey), def.NameKey);
                Assert.IsTrue(table.ContainsKey(def.DescKey), def.DescKey);
            }
        }
    }
}
