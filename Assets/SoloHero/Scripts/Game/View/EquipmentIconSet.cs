using SoloHero.Core.Gacha;
using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Equipment icons (E8-07): slot x grade, index = slot * 7 + grade (D-109: 8 slots; D-113: 7 grades, 56 icons). The
    /// grade reads from the metal colour (grey / green / blue / purple / gold / crimson / jade) so slot and grade are
    /// clear from the icon alone.
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Equipment Icons")]
    public sealed class EquipmentIconSet : ScriptableObject
    {
        public const int GradeCount = GachaCatalog.GradeCount;

        public Sprite[] icons = new Sprite[GachaCatalog.SlotCount * GradeCount];

        public Sprite Get(EquipmentSlot slot, GearGrade grade)
        {
            int i = (int)slot * GradeCount + (int)grade;
            return i >= 0 && i < icons.Length ? icons[i] : null;
        }
    }
}
