using UnityEngine;

/// <summary>
/// 죽을 때 래그돌 Prefab으로 교체. EnemyController에서 호출.
/// </summary>
public class RagdollEvent : MonoBehaviour
{
    [SerializeField] private GameObject _ragdollPrefab;

    public void Replace()
    {
        if (_ragdollPrefab == null)
        {
            Destroy(gameObject);
            return;
        }

        var ragdoll = Instantiate(_ragdollPrefab, transform.position, transform.rotation);
        ragdoll.SetActive(false);

        // 같은 이름의 본 계층 구조 복사
        CopyHierarchy(transform, ragdoll.transform);

        ragdoll.SetActive(true);
        Destroy(gameObject);
    }

    static void CopyHierarchy(Transform src, Transform dst)
    {
        if (src.childCount == 0 || dst.childCount == 0) return;

        for (int i = 0; i < src.childCount && i < dst.childCount; i++)
        {
            var sc = src.GetChild(i);
            var dc = dst.GetChild(i);

            if (sc.name == dc.name)
            {
                dc.position = sc.position;
                dc.rotation = sc.rotation;
                CopyHierarchy(sc, dc);
            }
        }
    }
}
