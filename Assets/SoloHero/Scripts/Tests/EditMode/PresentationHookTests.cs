using System.Collections.Generic;
using NUnit.Framework;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>E8 presentation hooks: Core raises what views need and nothing about combat results changes.</summary>
    public sealed class PresentationHookTests
    {
        private const float Dt = 1f / 30f;

        [Test]
        public void Settings_SetWritesSaveFieldAndRaisesChangedOnce()
        {
            var save = SaveDataV2.CreateNew();
            var settings = new SettingsService(save);
            int changed = 0;
            settings.Changed += () => changed++;

            settings.SetBgmMuted(true);
            settings.SetBgmMuted(true);
            settings.SetSfxMuted(true);
            settings.SetLowEffect(true);
            settings.SetFps30(true);

            Assert.IsTrue(save.bgmMuted);
            Assert.IsTrue(save.sfxMuted);
            Assert.IsTrue(save.lowEffectMode);
            Assert.IsTrue(save.fps30Mode);
            Assert.AreEqual(30, settings.TargetFrameRate);
            Assert.AreEqual(4, changed);
        }

        [Test]
        public void Settings_Defaults_Are60FpsAndSoundOn()
        {
            var settings = new SettingsService(SaveDataV2.CreateNew());

            Assert.IsFalse(settings.BgmMuted);
            Assert.IsFalse(settings.SfxMuted);
            Assert.IsFalse(settings.LowEffect);
            Assert.AreEqual(60, settings.TargetFrameRate);
        }

        [Test]
        public void SkillCast_RaisedOnlyForSuccessfulCasts()
        {
            var b = new BalanceValues();
            var hero = new HeroBrain(b, new SystemRandom(new System.Random(1)), new HeroStats(1000d, 100d, b.DEF_BASE, b.ATKSPD_BASE, 0d));
            var world = new CombatWorld(b);
            world.BindHero(hero);
            Assert.IsTrue(world.TryActivateSlot(out EnemyBrain e));
            e.Reset(b, 1e6, 1d, 99f, 1d, false);
            var skills = new SkillAutoCaster(b);
            var cast = new List<SkillSlot>();
            skills.SkillCast += cast.Add;

            Assert.IsTrue(skills.TryCast(SkillSlot.Slot1, hero, world).Ok);
            Assert.IsFalse(skills.TryCast(SkillSlot.Slot1, hero, world).Ok);

            CollectionAssert.AreEqual(new[] { SkillSlot.Slot1 }, cast);
        }

        [Test]
        public void Spawns_CarryIncreasingSpawnIndexWithinAStage()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var runner = new StageRunner(b, new SystemRandom(new System.Random(1)), new HeroStats(1e9, 1d, b.DEF_BASE, b.ATKSPD_BASE, 0d), save);
            runner.Resume(1, false);
            var seen = new HashSet<int>();

            for (int step = 0; step < 30 * 20; step++)
            {
                runner.Tick(Dt);
                for (int i = 0; i < runner.World.SlotCount; i++)
                {
                    EnemyBrain enemy = runner.World.GetSlot(i);
                    if (enemy.IsActive) seen.Add(enemy.SpawnIndex);
                }
            }

            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(b.SPAWN_MAX_ALIVE));
            foreach (int index in seen) Assert.That(index, Is.InRange(0, b.KILL_TARGET_NORMAL - 1));
        }
    }
}
