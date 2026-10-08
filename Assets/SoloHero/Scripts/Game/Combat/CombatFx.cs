using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;
using SoloHero.Core.Settings;
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
    /// information. D-096: hit-stop on crits and kills, a bigger sword wave on the basic skill.
    /// D-112: pixel shards burst from every hit (more and gold on crits) and a coloured spray from every kill.
    /// D-146: skills (casts, impacts, skill hits and their crits) moved to SkillFx; the job's main attack draws over a
    /// faint additive glow.
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
        private const float CritShake = 0.06f;
        private const float CritShakeSeconds = 0.12f;
        private const float BossShake = 0.18f;
        private const float MainGlowAlpha = 0.38f;
        private static readonly Color CritTint = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color LevelTint = new Color(0.55f, 1f, 0.6f, 1f);
        private static readonly Color ComboTint = new Color(0.86f, 0.55f, 1f, 1f);
        private const float ComboFxGap = 0.2f;
        // Warm, saturated shards read on both the bright meadow and the dark dusk chapters (white vanished on snow).
        private const float ShardSize = 0.14f;
        private static readonly Color HitShard = new Color(1f, 0.88f, 0.4f, 1f);
        private static readonly Color CritShard = new Color(1f, 0.55f, 0.12f, 1f);
        private static readonly Color KillShardA = new Color(1f, 0.62f, 0.2f, 1f);
        private static readonly Color KillShardB = new Color(1f, 0.95f, 0.55f, 1f);
        private float _lastComboFx = -1f;

        [SerializeField] private CombatSession _session;
        [SerializeField] private CombatWorldView _view;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private VfxSet _set;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private ChapterThemeSet _themes;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private HitParticles _particles;

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
            runner.StateChanged += OnStateChanged;
            runner.StageCleared += OnStageCleared;
            _shownLevel = _save != null ? _save.heroLevel : -1;
        }

        private void Unhook()
        {
            if (_hooked == null) return;
            _hooked.World.HitLanded -= OnHitLanded;
            _hooked.Hero.AttackRequested -= OnBasicSwing;
            _hooked.StateChanged -= OnStateChanged;
            _hooked.StageCleared -= OnStageCleared;
            _hooked = null;
        }

        /// <summary>
        /// D-093 basic skill: a sword wave sweeps forward from the hero on every swing. D-104: a job's main attack
        /// plays its own clip and tint instead (bolt, fire, ice, arrow), ranged ones further in front.
        /// </summary>
        private void OnBasicSwing()
        {
            if (_hooked == null || LowEffect || _set == null || _vfx == null) return;
            MainAttack main = _save != null ? JobCatalog.Find(_save.jobId).Main : null;
            VfxClip clip = _set.Find(main != null ? main.Vfx : BasicWaveClip) ?? _set.Find(BasicWaveClip);
            if (clip == null) return;
            float scale = main != null ? main.VfxScale : BasicWaveScale;
            float x = (float)_hooked.Hero.X + (main != null && main.Range > 3d ? (float)main.Range * 0.4f : BasicWaveOffset);
            float y = clip.ground ? clip.halfHeight * scale : EffectY;
            Color tint = main != null && main.Tint != 0 ? Tint(main.Tint) : Color.white;
            if (clip.glow != null && clip.glow.Length > 0)
            {
                Color glow = tint;
                glow.a = MainGlowAlpha;
                _vfx.Play(clip.glow, clip.fps, new Vector3(x, y, 0f), scale, glow, VfxPool.SortingOrder - 1, true);
            }

            _vfx.Play(clip.frames, clip.fps, new Vector3(x, y, 0f), scale, tint);
        }

        private void OnHitLanded(EnemyBrain target, double amount, HitKind kind)
        {
            // Skill hits bring their own clip and sound; burn ticks stay quiet.
            // D-102: PetView plays the pet's own effect; only the hit sound here.
            if (kind == HitKind.Pet)
            {
                Play(SfxId.Hit);
                return;
            }

            if (kind == HitKind.Combo)
            {
                // D-103: one combo burst (sound, stop, sparkle) per moment, not one per wave of a multi-hit skill.
                if (Time.time - _lastComboFx < ComboFxGap) return;
                _lastComboFx = Time.time;
                Play(SfxId.Crit);
                HitStop.Trigger(CritStop);
                // D-108: the spark sheet is 40 px now (was 16), so the scale drops to keep it a punchy accent.
                if (!LowEffect) PlayVfx(_set != null ? _set.spark : null, (float)target.X, 0.9f, ComboTint, EffectY + DepthLanes.For(target));
                return;
            }

            // D-146: skill hits and skill crits are SkillFx's (element sparks); burn ticks stay quiet.
            if (kind == HitKind.Dot || kind == HitKind.Skill || kind == HitKind.SkillCrit) return;

            bool crit = kind == HitKind.Crit;
            Play(crit ? SfxId.Crit : SfxId.Hit);
            if (crit) HitStop.Trigger(CritStop);
            if (LowEffect) return;
            Shards(target, crit ? 8 : 4, crit ? CritShard : HitShard, crit ? 4.8f : 3.4f);
            // D-108: the slash sheet is 56 px (was 32); 1.0 / 1.6 keeps the old reach with a thicker swing.
            PlayVfx(_set != null ? _set.slash : null, (float)target.X, crit ? 1.6f : 1.0f, crit ? CritTint : Color.white, EffectY + DepthLanes.For(target));
            if (crit) Shake(CritShake, CritShakeSeconds);
        }

        private static Color Tint(uint rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private void OnEnemyDied(Vector3 position, bool boss, bool hasDeathClip)
        {
            Play(SfxId.EnemyDeath);
            HitStop.Trigger(boss ? BossKillStop : KillStop);
            if (LowEffect || _set == null) return;
            // Looks without a death clip vanish into a puff; the others get a small puff over their own clip.
            float scale = boss ? 2f : hasDeathClip ? 0.6f : 1f;
            PlayVfx(_set.boom, position.x, scale, Color.white, 0.2f + position.y);
            if (_particles != null)
            {
                Vector3 at = new Vector3(position.x, position.y + 0.5f, 0f);
                _particles.Burst(at, boss ? 18 : 7, KillShardA, boss ? 6f : 4.4f, ShardSize, 80f);
                _particles.Burst(at, boss ? 10 : 4, KillShardB, boss ? 5f : 3.6f, ShardSize * 0.8f, 80f);
            }

            if (boss) Shake(BossShake, 0.4f);
        }

        /// <summary>D-112: a few pixel shards thrown up from a struck enemy (none in low-effect mode).</summary>
        private void Shards(EnemyBrain target, int count, Color color, float speed)
        {
            if (_particles == null || LowEffect || target == null) return;
            _particles.Burst(new Vector3((float)target.X, EffectY + DepthLanes.For(target), 0f), count, color, speed, ShardSize);
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
            // D-100: a dungeon run plays the boss track too - it is the short, intense part of the day.
            bool boss = runner.InDungeon || (runner.IsBoss && (runner.State == StageState.BossIntro || runner.State == StageState.BossTimer));
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
            if (_set != null) PlayVfx(_set.ring, (float)runner.Hero.X, 2.2f, LevelTint, 0.4f);
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
