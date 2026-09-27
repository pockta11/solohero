using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// One chapter's background (E3-11, E8-05): parallax layers back to front, how much each follows the camera
    /// (1 = fixed to the camera, 0 = fixed to the world), an integer pixel scale and the sky colour above the art.
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Chapter Theme")]
    public sealed class ChapterTheme : ScriptableObject
    {
        public Sprite[] layers = new Sprite[0];
        public float[] follow = new float[0];
        public int pixelScale = 1;
        public Color sky = Color.black;
    }
}
