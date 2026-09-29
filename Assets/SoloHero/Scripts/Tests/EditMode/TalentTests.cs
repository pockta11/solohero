using NUnit.Framework;
using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;
using SoloHero.Core.Talents;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-087: talent points, tier gates, reset, and the stat / combat effects.</summary>
    public sealed class TalentTests
    {
        private sealed class FakeSaveRequester : ISaveRequester
        {
            public int RequestCount;
            public void RequestSave() => RequestCount++;
        }

        private sealed class FixedRandom : IRandom
        {
            public double NextDouble() => 0.99;
            public int Next(int maxExclusive) => 0;
        }

        private static SaveDataV2 Save(int level)
        {
            SaveDataV2 save = SaveDataV2.CreateNew();
            save.heroLevel = level;
            return save;
        }

        [Test]
        public void Earned_OnePointPerLevelAfterFirst()
        {
            var b = new BalanceValues();
            Assert.AreEqual(0, TalentService.Earned(b, Save(1)));
            Assert.AreEqual(9, TalentService.Earned(b, Save(10)));
        }

        [Test]
        public void TryLearn_WithPoint_RaisesRankSpendsPointAndSaves()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(3);
            var requester = new FakeSaveRequester();
            var talents = new TalentService(save, b, requester);

            Assert.IsTrue(talents.TryLearn("sharpness").Ok);
            Assert.AreEqual(1, talents.RankOf("sharpness"));
            Assert.AreEqual(1, talents.AvailablePoints);
            Assert.AreEqual(1, requester.RequestCount);
        }

        [Test]
        public void TryLearn_NoPoints_ReturnsNoTalentPoints()
        {
            var talents = new TalentService(Save(1), new BalanceValues());
            Assert.AreEqual(FailReason.NoTalentPoints, talents.TryLearn("sharpness").Reason);
        }

        [Test]
        public void TryLearn_TierBelowRequirement_ReturnsLocked()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(30);
            var talents = new TalentService(save, b);
            Assert.AreEqual(FailReason.Locked, talents.TryLearn("ferocity").Reason);

            for (int i = 0; i < b.TALENT_TIER_STEP; i++) Assert.IsTrue(talents.TryLearn("sharpness").Ok);
            Assert.IsTrue(talents.TryLearn("ferocity").Ok);
            // Points in another branch do not open this one.
            Assert.AreEqual(FailReason.Locked, talents.TryLearn("endurance").Reason);
        }

        [Test]
        public void TryLearn_AtMaxRank_ReturnsMaxLevel()
        {
            var talents = new TalentService(Save(30), new BalanceValues());
            for (int i = 0; i < 5; i++) Assert.IsTrue(talents.TryLearn("vitality").Ok);
            Assert.AreEqual(FailReason.MaxLevel, talents.TryLearn("vitality").Reason);
        }

        [Test]
        public void TryReset_WithGems_ReturnsEveryPointForGems()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(10);
            save.gem = b.TALENT_RESET_GEM;
            var talents = new TalentService(save, b);
            talents.TryLearn("sharpness");
            talents.TryLearn("vitality");

            Assert.IsTrue(talents.TryReset().Ok);
            Assert.AreEqual(0d, save.gem);
            Assert.AreEqual(9, talents.AvailablePoints);
            Assert.AreEqual(0, talents.RankOf("sharpness"));
        }

        [Test]
        public void TryReset_NotEnoughGem_KeepsRanks()
        {
            SaveDataV2 save = Save(10);
            var talents = new TalentService(save, new BalanceValues());
            talents.TryLearn("sharpness");
            Assert.AreEqual(FailReason.NotEnoughGem, talents.TryReset().Reason);
            Assert.AreEqual(1, talents.RankOf("sharpness"));
        }

        [Test]
        public void ComputeStats_WithTalents_AppliesAtkHpAndCritDamage()
        {
            var b = new BalanceValues();
            SaveDataV2 save = Save(30);
            HeroStats bare = CombatLoadout.ComputeStats(b, save);
            var talents = new TalentService(save, b);
            for (int i = 0; i < 5; i++)
            {
                talents.TryLearn("sharpness");
                talents.TryLearn("vitality");
            }

            talents.TryLearn("ferocity");
            HeroStats now = CombatLoadout.ComputeStats(b, save);
            Assert.AreEqual(bare.Atk * 1.10, now.Atk, 1e-9);
            Assert.AreEqual(bare.Hp * 1.15, now.Hp, 1e-9);
            Assert.AreEqual(0.08, now.CritDamageBonus, 1e-9);
        }

        [Test]
        public void LastStand_FirstLethalHit_LeavesHeroAliveOnce()
        {
            var b = new BalanceValues();
            var hero = new HeroBrain(b, new FixedRandom(), new HeroStats(100, 10, 0, 1, 0));
            var effects = new TalentEffects();
            effects.Add(TalentStat.LastStand, 1);
            hero.SetTalents(effects);

            hero.ApplyDamage(500);
            Assert.AreNotEqual(HeroState.Dead, hero.State);
            Assert.AreEqual(100 * TalentCatalog.LastStandHealPct, hero.Hp, 1e-9);

            hero.ApplyDamage(500);
            Assert.AreEqual(HeroState.Dead, hero.State);
        }

        [Test]
        public void Cooldown_WithHaste_ShortensSkillCooldown()
        {
            var b = new BalanceValues();
            var caster = new SkillAutoCaster(b);
            SkillDef def = SkillCatalog.Find(SkillCatalog.PowerStrike);
            var effects = new TalentEffects();
            effects.Add(TalentStat.CooldownPct, 0.1);
            caster.SetTalents(effects);
            Assert.AreEqual(def.Cooldown * 0.9f, caster.Cooldown(def), 1e-4);
        }

        [Test]
        public void Catalog_EveryNodeHasStringsAndValidTier()
        {
            foreach (TalentDef def in TalentCatalog.All)
            {
                Assert.That(def.Tier, Is.InRange(0, TalentCatalog.TierCount - 1), def.Id);
                Assert.That(def.Column, Is.InRange(0, 1), def.Id);
                Assert.Greater(def.PerRank, 0d, def.Id);
            }
        }
    }
}
