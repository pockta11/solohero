using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.Boot
{
    public sealed class LoadFailBanner : MonoBehaviour
    {
        public const string Message = "Could not load the cloud save. Local data is in use.";

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

            text.text = Message;
            text.gameObject.SetActive(true);
            text.enabled = true;
        }
    }
}
