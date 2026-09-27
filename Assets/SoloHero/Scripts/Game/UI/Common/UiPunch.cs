using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Scale punch for UI feedback (E8-11 upgrade success, gold gain). Runs on unscaled time and always settles back
    /// to scale 1, so an interrupted punch never leaves the element resized.
    /// </summary>
    public sealed class UiPunch : MonoBehaviour
    {
        [SerializeField] private float _scale = 1.15f;
        [SerializeField] private float _seconds = 0.2f;

        private float _age = -1f;
        private float _strength = 1f;

        public void Play(float strength = 1f)
        {
            _strength = strength;
            _age = 0f;
        }

        private void OnDisable()
        {
            _age = -1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (_age < 0f) return;
            _age += Time.unscaledDeltaTime;
            float t = _seconds > 0f ? _age / _seconds : 1f;
            if (t >= 1f)
            {
                _age = -1f;
                transform.localScale = Vector3.one;
                return;
            }

            // Up fast, settle slower: sin over half a period, weighted to the first third.
            float bump = Mathf.Sin(Mathf.Pow(t, 0.6f) * Mathf.PI);
            transform.localScale = Vector3.one * (1f + (_scale - 1f) * _strength * bump);
        }
    }
}
