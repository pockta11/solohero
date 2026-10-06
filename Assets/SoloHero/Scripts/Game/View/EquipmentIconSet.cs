using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Equipment icons (E8-07): slot x grade, index = slot * 4 + grade (D-109: 8 slots, 32 icons). The grade reads from
    /// the metal colour (grey / blue / purple / gold) so slot and grade are clear from the icon alone.
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Equipment Icons")]
    public sealed class EquipmentIconSet : ScriptableObject
    {
        public const int GradeCount = 4;

        public Sprite[] icons = new Sprite[32];

        public Sprite Get(EquipmentSlot slot, Grade grade)
        {
            int i = (int)slot * GradeCount + (int)grade;
            return i >= 0 && i < icons.Length ? icons[i] : null;
        }
    }
}
