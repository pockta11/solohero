using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Equipment icons (E8-07): slot x grade, index = slot * 7 + grade (D-109: 8 slots; D-113: 7 grades, 56 icons). The
    /// grade reads from the metal colour (grey / green / blue / purple / gold / crimson / jade) so slot and grade are
    /// clear from the icon alone. D-140: the weapon slot shows a staff for mages and a bow for archers (7 each).
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Equipment Icons")]
    public sealed class EquipmentIconSet : ScriptableObject
    {
        public const int GradeCount = GachaCatalog.GradeCount;

        public Sprite[] icons = new Sprite[GachaCatalog.SlotCount * GradeCount];
        public Sprite[] staffIcons = new Sprite[GradeCount];
        public Sprite[] bowIcons = new Sprite[GradeCount];

        public Sprite Get(EquipmentSlot slot, GearGrade grade)
        {
            int i = (int)slot * GradeCount + (int)grade;
            return i >= 0 && i < icons.Length ? icons[i] : null;
        }

        /// <summary>D-140: as <see cref="Get(EquipmentSlot, GearGrade)"/>, with the weapon drawn for the job line.</summary>
        public Sprite Get(EquipmentSlot slot, GearGrade grade, WeaponKind weapon)
        {
            if (slot == EquipmentSlot.Sword && weapon != WeaponKind.Sword)
            {
                Sprite[] set = weapon == WeaponKind.Staff ? staffIcons : bowIcons;
                int g = (int)grade;
                if (set != null && g >= 0 && g < set.Length && set[g] != null) return set[g];
            }

            return Get(slot, grade);
        }
    }
}
