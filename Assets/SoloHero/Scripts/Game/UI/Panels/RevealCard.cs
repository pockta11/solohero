using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// What one reveal card shows (equipment or skill summon): grade, title, note and icon. The grade is on the gear
    /// ladder (D-113); a skill's four grades map onto it by name.
    /// </summary>
    public readonly struct RevealCard
    {
        public readonly GearGrade Grade;
        public readonly string Title;
        public readonly string Note;
        public readonly Sprite Icon;

        /// <summary>D-114: pets are small creatures on a 48 px frame with wide margins, drawn larger than item icons.</summary>
        public readonly float IconScale;

        public RevealCard(GearGrade grade, string title, string note, Sprite icon, float iconScale = 1f)
        {
            Grade = grade;
            Title = title;
            Note = note;
            Icon = icon;
            IconScale = iconScale;
        }
    }
}
