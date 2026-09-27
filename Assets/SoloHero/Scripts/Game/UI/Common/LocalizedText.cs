using SoloHero.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>Fixed label whose text comes from the Strings table (E7-17). The scene stores only the key.</summary>
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

        private void OnEnable() => Apply();

        private void Apply()
        {
            if (string.IsNullOrEmpty(_key) || !Strings.Has(_key)) return;
            GetComponent<Text>().text = Strings.Get(_key);
        }
    }
}
