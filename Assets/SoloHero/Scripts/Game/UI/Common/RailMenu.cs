using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Right-side menu rail on the battle screen (D-089): a menu button folds a column of shortcuts (settings, stage,
    /// credits) in and out. Starts folded so the battle stays clear; picking an item folds it again.
    /// </summary>
    public sealed class RailMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _content;

        public bool IsOpen => _content != null && _content.activeSelf;

        private void Awake()
        {
            if (_content != null) _content.SetActive(false);
        }

        public void Toggle()
        {
            if (_content == null) return;
            _content.SetActive(!_content.activeSelf);
            AudioService audio = PanelServices.TryGet<AudioService>();
            if (audio != null) audio.Play(SfxId.Tap);
        }

        public void Fold()
        {
            if (_content != null) _content.SetActive(false);
        }
    }
}
