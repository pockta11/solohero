using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Pooling
{
    /// <summary>One pooled world-space effect: plays a clip once and hands itself back to its pool.</summary>
    [RequireComponent(typeof(SpriteFlipbook))]
    public sealed class VfxItem : MonoBehaviour
    {
        private SpriteFlipbook _book;
        private VfxPool _owner;
        private Material _default;

        /// <param name="material">Null keeps the template's sprite material; D-146 glows pass the additive one.</param>
        public void Play(VfxPool owner, Sprite[] clip, float fps, Vector3 position, float scale, Color color, int sortingOrder,
            Material material = null)
        {
            if (_book == null) _book = GetComponent<SpriteFlipbook>();
            if (_default == null) _default = _book.Renderer.sharedMaterial;
            Material wanted = material != null ? material : _default;
            if (_book.Renderer.sharedMaterial != wanted) _book.Renderer.sharedMaterial = wanted;
            _owner = owner;
            transform.position = position;
            transform.localScale = new Vector3(scale, scale, 1f);
            _book.Renderer.color = color;
            _book.Renderer.sortingOrder = sortingOrder;
            _book.Play(clip, fps, loop: false, restart: true);
        }

        private void LateUpdate()
        {
            if (_book != null && _book.Finished && _owner != null) _owner.Return(this);
        }
    }
}
