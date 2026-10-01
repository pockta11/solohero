using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Drives the SoloHero/SpriteFlash shader's white overlay on one SpriteRenderer (D-096). The property block is
    /// read back first so the sprite texture the renderer sets itself is kept. Writes only when the value changes.
    /// </summary>
    public sealed class SpriteFlash
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        private readonly SpriteRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _shown = -1f;

        public SpriteFlash(SpriteRenderer renderer)
        {
            _renderer = renderer;
        }

        public void Set(float amount)
        {
            if (_renderer == null) return;
            amount = Mathf.Clamp01(amount);
            if (Mathf.Approximately(amount, _shown)) return;
            _shown = amount;
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(FlashId, amount);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
