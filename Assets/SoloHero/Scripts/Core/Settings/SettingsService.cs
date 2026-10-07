using System;
using SoloHero.Core.Save;

namespace SoloHero.Core.Settings
{
    /// <summary>
    /// Player settings (E7-09, E8-13, E8-15): BGM / SFX mute, low-effect mode, 30 fps mode and the skill auto/manual
    /// toggle (D-085). The values live in
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

        /// <summary>D-085: skills cast only when the player taps them. Off (auto) by default.</summary>
        public bool SkillManual => _data.skillManualMode;

        public void SetBgmMuted(bool value) => Set(ref _data.bgmMuted, value);

        public void SetSfxMuted(bool value) => Set(ref _data.sfxMuted, value);

        public void SetLowEffect(bool value) => Set(ref _data.lowEffectMode, value);

        public void SetFps30(bool value) => Set(ref _data.fps30Mode, value);

        public void SetSkillManual(bool value) => Set(ref _data.skillManualMode, value);

        /// <summary>D-119: local reminders (offline reward full, a new day). On by default.</summary>
        public bool Notifications => !_data.notificationsOff;

        public void SetNotifications(bool value) => Set(ref _data.notificationsOff, !value);

        private void Set(ref bool field, bool value)
        {
            if (field == value) return;
            field = value;
            Changed?.Invoke();
        }
    }
}
