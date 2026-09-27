using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Gacha
{
    /// <summary>The v1.0 gacha pool: 4 slots x 4 grades, ids "Equipment_{Slot}_{Grade}".</summary>
    public static class GachaCatalog
    {
        public const int SlotCount = 4;
        public const int GradeCount = 4;

        public static string IdOf(EquipmentSlot slot, Grade grade) => "Equipment_" + slot + "_" + grade;

        public static double RefundOf(BalanceValues balance, Grade grade)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            switch (grade)
            {
                case Grade.Common: return balance.REFUND_C;
                case Grade.Rare: return balance.REFUND_R;
                case Grade.Epic: return balance.REFUND_E;
                case Grade.Legendary: return balance.REFUND_L;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }
        }

        public static GachaEquipmentDef[] Standard(BalanceValues balance)
        {
            var defs = new GachaEquipmentDef[SlotCount * GradeCount];
            for (int s = 0; s < SlotCount; s++)
            {
                for (int g = 0; g < GradeCount; g++)
                {
                    var slot = (EquipmentSlot)s;
                    var grade = (Grade)g;
                    defs[s * GradeCount + g] = new GachaEquipmentDef(IdOf(slot, grade), slot, grade, RefundOf(balance, grade));
                }
            }

            return defs;
        }
    }
}
