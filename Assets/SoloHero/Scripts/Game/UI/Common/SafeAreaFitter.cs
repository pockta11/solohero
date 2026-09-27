using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Fits its RectTransform to <see cref="Screen.safeArea"/> (E7-02, ported from Legacy SafeAreaAdjuster) so no
    /// control sits under a notch, punch hole or the gesture bar. Full-screen dims stay outside this container.
    /// Re-applies when the safe area changes (split screen, cutout mode switch).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied;
        private Vector2Int _screen;

        private void Awake() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _screen.x || Screen.height != _screen.y) Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
