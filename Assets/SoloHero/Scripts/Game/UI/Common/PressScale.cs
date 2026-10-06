using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-108 press feedback: the button squashes a little while held and springs back with a small overshoot on
    /// release (genre-standard candy buttons). Unscaled time; settles to scale 1 when disabled.
    /// </summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float _pressed = 0.93f;
        [SerializeField] private float _releaseSeconds = 0.22f;

        private Selectable _selectable;
        private bool _down;
        private float _release = -1f;
        private float _from = 1f;

        private void Awake() => _selectable = GetComponent<Selectable>();

        private void OnDisable()
        {
            _down = false;
            _release = -1f;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.IsInteractable()) return;
            _down = true;
            _release = -1f;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_down) Release();
        }

        private void Release()
        {
            if (!_down) return;
            _down = false;
            _from = transform.localScale.x;
            _release = 0f;
        }

        private void Update()
        {
            if (_down)
            {
                float s = Mathf.MoveTowards(transform.localScale.x, _pressed, Time.unscaledDeltaTime * 2.5f);
                transform.localScale = new Vector3(s, s, 1f);
                return;
            }

            if (_release < 0f) return;
            _release += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_release / _releaseSeconds);
            // Ease-out-back from the pressed size to 1.
            const float c1 = 2.2f;
            const float c3 = c1 + 1f;
            float k = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            float scale = Mathf.LerpUnclamped(_from, 1f, k);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (t >= 1f)
            {
                _release = -1f;
                transform.localScale = Vector3.one;
            }
        }
    }
}
