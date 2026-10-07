using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Equipment
{
    /// <summary>
    /// D-116 equipment promotion (genre "promotion"): the enhance levels of a gear item at EQUIP_MAX_LEVEL buy the same
    /// slot's next grade at +EQUIP_PROMOTE_ENHANCE (or that many levels on a copy already owned, capped at the max). The
    /// source stays in the collection at +0, so later duplicates can raise and promote it again, and if it was
    /// equipped the promoted item takes its slot. Up to EQUIP_PROMOTE_MAX_GRADE (Epic): Legendary and above only come
    /// from the summon. The caller requests the save and refreshes the loadout.
    /// </summary>
    public static class EquipPromotion
    {
        public static double Cost(BalanceValues balance, GearGrade from) =>
            Math.Floor(balance.EQUIP_PROMOTE_COST * Math.Pow(balance.EQUIP_PROMOTE_COST_GROWTH, (int)from));

        public static bool CanPromote(BalanceValues balance, SaveDataV2 data, string id) =>
            Target(balance, data, id, out _, out _);

        /// <summary>The next-grade id an owned max-level item promotes into; false when it cannot be promoted.</summary>
        public static bool Target(BalanceValues balance, SaveDataV2 data, string id, out string target, out GearGrade from)
        {
            target = null;
            from = default;
            if (balance == null || data == null || !data.ownedEquipment.Contains(id)) return false;
            if (!EquipmentBonus.TryParseAny(id, out EquipmentSlot slot, out from)) return false;
            if ((int)from + 1 > balance.EQUIP_PROMOTE_MAX_GRADE) return false;
            if (EquipmentLevels.Get(data, id) < balance.EQUIP_MAX_LEVEL) return false;
            target = GachaCatalog.IdOf(slot, (GearGrade)((int)from + 1));
            return true;
        }

        public static Result TryPromote(BalanceValues balance, SaveDataV2 data, string id)
        {
            if (!Target(balance, data, id, out string target, out GearGrade from)) return Result.Fail(FailReason.Locked);
            double cost = Cost(balance, from);
            if (data.gold < cost) return Result.Fail(FailReason.NotEnoughGold);
            data.gold -= cost;

            EquipmentBonus.TryParseAny(id, out EquipmentSlot slot, out _);
            bool equipped = EquippedSlots.Get(data, slot) == id;
            EquipmentLevels.Set(data, id, 0);
            if (data.ownedEquipment.Contains(target))
            {
                int level = EquipmentLevels.Get(data, target);
                EquipmentLevels.Set(data, target, Math.Min(balance.EQUIP_MAX_LEVEL, level + balance.EQUIP_PROMOTE_ENHANCE));
            }
            else
            {
                EquipmentLevels.AddOwned(data, target);
                EquipmentLevels.Set(data, target, balance.EQUIP_PROMOTE_ENHANCE);
            }

            if (equipped) EquippedSlots.Set(data, slot, target);
            return Result.Success;
        }

        /// <summary>Gold to promote every item that can be promoted right now (one step each).</summary>
        public static double TotalCost(BalanceValues balance, SaveDataV2 data, out int count)
        {
            count = 0;
            double total = 0d;
            if (balance == null || data == null) return 0d;
            for (int i = 0; i < data.ownedEquipment.Count; i++)
            {
                if (!Target(balance, data, data.ownedEquipment[i], out _, out GearGrade from)) continue;
                count++;
                total += Cost(balance, from);
            }

            return total;
        }

        /// <summary>
        /// "Promote all": lowest grade first while gold lasts, repeating as a promotion caps a copy that can promote in
        /// turn. Returns how many promotions were made and the gold spent.
        /// </summary>
        public static int PromoteAll(BalanceValues balance, SaveDataV2 data, out double spent)
        {
            spent = 0d;
            int made = 0;
            if (balance == null || data == null) return 0;
            for (int guard = 0; guard < 256; guard++)
            {
                string best = null;
                GearGrade bestGrade = default;
                for (int i = 0; i < data.ownedEquipment.Count; i++)
                {
                    string id = data.ownedEquipment[i];
                    if (!Target(balance, data, id, out _, out GearGrade from)) continue;
                    if (best != null && from >= bestGrade) continue;
                    best = id;
                    bestGrade = from;
                }

                if (best == null) break;
                double cost = Cost(balance, bestGrade);
                if (!TryPromote(balance, data, best).Ok) break;
                spent += cost;
                made++;
            }

            return made;
        }
    }
}
