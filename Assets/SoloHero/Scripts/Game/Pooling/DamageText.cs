using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// One floating damage number (E8-09). Normal hits rise and fade; crits pop in large and arc sideways so they
    /// read apart from the stream of normal numbers. Hands itself back to its pool at the end.
    /// </summary>
    public sealed class DamageText : MonoBehaviour
    {
        private const float LifeSeconds = 0.8f;
        private const float RisePixels = 90f;
        private const float CritPopSeconds = 0.15f;
        private const float CritPopScale = 1.7f;
        private const float CritDriftPixels = 70f;

        [SerializeField] private Text _label;

        private DamageTextPool _owner;
        private RectTransform _rect;
        private Vector2 _start;
        private Color _color;
        private float _age;
        private bool _crit;
        private float _drift;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (_label == null) _label = GetComponent<Text>();
        }

        public void Show(DamageTextPool owner, Vector2 anchoredPosition, string text, Color color, int fontSize, bool crit, float driftSign)
        {
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
        }

        private void Update()
        {
            _age += Time.deltaTime;
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
    }
}
