using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Item frames per grade plus the empty / locked slot frame. D-108: smooth slots (Art/UI/Hd/hd9_slot*), with a
    /// separate dark empty frame for slots that sit on the battle field instead of a cream panel.
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Grade Frames")]
    public sealed class GradeFrameSet : ScriptableObject
    {
        /// <summary>Indexed by <see cref="GearGrade"/> (D-113: Common .. Ancient, seven frames).</summary>
        public Sprite[] frames = new Sprite[GearGrades.Count];
        public Sprite empty;
        public Sprite emptyDark;

        public Sprite Get(GearGrade grade)
        {
            int i = (int)grade;
            return i >= 0 && i < frames.Length && frames[i] != null ? frames[i] : empty;
        }

        /// <summary>A skill or companion grade, framed like the gear grade of the same name.</summary>
        public Sprite Get(Grade grade) => Get(grade.ToGearGrade());

        /// <summary>Empty frame for the battle HUD (falls back to the panel one).</summary>
        public Sprite EmptyDark => emptyDark != null ? emptyDark : empty;
    }
}
