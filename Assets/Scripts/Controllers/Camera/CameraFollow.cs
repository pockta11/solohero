using UnityEngine;

/// <summary>
/// 쿼터뷰 카메라 추적. 플레이어 Transform을 _targetTransform에 할당.
/// offset: (0, 7, -5), rotation: (60, 0, 0)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private Vector3 _offsetPos = new Vector3(0f, 7f, -5f);
    [SerializeField] private Vector3 _offsetRotate = new Vector3(60f, 0f, 0f);

    void LateUpdate()
    {
        if (_targetTransform == null)
        {
            // 런타임에 Player 자동 탐색
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) _targetTransform = pc.transform;
            return;
        }
        transform.position = _targetTransform.position + _offsetPos;
        transform.rotation = Quaternion.Euler(_offsetRotate);
    }
}
