using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Bottom tab bar host (E7-04): one panel open at a time. Panels switch instantly and play the open sound (E8-13).
    /// D-108: a panel is always open (the area under the battle is never an empty wall), so tapping the open tab
    /// only bounces its icon; the active tab shows a raised plate, a larger icon and a bright label.
    /// </summary>
    public sealed class PanelHost : MonoBehaviour
    {
        [SerializeField] private GameObject[] _panels = new GameObject[0];
        [SerializeField] private Image[] _tabBackgrounds = new Image[0];
        [SerializeField] private Color _tabIdle = new Color(0.16f, 0.16f, 0.2f, 0.95f);
        [SerializeField] private Color _tabActive = new Color(0.32f, 0.3f, 0.45f, 1f);
        [SerializeField] private Sprite _tabIdleSprite;
        [SerializeField] private Sprite _tabActiveSprite;
        [Tooltip("Panel opened when the game starts (-1 = none). Idle RPGs start with the growth panel open.")]
        [SerializeField] private int _openOnStart = 0;
        [Tooltip("D-108: never close the panel area (the open tab only bounces).")]
        [SerializeField] private bool _alwaysOpen = true;
        [SerializeField] private RectTransform[] _tabIcons = new RectTransform[0];
        [SerializeField] private Text[] _tabLabels = new Text[0];
        [SerializeField] private Color _labelIdle = new Color(0.8f, 0.82f, 0.95f, 1f);
        [SerializeField] private Color _labelActive = Color.white;
        [Tooltip("Seconds for a panel to slide up from the tab bar when opened from closed.")]
        [SerializeField] private float _slideInSeconds = 0.18f;
        [Tooltip("Seconds for a panel to slide back down on close.")]
        [SerializeField] private float _slideOutSeconds = 0.12f;

        private int _open = -1;
        private Vector2[] _restPositions = new Vector2[0];
        private RectTransform _sliding;
        private int _slidingIndex = -1;
        private bool _slidingOut;
        private float _slideTime;
        private float _slideDuration;
        private float _slideHeight;

        public int OpenIndex => _open;

        /// <summary>False while panels are pinned open (D-108): the back key goes on to the quit prompt.</summary>
        public bool CanClose => !_alwaysOpen && _open >= 0;

        private void Awake()
        {
            _restPositions = new Vector2[_panels.Length];
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] == null) continue;
                _restPositions[i] = ((RectTransform)_panels[i].transform).anchoredPosition;
                _panels[i].SetActive(false);
            }

            RefreshTabs();
        }

        private void Start()
        {
            if (_openOnStart >= 0 && _openOnStart < _panels.Length && _open < 0) OpenQuiet(_openOnStart);
        }

        /// <summary>Opens without the panel sound (start of the game).</summary>
        private void OpenQuiet(int index)
        {
            _open = index;
            if (_panels[_open] != null) _panels[_open].SetActive(true);
            RefreshTabs();
        }

        public void Toggle(int index)
        {
            if (index < 0 || index >= _panels.Length) return;
            if (_open == index)
            {
                if (_alwaysOpen)
                {
                    Bounce(index);
                    return;
                }

                Close();
                return;
            }

            Sound(SfxId.PanelOpen);

            FinishSlide();
            // Tab to tab swaps at once; only opening from closed slides up (genre convention).
            bool fromClosed = _open < 0;
            if (_open >= 0 && _panels[_open] != null) _panels[_open].SetActive(false);
            _open = index;
            if (_panels[_open] != null)
            {
                _panels[_open].SetActive(true);
                if (fromClosed) BeginSlide(_open, false);
            }

            RefreshTabs();
            Bounce(index);
        }

        public void Close()
        {
            if (_open < 0 || _alwaysOpen) return;
            Sound(SfxId.PanelClose);
            FinishSlide();
            if (_panels[_open] != null) BeginSlide(_open, true);
            _open = -1;
            RefreshTabs();
        }

        private void Update()
        {
            if (_sliding == null) return;
            // Unscaled: panels keep moving while a popup pauses time.
            _slideTime += Time.unscaledDeltaTime;
            float t = _slideDuration > 0f ? Mathf.Clamp01(_slideTime / _slideDuration) : 1f;
            float eased = _slidingOut ? t * t : 1f - (1f - t) * (1f - t);
            float offset = _slidingOut ? -_slideHeight * eased : -_slideHeight * (1f - eased);
            _sliding.anchoredPosition = _restPositions[_slidingIndex] + new Vector2(0f, offset);
            if (t >= 1f) FinishSlide();
        }

        private void BeginSlide(int index, bool slideOut)
        {
            var rect = (RectTransform)_panels[index].transform;
            _sliding = rect;
            _slidingIndex = index;
            _slidingOut = slideOut;
            _slideTime = 0f;
            _slideDuration = slideOut ? _slideOutSeconds : _slideInSeconds;
            _slideHeight = rect.rect.height;
            if (!slideOut) rect.anchoredPosition = _restPositions[index] - new Vector2(0f, _slideHeight);
        }

        /// <summary>Snaps a running slide to its end: rest position, and hidden if it was closing.</summary>
        private void FinishSlide()
        {
            if (_sliding == null) return;
            _sliding.anchoredPosition = _restPositions[_slidingIndex];
            if (_slidingOut) _sliding.gameObject.SetActive(false);
            _sliding = null;
            _slidingIndex = -1;
        }

        private static void Sound(SfxId id)
        {
            AudioService audio = PanelServices.TryGet<AudioService>();
            if (audio != null) audio.Play(id);
        }

        private void Bounce(int index)
        {
            if (index < 0 || index >= _tabIcons.Length || _tabIcons[index] == null) return;
            UiPunch punch = _tabIcons[index].GetComponent<UiPunch>();
            if (punch != null) punch.Play();
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabBackgrounds.Length; i++)
            {
                Image tab = _tabBackgrounds[i];
                if (tab == null) continue;
                bool active = i == _open;
                if (_tabActiveSprite != null && _tabIdleSprite == null)
                {
                    // D-108: only the active tab has a plate.
                    tab.sprite = _tabActiveSprite;
                    tab.color = Color.white;
                    tab.enabled = active;
                }
                else if (_tabIdleSprite != null && _tabActiveSprite != null)
                {
                    tab.sprite = active ? _tabActiveSprite : _tabIdleSprite;
                    tab.color = Color.white;
                }
                else
                {
                    tab.color = active ? _tabActive : _tabIdle;
                }
            }

            for (int i = 0; i < _tabIcons.Length; i++)
            {
                if (_tabIcons[i] == null) continue;
                bool active = i == _open;
                // The icon's UiPunch owns localScale, so the active size goes through sizeDelta (64 / 80 = 4x / 5x).
                _tabIcons[i].anchoredPosition = new Vector2(0f, active ? 10f : 0f);
                _tabIcons[i].sizeDelta = active ? new Vector2(80f, 80f) : new Vector2(64f, 64f);
            }

            for (int i = 0; i < _tabLabels.Length; i++)
            {
                if (_tabLabels[i] == null) continue;
                _tabLabels[i].color = i == _open ? _labelActive : _labelIdle;
            }
        }
    }
}
