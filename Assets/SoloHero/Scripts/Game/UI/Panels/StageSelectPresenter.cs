using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Stage select sheet (E3-09, GDD M-5): opened from the stage label, one chapter at a time. Any cleared normal
    /// stage can be farmed; locked stages do not react; boss stages show "boss" and are not farmable (D-072).
    /// </summary>
    public sealed class StageSelectPresenter : MonoBehaviour
    {
        // Tints over the skinned button sprite: white keeps its colour, the current stage glows gold.
        private static readonly Color Selectable = Color.white;
        private static readonly Color Current = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color Disabled = new Color(0.55f, 0.55f, 0.6f, 1f);

        [SerializeField] private GameObject _popup;
        [SerializeField] private Text _chapterText;
        [SerializeField] private Button[] _stageButtons = new Button[10];
        [SerializeField] private Text[] _stageLabels = new Text[10];
        [SerializeField] private Button _prev;
        [SerializeField] private Button _next;
        [SerializeField] private CombatSession _session;

        private BalanceValues _balance;
        private SaveDataV2 _save;
        private FarmingStageService _farming;
        private int _chapter = 1;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void Open()
        {
            _balance = PanelServices.TryGet<BalanceValues>() ?? new BalanceValues();
            _save = PanelServices.TryGet<SaveDataV2>();
            _farming = new FarmingStageService(PanelServices.TryGet<Core.Growth.ISaveRequester>());
            if (_save == null) return;
            int current = _session != null && _session.Runner != null ? _session.Runner.GlobalStage : _save.farmingStage;
            StageIndex.FromGlobal(current, _balance.STAGES_PER_CHAPTER, out _chapter, out _);
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void PrevChapter() => Step(-1);

        public void NextChapter() => Step(1);

        public void Pick(int index)
        {
            if (_save == null || _session == null || _session.Runner == null) return;
            int g = (_chapter - 1) * _balance.STAGES_PER_CHAPTER + index + 1;
            if (!_session.Runner.FarmAt(g)) return;
            _farming.TrySet(_save, g);
            _save.retreatMode = true;
            Close();
        }

        private void Step(int delta)
        {
            int next = _chapter + delta;
            if (next < 1 || next > MaxChapter()) return;
            _chapter = next;
            Refresh();
        }

        /// <summary>The frontier's chapter: farther chapters have nothing to pick yet.</summary>
        private int MaxChapter()
        {
            StageIndex.FromGlobal(_save.highestStage + 1, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
            return chapter;
        }

        private void Refresh()
        {
            if (_save == null) return;
            if (_chapterText != null) _chapterText.text = Strings.Format("stage.chapter", _chapter);
            if (_prev != null) _prev.interactable = _chapter > 1;
            if (_next != null) _next.interactable = _chapter < MaxChapter();
            int current = _session != null && _session.Runner != null ? _session.Runner.GlobalStage : _save.farmingStage;

            for (int i = 0; i < _stageButtons.Length; i++)
            {
                int g = (_chapter - 1) * _balance.STAGES_PER_CHAPTER + i + 1;
                bool boss = StageIndex.IsBoss(g, _balance.STAGES_PER_CHAPTER);
                bool unlocked = g <= _save.highestStage;
                bool pickable = unlocked && !boss;
                Button button = _stageButtons[i];
                if (button != null)
                {
                    button.interactable = pickable;
                    button.image.color = g == current ? Current : pickable ? Selectable : Disabled;
                }

                if (i < _stageLabels.Length && _stageLabels[i] != null)
                {
                    _stageLabels[i].text = boss ? Strings.Get("stage.boss") : _chapter + "-" + (i + 1);
                    _stageLabels[i].color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
            }
        }
    }
}
