namespace SoloHero.Core.Gacha
{
    public sealed record GachaEquipmentDef(
        string Id,
        EquipmentSlot Slot,
        Grade Grade,
        double RefundGold);
}
