using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// One floating damage number (E8-09). Normal hits rise and fade; crits pop in large and arc sideways so they
    /// read apart from the stream of normal numbers. Labels (skill names) sit on a dark plate, rise a little and hold
    /// before fading so they read over any background. Hands itself back to its pool at the end.
    /// </summary>
    public sealed class DamageText : MonoBehaviour
    {
        private const float LifeSeconds = 0.8f;
        private const float RisePixels = 90f;
        private const float CritPopSeconds = 0.15f;
        private const float CritPopScale = 1.7f;
        private const float CritDriftPixels = 70f;
        private const float LabelLifeSeconds = 1.2f;
        private const float LabelRisePixels = 36f;
        private const float LabelHold = 0.6f;
        private const float PlatePadX = 44f;
        private const float PlatePadY = 22f;

        [SerializeField] private Text _label;
        [Tooltip("Backing plate shown only for labels; sized to the text on each show.")]
        [SerializeField] private RectTransform _plate;

        private DamageTextPool _owner;
        private RectTransform _rect;
        private Vector2 _start;
        private Color _color;
        private float _age;
        private bool _crit;
        private float _drift;
        private bool _isLabel;
        private Image _plateImage;
        private float _plateBaseAlpha;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (_label == null) _label = GetComponent<Text>();
            if (_plate == null) return;
            _plateImage = _plate.GetComponent<Image>();
            if (_plateImage != null) _plateBaseAlpha = _plateImage.color.a;
        }

        public void Show(DamageTextPool owner, Vector2 anchoredPosition, string text, Color color, int fontSize, bool crit, float driftSign, bool label = false)
        {
            _isLabel = label;
            if (label) crit = false;
            _owner = owner;
            _start = anchoredPosition;
            _color = color;
            _age = 0f;
            _crit = crit;
            _drift = crit ? CritDriftPixels * driftSign : 0f;
            _label.text = text;
            _label.fontSize = fontSize;
            _label.color = color;
            _rect.anchoredPosition = anchoredPosition;
            _rect.localScale = crit ? Vector3.one * CritPopScale : Vector3.one;
            if (_plate == null) return;
            _plate.gameObject.SetActive(label);
            if (label) _plate.sizeDelta = new Vector2(_label.preferredWidth + PlatePadX, fontSize + PlatePadY);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_isLabel)
            {
                UpdateLabel();
                return;
            }

            float t = _age / LifeSeconds;
            if (t >= 1f)
            {
                _rect.localScale = Vector3.one;
                _owner.Return(this);
                return;
            }

            // Crits: horizontal drift with a parabolic rise (an arc); normal hits: straight up.
            float rise = _crit ? RisePixels * (2f * t - t * t) * 1.2f : RisePixels * t;
            _rect.anchoredPosition = _start + new Vector2(_drift * t, rise);
            if (_crit)
            {
                float pop = Mathf.Clamp01(_age / CritPopSeconds);
                _rect.localScale = Vector3.one * Mathf.Lerp(CritPopScale, 1f, pop);
            }

            Color c = _color;
            c.a = 1f - t * t;
            _label.color = c;
        }

        /// <summary>Ease up, hold, then fade text and plate together.</summary>
        private void UpdateLabel()
        {
            float t = _age / LabelLifeSeconds;
            if (t >= 1f)
            {
                if (_plate != null) _plate.gameObject.SetActive(false);
                _owner.Return(this);
                return;
            }

            float rise = Mathf.Min(1f, t * 4f);
            _rect.anchoredPosition = _start + new Vector2(0f, LabelRisePixels * (2f * rise - rise * rise));
            _rect.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(_age / CritPopSeconds));
            float alpha = t < LabelHold ? 1f : 1f - (t - LabelHold) / (1f - LabelHold);
            Color c = _color;
            c.a = alpha;
            _label.color = c;
            if (_plateImage == null) return;
            Color p = _plateImage.color;
            p.a = _plateBaseAlpha * alpha;
            _plateImage.color = p;
        }
    }
}
