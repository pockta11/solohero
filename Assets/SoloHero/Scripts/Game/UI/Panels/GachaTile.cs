using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-172: one small result of a bulk summon (more than ten pulls) - the grade frame, the item's icon and a NEW mark.
    /// The reveal lays a hundred of these out as a grid instead of the ten turning cards.
    /// </summary>
    public sealed class GachaTile : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _newMark;

        public RectTransform Rect => (RectTransform)transform;

        public void Show(Sprite frame, Sprite icon, float iconScale, bool isNew)
        {
            gameObject.SetActive(true);
            if (_frame != null && frame != null) _frame.sprite = frame;
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
                _icon.rectTransform.localScale = Vector3.one * iconScale;
            }

            if (_newMark != null) _newMark.SetActive(isNew);
            Rect.localScale = Vector3.zero;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
