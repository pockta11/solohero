using System;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>Shared lookups and formatting for the growth panels.</summary>
    public static class PanelServices
    {
        public static T TryGet<T>() where T : class
        {
            try
            {
                return Services.Get<T>();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>GDD grade colors: Common #9E9E9E, Rare #3D8BFF, Epic #A24BFF, Legendary #FFC531.</summary>
        public static Color GradeColor(Grade grade)
        {
            switch (grade)
            {
                case Grade.Common: return new Color32(0x9E, 0x9E, 0x9E, 0xFF);
                case Grade.Rare: return new Color32(0x3D, 0x8B, 0xFF, 0xFF);
                case Grade.Epic: return new Color32(0xA2, 0x4B, 0xFF, 0xFF);
                default: return new Color32(0xFF, 0xC5, 0x31, 0xFF);
            }
        }

        public static string GradeName(Grade grade)
        {
            switch (grade)
            {
                case Grade.Common: return "Common";
                case Grade.Rare: return "Rare";
                case Grade.Epic: return "Epic";
                default: return "Legendary";
            }
        }
    }
}
