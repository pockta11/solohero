using SoloHero.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.Boot
{
    public sealed class LoadFailBanner : MonoBehaviour
    {
        public const string MessageKey = "boot.load_failed";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Show()
        {
            Text text = GetComponent<Text>();
            if (text == null)
                text = GetComponentInChildren<Text>(true);
            if (text == null)
                return;

            text.text = Strings.Get(MessageKey);
            text.gameObject.SetActive(true);
            text.enabled = true;
        }
    }
}
