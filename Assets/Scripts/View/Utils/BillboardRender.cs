using UnityEngine;

/// <summary>
/// 빌보드 처리할 Canvas에 부착. 항상 카메라를 향하도록.
/// </summary>
public class BillboardRender : MonoBehaviour
{
    Transform _mainCam;

    void Start()
    {
        _mainCam = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + _mainCam.rotation * Vector3.forward,
                         _mainCam.rotation * Vector3.up);
    }
}
