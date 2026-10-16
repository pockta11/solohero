using UnityEngine;
using UnityEngine.EventSystems;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Swallows pointer clicks so they stop here instead of bubbling to a parent handler. D-108 popups close on a tap
    /// outside their window through a Button on the full-screen dim; uGUI hands a click to the nearest ancestor that
    /// handles it, so a tap on a plain part of the window (a card, a label, the frame) used to reach that Button and
    /// close the popup. The window box carries this component, and only its own buttons react.
    /// </summary>
    public sealed class TapBlocker : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
        }
    }
}
