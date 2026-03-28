using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라 시야를 가리는 오브젝트에 부착. 자동으로 투명/불투명 전환.
/// </summary>
public class TransparentObject : MonoBehaviour
{
    public bool IsTransparent { get; private set; }

    private MeshRenderer[] _renderers;
    private Coroutine       _fadeCoroutine;
    private Coroutine       _timerCoroutine;

    private const float ALPHA_MIN    = 0.25f;
    private const float HOLD_TIME    = 0.5f;

    void Awake()
    {
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }

    public void BecomeTransparent()
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        if (!IsTransparent)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            SetMode(transparent: true);
            IsTransparent = true;
            _fadeCoroutine = StartCoroutine(FadeOut());
        }
        else
        {
            _timerCoroutine = StartCoroutine(HoldTimer());
        }
    }

    IEnumerator FadeOut()
    {
        bool done = false;
        while (!done)
        {
            done = true;
            foreach (var r in _renderers)
                foreach (var m in r.materials)
                {
                    if (m.color.a > ALPHA_MIN)
                    {
                        done = false;
                        Color c = m.color; c.a -= Time.deltaTime; m.color = c;
                    }
                }
            yield return null;
        }
        _timerCoroutine = StartCoroutine(HoldTimer());
    }

    IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(HOLD_TIME);
        SetMode(transparent: false);
        IsTransparent = false;
        yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        bool done = false;
        while (!done)
        {
            done = true;
            foreach (var r in _renderers)
                foreach (var m in r.materials)
                {
                    if (m.color.a < 1f)
                    {
                        done = false;
                        Color c = m.color; c.a += Time.deltaTime; m.color = c;
                    }
                }
            yield return null;
        }
    }

    void SetMode(bool transparent)
    {
        float mode = transparent ? 3f : 0f;
        int queue   = transparent ? 3000 : -1;
        foreach (var r in _renderers)
            foreach (var m in r.materials)
            {
                m.SetFloat("_Mode", mode);
                m.SetInt("_SrcBlend", transparent
                    ? (int)UnityEngine.Rendering.BlendMode.SrcAlpha
                    : (int)UnityEngine.Rendering.BlendMode.One);
                m.SetInt("_DstBlend", transparent
                    ? (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                    : (int)UnityEngine.Rendering.BlendMode.Zero);
                m.SetInt("_ZWrite", transparent ? 0 : 1);
                if (transparent) m.EnableKeyword("_ALPHABLEND_ON");
                else             m.DisableKeyword("_ALPHABLEND_ON");
                m.renderQueue = queue;
            }
    }
}
