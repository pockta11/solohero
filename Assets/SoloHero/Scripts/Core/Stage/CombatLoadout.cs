using System;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// Pushes the saved growth state (level, upgrades, equipment, skill collection and slots, talents) into a running stage.
    /// D-109: gloves and necklace multiply with sword and armor; ring and earring ride the talent channel; the gear owned
    /// bonus adds to the skill owned bonus, and so does the D-114 pet owned bonus. The equipped pet joins the fight.
    /// Call after boot and after every growth change so combat always uses the current loadout.
    /// </summary>
    public static class CombatLoadout
    {
        public static HeroStats ComputeStats(BalanceValues balance, SaveDataV2 save) => Compute(balance, save, (UpgradeLane)(-1));

        /// <summary>Upgrade preview: the stats <see cref="ComputeStats"/> would return with one more level in <paramref name="lane"/>.</summary>
        public static HeroStats ComputeStatsAfterUpgrade(BalanceValues balance, SaveDataV2 save, UpgradeLane lane) => Compute(balance, save, lane);

        private static HeroStats Compute(BalanceValues balance, SaveDataV2 save, UpgradeLane raised)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (save == null) throw new ArgumentNullException(nameof(save));

            LaneLevels lanes = LaneLevels.From(save);
            if (raised >= 0) lanes = lanes.Plus(raised);
            return Compute(balance, save, ApPoints.From(save), lanes);
        }

        /// <summary>D-141 stat window preview: the stats with <paramref name="ap"/> instead of the saved AP.</summary>
        public static HeroStats ComputeStatsWithAp(BalanceValues balance, SaveDataV2 save, ApPoints ap) =>
            Compute(balance, save, ap, LaneLevels.From(save));

        private static HeroStats Compute(BalanceValues balance, SaveDataV2 save, ApPoints ap, LaneLevels lanes)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (save == null) throw new ArgumentNullException(nameof(save));

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, save);
            return StatAggregator.Compute(
                balance,
                ap,
                lanes,
                bonus.AtkMult,
                bonus.HpMult,
                bonus.HelmMult,
                bonus.BootsSpeedBonus,
                bonus.BootsCritBonus,
                default,
                SkillService.OwnedAtkBonus(balance, save) + bonus.OwnedAtk + PetService.OwnedAtkBonus(balance, save),
                Effects(balance, save, bonus),
                JobService.TierOf(save));
        }

        /// <summary>Talents and job mastery (D-087, D-104) plus the D-109 ring and earring, which share their channel.</summary>
        public static TalentEffects Effects(BalanceValues balance, SaveDataV2 save) =>
            Effects(balance, save, EquipmentBonus.Resolve(balance, save));

        private static TalentEffects Effects(BalanceValues balance, SaveDataV2 save, EquipmentBonus bonus)
        {
            TalentEffects effects = JobService.Effects(save);
            bonus.AddTo(effects);
            return effects;
        }

        public static void Apply(StageRunner runner, BalanceValues balance, SaveDataV2 save)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            runner.SetHeroStats(ComputeStats(balance, save));
            int pet = PetCatalog.IndexOf(save.companionEquipped);
            runner.Pet.Set(PetService.IsOwned(save, pet) ? PetCatalog.All[pet] : null, PetService.AttackScale(balance, save, pet));
            runner.SetTalents(Effects(balance, save));
            int slots = SkillService.SlotCount(balance);
            for (int slot = 0; slot < slots; slot++)
            {
                SkillDef def = SkillService.IsSlotUnlocked(balance, slot, save.heroLevel)
                    ? SkillCatalog.Find(SkillBook.EquippedAt(save, slot))
                    : null;
                // D-104: a skill of another line (an old save, or a job change) stays equipped but does not cast.
                if (!JobService.CanUse(save, def)) def = null;
                runner.Skills.SetSlot(slot, def, def != null ? SkillBook.GetLevel(save, def.Id) : 1);
            }

            JobDef job = JobCatalog.Find(save.jobId);
            runner.Hero.SetMainAttack(job.Main);
            runner.Skills.SetSlot(runner.Skills.UltimateSlot, job.Ultimate, 1);
        }
    }
}
