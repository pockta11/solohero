using SoloHero.Core.Common;
using SoloHero.Core.Jobs;
using SoloHero.Core.Skills;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-146 skill cut-ins over the battle (genre staple), on unscaled time and never blocking taps.
    /// A job ultimate: the battle dims, a slanted band with speed lines slides in from the right carrying the job's
    /// portrait and the ultimate's name, holds, and slides out left. A legendary book skill: a name ribbon pops in over
    /// the battle and fades. SkillFx skips both in low-effect mode.
    /// </summary>
    public sealed class SkillCutIn : MonoBehaviour
    {
        private const float InSeconds = 0.14f;
        private const float HoldSeconds = 0.62f;
        private const float OutSeconds = 0.16f;
        private const float BandTravel = 1500f;
        private const float DimAlpha = 0.42f;
        private const float BannerSeconds = 1.1f;
        private const float LineSpeed = 2600f;

        [Header("Ultimate band")]
        [SerializeField] private Image _dim;
        [SerializeField] private RectTransform _band;
        [SerializeField] private Image _bandAccent;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _name;
        [SerializeField] private RectTransform[] _lines = new RectTransform[0];
        [Tooltip("Hero looks index-aligned with JobCatalog.All; the portrait is the idle clip's first frame.")]
        [SerializeField] private CharacterArt[] _jobArts = new CharacterArt[0];

        [Header("Legendary ribbon")]
        [SerializeField] private RectTransform _banner;
        [SerializeField] private CanvasGroup _bannerGroup;
        [SerializeField] private Image _bannerAccent;
        [SerializeField] private Text _bannerName;

        private float _bandAge = -1f;
        private float _bannerAge = -1f;
        private float[] _lineX = new float[0];
        private bool _active;

        public bool Playing => _bandAge >= 0f;

        private void Awake()
        {
            Hide();
            _lineX = new float[_lines.Length];
            for (int i = 0; i < _lines.Length; i++) _lineX[i] = (i * 397f) % BandTravel - BandTravel * 0.5f;
        }

        /// <summary>The ultimate band for <paramref name="def"/> cast by <paramref name="job"/>, in <paramref name="color"/>.</summary>
        public void PlayUltimate(JobDef job, SkillDef def, Color color)
        {
            if (_band == null || def == null) return;
            int index = job != null ? JobCatalog.IndexOf(job.Id) : 0;
            CharacterArt art = index >= 0 && index < _jobArts.Length ? _jobArts[index] : null;
            if (_portrait != null)
            {
                Sprite face = art != null && art.idle != null && art.idle.Length > 0 ? art.idle[0] : null;
                _portrait.sprite = face;
                _portrait.enabled = face != null;
            }

            if (_name != null) _name.text = Strings.Get(def.NameKey);
            if (_bandAccent != null) _bandAccent.color = color;
            _bandAge = 0f;
            _active = true;
            _band.gameObject.SetActive(true);
            if (_dim != null) _dim.enabled = true;
            Apply();
        }

        /// <summary>The name ribbon of a legendary skill.</summary>
        public void PlayBanner(SkillDef def, Color color)
        {
            if (_banner == null || def == null || Playing) return;
            if (_bannerName != null) _bannerName.text = Strings.Get(def.NameKey);
            if (_bannerAccent != null) _bannerAccent.color = color;
            _bannerAge = 0f;
            _active = true;
            _banner.gameObject.SetActive(true);
            Apply();
        }

        private void Update()
        {
            if (!_active) return;
            float dt = Time.unscaledDeltaTime;
            if (_bandAge >= 0f)
            {
                _bandAge += dt;
                if (_bandAge >= InSeconds + HoldSeconds + OutSeconds) _bandAge = -1f;
            }

            if (_bannerAge >= 0f)
            {
                _bannerAge += dt;
                if (_bannerAge >= BannerSeconds) _bannerAge = -1f;
            }

            for (int i = 0; i < _lines.Length; i++)
            {
                _lineX[i] -= LineSpeed * (0.7f + 0.15f * (i % 3)) * dt;
                if (_lineX[i] < -BandTravel * 0.5f) _lineX[i] += BandTravel;
            }

            Apply();
            // Apply has just hidden whatever ended: nothing to do until the next cut-in.
            if (_bandAge < 0f && _bannerAge < 0f) _active = false;
        }

        private void Apply()
        {
            if (_band != null)
            {
                bool show = _bandAge >= 0f;
                if (_band.gameObject.activeSelf != show) _band.gameObject.SetActive(show);
                float x = 0f;
                float dim = 0f;
                if (show)
                {
                    if (_bandAge < InSeconds)
                    {
                        float t = 1f - _bandAge / InSeconds;
                        x = BandTravel * t * t;
                        dim = DimAlpha * (1f - t);
                    }
                    else if (_bandAge < InSeconds + HoldSeconds)
                    {
                        // A slow drift while it holds keeps it alive.
                        x = -40f * (_bandAge - InSeconds) / HoldSeconds;
                        dim = DimAlpha;
                    }
                    else
                    {
                        float t = (_bandAge - InSeconds - HoldSeconds) / OutSeconds;
                        x = -40f - BandTravel * t * t;
                        dim = DimAlpha * (1f - t);
                    }

                    _band.anchoredPosition = new Vector2(x, _band.anchoredPosition.y);
                    for (int i = 0; i < _lines.Length; i++)
                        if (_lines[i] != null) _lines[i].anchoredPosition = new Vector2(_lineX[i], _lines[i].anchoredPosition.y);
                }

                if (_dim != null)
                {
                    _dim.enabled = dim > 0.001f;
                    Color c = _dim.color;
                    c.a = dim;
                    _dim.color = c;
                }
            }

            if (_banner != null)
            {
                bool show = _bannerAge >= 0f;
                if (_banner.gameObject.activeSelf != show) _banner.gameObject.SetActive(show);
                if (show)
                {
                    float pop = _bannerAge < 0.12f ? Mathf.Lerp(0.6f, 1.08f, _bannerAge / 0.12f)
                        : _bannerAge < 0.2f ? Mathf.Lerp(1.08f, 1f, (_bannerAge - 0.12f) / 0.08f) : 1f;
                    _banner.localScale = new Vector3(pop, pop, 1f);
                    if (_bannerGroup != null)
                        _bannerGroup.alpha = _bannerAge > BannerSeconds - 0.3f ? (BannerSeconds - _bannerAge) / 0.3f : 1f;
                }
            }
        }

        private void Hide()
        {
            _bandAge = -1f;
            _bannerAge = -1f;
            _active = false;
            if (_band != null) _band.gameObject.SetActive(false);
            if (_banner != null) _banner.gameObject.SetActive(false);
            if (_dim != null) _dim.enabled = false;
        }
    }
}
