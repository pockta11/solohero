using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>One result card of the gacha reveal (E5-11): a back, and a face tinted by grade with its texts.</summary>
    public sealed class GachaCard : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Text _grade;
        [SerializeField] private Text _slot;
        [SerializeField] private Text _note;
        [SerializeField] private Sprite _back;
        [SerializeField] private Sprite _face;

        public RectTransform Rect => (RectTransform)transform;

        public void ShowBack()
        {
            gameObject.SetActive(true);
            _image.sprite = _back;
            _image.color = Color.white;
            SetTexts(false);
            Rect.localScale = Vector3.one;
        }

        public void ShowFace(Color gradeColor, string grade, string slot, string note)
        {
            _image.sprite = _face;
            _image.color = Color.Lerp(new Color(0.1f, 0.1f, 0.14f, 1f), gradeColor, 0.45f);
            _grade.text = grade;
            _grade.color = Color.Lerp(gradeColor, Color.white, 0.35f);
            _slot.text = slot;
            _note.text = note;
            SetTexts(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void SetTexts(bool on)
        {
            if (_grade != null) _grade.enabled = on;
            if (_slot != null) _slot.enabled = on;
            if (_note != null) _note.enabled = on;
        }
    }
}
