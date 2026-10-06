using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// One result card of the gacha reveal (E5-11): a back, and a face tinted by grade with its texts.
    /// D-108: the back carries a separate star emblem (so the 9-sliced card never stretches it).
    /// </summary>
    public sealed class GachaCard : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _grade;
        [SerializeField] private Text _slot;
        [SerializeField] private Text _note;
        [SerializeField] private Sprite _back;
        [SerializeField] private Sprite _face;
        [SerializeField] private GameObject _emblem;

        public RectTransform Rect => (RectTransform)transform;

        public void ShowBack()
        {
            gameObject.SetActive(true);
            _image.sprite = _back;
            _image.color = Color.white;
            if (_emblem != null) _emblem.SetActive(true);
            SetTexts(false);
            Rect.localScale = Vector3.one;
        }

        public void ShowFace(Color gradeColor, string grade, string slot, string note, Sprite icon)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            _image.sprite = _face;
            _image.color = Color.Lerp(Color.white, gradeColor, 0.7f);
            if (_emblem != null) _emblem.SetActive(false);
            _grade.text = grade;
            _grade.color = Color.white;
            _slot.text = slot;
            _note.text = note;
            SetTexts(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void SetTexts(bool on)
        {
            if (_grade != null) _grade.enabled = on;
            if (_icon != null && !on) _icon.enabled = false;
            if (_slot != null) _slot.enabled = on;
            if (_note != null) _note.enabled = on;
        }
    }
}
