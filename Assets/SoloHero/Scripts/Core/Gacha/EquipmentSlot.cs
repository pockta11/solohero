namespace SoloHero.Core.Gacha
{
    /// <summary>
    /// Equipment slots. The first four are the v1.0 gear; D-109 added four accessories after them. The names are part
    /// of the saved ids ("Equipment_{Slot}_{Grade}"), so never rename one; new slots go at the end.
    /// </summary>
    public enum EquipmentSlot
    {
        Sword = 0,
        Helm = 1,
        Armor = 2,
        Boots = 3,
        Gloves = 4,
        Necklace = 5,
        Ring = 6,
        Earring = 7
    }
}
