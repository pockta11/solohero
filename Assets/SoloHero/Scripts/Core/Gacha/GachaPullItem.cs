namespace SoloHero.Core.Gacha
{
    public readonly struct GachaPullItem
    {
        public readonly string EquipmentId;
        public readonly EquipmentSlot Slot;
        public readonly Grade Grade;
        public readonly bool WasDuplicate;
        public readonly double RefundGold;
        public readonly bool AutoEquipped;

        public GachaPullItem(
            string equipmentId,
            EquipmentSlot slot,
            Grade grade,
            bool wasDuplicate,
            double refundGold,
            bool autoEquipped)
        {
            EquipmentId = equipmentId;
            Slot = slot;
            Grade = grade;
            WasDuplicate = wasDuplicate;
            RefundGold = refundGold;
            AutoEquipped = autoEquipped;
        }
    }
}
