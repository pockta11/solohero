using System;
using SoloHero.Core.Save;

namespace SoloHero.Core.Settings
{
    /// <summary>
    /// Player settings (E7-09, E8-13, E8-15): BGM / SFX mute, low-effect mode and 30 fps mode. The values live in
    /// <see cref="SaveDataV2"/> (architecture: no separate store) and are written with the next pause / quit save;
    /// a settings change is not one of the seven save triggers.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly SaveDataV2 _data;

        public SettingsService(SaveDataV2 data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>Raised after any setting changes. Audio, VFX and frame rate listeners re-apply from it.</summary>
        public event Action Changed;

        public bool BgmMuted => _data.bgmMuted;

        public bool SfxMuted => _data.sfxMuted;

        /// <summary>GDD: low-effect mode turns off particles and camera shake; damage numbers stay.</summary>
        public bool LowEffect => _data.lowEffectMode;

        public bool Fps30 => _data.fps30Mode;

        public int TargetFrameRate => _data.fps30Mode ? 30 : 60;

        public void SetBgmMuted(bool value) => Set(ref _data.bgmMuted, value);

        public void SetSfxMuted(bool value) => Set(ref _data.sfxMuted, value);

        public void SetLowEffect(bool value) => Set(ref _data.lowEffectMode, value);

        public void SetFps30(bool value) => Set(ref _data.fps30Mode, value);

        private void Set(ref bool field, bool value)
        {
            if (field == value) return;
            field = value;
            Changed?.Invoke();
        }
    }
}
