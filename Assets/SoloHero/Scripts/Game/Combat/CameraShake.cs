using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Camera shake offset (GDD juice #6: boss, crit, Epic+ pulls). It only computes an offset; CombatWorldView adds
    /// it when it places the camera, so shaking never fights the hero follow. Offsets snap to the pixel grid.
    /// Low-effect mode is handled by callers (they do not call <see cref="Shake"/>).
    /// </summary>
    public sealed class CameraShake : MonoBehaviour
    {
        private const float PixelsPerUnit = 32f;

        private float _amplitude;
        private float _duration;
        private float _remaining;

        public Vector2 Offset { get; private set; }

        /// <summary>Amplitude in world units. A stronger shake replaces a weaker one; a weaker one is ignored.</summary>
        public void Shake(float amplitude, float seconds)
        {
            if (_remaining > 0f && amplitude * seconds < _amplitude * _remaining) return;
            _amplitude = amplitude;
            _duration = seconds > 0f ? seconds : 0.01f;
            _remaining = _duration;
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                Offset = Vector2.zero;
                return;
            }

            _remaining -= Time.deltaTime;
            float strength = _amplitude * Mathf.Clamp01(_remaining / _duration);
            Vector2 raw = Random.insideUnitCircle * strength;
            Offset = new Vector2(Mathf.Round(raw.x * PixelsPerUnit) / PixelsPerUnit, Mathf.Round(raw.y * PixelsPerUnit) / PixelsPerUnit);
        }
    }
}
