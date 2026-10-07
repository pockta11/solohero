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
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        private readonly SpriteRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _shown = -1f;
        private Color _flashColor = new Color(-1f, 0f, 0f, 0f);
        private Color _outline = new Color(-1f, 0f, 0f, 0f);

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

        /// <summary>D-112: the colour the sprite flashes toward (the hero flashes red when hurt).</summary>
        public void SetFlashColor(Color color)
        {
            if (_renderer == null || color == _flashColor) return;
            _flashColor = color;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(FlashColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>D-112: the silhouette outline colour (bosses wear a crimson one).</summary>
        public void SetOutline(Color color)
        {
            if (_renderer == null || color == _outline) return;
            _outline = color;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(OutlineColorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
