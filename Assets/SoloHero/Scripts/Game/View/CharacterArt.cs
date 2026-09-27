using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>Sprite clips for one character (E8-02..04). Frames come from the sliced sheets under Art/.</summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Character")]
    public sealed class CharacterArt : ScriptableObject
    {
        public Sprite[] idle = new Sprite[0];
        public Sprite[] run = new Sprite[0];
        public Sprite[] attack = new Sprite[0];
        public Sprite[] hit = new Sprite[0];
        public Sprite[] dead = new Sprite[0];
        public float fps = 10f;

        /// <summary>Integer world scale so the pixel grid stays aligned with the Pixel Perfect Camera.</summary>
        public int pixelScale = 2;
    }
}
