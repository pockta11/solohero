using UnityEngine;

namespace SoloHero.Game.Audio
{
    /// <summary>Clips for every <see cref="SfxId"/> (by index) and the boss BGM. Chapter BGM lives on ChapterTheme.</summary>
    [CreateAssetMenu(menuName = "SoloHero/Audio/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        public AudioClip[] sfx = new AudioClip[0];
        public AudioClip bossBgm;

        [Range(0f, 1f)] public float bgmVolume = 0.55f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        public AudioClip Get(SfxId id)
        {
            int i = (int)id;
            return i >= 0 && i < sfx.Length ? sfx[i] : null;
        }
    }
}
