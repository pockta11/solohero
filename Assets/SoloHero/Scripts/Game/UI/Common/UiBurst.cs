using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Grade particles for UI (E8-10): a fixed set of small Images (created by the scene builder) thrown out from a
    /// point with gravity and a fade. Screen-space overlay canvases cannot draw ParticleSystems, so this stands in.
    /// A new burst reuses the same particles; nothing is created at runtime.
    /// </summary>
    public sealed class UiBurst : MonoBehaviour
    {
        private const float LifeSeconds = 0.9f;
        private const float Gravity = -1400f;

        [SerializeField] private Image[] _particles = new Image[0];
        [SerializeField] private float _speedMin = 350f;
        [SerializeField] private float _speedMax = 900f;

        private Vector2[] _velocity = new Vector2[0];
        private Vector2[] _position = new Vector2[0];
        private float _age = -1f;
        private Color _color = Color.white;

        private void Awake()
        {
            _velocity = new Vector2[_particles.Length];
            _position = new Vector2[_particles.Length];
            Hide();
        }

        private void OnDisable() => Hide();

        public void Play(Vector2 localPosition, Color color)
        {
            _color = color;
            _age = 0f;
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null) continue;
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = Random.Range(_speedMin, _speedMax);
                _velocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                _position[i] = localPosition;
                _particles[i].gameObject.SetActive(true);
                _particles[i].color = i % 3 == 0 ? Color.white : color;
                _particles[i].rectTransform.anchoredPosition = localPosition;
            }
        }

        private void Update()
        {
            if (_age < 0f) return;
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            if (_age >= LifeSeconds)
            {
                Hide();
                return;
            }

            float alpha = 1f - _age / LifeSeconds;
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null) continue;
                _velocity[i].y += Gravity * dt;
                _position[i] += _velocity[i] * dt;
                _particles[i].rectTransform.anchoredPosition = _position[i];
                Color c = i % 3 == 0 ? Color.white : _color;
                c.a = alpha;
                _particles[i].color = c;
            }
        }

        private void Hide()
        {
            _age = -1f;
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] != null) _particles[i].gameObject.SetActive(false);
            }
        }
    }
}
