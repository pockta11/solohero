namespace SoloHero.Core.Gacha
{
    public sealed record GachaEquipmentDef(
        string Id,
        EquipmentSlot Slot,
        GearGrade Grade,
        double RefundGold);
}
