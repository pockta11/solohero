using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
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
    /// D-146 flashy skills, view only (it never changes combat). A cast lays a grade circle under the hero (common
    /// silver ring .. legendary gold star circle), lifts motes in the grade colour and flashes the hero; Epic and up add
    /// a light pillar, a legendary skill its name ribbon and a job ultimate the cut-in band. Every skill clip draws over
    /// its additive glow and grows with the grade. Area impacts throw a ground shockwave, Epic+ impacts a starburst, and
    /// impacts and skill hits spray particles in the skill's element (embers rise, ice shards and sparks fly, motes
    /// drift). Shake, hit-stop and the screen flash grow with the grade. Low-effect mode keeps only the skill's clip,
    /// its name and the sound. Replaces the D-078 skill part of CombatFx.
    /// </summary>
    public sealed class SkillFx : MonoBehaviour
    {
        private const float EffectY = 0.7f;
        private const float SkillNameY = 1.9f;
        private const int SkillNameSize = 44;
        private const int CircleOrder = 3;

        /// <summary>The light pillar rises behind the characters (above the circle), so the hero stays readable in it.</summary>
        private const int PillarOrder = 6;
        private const int GlowOrder = VfxPool.SortingOrder - 1;
        private const int ClipOrder = VfxPool.SortingOrder;
        private const int LightOrder = VfxPool.SortingOrder + 2;
        private const float BigSkillStop = 0.08f;
        private const float UltimateStop = 0.12f;
        private const float EpicFlashAlpha = 0.1f;
        private const float LegendaryFlashAlpha = 0.16f;
        private const float UltimateFlashAlpha = 0.24f;
        private const float FlashSeconds = 0.38f;
        private const float CritSoundGap = 0.2f;

        /// <summary>Impacts of one wave that carry a glow and particles (every glow is one more draw call).</summary>
        private const int ImpactBurstsPerWave = 4;

        /// <summary>Normal skill hits per frame whose spark carries a glow; crits always do.</summary>
        private const int HitGlowsPerFrame = 2;

        /// <summary>The shockwave sheet draws its ring 10 px below the frame centre (dust rises above it): lift by that.</summary>
        private const float ShockRingOffset = 10f / 32f;
        private static readonly float[] GradeShake = { 0.04f, 0.07f, 0.11f, 0.17f };
        private static readonly Color CritTint = new Color(1f, 0.85f, 0.3f, 1f);

        [SerializeField] private CombatSession _session;
        [SerializeField] private CombatWorldView _view;
        [SerializeField] private VfxPool _vfx;
        [SerializeField] private VfxSet _set;
        [SerializeField] private SkillParticles _particles;
        [SerializeField] private CameraShake _shake;
        [SerializeField] private ScreenFlash _flash;
        [SerializeField] private DamageTextPool _labels;
        [SerializeField] private SkillCutIn _cutIn;

        private AudioService _audio;
        private SettingsService _settings;
        private SaveDataV2 _save;
        private StageRunner _hooked;
        private int _waveFrame = -1;
        private SkillDef _waveDef;
        private int _waveImpacts;
        private int _hitFrame = -1;
        private int _hitGlows;
        private float _lastCritSound = -1f;

        private bool LowEffect => _settings != null && _settings.LowEffect;

        private void Awake()
        {
            _audio = Get<AudioService>();
            _settings = Get<SettingsService>();
            _save = Get<SaveDataV2>();
        }

        private void OnDisable() => Unhook();

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner == _hooked) return;
            Unhook();
            _hooked = runner;
            if (runner == null) return;
            runner.Skills.SkillCast += OnSkillCast;
            runner.Skills.SkillImpact += OnSkillImpact;
            runner.World.HitLanded += OnHitLanded;
        }

        private void Unhook()
        {
            if (_hooked == null) return;
            _hooked.Skills.SkillCast -= OnSkillCast;
            _hooked.Skills.SkillImpact -= OnSkillImpact;
            _hooked.World.HitLanded -= OnHitLanded;
            _hooked = null;
        }

        private void OnSkillCast(int slot, SkillDef def)
        {
            if (_hooked == null || def == null) return;
            bool ultimate = slot == _hooked.Skills.UltimateSlot;
            int grade = (int)def.Grade;
            SkillElement element = SkillLooks.ElementOf(def);
            Color color = SkillLooks.EffectColor(def, element);
            Color gradeColor = PanelServices.GradeColor(def.Grade);
            float heroX = (float)_hooked.Hero.X;
            Play(SoundOf(def));

            if (!LowEffect)
            {
                CastLayers(def, ultimate, grade, element, gradeColor, heroX);
                if (ultimate && _cutIn != null) _cutIn.PlayUltimate(JobCatalog.Find(_save != null ? _save.jobId : ""), def, color);
                else if (def.Grade == Grade.Legendary && _cutIn != null) _cutIn.PlayBanner(def, gradeColor);
                else ShowName(def, gradeColor, heroX);
            }
            else
            {
                ShowName(def, gradeColor, heroX);
            }

            if (def.VfxAt == SkillVfxAt.Hero) PlaySkillClip(def, heroX, ultimate);
            else if (def.VfxAt == SkillVfxAt.Front) PlaySkillClip(def, heroX + (float)def.Range * 0.5f, ultimate);

            if (def.DealsDamage) Shake(GradeShake[grade] * (ultimate ? 1.5f : 1f), 0.12f + grade * 0.08f + (ultimate ? 0.2f : 0f));
            if (def.DealsDamage && (def.Grade >= Grade.Epic || ultimate)) HitStop.Trigger(ultimate ? UltimateStop : BigSkillStop);
            if (_flash == null || LowEffect || (def.Grade < Grade.Epic && !ultimate)) return;
            // Mostly white with a hint of the element: a gold flash over the red chapters reads as mud.
            Color flash = Color.Lerp(Color.white, color, 0.35f);
            flash.a = ultimate ? UltimateFlashAlpha : def.Grade == Grade.Legendary ? LegendaryFlashAlpha : EpicFlashAlpha;
            _flash.Flash(flash, FlashSeconds);
        }

        /// <summary>The circle under the hero, the Epic+ light pillar, rising motes and the hero's flash.</summary>
        private void CastLayers(SkillDef def, bool ultimate, int grade, SkillElement element, Color gradeColor, float heroX)
        {
            int circle = ultimate ? 4 : grade + 1;
            float circleScale = ultimate ? 1.25f : 0.8f + grade * 0.1f;
            Color light = gradeColor;
            light.a = 0.32f + grade * 0.08f;
            PlayLayered(_set != null ? _set.Find("circle" + circle) : null, heroX, 0f, circleScale, Color.white, light, CircleOrder, CircleOrder - 1);

            if (grade >= (int)Grade.Epic || ultimate)
            {
                VfxClip pillar = _set != null ? _set.Find("pillar") : null;
                if (pillar != null && _vfx != null)
                {
                    float scale = ultimate ? 1f : 0.75f + (grade - 2) * 0.1f;
                    Color beam = Color.Lerp(gradeColor, Color.white, 0.25f);
                    beam.a = 0.72f;
                    var at = new Vector3(heroX, pillar.halfHeight * scale, 0f);
                    if (pillar.glow.Length > 0)
                    {
                        Color halo = gradeColor;
                        halo.a = 0.38f;
                        _vfx.Play(pillar.glow, pillar.fps, at, scale, halo, PillarOrder - 1, true);
                    }

                    _vfx.Play(pillar.frames, pillar.fps, at, scale, beam, PillarOrder, true);
                }
            }

            if (_particles != null)
            {
                SkillLooks.Colors(element, out Color bright, out _);
                int motes = 6 + grade * 5 + (ultimate ? 12 : 0);
                _particles.Burst(new Vector3(heroX, 0.35f, 0f), motes, Color.Lerp(bright, Color.white, 0.3f), gradeColor,
                    ParticleMotion.Rise, 2.2f, 0.12f, 0.5f);
            }

            if (_view != null) _view.FlashHero(Color.Lerp(gradeColor, Color.white, 0.35f), 0.2f + grade * 0.05f + (ultimate ? 0.1f : 0f));
        }

        private void ShowName(SkillDef def, Color gradeColor, float heroX)
        {
            if (_labels != null)
                _labels.ShowLabel(new Vector3(heroX, SkillNameY, 0f), Strings.Get(def.NameKey), Color.Lerp(gradeColor, Color.white, 0.35f), SkillNameSize);
        }

        private void OnSkillImpact(SkillDef def, double x)
        {
            if (def == null) return;
            bool ultimate = IsUltimate(def);

            // EachTarget skills land on every enemy in one frame: the wave extras (shockwave, starburst) play once and
            // only the first few impacts carry a glow and particles.
            bool firstOfWave = Time.frameCount != _waveFrame || def != _waveDef;
            if (firstOfWave)
            {
                _waveFrame = Time.frameCount;
                _waveDef = def;
                _waveImpacts = 0;
            }

            _waveImpacts++;
            bool lead = _waveImpacts <= ImpactBurstsPerWave;
            PlaySkillClip(def, (float)x, ultimate, lead);
            if (LowEffect) return;

            int grade = (int)def.Grade;
            SkillElement element = SkillLooks.ElementOf(def);
            Color color = SkillLooks.EffectColor(def, element);
            if (_particles != null && lead)
            {
                SkillLooks.Colors(element, out Color bright, out Color deep);
                ParticleMotion motion = SkillLooks.MotionOf(element, out float speed);
                _particles.Burst(new Vector3((float)x, EffectY, 0f), 4 + grade * 2 + (ultimate ? 4 : 0), bright, deep, motion, speed, 0.11f, 0.35f);
            }

            if (!firstOfWave) return;
            if (def.Kind == SkillKind.Area)
            {
                Color ring = color;
                ring.a = 0.85f;
                Color halo = color;
                halo.a = 0.45f;
                float shockScale = 0.8f + grade * 0.14f + (ultimate ? 0.25f : 0f);
                PlayLayered(_set != null ? _set.Find("shock") : null, (float)x, ShockRingOffset * shockScale, shockScale,
                    ring, halo, CircleOrder + 1, CircleOrder);
            }

            if (def.Grade >= Grade.Epic || ultimate)
            {
                VfxClip burst = _set != null ? _set.Find("flash") : null;
                if (burst != null && _vfx != null)
                {
                    Color star = Color.Lerp(color, Color.white, 0.35f);
                    star.a = 0.9f;
                    float scale = ultimate ? 1.35f : 0.9f + (grade - 2) * 0.22f;
                    _vfx.Play(burst.frames, burst.fps, new Vector3((float)x, EffectY, 0f), scale, star, LightOrder, true);
                }
            }
        }

        /// <summary>Skill hits (and skill crits) spark in the skill's element; normal hits, combos and pets stay CombatFx's.</summary>
        private void OnHitLanded(EnemyBrain target, double amount, HitKind kind)
        {
            if (kind != HitKind.Skill && kind != HitKind.SkillCrit) return;
            bool crit = kind == HitKind.SkillCrit;
            if (crit && Time.time - _lastCritSound >= CritSoundGap)
            {
                // D-142: an area skill can crit a whole wave at once; one crit sound per moment.
                _lastCritSound = Time.time;
                Play(SfxId.Crit);
            }

            if (LowEffect || target == null) return;
            SkillDef def = _hooked != null ? _hooked.Skills.ActiveDef : null;
            SkillElement element = SkillLooks.ElementOf(def);
            SkillLooks.Colors(element, out Color bright, out Color deep);
            float y = EffectY + DepthLanes.For(target);
            Color tint = crit ? CritTint : bright;
            Color halo = crit ? CritTint : deep;
            if (Time.frameCount != _hitFrame)
            {
                _hitFrame = Time.frameCount;
                _hitGlows = 0;
            }

            // An area skill hits a whole wave in one frame: a couple of glows read as the same light for fewer draws.
            halo.a = crit || _hitGlows++ < HitGlowsPerFrame ? 0.6f : 0f;
            PlayLayered(_set != null ? _set.Find("spark") : null, (float)target.X, y, crit ? 2.2f : 1.8f, tint, halo, ClipOrder, GlowOrder);
            if (_particles != null)
            {
                ParticleMotion motion = SkillLooks.MotionOf(element, out float speed);
                _particles.Burst(new Vector3((float)target.X, y, 0f), crit ? 5 : 3, crit ? CritTint : bright, deep,
                    motion == ParticleMotion.Rise ? ParticleMotion.Spray : motion, Mathf.Max(speed, 3f), 0.1f, 0.15f);
            }
        }

        /// <summary>The skill's clip over its glow (unless <paramref name="glow"/> is off), bigger with the grade; ground clips stand on the floor.</summary>
        private void PlaySkillClip(SkillDef def, float x, bool ultimate, bool glow = true)
        {
            if (_vfx == null || _set == null) return;
            Color tint = def.Tint != 0 ? SkillLooks.Tint(def.Tint) : Color.white;
            float scale = def.VfxScale * (LowEffect ? 1f : SkillLooks.ScaleOf(def.Grade, ultimate));
            VfxClip clip = _set.Find(def.Vfx);
            if (clip == null) return;
            float y = clip.ground ? clip.halfHeight * scale : EffectY;
            Color halo = def.Tint != 0 ? tint : Color.white;
            halo.a = glow ? SkillLooks.GlowOf(def.Grade, ultimate) : 0f;
            PlayLayered(clip, x, y, scale, tint, halo, ClipOrder, GlowOrder);
        }

        /// <summary>A clip with its additive glow under it (no glow in low-effect mode or when the clip has none).</summary>
        private void PlayLayered(VfxClip clip, float x, float y, float scale, Color tint, Color glow, int order, int glowOrder)
        {
            if (clip == null || _vfx == null) return;
            var at = new Vector3(x, y, 0f);
            if (!LowEffect && clip.glow != null && clip.glow.Length > 0 && glow.a > 0f)
                _vfx.Play(clip.glow, clip.fps, at, scale, glow, glowOrder, true);
            _vfx.Play(clip.frames, clip.fps, at, scale, tint, order, false);
        }

        private bool IsUltimate(SkillDef def)
        {
            if (_save == null) return false;
            JobDef job = JobCatalog.Find(_save.jobId);
            return job.Ultimate != null && ReferenceEquals(job.Ultimate, def);
        }

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
