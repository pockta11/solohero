using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>Chapter themes in order. Chapter 6+ reuses them with (chapter - 1) % count (E3-12).</summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Chapter Theme Set")]
    public sealed class ChapterThemeSet : ScriptableObject
    {
        public ChapterTheme[] themes = new ChapterTheme[0];
    }
}
