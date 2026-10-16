using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-178: lock veils over the rail menu items of stage-gated systems (dungeon, tower, pet, raid). A locked item is
    /// dimmed under a lock and its caption says the stage that opens it; the item still opens its popup, which repeats
    /// the rule. Lives on the folded column, so it refreshes when the menu opens and whenever the highest stage moves.
    /// </summary>
    public sealed class RailLocks : MonoBehaviour
    {
        [Tooltip("Per gated item: the veil (lock icon and caption) shown while locked.")]
        [SerializeField] private GameObject[] _veils = new GameObject[0];
        [Tooltip("Per gated item: the caption inside the veil that names the opening stage.")]
        [SerializeField] private Text[] _lockCaptions = new Text[0];
        [Tooltip("Per gated item: the item's own caption, hidden while locked.")]
        [SerializeField] private Text[] _captions = new Text[0];
        [Tooltip("Per gated item: the SystemGate value it waits for.")]
        [SerializeField] private int[] _gates = new int[0];

        private BalanceValues _balance;
        private SaveDataV2 _save;
        private int _shownHighest = -1;

        private void OnEnable()
        {
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _shownHighest = -1;
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save != null && _save.highestStage != _shownHighest) Refresh();
        }

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownHighest = _save.highestStage;
            for (int i = 0; i < _veils.Length; i++)
            {
                var gate = (SystemGate)(i < _gates.Length ? _gates[i] : 0);
                bool open = SystemUnlocks.IsOpen(_balance, _save, gate);
                if (_veils[i] != null && _veils[i].activeSelf == open) _veils[i].SetActive(!open);
                if (i < _captions.Length && _captions[i] != null) _captions[i].enabled = open;
                if (open || i >= _lockCaptions.Length || _lockCaptions[i] == null) continue;
                StageIndex.FromGlobal(SystemUnlocks.StageOf(_balance, gate), _balance.STAGES_PER_CHAPTER, out int chapter, out int stage);
                _lockCaptions[i].text = Strings.Format("rail.locked", chapter + "-" + stage);
            }
        }
    }
}
