using UnityEngine;

namespace SoloHero.Game.Pooling
{
    /// <summary>
    /// World-space VFX pool (E8-08, architecture D11). Pre-warmed at scene start from a disabled template; when every
    /// effect is busy the new one is skipped (no runtime Instantiate). Effects draw above the characters.
    /// D-146: an effect can draw additively (glows, light pillars) and at its own sorting order (the cast circle lies
    /// under the characters); the pool holds more effects for the layered skills.
    /// </summary>
    public sealed class VfxPool : MonoBehaviour
    {
        public const int Capacity = 72;
        public const int SortingOrder = 250;

        [SerializeField] private VfxItem _template;
        [Tooltip("D-146: SoloHero/SpriteAdditive material for glows and light.")]
        [SerializeField] private Material _additive;

        private ViewPool<VfxItem> _pool;

        public int ActiveCount => _pool != null ? _pool.CountActive : 0;

        private void Awake()
        {
            if (_template == null) return;
            _template.gameObject.SetActive(false);
            _pool = new ViewPool<VfxItem>(_template, transform, Capacity);
        }

        public void Play(Sprite[] clip, float fps, Vector3 position, float scale, Color color) =>
            Play(clip, fps, position, scale, color, SortingOrder, false);

        /// <param name="additive">Draw with the additive material (adds light) instead of the sprite's own.</param>
        public void Play(Sprite[] clip, float fps, Vector3 position, float scale, Color color, int sortingOrder, bool additive)
        {
            if (_pool == null || clip == null || clip.Length == 0) return;
            if (!_pool.TryGet(out VfxItem item)) return;
            item.Play(this, clip, fps, position, scale, color, sortingOrder, additive ? _additive : null);
        }

        public void Return(VfxItem item) => _pool.Release(item);
    }
}
