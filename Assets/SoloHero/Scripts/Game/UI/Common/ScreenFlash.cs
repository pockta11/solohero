using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Full-screen colour flash for big skills (D-078): an Image over the battle that fades out on unscaled time.
    /// Raycasts are off, so it never blocks taps. CombatFx skips it in low-effect mode.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenFlash : MonoBehaviour
    {
        private Image _image;
        private Color _color;
        private float _seconds;
        private float _age;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.raycastTarget = false;
            _image.enabled = false;
        }

        public void Flash(Color color, float seconds)
        {
            if (_image == null || seconds <= 0f) return;
            _color = color;
            _seconds = seconds;
            _age = 0f;
            _image.color = color;
            _image.enabled = true;
        }

        private void Update()
        {
            if (_image == null || !_image.enabled) return;
            _age += Time.unscaledDeltaTime;
            float t = _age / _seconds;
            if (t >= 1f)
            {
                _image.enabled = false;
                return;
            }

            Color c = _color;
            c.a = _color.a * (1f - t) * (1f - t);
            _image.color = c;
        }
    }
}
