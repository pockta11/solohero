using System;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// Pushes the saved growth state (level, upgrades, equipment, skill collection and slots) into a running stage.
    /// Call after boot and after every growth change so combat always uses the current loadout.
    /// </summary>
    public static class CombatLoadout
    {
        public static HeroStats ComputeStats(BalanceValues balance, SaveDataV2 save)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (save == null) throw new ArgumentNullException(nameof(save));

            EquipmentBonus bonus = EquipmentBonus.Resolve(balance, save);
            return StatAggregator.Compute(
                balance,
                save.heroLevel,
                save.upgradeHp,
                save.upgradeAtk,
                save.upgradeDef,
                save.upgradeSpd,
                bonus.SwordMult,
                bonus.ArmorMult,
                bonus.HelmMult,
                bonus.BootsSpeedBonus,
                bonus.BootsCritBonus,
                default,
                SkillService.OwnedAtkBonus(balance, save));
        }

        public static void Apply(StageRunner runner, BalanceValues balance, SaveDataV2 save)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            runner.SetHeroStats(ComputeStats(balance, save));
            for (int slot = 0; slot < runner.Skills.SlotCount; slot++)
            {
                SkillDef def = SkillService.IsSlotUnlocked(balance, slot, save.heroLevel)
                    ? SkillCatalog.Find(SkillBook.EquippedAt(save, slot))
                    : null;
                runner.Skills.SetSlot(slot, def, def != null ? SkillBook.GetLevel(save, def.Id) : 1);
            }
        }
    }
}
