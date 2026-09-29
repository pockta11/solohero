using UnityEngine;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// World-space VFX pool (E8-08, architecture D11). Pre-warmed at scene start from a disabled template; when every
    /// effect is busy the new one is skipped (no runtime Instantiate). Effects draw above the characters.
    /// </summary>
    public sealed class VfxPool : MonoBehaviour
    {
        public const int Capacity = 24;
        private const int SortingOrder = 250;

        [SerializeField] private VfxItem _template;

        private ViewPool<VfxItem> _pool;

        public int ActiveCount => _pool != null ? _pool.CountActive : 0;

        private void Awake()
        {
            if (_template == null) return;
            _template.gameObject.SetActive(false);
            _pool = new ViewPool<VfxItem>(_template, transform, Capacity);
        }

        public void Play(Sprite[] clip, float fps, Vector3 position, float scale, Color color)
        {
            if (_pool == null || clip == null || clip.Length == 0) return;
            if (!_pool.TryGet(out VfxItem item)) return;
            item.Play(this, clip, fps, position, scale, color, SortingOrder);
        }

        public void Return(VfxItem item) => _pool.Release(item);
    }
}
