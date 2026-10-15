using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Equipment
{
    /// <summary>
    /// D-160 (L-09) stone enhancement: an owned gear item of EQUIP_STONE_MIN_GRADE (Epic) or above that duplicates have
    /// taken to EQUIP_MAX_LEVEL climbs on to EQUIP_STONE_MAX_LEVEL with break stones (the stones the limit breaks leave
    /// over). Each level is the same x(1 + EQUIP_ENHANCE_GAIN) on the item's slot effect and owned bonus. Promotion only
    /// touches lower grades, so stone levels are never lost. The caller requests the save and refreshes the loadout.
    /// </summary>
    public static class StoneEnhance
    {
        /// <summary>Stones for the step from <paramref name="level"/> to the next.</summary>
        public static int Cost(BalanceValues balance, int level) =>
            balance.EQUIP_STONE_COST * Math.Max(1, level + 1 - balance.EQUIP_MAX_LEVEL);

        /// <summary>The item's grade takes stone levels (Epic and up), whatever its level now.</summary>
        public static bool GradeAllows(BalanceValues balance, string id) =>
            EquipmentBonus.TryParseAny(id, out _, out GearGrade grade) && (int)grade >= balance.EQUIP_STONE_MIN_GRADE;

        /// <summary>Owned, of a grade that takes stones, at least at EQUIP_MAX_LEVEL and below the stone cap.</summary>
        public static bool CanEnhance(BalanceValues balance, SaveDataV2 data, string id)
        {
            if (balance == null || data == null || string.IsNullOrEmpty(id) || !data.ownedEquipment.Contains(id)) return false;
            if (!GradeAllows(balance, id)) return false;
            int level = EquipmentLevels.Get(data, id);
            return level >= balance.EQUIP_MAX_LEVEL && level < balance.EQUIP_STONE_MAX_LEVEL;
        }

        public static Result TryEnhance(BalanceValues balance, SaveDataV2 data, string id)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(id) || !data.ownedEquipment.Contains(id) || !GradeAllows(balance, id)) return Result.Fail(FailReason.Locked);
            int level = EquipmentLevels.Get(data, id);
            if (level >= balance.EQUIP_STONE_MAX_LEVEL) return Result.Fail(FailReason.MaxLevel);
            if (level < balance.EQUIP_MAX_LEVEL) return Result.Fail(FailReason.Locked);
            int cost = Cost(balance, level);
            if (data.breakStones < cost) return Result.Fail(FailReason.NotEnoughStones);
            data.breakStones -= cost;
            EquipmentLevels.Set(data, id, level + 1);
            return Result.Success;
        }
    }
}
