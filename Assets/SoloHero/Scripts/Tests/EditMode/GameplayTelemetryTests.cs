using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>E6-15: telemetry reports frontier flow, retreat cadence, growth and gold flow without touching play.</summary>
    public sealed class GameplayTelemetryTests
    {
        private const float Dt = 1f / 30f;

        private sealed class Recorder : IAnalytics
        {
            public readonly List<(string name, Dictionary<string, string> p)> Events = new List<(string, Dictionary<string, string>)>();
            public readonly Dictionary<string, string> Properties = new Dictionary<string, string>();

            public void Log(string eventName, IReadOnlyList<AnalyticsParam> parameters)
            {
                var copy = new Dictionary<string, string>();
                foreach (AnalyticsParam p in parameters)
                {
                    string text = p.ToString();
                    copy[p.Key] = text.Substring(p.Key.Length + 1);
                }

                Events.Add((eventName, copy));
            }

            public void SetUserProperty(string name, string value) => Properties[name] = value;

            public IEnumerable<Dictionary<string, string>> Named(string name) => Events.Where(e => e.name == name).Select(e => e.p);
        }

        private static StageRunner Runner(BalanceValues b, SaveDataV2 save, double hp, double atk)
        {
            var runner = new StageRunner(b, new SystemRandom(new System.Random(1)), new HeroStats(hp, atk, b.DEF_BASE, b.ATKSPD_BASE, 0d), save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            return runner;
        }

        private static void Run(StageRunner runner, GameplayTelemetry telemetry, float seconds)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                runner.Tick(Dt);
                telemetry.Tick(Dt);
            }
        }

        [Test]
        public void FrontierClear_LogsReachAndFirstClearWithSeconds()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var rec = new Recorder();
            StageRunner runner = Runner(b, save, 1e9, 1e9);
            var telemetry = new GameplayTelemetry(runner, save, b, rec);

            runner.Resume(1, false);
            Run(runner, telemetry, 60f);

            // A new save starts with highestStage = 1 (stage 1 counts as cleared), so the first frontier is stage 2.
            Dictionary<string, string> reach = rec.Named(AnalyticsEvents.StageReach).First();
            Assert.AreEqual("2", reach[AnalyticsEvents.PStage]);
            Assert.AreEqual("1", reach[AnalyticsEvents.PChapter]);
            Dictionary<string, string> clear = rec.Named(AnalyticsEvents.StageClear).First();
            Assert.AreEqual("2", clear[AnalyticsEvents.PStage]);
            Assert.AreEqual("1", clear[AnalyticsEvents.PAttempt]);
            Assert.That(long.Parse(clear[AnalyticsEvents.PSeconds]), Is.GreaterThan(0));
            Assert.AreEqual("1", rec.Properties[AnalyticsEvents.UserHighestChapter]);
            Assert.That(rec.Named(AnalyticsEvents.StageClear).Count(), Is.GreaterThanOrEqualTo(2), "each new stage's first clear is logged");
        }

        [Test]
        public void NormalDeath_LogsFailThenRetreat_ChallengeLogsFarmSeconds()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 4;
            var rec = new Recorder();
            StageRunner runner = Runner(b, save, 1d, 1d);
            var telemetry = new GameplayTelemetry(runner, save, b, rec);

            runner.Resume(5, false);
            Run(runner, telemetry, 30f);
            Assert.IsTrue(runner.RetreatMode, "D-058 auto farm after a normal death");

            Dictionary<string, string> fail = rec.Named(AnalyticsEvents.StageFail).First();
            Assert.AreEqual("5", fail[AnalyticsEvents.PStage]);
            Assert.AreEqual("0", fail[AnalyticsEvents.PBoss]);
            Dictionary<string, string> retreat = rec.Named(AnalyticsEvents.RetreatEnter).Single();
            Assert.AreEqual("5", retreat[AnalyticsEvents.PStage]);

            runner.ChallengeBoss();
            telemetry.Tick(Dt);
            Dictionary<string, string> challenge = rec.Named(AnalyticsEvents.Challenge).Single();
            Assert.AreEqual("5", challenge[AnalyticsEvents.PStage]);
            Assert.That(long.Parse(challenge[AnalyticsEvents.PSeconds]), Is.GreaterThan(0));
        }

        [Test]
        public void SavedRetreat_OnResume_IsNotReportedAsANewRetreat()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 4;
            save.retreatMode = true;
            var rec = new Recorder();
            StageRunner runner = Runner(b, save, 1e9, 1d);
            var telemetry = new GameplayTelemetry(runner, save, b, rec);

            runner.Resume(4, true);
            Run(runner, telemetry, 1f);

            Assert.IsEmpty(rec.Named(AnalyticsEvents.RetreatEnter));
        }

        [Test]
        public void GoldFlow_LevelAndTutorial_AreReported()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var rec = new Recorder();
            StageRunner runner = Runner(b, save, 1e9, 1d);
            var telemetry = new GameplayTelemetry(runner, save, b, rec);

            save.gold += 500d;
            telemetry.Tick(1f);
            save.gold -= 200d;
            save.heroLevel += 1;
            save.tutorialStep = 2;
            telemetry.Tick(1f);
            telemetry.Flush();
            telemetry.Flush();

            Dictionary<string, string> gold = rec.Named(AnalyticsEvents.GoldSession).Single();
            Assert.AreEqual("500", gold[AnalyticsEvents.PEarned]);
            Assert.AreEqual("200", gold[AnalyticsEvents.PSpent]);
            Assert.AreEqual(save.heroLevel.ToString(), rec.Named(AnalyticsEvents.LevelUp).Single()[AnalyticsEvents.PLevel]);
            Assert.AreEqual("2", rec.Named(AnalyticsEvents.TutorialStep).Single()[AnalyticsEvents.PStep]);
        }

        private sealed class FakeClock : IClock
        {
            public System.DateTime Local = new System.DateTime(2026, 11, 2, 10, 0, 0);

            public long UtcNowSeconds => 1_800_000_000L;

            public System.DateTime LocalNow => Local;
        }

        [Test]
        public void StageStall_OncePerDay_WhileTheFrontierDoesNotMove()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.highestStage = 30;
            var rec = new Recorder();
            var clock = new FakeClock();
            var telemetry = new GameplayTelemetry(Runner(b, save, 1e9, 1d), save, b, rec, clock);

            telemetry.Tick(11f);
            Assert.IsEmpty(rec.Named(AnalyticsEvents.StageStall), "the day the frontier is first seen is day 0");

            clock.Local = clock.Local.AddDays(2);
            telemetry.Tick(11f);
            telemetry.Tick(11f);
            Dictionary<string, string> stall = rec.Named(AnalyticsEvents.StageStall).Single();
            Assert.AreEqual("31", stall[AnalyticsEvents.PStage]);
            Assert.AreEqual("4", stall[AnalyticsEvents.PChapter]);
            Assert.AreEqual("2", stall[AnalyticsEvents.PDays]);
            Assert.AreEqual("0", stall[AnalyticsEvents.PBoss]);

            save.highestStage = 31;
            clock.Local = clock.Local.AddDays(1);
            telemetry.Tick(11f);
            Assert.AreEqual(1, rec.Named(AnalyticsEvents.StageStall).Count(), "a moved frontier starts the count again");
        }

        [Test]
        public void AxisCap_ReportedOncePerSave()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var rec = new Recorder();
            var telemetry = new GameplayTelemetry(Runner(b, save, 1e9, 1d), save, b, rec);

            telemetry.Tick(11f);
            Assert.IsEmpty(rec.Named(AnalyticsEvents.AxisCap));

            save.jobId = "paladin";
            save.totalPullCount = int.MaxValue / 2;
            telemetry.Tick(11f);
            telemetry.Tick(11f);
            var axes = rec.Named(AnalyticsEvents.AxisCap).Select(p => p[AnalyticsEvents.PAxis]).ToList();
            CollectionAssert.AreEquivalent(new[] { AnalyticsEvents.AxisFinalJob, AnalyticsEvents.AxisGearSummon }, axes);

            var again = new GameplayTelemetry(Runner(b, save, 1e9, 1d), save, b, rec);
            again.Tick(11f);
            Assert.AreEqual(2, rec.Named(AnalyticsEvents.AxisCap).Count(), "the save remembers what was reported");
        }

        [Test]
        public void TierEnter_WhenTheFrontierCrossesIntoTheNextTier()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            int last = DifficultyTier.StagesPerTier(b);
            save.highestStage = last;
            var rec = new Recorder();
            StageRunner runner = Runner(b, save, 1e9, 1d);
            var telemetry = new GameplayTelemetry(runner, save, b, rec);

            runner.Resume(last + 1, false);
            telemetry.Tick(Dt);

            Dictionary<string, string> tier = rec.Named(AnalyticsEvents.TierEnter).Single();
            Assert.AreEqual("1", tier[AnalyticsEvents.PTier]);
            Assert.AreEqual((last + 1).ToString(), tier[AnalyticsEvents.PStage]);
            Assert.AreEqual("1", rec.Properties[AnalyticsEvents.UserDifficultyTier]);
        }

        [Test]
        public void EventNames_FollowFirebaseRules()
        {
            foreach (System.Reflection.FieldInfo f in typeof(AnalyticsEvents).GetFields())
            {
                string value = (string)f.GetValue(null);
                Assert.That(value, Does.Match("^[a-z][a-z0-9_]{0,39}$"), f.Name);
                Assert.IsFalse(value.StartsWith("firebase_") || value.StartsWith("google_") || value.StartsWith("ga_"), f.Name);
            }
        }
    }
}
