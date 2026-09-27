using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// One chapter's look (E3-11, E8-03..05, E8-13): parallax layers back to front, how much each follows the camera
    /// (1 = fixed to the camera, 0 = fixed to the world), an integer pixel scale, the sky colour above the art,
    /// the enemy looks mixed in its stages, its boss and its music. Looks never change stats (GDD: stats follow g).
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Chapter Theme")]
    public sealed class ChapterTheme : ScriptableObject
    {
        public Sprite[] layers = new Sprite[0];
        public float[] follow = new float[0];
        public int pixelScale = 1;
        public Color sky = Color.black;
        public CharacterArt[] enemies = new CharacterArt[0];
        public CharacterArt boss;
        public AudioClip bgm;

        /// <summary>The n-th enemy of a stage cycles through the chapter's looks.</summary>
        public CharacterArt EnemyFor(int spawnIndex) =>
            enemies.Length == 0 ? null : enemies[(spawnIndex < 0 ? 0 : spawnIndex) % enemies.Length];
    }
}
