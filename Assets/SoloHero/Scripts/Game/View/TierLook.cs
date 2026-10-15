using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// D-153 (L-02) how a difficulty tier recolours the chapter art it reuses: a multiply over the background layers and
    /// the floor, a tint over the enemies, and the boss outline colour. The normal tier draws the art as made; tiers past
    /// abyss cycle hard to abyss. Tints go through SpriteRenderer.color, so atlases and batching stay as they are (D-149).
    /// </summary>
    public static class TierLook
    {
        private static readonly Color[] Background =
        {
            Color.white,
            new Color(0.9f, 0.83f, 0.8f, 1f),
            new Color(1f, 0.74f, 0.64f, 1f),
            new Color(0.8f, 0.72f, 0.96f, 1f),
            new Color(0.66f, 0.76f, 0.94f, 1f)
        };

        private static readonly Color[] Enemy =
        {
            Color.white,
            new Color(1f, 0.9f, 0.88f, 1f),
            new Color(1f, 0.8f, 0.72f, 1f),
            new Color(0.9f, 0.82f, 1f, 1f),
            new Color(0.8f, 0.88f, 1f, 1f)
        };

        private static readonly Color[] BossOutline =
        {
            new Color(0.42f, 0.04f, 0.12f, 1f),
            new Color(0.6f, 0.1f, 0.04f, 1f),
            new Color(0.9f, 0.3f, 0.04f, 1f),
            new Color(0.5f, 0.12f, 0.75f, 1f),
            new Color(0.08f, 0.4f, 0.72f, 1f)
        };

        public static Color BackgroundTint(int tier) => Background[Index(tier)];

        public static Color EnemyTint(int tier) => Enemy[Index(tier)];

        public static Color BossOutlineColor(int tier) => BossOutline[Index(tier)];

        private static int Index(int tier) => tier <= 0 ? 0 : (tier - 1) % (Background.Length - 1) + 1;
    }
}
