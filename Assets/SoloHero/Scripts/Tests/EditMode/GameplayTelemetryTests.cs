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
