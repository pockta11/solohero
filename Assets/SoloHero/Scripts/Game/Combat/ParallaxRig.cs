using SoloHero.Core.Config;
using SoloHero.Core.Stage;
using SoloHero.Game.View;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    /// <summary>
    /// Chapter background (E2-02, E3-11, E3-12, E8-05): tiled parallax layers that loop forever. Each layer follows
    /// the camera by its theme factor; the ground strip stays in the world. The theme switches with the chapter
    /// and chapters past the theme count cycle. Layer renderers are created by the scene builder, never at runtime.
    /// Runs after the combat view (D-096) so the layers read this frame's camera position, not last frame's - reading
    /// a stale position made the background lag a frame behind and judder.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ParallaxRig : MonoBehaviour
    {
        private const float TileCopies = 4f;
        private const int LayerSortingBase = -40;
        private const float GroundCopies = 8f;

        /// <summary>D-091: world Y of the floor's far edge (the characters' ground line is 0).</summary>
        public const float FloorTop = 1.25f;

        [SerializeField] private Camera _camera;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ChapterThemeSet _themes;
        [SerializeField] private SpriteRenderer[] _layers = new SpriteRenderer[0];
        [SerializeField] private SpriteRenderer _ground;

        // Every theme's art covers the sky, so the camera colour only shows under the ground strip.
        [SerializeField] private Color _belowGround = new Color(0.1f, 0.08f, 0.11f, 1f);

        private BalanceValues _balance = new BalanceValues();
        private ChapterTheme _shown;
        private float[] _tileWidths = new float[0];
        private float _groundWidth = 1f;

        private void Awake()
        {
            try
            {
                _balance = Core.Common.Services.Get<BalanceValues>();
            }
            catch (System.InvalidOperationException)
            {
                _balance = new BalanceValues();
            }

            _tileWidths = new float[_layers.Length];
            if (_ground != null && _ground.sprite != null) _groundWidth = _ground.sprite.bounds.size.x * _ground.transform.localScale.x;
            ApplyTheme(ThemeFor(1));
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            StageRunner runner = _session != null ? _session.Runner : null;
            if (runner != null)
            {
                StageIndex.FromGlobal(runner.GlobalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
                ChapterTheme theme = ThemeFor(chapter);
                if (theme != _shown) ApplyTheme(theme);
            }

            float cameraX = _camera.transform.position.x;
            for (int i = 0; i < _layers.Length; i++)
            {
                SpriteRenderer layer = _layers[i];
                if (layer == null || !layer.enabled || _tileWidths[i] <= 0f) continue;
                float follow = _shown != null && i < _shown.follow.Length ? _shown.follow[i] : 0f;
                float x = cameraX - Mathf.Repeat(cameraX * (1f - follow), _tileWidths[i]);
                layer.transform.position = new Vector3(x, 0f, 0f);
            }

            if (_ground != null)
            {
                Vector3 p = _ground.transform.position;
                p.x = cameraX - Mathf.Repeat(cameraX, _groundWidth);
                _ground.transform.position = p;
            }
        }

        /// <summary>The chapter's floor plane: far edge at FloorTop, tiled wide enough to cover the view.</summary>
        private void ApplyFloor(Sprite floor)
        {
            if (_ground == null || floor == null) return;
            _ground.sprite = floor;
            _ground.drawMode = SpriteDrawMode.Tiled;
            Vector2 size = floor.bounds.size;
            _ground.size = new Vector2(size.x * GroundCopies, size.y);
            _groundWidth = size.x * _ground.transform.localScale.x;
            Vector3 p = _ground.transform.position;
            p.y = FloorTop - size.y * _ground.transform.localScale.y;
            _ground.transform.position = p;
        }

        private ChapterTheme ThemeFor(int chapter)
        {
            if (_themes == null || _themes.themes.Length == 0) return null;
            return _themes.themes[StageIndex.ThemeIndex(chapter, _themes.themes.Length)];
        }

        private void ApplyTheme(ChapterTheme theme)
        {
            _shown = theme;
            if (theme == null) return;
            if (_camera != null) _camera.backgroundColor = _belowGround;
            ApplyFloor(theme.floor);

            for (int i = 0; i < _layers.Length; i++)
            {
                SpriteRenderer layer = _layers[i];
                if (layer == null) continue;
                bool used = i < theme.layers.Length && theme.layers[i] != null;
                layer.enabled = used;
                _tileWidths[i] = 0f;
                if (!used) continue;

                Sprite sprite = theme.layers[i];
                layer.sprite = sprite;
                layer.drawMode = SpriteDrawMode.Tiled;
                layer.sortingOrder = LayerSortingBase + i;
                layer.transform.localScale = new Vector3(theme.pixelScale, theme.pixelScale, 1f);
                Vector2 size = sprite.bounds.size;
                layer.size = new Vector2(size.x * TileCopies, size.y);
                _tileWidths[i] = size.x * theme.pixelScale;
            }
        }
    }
}
