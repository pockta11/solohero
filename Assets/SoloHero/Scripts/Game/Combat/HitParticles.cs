using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// D-112 pixel debris: one pre-placed ParticleSystem (ArtBuilder sets it up, no runtime Instantiate) that throws
    /// small square shards up and out of a hit or a kill; gravity brings them down and they fade. View only.
    /// </summary>
    public sealed class HitParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _system;

        private ParticleSystem.EmitParams _emit;

        private void Awake()
        {
            if (_system == null) _system = GetComponent<ParticleSystem>();
            _emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            if (_system == null) return;
            // Emission stays off; the system only has to be playing so emitted shards simulate and render.
            // The system sits at the origin while the camera follows the hero far to the right: never cull it, or a
            // looping system pauses off-screen and the shards it emits there never move or draw.
            ParticleSystem.MainModule main = _system.main;
            main.loop = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            _system.Play();
        }

        /// <param name="spread">Half-angle of the upward fan in degrees.</param>
        public void Burst(Vector3 position, int count, Color color, float speed, float size, float spread = 70f)
        {
            if (_system == null || count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                float angle = (90f + Random.Range(-spread, spread)) * Mathf.Deg2Rad;
                float v = speed * Random.Range(0.55f, 1f);
                _emit.position = position;
                _emit.velocity = new Vector3(Mathf.Cos(angle) * v, Mathf.Sin(angle) * v, 0f);
                _emit.startColor = color;
                _emit.startSize = size * Random.Range(0.75f, 1.3f);
                _emit.startLifetime = Random.Range(0.35f, 0.6f);
                _system.Emit(_emit, 1);
            }
        }
    }
}
