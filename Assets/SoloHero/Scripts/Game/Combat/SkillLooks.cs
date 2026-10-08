using SoloHero.Core.Gacha;
using SoloHero.Core.Skills;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>The element a skill looks like (D-146): picks its particle colours and how they move. View only.</summary>
    public enum SkillElement
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Holy,
        Dark,
        Arcane,
        Poison,
        Wind,
        Nature
    }

    /// <summary>
    /// D-146 skill looks, derived from the data the skills already have (their clip and tint), so no skill needs a new
    /// field: the element, its two particle colours and motion, and how much bigger and brighter each grade draws.
    /// </summary>
    public static class SkillLooks
    {
        /// <summary>Clip scale per grade (common .. legendary); job ultimates use <see cref="UltimateScale"/>.</summary>
        private static readonly float[] GradeScale = { 1f, 1.08f, 1.18f, 1.3f };

        /// <summary>Glow strength (alpha of the additive halo) per grade.</summary>
        private static readonly float[] GradeGlow = { 0.5f, 0.6f, 0.72f, 0.86f };

        public const float UltimateScale = 1.4f;

        public static float ScaleOf(Grade grade, bool ultimate) => ultimate ? UltimateScale : GradeScale[Index(grade)];

        public static float GlowOf(Grade grade, bool ultimate) => ultimate ? 0.95f : GradeGlow[Index(grade)];

        private static int Index(Grade grade) => Mathf.Clamp((int)grade, 0, GradeScale.Length - 1);

        public static SkillElement ElementOf(SkillDef def)
        {
            if (def == null) return SkillElement.Physical;
            switch (def.Vfx)
            {
                case "fire":
                case "breath":
                case "meteor":
                case "phoenix":
                    return SkillElement.Fire;
                case "ice":
                    return SkillElement.Ice;
                case "bolt":
                    // A violet bolt (magic missile, chain lightning) reads as arcane; the rest stay lightning.
                    return def.Tint != 0 && ByTint(def.Tint, SkillElement.Lightning) == SkillElement.Arcane
                        ? SkillElement.Arcane
                        : SkillElement.Lightning;
                case "spark":
                    return SkillElement.Lightning;
                case "holy":
                case "judge":
                    return def.Tint != 0 ? ByTint(def.Tint, SkillElement.Holy) : SkillElement.Holy;
                case "heal":
                    return SkillElement.Nature;
                case "vortex":
                    return SkillElement.Dark;
                case "clock":
                    return SkillElement.Arcane;
                case "poison":
                    return SkillElement.Poison;
                case "tornado":
                    return SkillElement.Wind;
                case "spear":
                    return def.Tint == 0 ? SkillElement.Ice : ByTint(def.Tint, SkillElement.Physical);
                default:
                    return def.Tint != 0 ? ByTint(def.Tint, SkillElement.Physical) : SkillElement.Physical;
            }
        }

        /// <summary>The element a tint reads as: greys are physical, then by hue.</summary>
        private static SkillElement ByTint(uint rgb, SkillElement grey)
        {
            Color c = Tint(rgb);
            Color.RGBToHSV(c, out float h, out float s, out _);
            if (s < 0.22f) return grey;
            float deg = h * 360f;
            if (deg < 38f || deg >= 340f) return SkillElement.Fire;
            if (deg < 70f) return SkillElement.Holy;
            if (deg < 160f) return SkillElement.Nature;
            if (deg < 235f) return SkillElement.Ice;
            return SkillElement.Arcane;
        }

        /// <summary>Bright and deep particle colours of an element.</summary>
        public static void Colors(SkillElement element, out Color bright, out Color deep)
        {
            switch (element)
            {
                case SkillElement.Fire: bright = new Color(1f, 0.86f, 0.32f); deep = new Color(1f, 0.38f, 0.1f); break;
                case SkillElement.Ice: bright = new Color(0.86f, 0.97f, 1f); deep = new Color(0.38f, 0.74f, 1f); break;
                case SkillElement.Lightning: bright = new Color(1f, 1f, 0.78f); deep = new Color(0.55f, 0.82f, 1f); break;
                case SkillElement.Holy: bright = new Color(1f, 0.97f, 0.72f); deep = new Color(1f, 0.78f, 0.28f); break;
                case SkillElement.Dark: bright = new Color(0.82f, 0.52f, 1f); deep = new Color(0.42f, 0.18f, 0.82f); break;
                case SkillElement.Arcane: bright = new Color(0.9f, 0.76f, 1f); deep = new Color(0.52f, 0.58f, 1f); break;
                case SkillElement.Poison: bright = new Color(0.78f, 1f, 0.45f); deep = new Color(0.32f, 0.8f, 0.24f); break;
                case SkillElement.Wind: bright = new Color(0.9f, 1f, 0.96f); deep = new Color(0.5f, 0.9f, 0.84f); break;
                case SkillElement.Nature: bright = new Color(0.78f, 1f, 0.62f); deep = new Color(0.38f, 0.9f, 0.46f); break;
                default: bright = new Color(1f, 0.96f, 0.82f); deep = new Color(1f, 0.72f, 0.36f); break;
            }
        }

        /// <summary>How an element's particles move and how fast.</summary>
        public static ParticleMotion MotionOf(SkillElement element, out float speed)
        {
            switch (element)
            {
                case SkillElement.Fire: speed = 2.4f; return ParticleMotion.Rise;
                case SkillElement.Ice: speed = 3.2f; return ParticleMotion.Spray;
                case SkillElement.Lightning: speed = 5.2f; return ParticleMotion.Spray;
                case SkillElement.Holy: speed = 1.8f; return ParticleMotion.Rise;
                case SkillElement.Dark: speed = 1.3f; return ParticleMotion.Rise;
                case SkillElement.Arcane: speed = 1.7f; return ParticleMotion.Rise;
                case SkillElement.Poison: speed = 1.1f; return ParticleMotion.Rise;
                case SkillElement.Wind: speed = 3.8f; return ParticleMotion.Spray;
                case SkillElement.Nature: speed = 1.5f; return ParticleMotion.Rise;
                default: speed = 4.2f; return ParticleMotion.Spray;
            }
        }

        /// <summary>The colour a skill's effects are drawn in: its tint, else its element's bright colour.</summary>
        public static Color EffectColor(SkillDef def, SkillElement element)
        {
            if (def != null && def.Tint != 0) return Tint(def.Tint);
            Colors(element, out Color bright, out _);
            return bright;
        }

        public static Color Tint(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
