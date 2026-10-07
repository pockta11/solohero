namespace SoloHero.Core.Gacha
{
    /// <summary>
    /// D-113 equipment grades, seven tiers; skills, companions and jobs keep the four-tier <see cref="Grade"/>.
    /// The names are part of the saved ids ("Equipment_{Slot}_{Grade}"): Common, Rare, Epic and Legendary kept the
    /// names they had as a <see cref="Grade"/>, so older saves stay valid. Never rename one.
    /// </summary>
    public enum GearGrade
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5,
        Ancient = 6
    }

    public static class GearGrades
    {
        public const int Count = 7;

        /// <summary>
        /// Where a four-tier grade sits on the seven-tier ladder, so skills and gear share one set of grade colours,
        /// frames and names.
        /// </summary>
        public static GearGrade ToGearGrade(this Grade grade)
        {
            switch (grade)
            {
                case Grade.Rare: return GearGrade.Rare;
                case Grade.Epic: return GearGrade.Epic;
                case Grade.Legendary: return GearGrade.Legendary;
                default: return GearGrade.Common;
            }
        }
    }
}
