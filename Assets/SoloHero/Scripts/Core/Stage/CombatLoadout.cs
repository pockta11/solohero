using System;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// Pushes the saved growth state (level, upgrades, equipment, skill collection and slots, talents) into a running stage.
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

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, save);
            return StatAggregator.Compute(
                balance,
                save.heroLevel,
                save.upgradeHp + (raised == UpgradeLane.Hp ? 1 : 0),
                save.upgradeAtk + (raised == UpgradeLane.Atk ? 1 : 0),
                save.upgradeDef + (raised == UpgradeLane.Def ? 1 : 0),
                save.upgradeSpd + (raised == UpgradeLane.Spd ? 1 : 0),
                bonus.SwordMult,
                bonus.ArmorMult,
                bonus.HelmMult,
                bonus.BootsSpeedBonus,
                bonus.BootsCritBonus,
                default,
                SkillService.OwnedAtkBonus(balance, save),
                JobService.Effects(save),
                JobService.TierOf(save));
        }

        public static void Apply(StageRunner runner, BalanceValues balance, SaveDataV2 save)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            runner.SetHeroStats(ComputeStats(balance, save));
            int companion = SoloHero.Core.Companions.CompanionCatalog.IndexOf(save.companionEquipped);
            runner.Companion.Set(companion >= 0 ? SoloHero.Core.Companions.CompanionCatalog.All[companion] : null,
                SoloHero.Core.Companions.CompanionService.Level(save, companion));
            runner.SetTalents(JobService.Effects(save));
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
