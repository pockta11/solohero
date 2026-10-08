using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>How a burst of skill particles moves (D-146).</summary>
    public enum ParticleMotion
    {
        /// <summary>Embers, motes and bubbles: drift up and sway, live long.</summary>
        Rise,

        /// <summary>Sparks and shards: thrown out in every direction, quick.</summary>
        Spray,

        /// <summary>Snow and ash: drift down.</summary>
        Fall,

        /// <summary>A ring of light points sliding outward along the ground.</summary>
        Ring
    }

    /// <summary>
    /// D-146 skill light particles: one pre-placed additive ParticleSystem (ArtBuilder sets it up, no runtime
    /// Instantiate) without gravity; every burst gives its particles their own speed, colour and life, so embers rise,
    /// sparks spray and snow falls from the same system. Like HitParticles it never culls and only emits by hand.
    /// </summary>
    public sealed class SkillParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _system;

        private ParticleSystem.EmitParams _emit;

        private void Awake()
        {
            if (_system == null) _system = GetComponent<ParticleSystem>();
            _emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            if (_system == null) return;
            ParticleSystem.MainModule main = _system.main;
            main.loop = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            _system.Play();
        }

        /// <param name="spread">Half-width of the area the particles start in (world units).</param>
        public void Burst(Vector3 position, int count, Color a, Color b, ParticleMotion motion, float speed, float size, float spread = 0.3f)
        {
            if (_system == null || count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = position + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread) * 0.5f, 0f);
                Vector3 velocity;
                float life;
                switch (motion)
                {
                    case ParticleMotion.Rise:
                        velocity = new Vector3(Random.Range(-0.35f, 0.35f) * speed, Random.Range(0.55f, 1f) * speed, 0f);
                        life = Random.Range(0.55f, 1.05f);
                        break;
                    case ParticleMotion.Fall:
                        velocity = new Vector3(Random.Range(-0.4f, 0.4f) * speed, -Random.Range(0.35f, 0.8f) * speed, 0f);
                        at.y += spread * 1.5f;
                        life = Random.Range(0.6f, 1.0f);
                        break;
                    case ParticleMotion.Ring:
                    {
                        float angle = i * Mathf.PI * 2f / count + Random.Range(-0.1f, 0.1f);
                        velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * 0.3f, 0f);
                        at = position;
                        life = Random.Range(0.35f, 0.5f);
                        break;
                    }
                    default:
                    {
                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        float v = speed * Random.Range(0.45f, 1f);
                        velocity = new Vector3(Mathf.Cos(angle) * v, Mathf.Sin(angle) * v + speed * 0.25f, 0f);
                        life = Random.Range(0.25f, 0.5f);
                        break;
                    }
                }

                _emit.position = at;
                _emit.velocity = velocity;
                _emit.startColor = Color.Lerp(a, b, Random.value);
                _emit.startSize = size * Random.Range(0.7f, 1.35f);
                _emit.startLifetime = life;
                _system.Emit(_emit, 1);
            }
        }
    }
}
