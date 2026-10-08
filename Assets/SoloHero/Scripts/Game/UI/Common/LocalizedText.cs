using SoloHero.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Fixed label whose text comes from the Strings table (E7-17). The scene stores only the key. D-140: re-reads its
    /// text when the job terms change (attack becomes spell power after a mage advancement) while it is shown.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key = "";

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Apply();
            }
        }

        private void OnEnable()
        {
            Strings.TermsChanged += Apply;
            Apply();
        }

        private void OnDisable() => Strings.TermsChanged -= Apply;

        private void Apply()
        {
            if (string.IsNullOrEmpty(_key) || !Strings.Has(_key)) return;
            GetComponent<Text>().text = Strings.Get(_key);
        }
    }
}
