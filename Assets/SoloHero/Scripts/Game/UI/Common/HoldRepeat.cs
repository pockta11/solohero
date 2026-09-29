using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Press and hold to repeat a button (genre convention for upgrade buttons): after a short delay the click fires
    /// again and again, faster over time, while the button stays interactable. The release after a repeat does not
    /// count as one more click. Works with <see cref="TapGuardButton"/>, which re-enables the button every frame.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class HoldRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float _delay = 0.45f;
        [SerializeField] private float _firstInterval = 0.16f;
        [SerializeField] private float _minInterval = 0.05f;
        [SerializeField] private float _speedUp = 0.85f;

        private Button _button;
        private bool _held;
        private bool _repeated;
        private float _timer;
        private float _interval;

        private void Awake() => _button = GetComponent<Button>();

        private void OnDisable() => _held = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _held = true;
            _repeated = false;
            _timer = _delay;
            _interval = _firstInterval;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_repeated) eventData.eligibleForClick = false;
            _held = false;
        }

        public void OnPointerExit(PointerEventData eventData) => _held = false;

        private void Update()
        {
            if (!_held || !_button.interactable) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = _interval;
            _interval = Mathf.Max(_minInterval, _interval * _speedUp);
            _repeated = true;
            _button.onClick.Invoke();
        }
    }
}
