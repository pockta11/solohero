using System;
using NUnit.Framework;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;

namespace SoloHero.Tests.EditMode
{
    /// <summary>D-127 recommended combat power, D-128 tickets and shop, D-129 speed booster, D-130 infinite tower.</summary>
    public sealed class TowerShopTests
    {
        private sealed class FakeClock : IClock
        {
            public DateTime Local = new DateTime(2026, 10, 8, 10, 0, 0);
            public long Utc = 1_800_000_000L;

            public long UtcNowSeconds => Utc;

            public DateTime LocalNow => Local;
        }

        [Test]
        public void RecommendedCp_GrowsEveryStage_BossesAskMore()
        {
            var b = new BalanceValues();
            double s11 = CombatPower.Recommended(b, 11);
            double s12 = CombatPower.Recommended(b, 12);
            Assert.AreEqual(b.REC_CP_GROWTH, s12 / s11, 0.01);
            double boss = CombatPower.Recommended(b, 20);
            double plain = CombatPower.Recommended(b, 20, false);
            Assert.AreEqual(b.REC_CP_BOSS_MULT, boss / plain, 0.01);
            Assert.AreEqual(CombatPower.Recommended(b, TowerService.StageOf(b, 7), true), TowerService.RecommendedCp(b, 7));
        }

        [Test]
        public void Shop_FreeBundleOncePerDay_DealsCostGemsAndGiveTickets()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var shop = new ShopService(b, save, clock);

            Assert.IsTrue(shop.FreeReady);
            Assert.IsTrue(shop.TryBuy(ShopItem.FreeGear).Ok);
            Assert.AreEqual(b.SHOP_FREE_GEAR_TICKETS, save.gearTickets);
            Assert.AreEqual(FailReason.DailyLimit, shop.TryBuy(ShopItem.FreeGear).Reason);
            Assert.IsFalse(shop.FreeReady);

            Assert.AreEqual(FailReason.NotEnoughGem, shop.TryBuy(ShopItem.DealSkill).Reason);
            save.gem = b.SHOP_DEAL_SKILL_GEM;
            Assert.IsTrue(shop.TryBuy(ShopItem.DealSkill).Ok);
            Assert.AreEqual(b.SHOP_DEAL_TICKETS, save.skillTickets);
            Assert.AreEqual(0d, save.gem, 1e-9);

            clock.Local = clock.Local.AddDays(1);
            Assert.IsTrue(shop.FreeReady, "a new day refills the daily items");
            Assert.IsFalse(shop.Bought(ShopItem.DealSkill));
        }

        [Test]
        public void Shop_GoldPack_HasNoDailyLimit()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            save.gem = b.GEM_GOLD_PACK_COST * 2;
            var shop = new ShopService(b, save, new FakeClock());
            Assert.IsTrue(shop.TryBuy(ShopItem.GoldPack).Ok);
            Assert.IsTrue(shop.TryBuy(ShopItem.GoldPack).Ok);
            Assert.Greater(save.gold, 0d);
        }

        [Test]
        public void Tickets_PayForPulls_UpToTen()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var gacha = new GachaService(b, GearTableValues.FromBalance(b), new SystemRandom(new Random(2)), GachaCatalog.Standard(b));
            Assert.AreEqual(FailReason.NotEnoughTicket, gacha.TryPullTickets(save, 10).Status.Reason);

            save.gearTickets = 13;
            GachaBatchResult r = gacha.TryPullTickets(save, 10);
            Assert.IsTrue(r.Status.Ok);
            Assert.AreEqual(10, r.Items.Length);
            Assert.AreEqual(3, save.gearTickets);
            Assert.AreEqual(10, save.totalPullCount);
            Assert.AreEqual(3, gacha.TryPullTickets(save, 10).Items.Length);

            var skills = new SkillSummonService(b, GachaTableValues.FromBalance(b), new SystemRandom(new Random(3)));
            save.skillTickets = 5;
            Assert.AreEqual(FailReason.JobLocked, skills.TryPullTickets(save, 10).Status.Reason, "skills follow the job line");

            var pets = new PetSummonService(b, GearTableValues.FromBalance(b), new SystemRandom(new Random(4)));
            PetService.EnsureOwned(save);
            save.petTickets = 5;
            Assert.AreEqual(FailReason.Locked, pets.TryPullTickets(save, 10).Status.Reason, "pets open after the first boss");
            save.highestStage = b.PET_UNLOCK_STAGE;
            Assert.AreEqual(5, pets.TryPullTickets(save, 10).Items.Length);
            Assert.AreEqual(0, save.petTickets);
        }

        [Test]
        public void SpeedBooster_RunsFasterUntilItEnds_AndCannotStack()
        {
            var b = new BalanceValues { AD_SPEED_DAILY = 3 };
            var save = SaveDataV2.CreateNew();
            var clock = new FakeClock();
            var ads = new AdSlotPolicy(b, save, clock);

            Assert.AreEqual(1f, ads.BattleSpeedMultiplier);
            Assert.IsTrue(ads.Complete(AdSlot.BattleSpeed, AdOutcome.Rewarded).Ok);
            Assert.AreEqual(b.AD_SPEED_MULT, ads.BattleSpeedMultiplier);
            Assert.AreEqual(FailReason.Busy, ads.CanUse(AdSlot.BattleSpeed).Reason);

            clock.Utc += (long)b.AD_SPEED_SECONDS;
            Assert.AreEqual(1f, ads.BattleSpeedMultiplier);
            Assert.AreEqual(b.AD_SPEED_DAILY - 1, ads.Remaining(AdSlot.BattleSpeed));
        }

        [Test]
        public void Tower_RewardsMilestones()
        {
            var b = new BalanceValues();
            TowerReward first = TowerService.Preview(b, 1);
            Assert.Greater(first.Gems, 0d);
            Assert.AreEqual(0, first.GearTickets);
            Assert.AreEqual(b.TOWER_GEAR_TICKETS, TowerService.Preview(b, b.TOWER_TICKET_EVERY).GearTickets);
            TowerReward rare = TowerService.Preview(b, b.TOWER_RARE_EVERY);
            Assert.AreEqual(b.TOWER_SKILL_TICKETS, rare.SkillTickets);
            Assert.AreEqual(b.TOWER_PET_TICKETS, rare.PetTickets);
            Assert.Greater(TowerService.Gems(b, 50), TowerService.Gems(b, 1));
        }

        [Test]
        public void Tower_StrongHeroClimbsAndIsPaid_WeakHeroReturnsToItsStage()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            PetService.EnsureOwned(save);
            save.highestStage = b.TOWER_UNLOCK_STAGE;
            var runner = new StageRunner(b, new SystemRandom(new Random(5)), new HeroStats(1e9, 1e7, 1e7, b.ATKSPD_BASE, 0d), save);
            runner.Hero.AttackRequested += () => runner.Hero.OnHitFrame(runner.World);
            runner.Resume(3, false);
            var tower = new TowerService(b, save);

            Assert.IsTrue(tower.TryEnter(runner).Ok);
            Assert.IsTrue(runner.InTower);
            int cleared = 0;
            runner.TowerFloorCleared += reward => cleared++;
            for (int i = 0; i < 400; i++) runner.Tick(0.05f);
            Assert.Greater(cleared, 2, "a hero this strong climbs floor after floor");
            Assert.AreEqual(cleared, save.towerFloor);
            Assert.Greater(save.gem, 0d);

            // A weak hero cannot beat the next floor in time: the run ends and the runner goes back to stage 3.
            var weak = new StageRunner(b, new SystemRandom(new Random(6)), new HeroStats(1e9, 1d, 1e7, b.ATKSPD_BASE, 0d), save);
            weak.Resume(3, false);
            Assert.IsTrue(weak.StartTower(save.towerFloor + 1));
            for (int i = 0; i < 2000 && weak.InTower; i++) weak.Tick(0.05f);
            Assert.IsFalse(weak.InTower);
            Assert.AreEqual(3, weak.GlobalStage);
        }

        [Test]
        public void Tower_LockedUntilTheFirstBoss()
        {
            var b = new BalanceValues();
            var save = SaveDataV2.CreateNew();
            var runner = new StageRunner(b, new SystemRandom(new Random(7)), new HeroStats(100, 10, 10, b.ATKSPD_BASE, 0d), save);
            runner.Resume(1, false);
            Assert.AreEqual(FailReason.Locked, new TowerService(b, save).TryEnter(runner).Reason);
        }
    }
}
