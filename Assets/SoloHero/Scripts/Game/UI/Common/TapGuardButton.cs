using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Spend button guard (E7-15): the first tap disables the button, runs the handler once, and the button
    /// comes back on the next frame. Presenters decide affordability through <see cref="SetAvailable"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class TapGuardButton : MonoBehaviour
    {
        [SerializeField] private UnityEvent _onTap = new UnityEvent();

        private Button _button;
        private bool _available = true;
        private bool _busy;

        public UnityEvent OnTap => _onTap;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        private void LateUpdate()
        {
            if (!_busy) return;
            _busy = false;
            _button.interactable = _available;
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            if (_button == null) _button = GetComponent<Button>();
            if (!_busy) _button.interactable = available;
        }

        private void HandleClick()
        {
            if (_busy) return;
            _busy = true;
            _button.interactable = false;
            _onTap.Invoke();
        }
    }
}
