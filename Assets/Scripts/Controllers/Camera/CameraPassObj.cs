using UnityEngine;

/// <summary>
/// 카메라와 플레이어 사이의 오브젝트를 반투명 처리.
/// </summary>
public class CameraPassObj : MonoBehaviour
{
    [SerializeField] Transform _target;
    private Renderer _obstacleRenderer;

    void LateUpdate()
    {
        if (_target == null) return;

        float distance = Vector3.Distance(transform.position, _target.position);
        Vector3 direction = (_target.position - transform.position).normalized;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance))
        {
            _obstacleRenderer = hit.transform.GetComponentInChildren<Renderer>();
            if (_obstacleRenderer != null)
            {
                foreach (var mat in _obstacleRenderer.materials)
                    SetTransparent(mat, 0.2f);
            }
        }
    }

    static void SetTransparent(Material mat, float alpha)
    {
        mat.SetFloat("_Mode", 3f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
    }
}
