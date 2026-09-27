using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>Sprite clips for combat VFX (E8-08). White art is tinted per use (crit gold, heal green, ...).</summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Vfx Set")]
    public sealed class VfxSet : ScriptableObject
    {
        public Sprite[] slash = new Sprite[0];
        public Sprite[] whirl = new Sprite[0];
        public Sprite[] ring = new Sprite[0];
        public Sprite[] boom = new Sprite[0];
        public Sprite[] spark = new Sprite[0];
        public float fps = 18f;
    }
}
