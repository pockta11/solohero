using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>Bevelled 9-slice item frames per grade (Art/UI/ui9_grade*_3.png) plus the empty / locked slot frame.</summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Grade Frames")]
    public sealed class GradeFrameSet : ScriptableObject
    {
        /// <summary>Indexed by <see cref="Grade"/>: Common, Rare, Epic, Legendary.</summary>
        public Sprite[] frames = new Sprite[4];
        public Sprite empty;

        public Sprite Get(Grade grade)
        {
            int i = (int)grade;
            return i >= 0 && i < frames.Length && frames[i] != null ? frames[i] : empty;
        }
    }
}
