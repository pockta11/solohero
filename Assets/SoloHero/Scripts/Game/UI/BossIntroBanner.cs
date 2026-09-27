using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI
{
    /// <summary>
    /// Boss entrance (GDD boss rule 12, E7-11): while the stage is in BossIntro the screen dims and a banner shows the
    /// chapter's boss name; camera shake and the intro sound come from CombatFx. Purely visual - the 1.5 s intro timer
    /// lives in StageRunner. Chapters past the named ones reuse the names cyclically like the themes.
    /// </summary>
    public sealed class BossIntroBanner : MonoBehaviour
    {
        private static readonly string[] BossNameKeys = { "boss.name.1", "boss.name.2", "boss.name.3", "boss.name.4", "boss.name.5" };
        private const float FadeSeconds = 0.25f;

        [SerializeField] private CombatSession _session;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Text _chapterText;
        [SerializeField] private Text _nameText;

        private BalanceValues _balance;
        private float _alpha;
        private int _shownStage = -1;

        private void Awake()
        {
            _balance = PanelServices.TryGet<BalanceValues>() ?? new BalanceValues();
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }
        }

        private void LateUpdate()
        {
            StageRunner runner = _session != null ? _session.Runner : null;
            bool show = runner != null && runner.State == StageState.BossIntro;
            if (show && runner.GlobalStage != _shownStage)
            {
                _shownStage = runner.GlobalStage;
                StageIndex.FromGlobal(runner.GlobalStage, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
                string nameKey = BossNameKeys[(chapter - 1) % BossNameKeys.Length];
                if (_chapterText != null) _chapterText.text = Strings.Format("boss.banner", chapter);
                if (_nameText != null) _nameText.text = Strings.Get(nameKey);
            }

            if (!show) _shownStage = -1;
            float target = show ? 1f : 0f;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime / FadeSeconds);
            if (_group != null && _group.alpha != _alpha) _group.alpha = _alpha;
        }
    }
}
