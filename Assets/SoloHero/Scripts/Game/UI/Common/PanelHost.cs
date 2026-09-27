using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Bottom tab bar host (E7-04): one panel open at a time, tapping the open tab closes it.
    /// Panels switch instantly (no slide) and play the open / close sound (E8-13).
    /// </summary>
    public sealed class PanelHost : MonoBehaviour
    {
        [SerializeField] private GameObject[] _panels = new GameObject[0];
        [SerializeField] private Image[] _tabBackgrounds = new Image[0];
        [SerializeField] private Color _tabIdle = new Color(0.16f, 0.16f, 0.2f, 0.95f);
        [SerializeField] private Color _tabActive = new Color(0.32f, 0.3f, 0.45f, 1f);

        private int _open = -1;

        public int OpenIndex => _open;

        private void Awake()
        {
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null) _panels[i].SetActive(false);
            }

            RefreshTabs();
        }

        public void Toggle(int index)
        {
            if (index < 0 || index >= _panels.Length) return;
            if (_open == index)
            {
                Close();
                return;
            }

            Sound(SfxId.PanelOpen);

            if (_open >= 0 && _panels[_open] != null) _panels[_open].SetActive(false);
            _open = index;
            if (_panels[_open] != null) _panels[_open].SetActive(true);
            RefreshTabs();
        }

        public void Close()
        {
            if (_open < 0) return;
            Sound(SfxId.PanelClose);
            if (_panels[_open] != null) _panels[_open].SetActive(false);
            _open = -1;
            RefreshTabs();
        }

        private static void Sound(SfxId id)
        {
            AudioService audio = PanelServices.TryGet<AudioService>();
            if (audio != null) audio.Play(id);
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabBackgrounds.Length; i++)
            {
                if (_tabBackgrounds[i] != null) _tabBackgrounds[i].color = i == _open ? _tabActive : _tabIdle;
            }
        }
    }
}
