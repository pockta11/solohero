using UnityEngine;

/// <summary>
/// 3D 오브젝트의 위치를 따라가는 UI 요소. World Space Canvas 자식에 부착.
/// </summary>
public class UIFollow3D : MonoBehaviour
{
    [SerializeField] private Transform _target;

    void Update()
    {
        if (_target == null) return;
        transform.position = Camera.main.WorldToScreenPoint(_target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}
