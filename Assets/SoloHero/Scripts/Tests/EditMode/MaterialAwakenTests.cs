using System;
using NUnit.Framework;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-174: the material dungeon pays awakening stones; a max-level skill spends them on stars.</summary>
    public sealed class MaterialAwakenTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime Local = new DateTime(2026, 10, 16, 9, 0, 0);

            public long UtcNowSeconds => 1_800_000_000L;

            public DateTime LocalNow => Local;
        }

        private sealed class FixedRandom : IRandom
        {
            public double NextDouble() => 0.99d;

            public int Next(int maxExclusive) => 0;
        }

        private static StageRunner Runner(BalanceValues b, SaveDataV2 save)
        {
            var runner = new StageRunner(b, new FixedRandom(), new HeroStats(1_000_000d, 10_000d, 10_000d, b.ATKSPD_BASE, 0d), save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            return runner;
        }

        [Test]
        public void MaterialDungeon_OpensLater_PaysAStonePerFewKills_RecordsTheSweep()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.highestStage = b.DUNGEON_UNLOCK_STAGE;
            save.farmingStage = 12;
            var dungeons = new DungeonService(b, save, new FakeClock());
            StageRunner runner = Runner(b, save);
            runner.Resume(12, false);

            Assert.IsTrue(dungeons.IsOpen(DungeonKind.Gold));
            Assert.IsFalse(dungeons.IsOpen(DungeonKind.Material));
            Assert.AreEqual(FailReason.Locked, dungeons.TryEnter(DungeonKind.Material, runner).Reason);

            save.highestStage = b.MATERIAL_UNLOCK_STAGE;
            Assert.IsTrue(dungeons.TryEnter(DungeonKind.Material, runner).Ok);
            Assert.AreEqual(b.MATERIAL_DAILY_TICKETS - 1, dungeons.Remaining(DungeonKind.Material));
            for (float t = 0f; t < b.DUNGEON_TIME + 0.1f; t += 0.05f) runner.Tick(0.05f);

            Assert.AreEqual(StageState.DungeonResult, runner.State);
            Assert.Greater(runner.Kills, b.MATERIAL_KILLS_PER_STONE);
            int stones = DungeonService.MaterialStones(b, runner.Kills);
            Assert.AreEqual(stones, save.awakenStones);
            Assert.AreEqual(stones, runner.DungeonEarned, 1e-9);
            Assert.AreEqual(runner.Kills, save.dungeonMaterialBest);

            Assert.IsTrue(dungeons.CanSweep(DungeonKind.Material));
            Assert.IsTrue(dungeons.TrySweep(DungeonKind.Material, out double swept).Ok);
            Assert.AreEqual(stones, swept, 1e-9);
            Assert.AreEqual(2 * stones, save.awakenStones);
            Assert.AreEqual(0, dungeons.Remaining(DungeonKind.Material));
        }

        [Test]
        public void Awaken_NeedsMaxLevel_CostsRiseByStarAndGrade_StopsAtMax()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            var skills = new SkillService(save, b);
            string id = save.ownedSkills[0];
            SkillDef def = SkillCatalog.Find(id);

            Assert.AreEqual(FailReason.NeedMaxLevel, skills.TryAwaken(id).Reason);
            SkillBook.SetLevel(save, id, b.SKILL_MAX_LEVEL);
            Assert.AreEqual(FailReason.NotEnoughAwakenStones, skills.TryAwaken(id).Reason);

            int total = 0;
            for (int s = 0; s < b.AWAKEN_MAX_STARS; s++) total += Formulas.SkillAwakenCost(b, def.Grade, s);
            save.awakenStones = total;
            for (int s = 0; s < b.AWAKEN_MAX_STARS; s++) Assert.IsTrue(skills.TryAwaken(id).Ok, "star " + (s + 1));
            Assert.AreEqual(b.AWAKEN_MAX_STARS, skills.Stars(id));
            Assert.AreEqual(0, save.awakenStones);
            Assert.AreEqual(FailReason.MaxLevel, skills.TryAwaken(id).Reason);
            Assert.AreEqual(0, skills.AwakenCost(id));

            Assert.Less(Formulas.SkillAwakenCost(b, Grade.Common, 0), Formulas.SkillAwakenCost(b, Grade.Common, 1));
            Assert.Less(Formulas.SkillAwakenCost(b, Grade.Common, 0), Formulas.SkillAwakenCost(b, Grade.Legendary, 0));
        }

        [Test]
        public void Stars_RaiseTheOwnedEffect_AndTheCastersScale()
        {
            var b = new BalanceValues();
            SaveDataV2 save = SaveDataV2.CreateNew();
            new SkillService(save, b);
            string id = save.ownedSkills[0];
            SkillBook.SetLevel(save, id, b.SKILL_MAX_LEVEL);
            double before = SkillService.OwnedAtkBonus(b, save);
            SkillBook.SetStars(save, id, 2);
            double after = SkillService.OwnedAtkBonus(b, save);
            double one = Formulas.SkillOwnedAtk(b, SkillCatalog.Find(id).Grade, b.SKILL_MAX_LEVEL);
            Assert.AreEqual(one * 2 * b.AWAKEN_OWNED_GAIN / 100d, after - before, 1e-12);
            Assert.AreEqual(1d + 3 * b.AWAKEN_SKILL_GAIN / 100d, Formulas.SkillAwakenScale(b, 3), 1e-12);
        }

        [Test]
        public void OldSave_WithoutStars_IsPaddedWithZeros()
        {
            SaveDataV2 save = SaveDataV2.CreateNew();
            new SkillService(save, new BalanceValues());
            save.ownedSkillStars = null;
            Assert.AreEqual(0, SkillBook.GetStars(save, save.ownedSkills[0]));
            Assert.AreEqual(save.ownedSkills.Count, save.ownedSkillStars.Count);
        }
    }
}
