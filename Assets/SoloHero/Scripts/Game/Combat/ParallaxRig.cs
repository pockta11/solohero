using UnityEngine;

namespace SoloHero.Game.Combat
{
    public sealed class ParallaxRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _far;
        [SerializeField] private Transform _mid;
        [SerializeField] private Transform _near;
        [SerializeField] private Transform _ground;

        // 1 sticks to the camera. 0 stays in the world. Far layers use a high follow so they scroll slowly.
        [SerializeField] private float _farFactor = 0.85f;
        [SerializeField] private float _midFactor = 0.60f;
        [SerializeField] private float _nearFactor = 0.25f;
        [SerializeField] private float _groundFactor = 0f;

        private float _lastCameraX;

        private void OnEnable()
        {
            if (_camera != null)
                _lastCameraX = _camera.transform.position.x;
        }

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            float cameraX = _camera.transform.position.x;
            float delta = cameraX - _lastCameraX;
            _lastCameraX = cameraX;

            Shift(_far, delta * _farFactor);
            Shift(_mid, delta * _midFactor);
            Shift(_near, delta * _nearFactor);
            Shift(_ground, delta * _groundFactor);
        }

        private static void Shift(Transform layer, float deltaX)
        {
            if (layer == null)
                return;

            Vector3 position = layer.position;
            position.x += deltaX;
            layer.position = position;
        }
    }
}
