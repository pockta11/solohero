using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.Pooling
{
    /// <summary>One floating damage number. Rises and fades, then hands itself back to its pool.</summary>
    public sealed class DamageText : MonoBehaviour
    {
        private const float LifeSeconds = 0.8f;
        private const float RisePixels = 90f;

        [SerializeField] private Text _label;

        private DamageTextPool _owner;
        private RectTransform _rect;
        private Vector2 _start;
        private Color _color;
        private float _age;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            if (_label == null) _label = GetComponent<Text>();
        }

        public void Show(DamageTextPool owner, Vector2 anchoredPosition, string text, Color color, int fontSize)
        {
            _owner = owner;
            _start = anchoredPosition;
            _color = color;
            _age = 0f;
            _label.text = text;
            _label.fontSize = fontSize;
            _label.color = color;
            _rect.anchoredPosition = anchoredPosition;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / LifeSeconds;
            if (t >= 1f)
            {
                _owner.Return(this);
                return;
            }

            _rect.anchoredPosition = _start + new Vector2(0f, RisePixels * t);
            Color c = _color;
            c.a = 1f - t * t;
            _label.color = c;
        }
    }
}
