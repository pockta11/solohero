using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-108 popup entrance: each time the window is shown it pops in from a slightly smaller size with an overshoot
    /// and fades in. Put it on the window box (not the dim); unscaled time so it also plays while battle time is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupIntro : MonoBehaviour
    {
        [SerializeField] private float _seconds = 0.24f;
        [SerializeField] private float _startScale = 0.86f;

        private CanvasGroup _group;
        private float _age = -1f;

        private void Awake() => _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            _age = 0f;
            Apply(0f);
        }

        private void OnDisable()
        {
            _age = -1f;
            transform.localScale = Vector3.one;
            if (_group != null) _group.alpha = 1f;
        }

        private void Update()
        {
            if (_age < 0f) return;
            _age += Time.unscaledDeltaTime;
            float t = _seconds > 0f ? Mathf.Clamp01(_age / _seconds) : 1f;
            Apply(t);
            if (t >= 1f) _age = -1f;
        }

        private void Apply(float t)
        {
            const float c1 = 1.9f;
            const float c3 = c1 + 1f;
            float k = t >= 1f ? 1f : 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            float s = Mathf.LerpUnclamped(_startScale, 1f, k);
            transform.localScale = new Vector3(s, s, 1f);
            if (_group != null) _group.alpha = Mathf.Clamp01(t * 2.5f);
        }
    }
}
