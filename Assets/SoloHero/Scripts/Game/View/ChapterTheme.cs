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

        /// <summary>D-091 floor plane under the battle, tiled horizontally.</summary>
        public Sprite floor;
        public int pixelScale = 1;
        public Color sky = Color.black;
        public CharacterArt[] enemies = new CharacterArt[0];
        public CharacterArt boss;
        public AudioClip bgm;

        [Header("D-110 role looks (fall back to the roster above)")]
        public CharacterArt melee;
        public CharacterArt fast;
        public CharacterArt tank;
        public CharacterArt ranged;

        /// <summary>The n-th enemy of a stage cycles through the chapter's looks.</summary>
        public CharacterArt EnemyFor(int spawnIndex) =>
            enemies.Length == 0 ? null : enemies[(spawnIndex < 0 ? 0 : spawnIndex) % enemies.Length];

        /// <summary>D-110: the chapter's look for a role, so a behaviour always reads the same (skeletons throw bones).</summary>
        public CharacterArt EnemyFor(SoloHero.Core.Combat.EnemyRole role, int spawnIndex)
        {
            CharacterArt art;
            switch (role)
            {
                case SoloHero.Core.Combat.EnemyRole.Fast: art = fast; break;
                case SoloHero.Core.Combat.EnemyRole.Tank: art = tank; break;
                case SoloHero.Core.Combat.EnemyRole.Ranged: art = ranged; break;
                default: art = melee; break;
            }

            return art != null ? art : EnemyFor(spawnIndex);
        }
    }
}
