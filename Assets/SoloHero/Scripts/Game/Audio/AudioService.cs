using SoloHero.Core.Common;
using SoloHero.Core.Settings;
using UnityEngine;

namespace SoloHero.Game.Audio
{
    /// <summary>
    /// BGM + SFX player (E8-13, architecture D15). Lives on the Boot object so music survives the scene change.
    /// The BGM source and the SFX source pool are created by the scene builder (no runtime AddComponent); when every
    /// SFX source is busy the oldest one is reused. Mute follows <see cref="SettingsService"/>. The same effect is
    /// not restarted within <see cref="MinRepeatSeconds"/> so a burst of hits does not stack into noise.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        public const float MinRepeatSeconds = 0.06f;

        [SerializeField] private SoundBank _bank;
        [SerializeField] private AudioSource _bgm;
        [SerializeField] private AudioSource[] _sfx = new AudioSource[0];

        private float[] _lastPlayed = new float[0];
        private int _next;
        private SettingsService _settings;

        public SoundBank Bank => _bank;

        private void Awake()
        {
            int count = _bank != null ? _bank.sfx.Length : 0;
            _lastPlayed = new float[count];
            for (int i = 0; i < count; i++) _lastPlayed[i] = -1f;
            if (_bgm != null)
            {
                _bgm.loop = true;
                _bgm.playOnAwake = false;
            }
        }

        /// <summary>Called by boot after SettingsService is registered.</summary>
        public void Bind(SettingsService settings)
        {
            if (_settings != null) _settings.Changed -= Apply;
            _settings = settings;
            if (_settings != null) _settings.Changed += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= Apply;
        }

        public void Play(SfxId id)
        {
            if (_bank == null || _sfx.Length == 0) return;
            if (_settings != null && _settings.SfxMuted) return;
            AudioClip clip = _bank.Get(id);
            if (clip == null) return;

            int i = (int)id;
            float now = Time.unscaledTime;
            if (i < _lastPlayed.Length)
            {
                if (_lastPlayed[i] >= 0f && now - _lastPlayed[i] < MinRepeatSeconds) return;
                _lastPlayed[i] = now;
            }

            AudioSource source = FreeSource();
            if (source == null) return;
            source.clip = clip;
            source.volume = _bank.sfxVolume;
            source.pitch = PitchFor(id);
            source.Play();
        }

        /// <summary>D-112: hits, crits, kills and hurt sounds vary a little in pitch so a stream of them never drones.</summary>
        private static float PitchFor(SfxId id)
        {
            switch (id)
            {
                case SfxId.Hit:
                case SfxId.Crit:
                case SfxId.EnemyDeath:
                case SfxId.HeroHurt:
                    return Random.Range(0.92f, 1.08f);
                default:
                    return 1f;
            }
        }

        /// <summary>Starts looping music. The same clip keeps playing instead of restarting.</summary>
        public void PlayBgm(AudioClip clip)
        {
            if (_bgm == null || clip == null) return;
            if (_bgm.clip == clip && _bgm.isPlaying) return;
            _bgm.clip = clip;
            _bgm.volume = _bank != null ? _bank.bgmVolume : 0.5f;
            _bgm.mute = _settings != null && _settings.BgmMuted;
            _bgm.Play();
        }

        public void PlayBossBgm()
        {
            if (_bank != null) PlayBgm(_bank.bossBgm);
        }

        private AudioSource FreeSource()
        {
            for (int k = 0; k < _sfx.Length; k++)
            {
                int i = (_next + k) % _sfx.Length;
                if (_sfx[i] != null && !_sfx[i].isPlaying)
                {
                    _next = (i + 1) % _sfx.Length;
                    return _sfx[i];
                }
            }

            AudioSource oldest = _sfx[_next];
            _next = (_next + 1) % _sfx.Length;
            return oldest;
        }

        private void Apply()
        {
            bool bgmMuted = _settings != null && _settings.BgmMuted;
            bool sfxMuted = _settings != null && _settings.SfxMuted;
            if (_bgm != null) _bgm.mute = bgmMuted;
            for (int i = 0; i < _sfx.Length; i++)
            {
                if (_sfx[i] == null) continue;
                _sfx[i].mute = sfxMuted;
                if (sfxMuted) _sfx[i].Stop();
            }

            Log.Info(LogTag.Audio, "audio settings bgmMuted=" + bgmMuted + " sfxMuted=" + sfxMuted);
        }
    }
}
