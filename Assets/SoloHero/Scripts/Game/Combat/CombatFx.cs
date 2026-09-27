using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.Pooling;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Combat juice (E8-08, E8-11, E8-13): listens to Core and the combat view and plays pooled VFX, SFX, camera
    /// shake and music. It never changes combat. Low-effect mode (E8-15) drops hit sparks, death puffs and shake;
    /// damage numbers, hit flashes, skill and level-up effects stay because they carry information.
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        private const float EffectY = 0.7f;
        private const float CritShake = 0.06f;
        private const float CritShakeSeconds = 0.12f;
        private const float SkillShake = 0.1f;
        private const float BossShake = 0.18f;
        private static readonly Color CritTint = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color Skill1Tint = new Color(1f, 0.9f, 0.7f, 1f);
        private static readonly Color Skill2Tint = new Color(0.75f, 0.9f, 1f, 1f);
        private static readonly Color Skill3Tint = new Color(1f, 0.55f, 0.3f, 1f);
        private static readonly Color LevelTint = new Color(0.55f, 1f, 0.6f, 1f);

        [SerializeField] private CombatSession _session;
        [SerializeField] private CombatWorldView _view;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private VfxSet _set;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private ChapterThemeSet _themes;
        [SerializeField] private ToastQueue _toast;

        private AudioService _audio;
        private SettingsService _settings;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private StageRunner _hooked;
        private int _shownLevel = -1;
        private int _musicChapter = -1;
        private bool _bossMusic;

        private bool LowEffect => _settings != null && _settings.LowEffect;

        private void Awake()
        {
            _audio = Get<AudioService>();
            _settings = Get<SettingsService>();
            _save = Get<SaveDataV2>();
            _balance = Get<BalanceValues>() ?? new BalanceValues();
        }

        private void OnEnable()
        {
            if (_view == null) return;
            _view.EnemyDied += OnEnemyDied;
            _view.HeroHurt += OnHeroHurt;
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.EnemyDied -= OnEnemyDied;
                _view.HeroHurt -= OnHeroHurt;
            }

            Unhook();
        }

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner != _hooked)
            {
                Unhook();
                Hook(runner);
            }

            if (runner == null) return;
            UpdateMusic(runner);
            CheckLevelUp(runner);
        }

        private void Hook(StageRunner runner)
        {
            _hooked = runner;
            if (runner == null) return;
            runner.World.HitLanded += OnHitLanded;
            runner.Skills.SkillCast += OnSkillCast;
            runner.StateChanged += OnStateChanged;
            runner.StageCleared += OnStageCleared;
            _shownLevel = _save != null ? _save.heroLevel : -1;
        }

        private void Unhook()
        {
            if (_hooked == null) return;
            _hooked.World.HitLanded -= OnHitLanded;
            _hooked.Skills.SkillCast -= OnSkillCast;
            _hooked.StateChanged -= OnStateChanged;
            _hooked.StageCleared -= OnStageCleared;
            _hooked = null;
        }

        private void OnHitLanded(EnemyBrain target, double amount, bool crit)
        {
            Play(crit ? SfxId.Crit : SfxId.Hit);
            if (LowEffect) return;
            PlayVfx(_set != null ? _set.slash : null, (float)target.X, crit ? 2.5f : 1.5f, crit ? CritTint : Color.white);
            if (crit) Shake(CritShake, CritShakeSeconds);
        }

        private void OnSkillCast(SkillSlot slot)
        {
            if (_hooked == null || _set == null) return;
            float heroX = (float)_hooked.Hero.X;
            switch (slot)
            {
                case SkillSlot.Slot1:
                    Play(SfxId.Skill1);
                    PlayVfx(_set.boom, heroX + (float)_balance.ATTACK_RANGE, 1f, Skill1Tint);
                    Shake(SkillShake, 0.15f);
                    break;
                case SkillSlot.Slot2:
                    Play(SfxId.Skill2);
                    PlayVfx(_set.whirl, heroX + (float)_balance.WHIRLWIND_RANGE * 0.5f, 2f, Skill2Tint);
                    break;
                case SkillSlot.Slot3:
                    Play(SfxId.Skill3);
                    PlayVfx(_set.ring, heroX, 2f, Skill3Tint, 0.4f);
                    break;
            }
        }

        private void OnEnemyDied(Vector3 position, bool boss, bool hasDeathClip)
        {
            Play(SfxId.EnemyDeath);
            if (LowEffect || _set == null) return;
            // Looks without a death clip vanish into a puff; the others get a small puff over their own clip.
            float scale = boss ? 2f : hasDeathClip ? 0.6f : 1f;
            PlayVfx(_set.boom, position.x, scale, Color.white, 0.2f);
            if (boss) Shake(BossShake, 0.4f);
        }

        private void OnHeroHurt() => Play(SfxId.HeroHurt);

        private void OnStageCleared(int g) => Play(SfxId.Gold);

        private void OnStateChanged(StageState state)
        {
            if (state != StageState.BossIntro) return;
            Play(SfxId.BossIntro);
            Shake(BossShake, _balance.BOSS_INTRO_TIME * 0.5f);
        }

        private void UpdateMusic(StageRunner runner)
        {
            if (_audio == null) return;
            bool boss = runner.IsBoss && (runner.State == StageState.BossIntro || runner.State == StageState.BossTimer);
            StageIndex.FromGlobal(runner.GlobalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            if (boss == _bossMusic && chapter == _musicChapter) return;
            _bossMusic = boss;
            _musicChapter = chapter;
            if (boss)
            {
                _audio.PlayBossBgm();
                return;
            }

            if (_themes == null || _themes.themes.Length == 0) return;
            ChapterTheme theme = _themes.themes[StageIndex.ThemeIndex(chapter, _themes.themes.Length)];
            if (theme != null) _audio.PlayBgm(theme.bgm);
        }

        private void CheckLevelUp(StageRunner runner)
        {
            if (_save == null) return;
            if (_shownLevel < 0) _shownLevel = _save.heroLevel;
            if (_save.heroLevel <= _shownLevel) return;
            _shownLevel = _save.heroLevel;
            Play(SfxId.LevelUp);
            if (_set != null) PlayVfx(_set.ring, (float)runner.Hero.X, 3f, LevelTint, 0.4f);
            if (_toast != null) _toast.Show(Strings.Format("toast.level_up", _save.heroLevel));
        }

        private void PlayVfx(Sprite[] clip, float x, float scale, Color tint, float y = EffectY)
        {
            if (_vfx == null || clip == null) return;
            _vfx.Play(clip, _set != null ? _set.fps : 18f, new Vector3(x, y, 0f), scale, tint);
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_shake == null || LowEffect) return;
            _shake.Shake(amplitude, seconds);
        }

        private void Play(SfxId id)
        {
            if (_audio != null) _audio.Play(id);
        }

        private static T Get<T>() where T : class
        {
            try
            {
                return Services.Get<T>();
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }
    }
}
