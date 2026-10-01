using UnityEngine;

namespace SoloHero.Game.View
{
    /// <summary>
    /// Integer pixel zoom for the orthographic camera (D-096). Replaces the 2D Pixel Perfect package component, whose
    /// camera callbacks never run under URP (the size stayed fixed at 7.5, so 19.5:9 / 20:9 phones drew one art pixel
    /// as 4.875 screen pixels and scrolling shimmered). The zoom is the largest whole number of screen pixels per art
    /// pixel that fits the 270 px reference width; taller screens show more world vertically, never stretch.
    /// Rendering stays at full screen resolution, so the camera and parallax scroll by screen pixels, not art pixels.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PixelCameraFit : MonoBehaviour
    {
        [SerializeField] private int _referenceWidth = 270;
        [SerializeField] private int _pixelsPerUnit = 32;

        private Camera _camera;
        private int _width;
        private int _height;

        /// <summary>Screen pixels per art pixel.</summary>
        public int Zoom { get; private set; } = 1;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            Apply();
        }

        private void LateUpdate()
        {
            if (Screen.width != _width || Screen.height != _height) Apply();
        }

        private void Apply()
        {
            _width = Screen.width;
            _height = Screen.height;
            Zoom = Mathf.Max(1, _width / Mathf.Max(1, _referenceWidth));
            _camera.orthographic = true;
            _camera.orthographicSize = _height / (2f * Zoom * _pixelsPerUnit);
        }
    }
}
