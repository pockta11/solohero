using SoloHero.Core.Combat;
using SoloHero.Core.Common;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using UnityEngine;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// Shows every hero hit as a pooled floating number over the enemy (E2-10, E8-09).
    /// Pre-warmed at scene start; when all texts are busy a hit is simply not shown (no runtime Instantiate).
    /// </summary>
    public sealed class DamageTextPool : MonoBehaviour
    {
        public const int Capacity = 24;
        private const float EnemyHeadOffset = 0.9f;

        [SerializeField] private DamageText _template;
        [SerializeField] private CombatSession _session;
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _layer;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _critColor = new Color(1f, 0.77f, 0.19f, 1f);
        [SerializeField] private int _normalSize = 34;
        [SerializeField] private int _critSize = 46;

        private const float NormalJitterPixels = 18f;

        private ViewPool<DamageText> _pool;
        private CombatWorld _world;
        private float _nextDrift = 1f;

        public int ActiveCount => _pool != null ? _pool.CountActive : 0;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            if (_layer == null) _layer = (RectTransform)transform;
            if (_template == null) return;
            _template.gameObject.SetActive(false);
            _pool = new ViewPool<DamageText>(_template, _layer, Capacity);
        }

        private void OnDestroy()
        {
            if (_world != null) _world.HitLanded -= OnHitLanded;
        }

        private void LateUpdate()
        {
            if (_world != null || _session == null) return;
            StageRunner runner = _session.Runner;
            if (runner == null) return;
            _world = runner.World;
            _world.HitLanded += OnHitLanded;
        }

        public void Return(DamageText text) => _pool.Release(text);

        private void OnHitLanded(EnemyBrain target, double amount, bool crit)
        {
            if (_pool == null || _camera == null || !_pool.TryGet(out DamageText text)) return;

            Vector3 screen = _camera.WorldToScreenPoint(new Vector3((float)target.X, EnemyHeadOffset, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out Vector2 local);
            // Small sideways jitter keeps numbers from stacking on one spot; crits alternate their arc direction.
            if (!crit) local.x += Random.Range(-NormalJitterPixels, NormalJitterPixels);
            _nextDrift = -_nextDrift;
            text.Show(this, local, BigNumberFormat.Format(amount), crit ? _critColor : _normalColor, crit ? _critSize : _normalSize, crit, _nextDrift);
        }
    }
}
