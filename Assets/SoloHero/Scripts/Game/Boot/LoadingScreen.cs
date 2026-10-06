using SoloHero.Core.Common;
using SoloHero.Game.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SoloHero.Game.Boot
{
    /// <summary>
    /// D-108 loading screen: the title over the meadow with the hero, a progress bar and a tip, shown from the first
    /// boot frame until the battle is ready, then faded out. It lives on its own overlay canvas above the battle HUD and
    /// under the offline reward popup, and survives the scene change (BootSequence loads the game scene asynchronously
    /// and reports progress). Unscaled time; the bar eases toward the reported target so it never jumps backward.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _status;
        [SerializeField] private string _statusKey = "loading.status";
        [SerializeField] private Text _tip;
        [SerializeField] private string[] _tipKeys = new string[0];
        [SerializeField] private float _fadeSeconds = 0.4f;

        private float _shown;
        private float _target = 0.08f;
        private bool _ready;
        private float _fade = -1f;
        private float _poll;
        private bool _texts;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (_group != null) _group.alpha = 1f;
        }

        /// <summary>Raises the bar's target (0..1); lower values are ignored.</summary>
        public void SetProgress(float target) => _target = Mathf.Max(_target, Mathf.Clamp01(target));

        /// <summary>The labels fill in once BootSequence has loaded the Strings table (its Start may run after ours).</summary>
        private void ShowTexts()
        {
            _texts = true;
            if (_status != null) _status.text = Strings.Get(_statusKey);
            if (_tip == null || _tipKeys.Length == 0) return;
            string key = _tipKeys[Random.Range(0, _tipKeys.Length)];
            _tip.text = Strings.Get(key);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!_texts && Strings.Count > 0) ShowTexts();
            if (!_ready)
            {
                // Creep a little past the last report so a slow step still shows life, but never reach the end.
                _target = Mathf.Min(0.95f, _target + dt * 0.02f);
                _poll -= dt;
                if (_poll <= 0f)
                {
                    _poll = 0.2f;
                    _ready = BattleReady();
                    if (_ready) _target = 1f;
                }
            }

            _shown = Mathf.MoveTowards(_shown, _target, dt * (_ready ? 2.5f : 0.9f));
            if (_fill != null) _fill.anchorMax = new Vector2(Mathf.Max(0.04f, _shown), 1f);

            if (_ready && _shown >= 0.999f && _fade < 0f) _fade = 0f;
            if (_fade < 0f) return;
            _fade += dt;
            float a = _fadeSeconds > 0f ? 1f - Mathf.Clamp01(_fade / _fadeSeconds) : 0f;
            if (_group != null)
            {
                _group.alpha = a;
                _group.blocksRaycasts = a > 0.5f;
            }

            if (a <= 0f) gameObject.SetActive(false);
        }

        private static bool BattleReady()
        {
            if (SceneManager.GetActiveScene().name != BootSequence.GameSceneName) return false;
            CombatSession session = FindObjectOfType<CombatSession>();
            return session != null && session.Runner != null;
        }
    }
}
