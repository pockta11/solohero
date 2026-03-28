using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 가상 조이스틱. Canvas 안의 UI 이미지에 부착.
/// Horizontal / Vertical / Direction 프로퍼티로 입력값 제공.
/// </summary>
public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform _background;
    [SerializeField] private RectTransform _handle;

    private Vector2 _inputVec;
    private Canvas  _canvas;

    public float Horizontal  => _inputVec.x;
    public float Vertical    => _inputVec.y;
    public Vector2 Direction => _inputVec;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _background, eventData.position,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPos);

        float radius = _background.sizeDelta.x * 0.5f;
        localPos = Vector2.ClampMagnitude(localPos, radius);

        if (_handle != null)
            _handle.anchoredPosition = localPos;

        _inputVec = localPos / radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _inputVec = Vector2.zero;
        if (_handle != null)
            _handle.anchoredPosition = Vector2.zero;
    }
}
