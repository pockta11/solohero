using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>What one reveal card shows (equipment or skill summon): grade, title, note and icon.</summary>
    public readonly struct RevealCard
    {
        public readonly Grade Grade;
        public readonly string Title;
        public readonly string Note;
        public readonly Sprite Icon;

        public RevealCard(Grade grade, string title, string note, Sprite icon)
        {
            Grade = grade;
            Title = title;
            Note = note;
            Icon = icon;
        }
    }
}
