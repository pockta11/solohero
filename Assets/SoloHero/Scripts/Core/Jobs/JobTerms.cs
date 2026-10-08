using SoloHero.Core.Common;

namespace SoloHero.Core.Jobs
{
    /// <summary>What the weapon slot holds for a job line (D-140); the item ids stay sword_* for every line.</summary>
    public enum WeaponKind
    {
        Sword,
        Staff,
        Bow
    }

    /// <summary>
    /// D-140 job words. One ATK stat inside, named per line in the text: warriors (and the beginner) STR / attack / sword,
    /// mages INT / spell power / staff, archers DEX / attack / bow. The strings use {atk}, {main} and {weapon}
    /// (Strings terms); the game applies the hero's line at boot and on every advancement.
    /// </summary>
    public static class JobTerms
    {
        public static string SetOf(JobLine line)
        {
            switch (line)
            {
                case JobLine.Mage: return "mage";
                case JobLine.Archer: return "archer";
                default: return "";
            }
        }

        public static WeaponKind WeaponOf(JobLine line)
        {
            switch (line)
            {
                case JobLine.Mage: return WeaponKind.Staff;
                case JobLine.Archer: return WeaponKind.Bow;
                default: return WeaponKind.Sword;
            }
        }

        public static void Apply(JobLine line) => Strings.UseTermSet(SetOf(line));
    }
}
