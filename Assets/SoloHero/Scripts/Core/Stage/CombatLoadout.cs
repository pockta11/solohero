using System;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// Pushes the saved growth state (level, upgrades, equipment, skill levels and unlocks) into a running stage.
    /// Call after boot and after every growth change so combat always uses the current loadout.
    /// </summary>
    public static class CombatLoadout
    {
        private static readonly SkillSlot[] Slots = { SkillSlot.Slot1, SkillSlot.Slot2, SkillSlot.Slot3 };

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
                bonus.BootsCritBonus);
        }

        public static void Apply(StageRunner runner, BalanceValues balance, SaveDataV2 save)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            runner.SetHeroStats(ComputeStats(balance, save));
            for (int i = 0; i < Slots.Length; i++)
            {
                SkillSlot slot = Slots[i];
                runner.Skills.SetUnlocked(slot, SkillLevelService.IsUnlocked(balance, slot, save.heroLevel));
                runner.Skills.SetLevel(slot, SkillLevelService.EffectiveLevel(SavedSkillLevel(save, slot)));
            }
        }

        private static int SavedSkillLevel(SaveDataV2 save, SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Slot1: return save.skillLevel1;
                case SkillSlot.Slot2: return save.skillLevel2;
                case SkillSlot.Slot3: return save.skillLevel3;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
    }
}
