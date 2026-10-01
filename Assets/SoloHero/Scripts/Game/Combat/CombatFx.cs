using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
using SoloHero.Core.Skills;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.Pooling;
using SoloHero.Game.UI.Common;
using SoloHero.Game.UI.Panels;
using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Combat juice (E8-08, E8-11, E8-13): listens to Core and the combat view and plays pooled VFX, SFX, camera
    /// shake and music. It never changes combat. Low-effect mode (E8-15) drops hit sparks, death puffs, shake and
    /// the skill screen flash; damage numbers, hit flashes, skill and level-up effects stay because they carry
    /// information. Skills (D-078): a grade-coloured cast ring and name over the hero, the skill's own clip where its
    /// VfxAt says, a sound per family, shake growing with grade and a screen flash for Epic / Legendary.
    /// D-096: hit-stop on crits, kills and Epic+ skills, a bigger sword wave on the basic skill.
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        private const float EffectY = 0.7f;
        private const string BasicWaveClip = "wave";
        private const float BasicWaveOffset = 1.1f;
        private const float BasicWaveScale = 1.35f;
        private const float CritStop = 0.045f;
        private const float KillStop = 0.06f;
        private const float BossKillStop = 0.18f;
        private const float BigSkillStop = 0.08f;
        private const float CritShake = 0.06f;
        private const float CritShakeSeconds = 0.12f;
        private const float BossShake = 0.18f;
        private const float SkillNameY = 1.9f;
        private const int SkillNameSize = 44;
        private const float EpicFlashAlpha = 0.14f;
        private const float LegendaryFlashAlpha = 0.24f;
        private const float FlashSeconds = 0.35f;
        private static readonly float[] GradeShake = { 0.04f, 0.07f, 0.11f, 0.17f };
        private static readonly Color CritTint = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color LevelTint = new Color(0.55f, 1f, 0.6f, 1f);
        private static readonly Color ComboTint = new Color(0.86f, 0.55f, 1f, 1f);

        [SerializeField] private CombatSession _session;
        [SerializeField] private CombatWorldView _view;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private VfxSet _set;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private ChapterThemeSet _themes;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private ScreenFlash _flash;
        [SerializeField] private DamageTextPool _labels;

        private AudioService _audio;
        private SettingsService _settings;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private StageRunner _hooked;
        private int _shownLevel = -1;
        private double _shownGem = -1d;
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
            if (_save != null) _shownGem = _save.gem;
        }

        private void Hook(StageRunner runner)
        {
            _hooked = runner;
            if (runner == null) return;
            runner.World.HitLanded += OnHitLanded;
            runner.Hero.AttackRequested += OnBasicSwing;
            runner.Skills.SkillCast += OnSkillCast;
            runner.Skills.SkillImpact += OnSkillImpact;
            runner.StateChanged += OnStateChanged;
            runner.StageCleared += OnStageCleared;
            _shownLevel = _save != null ? _save.heroLevel : -1;
        }

        private void Unhook()
        {
            if (_hooked == null) return;
            _hooked.World.HitLanded -= OnHitLanded;
            _hooked.Hero.AttackRequested -= OnBasicSwing;
            _hooked.Skills.SkillCast -= OnSkillCast;
            _hooked.Skills.SkillImpact -= OnSkillImpact;
            _hooked.StateChanged -= OnStateChanged;
            _hooked.StageCleared -= OnStageCleared;
            _hooked = null;
        }

        /// <summary>D-093 basic skill: a sword wave sweeps forward from the hero on every swing.</summary>
        private void OnBasicSwing()
        {
            if (_hooked == null || LowEffect || _set == null || _vfx == null) return;
            VfxClip wave = _set.Find(BasicWaveClip);
            if (wave == null) return;
            _vfx.Play(wave.frames, wave.fps, new Vector3((float)_hooked.Hero.X + BasicWaveOffset, EffectY, 0f), BasicWaveScale, Color.white);
        }

        private void OnHitLanded(EnemyBrain target, double amount, HitKind kind)
        {
            // Skill hits bring their own clip and sound; burn ticks stay quiet.
            if (kind == HitKind.Combo)
            {
                Play(SfxId.Crit);
                HitStop.Trigger(CritStop);
                if (!LowEffect) PlayVfx(_set != null ? _set.spark : null, (float)target.X, 3f, ComboTint, EffectY + DepthLanes.For(target));
                return;
            }

            if (kind == HitKind.Dot || kind == HitKind.Skill)
            {
                if (kind == HitKind.Skill && !LowEffect) PlayVfx(_set != null ? _set.spark : null, (float)target.X, 2f, Color.white, EffectY + DepthLanes.For(target));
                return;
            }

            bool crit = kind == HitKind.Crit;
            Play(crit ? SfxId.Crit : SfxId.Hit);
            if (crit) HitStop.Trigger(CritStop);
            if (LowEffect) return;
            PlayVfx(_set != null ? _set.slash : null, (float)target.X, crit ? 2.5f : 1.5f, crit ? CritTint : Color.white, EffectY + DepthLanes.For(target));
            if (crit) Shake(CritShake, CritShakeSeconds);
        }

        private void OnSkillCast(int slot, SkillDef def)
        {
            if (_hooked == null || def == null) return;
            float heroX = (float)_hooked.Hero.X;
            Color gradeColor = PanelServices.GradeColor(def.Grade);
            Play(SoundOf(def));
            if (_set != null) PlayVfx(_set.ring, heroX, 1.2f + (int)def.Grade * 0.4f, gradeColor, 0.5f);
            if (_labels != null)
                _labels.ShowLabel(new Vector3(heroX, SkillNameY, 0f), Strings.Get(def.NameKey), Color.Lerp(gradeColor, Color.white, 0.35f), SkillNameSize);

            if (def.VfxAt == SkillVfxAt.Hero) PlaySkillClip(def, heroX);
            else if (def.VfxAt == SkillVfxAt.Front) PlaySkillClip(def, heroX + (float)def.Range * 0.5f);

            int grade = (int)def.Grade;
            if (def.DealsDamage) Shake(GradeShake[grade], 0.12f + grade * 0.08f);
            if (def.DealsDamage && def.Grade >= Grade.Epic) HitStop.Trigger(BigSkillStop);
            if (_flash == null || LowEffect || def.Grade < Grade.Epic) return;
            Color flash = gradeColor;
            flash.a = def.Grade == Grade.Legendary ? LegendaryFlashAlpha : EpicFlashAlpha;
            _flash.Flash(flash, FlashSeconds);
        }

        private void OnSkillImpact(SkillDef def, double x)
        {
            if (def == null) return;
            PlaySkillClip(def, (float)x);
        }

        /// <summary>The skill's named clip (coloured art) or a tinted basic clip; ground clips stand on the floor.</summary>
        private void PlaySkillClip(SkillDef def, float x)
        {
            if (_vfx == null || _set == null) return;
            Color tint = Tint(def.Tint);
            VfxClip clip = _set.Find(def.Vfx);
            if (clip != null)
            {
                float y = clip.ground ? clip.halfHeight * def.VfxScale : EffectY;
                _vfx.Play(clip.frames, clip.fps, new Vector3(x, y, 0f), def.VfxScale, tint);
                return;
            }

            PlayVfx(_set.Basic(def.Vfx), x, def.VfxScale, tint);
        }

        private static Color Tint(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private static SfxId SoundOf(SkillDef def)
        {
            switch (def.Sound)
            {
                case "whoosh": return SfxId.Skill2;
                case "cry": return SfxId.Skill3;
                case "fire": return SfxId.SkillFire;
                case "thunder": return SfxId.SkillThunder;
                case "ice": return SfxId.SkillIce;
                case "heal": return SfxId.SkillHeal;
                case "magic": return SfxId.SkillMagic;
                default: return SfxId.Skill1;
            }
        }

        private void OnEnemyDied(Vector3 position, bool boss, bool hasDeathClip)
        {
            Play(SfxId.EnemyDeath);
            HitStop.Trigger(boss ? BossKillStop : KillStop);
            if (LowEffect || _set == null) return;
            // Looks without a death clip vanish into a puff; the others get a small puff over their own clip.
            float scale = boss ? 2f : hasDeathClip ? 0.6f : 1f;
            PlayVfx(_set.boom, position.x, scale, Color.white, 0.2f + position.y);
            if (boss) Shake(BossShake, 0.4f);
        }

        private void OnHeroHurt() => Play(SfxId.HeroHurt);

        private void OnStageCleared(int g)
        {
            Play(SfxId.Gold);
            if (_hooked == null || !_hooked.IsBoss || _save == null || _shownGem < 0d) return;
            // A chapter's first boss kill pays CHAPTER_CLEAR_GEM (StageRunner applies it before this event).
            double gained = _save.gem - _shownGem;
            if (gained <= 0d) return;
            StageIndex.FromGlobal(g, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            Play(SfxId.GradeEpic);
            if (_toast != null) _toast.Show(Strings.Format("toast.chapter_clear", chapter, gained));
        }

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
