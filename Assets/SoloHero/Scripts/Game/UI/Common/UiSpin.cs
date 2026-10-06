using UnityEngine;

namespace SoloHero.Game.UI.Common
{
    /// <summary>D-108: slow constant rotation for decorative UI (the summon circle), on unscaled time.</summary>
    public sealed class UiSpin : MonoBehaviour
    {
        [SerializeField] private float _degreesPerSecond = -12f;

        private void Update()
        {
            transform.Rotate(0f, 0f, _degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
