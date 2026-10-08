using System;
using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>One named effect clip (D-078 skill effects). Ground clips stand on the ground line, others sit at body height.</summary>
    [Serializable]
    public sealed class VfxClip
    {
        public string name = "";
        public Sprite[] frames = new Sprite[0];
        public float fps = 18f;

        /// <summary>True for clips that fall from the sky or rise from the floor: their bottom touches the ground.</summary>
        public bool ground;

        /// <summary>Half the frame height in world units at scale 1 (ground clips are raised by this x scale).</summary>
        public float halfHeight = 0.5f;

        /// <summary>
        /// D-146: the clip's halo (Art/Vfx/vfx{name}glow, tools/art/vfxgen3.py), drawn additively under it at the same
        /// centre; its frames are padded, not shifted. Empty when the clip has none.
        /// </summary>
        public Sprite[] glow = new Sprite[0];
    }

    /// <summary>
    /// Sprite clips for combat VFX (E8-08, D-078). The five white clips are tinted per use (crit gold, heal green, ...);
    /// <see cref="clips"/> holds the coloured skill clips looked up by the SkillDef.Vfx name. D-146: clips carry their
    /// glow, the basic five are in <see cref="clips"/> too, and the cast layers (circle1..4, pillar, flash, shock) are
    /// found by name like any clip.
    /// </summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Vfx Set")]
    public sealed class VfxSet : ScriptableObject
    {
        public Sprite[] slash = new Sprite[0];
        public Sprite[] whirl = new Sprite[0];
        public Sprite[] ring = new Sprite[0];
        public Sprite[] boom = new Sprite[0];
        public Sprite[] spark = new Sprite[0];
        public float fps = 18f;
        public VfxClip[] clips = new VfxClip[0];

        /// <summary>A named skill clip, or null. The basic clips answer to their field names too.</summary>
        public VfxClip Find(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name == clipName) return clips[i];
            }

            return null;
        }

        /// <summary>Frames of a basic clip by field name ("slash", "whirl", "ring", "boom", "spark").</summary>
        public Sprite[] Basic(string clipName)
        {
            switch (clipName)
            {
                case "slash": return slash;
                case "whirl": return whirl;
                case "ring": return ring;
                case "boom": return boom;
                case "spark": return spark;
                default: return null;
            }
        }
    }
}
