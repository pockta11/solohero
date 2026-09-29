using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>Endless gentle scale breathing for a highlight (skill selection bracket). Unscaled time.</summary>
    public sealed class UiPulse : MonoBehaviour
    {
        [SerializeField] private float _scale = 1.06f;
        [SerializeField] private float _speed = 5f;

        private void OnDisable() => transform.localScale = Vector3.one;

        private void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _speed);
            transform.localScale = Vector3.one * (1f + (_scale - 1f) * k);
        }
    }
}
