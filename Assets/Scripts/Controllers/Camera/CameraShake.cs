using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라 흔들림 효과. Camera 오브젝트에 부착.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public float shakeTime   = 0.3f;
    public float shakeSpeed  = 10f;
    public float shakeAmount = 0.1f;

    public void ShakeCam()
    {
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        Vector3 originPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeTime)
        {
            Vector3 randPoint = originPos + Random.insideUnitSphere * shakeAmount;
            transform.localPosition = Vector3.Lerp(transform.localPosition, randPoint, Time.deltaTime * shakeSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originPos;
    }
}
