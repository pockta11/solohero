using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>Skill icons (D-078) by skill id, plus lock / empty slot art.</summary>
    [CreateAssetMenu(menuName = "SoloHero/Art/Skill Icons")]
    public sealed class SkillIconSet : ScriptableObject
    {
        public string[] ids = new string[0];
        public Sprite[] icons = new Sprite[0];
        public Sprite locked;
        public Sprite empty;

        public Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < ids.Length && i < icons.Length; i++)
            {
                if (ids[i] == id) return icons[i];
            }

            return null;
        }
    }
}
