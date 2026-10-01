using SoloHero.Game.UI;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Kill reward juice (D-096): coins pop out of a killed enemy, fall a little, then fly into the gold counter at the
    /// top of the screen. View only - gold itself is granted by Core. The coin renderers are made by the scene builder
    /// and reused round-robin, so nothing is created at runtime; when all are in flight the oldest is reused.
    /// </summary>
    public sealed class CoinBurst : MonoBehaviour
    {
        private const int CoinsPerKill = 3;
        private const int CoinsPerBossKill = 10;
        private const float PopSeconds = 0.32f;
        private const float FlySeconds = 0.38f;
        private const float Gravity = -14f;

        [SerializeField] private CombatWorldView _view;
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer[] _coins = new SpriteRenderer[0];

        private RectTransform _target;
        private Vector3[] _start;
        private Vector3[] _velocity;
        private Vector3[] _flyFrom;
        private float[] _age;
        private int _next;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            int n = _coins.Length;
            _start = new Vector3[n];
            _velocity = new Vector3[n];
            _flyFrom = new Vector3[n];
            _age = new float[n];
            for (int i = 0; i < n; i++)
            {
                _age[i] = -1f;
                if (_coins[i] != null) _coins[i].enabled = false;
            }

            BattleHud hud = FindObjectOfType<BattleHud>(true);
            _target = hud != null ? hud.GoldAnchor : null;
        }

        private void OnEnable()
        {
            if (_view != null) _view.EnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            if (_view != null) _view.EnemyDied -= OnEnemyDied;
        }

        private void OnEnemyDied(Vector3 position, bool boss, bool hasDeathClip)
        {
            int count = boss ? CoinsPerBossKill : CoinsPerKill;
            for (int k = 0; k < count && _coins.Length > 0; k++)
            {
                int i = _next;
                _next = (_next + 1) % _coins.Length;
                if (_coins[i] == null) continue;
                _start[i] = new Vector3(position.x, position.y + 0.5f, 0f);
                _velocity[i] = new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(3.2f, 4.6f), 0f);
                _age[i] = 0f;
                _coins[i].enabled = true;
                _coins[i].transform.position = _start[i];
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 target = TargetWorld();
            for (int i = 0; i < _coins.Length; i++)
            {
                if (_age[i] < 0f || _coins[i] == null) continue;
                _age[i] += dt;
                float t = _age[i];
                Transform tr = _coins[i].transform;
                if (t < PopSeconds)
                {
                    tr.position = _start[i] + _velocity[i] * t + new Vector3(0f, 0.5f * Gravity * t * t, 0f);
                    _flyFrom[i] = tr.position;
                    continue;
                }

                float f = (t - PopSeconds) / FlySeconds;
                if (f >= 1f)
                {
                    _age[i] = -1f;
                    _coins[i].enabled = false;
                    continue;
                }

                tr.position = Vector3.LerpUnclamped(_flyFrom[i], target, f * f);
            }
        }

        /// <summary>The gold counter's centre in world space (the HUD canvas is screen-space overlay).</summary>
        private Vector3 TargetWorld()
        {
            if (_camera == null) return Vector3.zero;
            Vector3 screen = _target != null ? _target.position : new Vector3(Screen.width * 0.15f, Screen.height * 0.96f, 0f);
            screen.z = -_camera.transform.position.z;
            Vector3 world = _camera.ScreenToWorldPoint(screen);
            world.z = 0f;
            return world;
        }
    }
}
