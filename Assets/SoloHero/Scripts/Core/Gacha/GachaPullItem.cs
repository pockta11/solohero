namespace SoloHero.Core.Gacha
{
    public readonly struct GachaPullItem
    {
        public readonly string EquipmentId;
        public readonly EquipmentSlot Slot;
        public readonly GearGrade Grade;
        public readonly bool WasDuplicate;
        public readonly double RefundGold;
        public readonly bool AutoEquipped;

        /// <summary>Enhancement level the duplicate raised the item to; 0 when it was new or refunded.</summary>
        public readonly int EnhancedLevel;

        public GachaPullItem(
            string equipmentId,
            EquipmentSlot slot,
            GearGrade grade,
            bool wasDuplicate,
            double refundGold,
            bool autoEquipped,
            int enhancedLevel = 0)
        {
            EquipmentId = equipmentId;
            Slot = slot;
            Grade = grade;
            WasDuplicate = wasDuplicate;
            RefundGold = refundGold;
            AutoEquipped = autoEquipped;
            EnhancedLevel = enhancedLevel;
        }
    }
}
