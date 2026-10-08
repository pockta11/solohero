using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-108 text and tint colours. "Ink" colours are for lettering on the cream panels, cards and windows (no outline);
    /// "Hud" colours are for outlined lettering on the dark HUD, battle field and overlays.
    /// </summary>
    public static class UiPalette
    {
        public static readonly Color InkTitle = new Color32(0x4A, 0x2C, 0x1C, 0xFF);
        public static readonly Color Ink = new Color32(0x6A, 0x46, 0x30, 0xFF);
        public static readonly Color InkMuted = new Color32(0xA3, 0x82, 0x66, 0xFF);
        public static readonly Color InkFaint = new Color32(0xC4, 0xAB, 0x8E, 0xFF);
        public static readonly Color InkGood = new Color32(0x2C, 0x9C, 0x45, 0xFF);
        public static readonly Color InkBad = new Color32(0xD8, 0x42, 0x3E, 0xFF);
        public static readonly Color InkGold = new Color32(0xD9, 0x7A, 0x00, 0xFF);
        public static readonly Color InkBlue = new Color32(0x2F, 0x6F, 0xD8, 0xFF);

        public static readonly Color HudGold = new Color32(0xFF, 0xD6, 0x5C, 0xFF);
        public static readonly Color HudGem = new Color32(0xA6, 0xE4, 0xFF, 0xFF);
        public static readonly Color HudMuted = new Color32(0xC8, 0xCC, 0xF0, 0xFF);
        public static readonly Color HudGood = new Color32(0x9C, 0xFF, 0x7A, 0xFF);

        /// <summary>D-127: a recommended combat power not met yet.</summary>
        public static readonly Color HudBad = new Color32(0xFF, 0x8A, 0x7E, 0xFF);

        /// <summary>Grade names on the cream surfaces (darker than the GDD grade colours so they read on cream).</summary>
        public static Color GradeInk(GearGrade grade)
        {
            switch (grade)
            {
                case GearGrade.Common: return new Color32(0x7C, 0x80, 0x8C, 0xFF);
                case GearGrade.Uncommon: return new Color32(0x2E, 0x9A, 0x3E, 0xFF);
                case GearGrade.Rare: return new Color32(0x2A, 0x74, 0xE6, 0xFF);
                case GearGrade.Epic: return new Color32(0x8C, 0x3C, 0xE6, 0xFF);
                case GearGrade.Mythic: return new Color32(0xD8, 0x28, 0x3A, 0xFF);
                case GearGrade.Ancient: return new Color32(0x0F, 0x9C, 0x8E, 0xFF);
                default: return new Color32(0xE0, 0x86, 0x00, 0xFF);
            }
        }

        public static Color GradeInk(Grade grade) => GradeInk(grade.ToGearGrade());

        /// <summary>Tint for a locked or unowned icon on cream: a soft silhouette.</summary>
        public static readonly Color Silhouette = new Color(0.35f, 0.27f, 0.22f, 0.35f);
    }
}
