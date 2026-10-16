using SoloHero.Core.Common;
using SoloHero.Core.Jobs;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-179 job advancement ceremony over everything (unscaled time, about 2.6 s, a tap skips): the screen dims, the
    /// old look bursts into light, a pillar and a turning sunburst rise behind the new job's animated look as it pops
    /// in with sparks, then the tier title, the job name and its main attack / ultimate line. It ends with the usual
    /// ribbon. CombatFx starts it when the save's job changes.
    /// </summary>
    public sealed class JobAdvanceFx : MonoBehaviour
    {
        private const float BurstSeconds = 0.3f;
        private const float PopStart = 0.3f;
        private const float PopSeconds = 0.45f;
        private const float TitleStart = 0.7f;
        private const float TitleSeconds = 0.3f;
        private const float SubStart = 1.0f;
        private const float HoldUntil = 2.4f;
        private const float OutSeconds = 0.35f;
        private const float SparkLife = 1.1f;
        private const float RaySpin = 40f;
        private const float PortraitScale = 8f;
        private const float OldScale = 6f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _dim;
        [SerializeField] private RectTransform _pillar;
        [SerializeField] private Image _pillarImage;
        [SerializeField] private RectTransform _rays;
        [SerializeField] private Image[] _rayImages = new Image[0];
        [SerializeField] private Image _oldPortrait;
        [SerializeField] private RectTransform _portraitRect;
        [SerializeField] private Image _portrait;
        [SerializeField] private UiFlipbook _flipbook;
        [SerializeField] private Image[] _sparks = new Image[0];
        [SerializeField] private RectTransform _titleRect;
        [SerializeField] private Text _title;
        [SerializeField] private RectTransform _nameRect;
        [SerializeField] private Text _name;
        [SerializeField] private Text _sub;
        [SerializeField] private CanvasGroup _textGroup;
        [Tooltip("Job looks index-aligned with JobCatalog.All.")]
        [SerializeField] private CharacterArt[] _jobArts = new CharacterArt[0];
        [SerializeField] private SkillCutIn _cutIn;
        [SerializeField] private ScreenFlash _flash;

        private static readonly Color DimColor = new Color(0.04f, 0.02f, 0.1f, 0.82f);
        private static readonly Color RayColor = new Color(1f, 0.9f, 0.55f, 0.42f);
        private static readonly Color PillarColor = new Color(1f, 0.95f, 0.7f, 0.95f);
        private static readonly Color SparkColor = new Color(1f, 0.85f, 0.4f, 1f);
        private static readonly Color RibbonColor = new Color(1f, 0.86f, 0.4f, 1f);

        private float _age = -1f;
        private Vector2[] _sparkDir = new Vector2[0];
        private float[] _sparkSpeed = new float[0];
        private float[] _sparkDelay = new float[0];
        private string _ribbonText = "";
        private AudioService _audio;
        private bool _popSoundPlayed;

        public bool Playing => _age >= 0f;

        /// <summary>Plays the ceremony for <paramref name="job"/>; <paramref name="previous"/> is the look that bursts.</summary>
        public void Play(JobDef job, JobDef previous)
        {
            if (job == null) return;
            // The holder sleeps inactive between plays (the builder leaves it so), so Awake may run only now.
            if (_sparkDir.Length != _sparks.Length)
            {
                _sparkDir = new Vector2[_sparks.Length];
                _sparkSpeed = new float[_sparks.Length];
                _sparkDelay = new float[_sparks.Length];
            }

            _audio = PanelServices.TryGet<AudioService>();
            gameObject.SetActive(true);
            _age = 0f;
            _popSoundPlayed = false;
            CharacterArt art = Art(job);
            CharacterArt old = Art(previous);
            if (_oldPortrait != null)
            {
                Sprite face = old != null && old.idle.Length > 0 ? old.idle[0] : null;
                UiFlipbook.Fit(_oldPortrait, face, OldScale);
                _oldPortrait.enabled = face != null;
            }

            if (_portrait != null)
            {
                Sprite face = art != null && art.idle.Length > 0 ? art.idle[0] : null;
                UiFlipbook.Fit(_portrait, face, PortraitScale);
                _portrait.enabled = face != null;
            }

            if (_flipbook != null) _flipbook.SetArt(art);
            if (_title != null)
                _title.text = job.Tier >= JobCatalog.MaxTier ? Strings.Get("fx.job_tier_max") : Strings.Format("fx.job_tier", job.Tier);
            if (_name != null) _name.text = Strings.Get(job.NameKey);
            if (_sub != null)
            {
                string main = Strings.Format("job.preview_main", Strings.Get(job.MainKey));
                _sub.text = job.Ultimate != null
                    ? main + "  " + Strings.Format("job.preview_ult", Strings.Get(job.Ultimate.NameKey))
                    : main;
            }

            _ribbonText = Strings.Format("fx.job_advance", Strings.Get(job.NameKey));
            for (int i = 0; i < _sparks.Length; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                _sparkDir[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _sparkSpeed[i] = Random.Range(420f, 980f);
                _sparkDelay[i] = Random.Range(0f, 0.25f);
                if (_sparks[i] != null) _sparks[i].color = SparkColor;
            }

            _audio?.Play(SfxId.Fanfare);
            if (_flash != null) _flash.Flash(new Color(1f, 1f, 1f, 0.7f), 0.35f);
            Apply();
        }

        /// <summary>A tap: jump to the fade-out.</summary>
        public void Skip()
        {
            if (_age >= 0f && _age < HoldUntil) _age = HoldUntil;
        }

        private void Update()
        {
            if (_age < 0f) return;
            _age += Time.unscaledDeltaTime;
            if (_age >= PopStart && !_popSoundPlayed)
            {
                _popSoundPlayed = true;
                _audio?.Play(SfxId.GradeLegendary);
            }

            if (_age >= HoldUntil + OutSeconds)
            {
                End();
                return;
            }

            Apply();
        }

        private void End()
        {
            _age = -1f;
            gameObject.SetActive(false);
            if (_cutIn != null) _cutIn.PlayText(_ribbonText, RibbonColor, true, 2.2f);
        }

        private void Apply()
        {
            float t = _age;
            float fade = t > HoldUntil ? 1f - Mathf.Clamp01((t - HoldUntil) / OutSeconds) : 1f;
            if (_group != null) _group.alpha = fade;

            if (_dim != null)
            {
                Color c = DimColor;
                c.a = DimColor.a * Mathf.Clamp01(t / 0.2f);
                _dim.color = c;
            }

            // The old look bursts: grows and fades in the first beat.
            if (_oldPortrait != null)
            {
                float k = Mathf.Clamp01(t / BurstSeconds);
                _oldPortrait.rectTransform.localScale = Vector3.one * (1f + 0.9f * k);
                Color c = Color.white;
                c.a = 1f - k;
                _oldPortrait.color = c;
                if (k >= 1f && _oldPortrait.enabled) _oldPortrait.enabled = false;
            }

            // The pillar rises then settles; the sunburst turns.
            if (_pillar != null)
            {
                float k = Mathf.Clamp01(t / 0.35f);
                _pillar.localScale = new Vector3(0.15f + 0.85f * EaseOut(k), 1f, 1f);
                if (_pillarImage != null)
                {
                    Color c = PillarColor;
                    c.a = PillarColor.a * k * (0.85f + 0.15f * Mathf.Sin(t * 9f));
                    _pillarImage.color = c;
                }
            }

            if (_rays != null)
            {
                _rays.localRotation = Quaternion.Euler(0f, 0f, t * RaySpin);
                float k = Mathf.Clamp01((t - 0.15f) / 0.4f);
                Color c = RayColor;
                c.a = RayColor.a * k;
                for (int i = 0; i < _rayImages.Length; i++)
                    if (_rayImages[i] != null) _rayImages[i].color = c;
            }

            // The new look pops in with an overshoot, then breathes.
            if (_portraitRect != null)
            {
                float k = Mathf.Clamp01((t - PopStart) / PopSeconds);
                float s = t < PopStart ? 0f : Mathf.LerpUnclamped(0.2f, 1f, EaseOutBack(k));
                if (k >= 1f) s = 1f + 0.03f * Mathf.Sin((t - PopStart - PopSeconds) * 4f);
                _portraitRect.localScale = new Vector3(s, s, 1f);
            }

            for (int i = 0; i < _sparks.Length; i++)
            {
                Image spark = _sparks[i];
                if (spark == null) continue;
                float life = t - PopStart - _sparkDelay[i];
                bool on = life >= 0f && life < SparkLife;
                if (spark.enabled != on) spark.enabled = on;
                if (!on) continue;
                float k = life / SparkLife;
                spark.rectTransform.anchoredPosition = _sparkDir[i] * (_sparkSpeed[i] * life * (1f - 0.35f * k));
                float size = 34f * (1f - k) + 6f;
                spark.rectTransform.sizeDelta = new Vector2(size, size);
                Color c = SparkColor;
                c.a = 1f - k * k;
                spark.color = c;
            }

            // Title drops in, the name scales down from large, the sub line fades in.
            float title = Mathf.Clamp01((t - TitleStart) / TitleSeconds);
            if (_titleRect != null) _titleRect.anchoredPosition = new Vector2(0f, 330f + 90f * (1f - EaseOut(title)));
            if (_nameRect != null)
            {
                float s = Mathf.LerpUnclamped(1.7f, 1f, EaseOutBack(title));
                _nameRect.localScale = new Vector3(s, s, 1f);
            }

            if (_textGroup != null) _textGroup.alpha = title;
            if (_sub != null)
            {
                Color c = _sub.color;
                c.a = Mathf.Clamp01((t - SubStart) / 0.3f);
                _sub.color = c;
            }
        }

        private CharacterArt Art(JobDef job)
        {
            if (job == null) return _jobArts.Length > 0 ? _jobArts[0] : null;
            int index = JobCatalog.IndexOf(job.Id);
            return index >= 0 && index < _jobArts.Length ? _jobArts[index] : null;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.7f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
