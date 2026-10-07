using SoloHero.Core.Common;
using SoloHero.Core.Gacha;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// Tutorial hints (E7-13): polls <see cref="TutorialService"/> a few times a second and keeps the upgrade hint up
    /// until the first upgrade. D-111 first minute: the welcome gift comes right after loading and its free summon
    /// plays the real card reveal, and a bobbing hand points at the ATK upgrade button while the hint is up.
    /// Text comes from the Strings table (E7-17).
    /// </summary>
    public sealed class TutorialHints : MonoBehaviour
    {
        private const float PollSeconds = 0.5f;
        private const float StartDelaySeconds = 1.2f;

        [SerializeField] private GameObject _banner;
        [SerializeField] private Text _bannerText;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private CombatSession _session;
        [SerializeField] private GachaPanelPresenter _gacha;

        [Header("D-111 pointer")]
        [SerializeField] private RectTransform _pointer;
        [SerializeField] private RectTransform _pointerTarget;
        [SerializeField] private Vector2 _pointerOffset = new Vector2(60f, -40f);
        [SerializeField] private float _bobHeight = 18f;
        [SerializeField] private float _bobSpeed = 7f;

        private TutorialService _tutorial;
        private SaveDataV2 _save;
        private float _poll;
        private bool _bannerShown;
        private float _since;

        private void OnEnable()
        {
            _tutorial = PanelServices.TryGet<TutorialService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _since = 0f;
            SetBanner(false, null);
            SetPointer(false);
        }

        private void LateUpdate()
        {
            if (_tutorial == null || _save == null || _save.tutorialStep >= TutorialService.StepDone)
            {
                SetPointer(false);
                return;
            }

            DrawPointer();
            // Let the loading screen fade before the welcome gift opens its reveal.
            _since += Time.unscaledDeltaTime;
            if (_since < StartDelaySeconds) return;
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f) return;
            _poll = PollSeconds;

            switch (_tutorial.Tick(_save))
            {
                case TutorialEvent.Rewarded:
                    if (_session != null) _session.RefreshLoadout();
                    if (_gacha != null) _gacha.ShowGearReveal(_tutorial.LastRewardItems);
                    if (_toast != null) _toast.Show(RewardText(_tutorial.LastRewardItems));
                    break;
                case TutorialEvent.HintUpgrade:
                    SetBanner(true, Strings.Get("tutorial.upgrade_tip"));
                    return;
                case TutorialEvent.HintSkill:
                    if (_toast != null) _toast.Show(Strings.Get("tutorial.skill_unlocked"));
                    break;
            }

            if (_save.tutorialStep != TutorialService.StepUpgrade) SetBanner(false, null);
        }

        /// <summary>The hand bobs over the ATK upgrade button while the upgrade hint is up and the button is on screen.</summary>
        private void DrawPointer()
        {
            bool show = _save.tutorialStep == TutorialService.StepUpgrade && _pointerTarget != null
                && _pointerTarget.gameObject.activeInHierarchy;
            SetPointer(show);
            if (!show || _pointer == null) return;
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * _bobSpeed)) * _bobHeight;
            Vector3 scale = _pointer.lossyScale;
            _pointer.position = _pointerTarget.position + new Vector3(_pointerOffset.x * scale.x, (_pointerOffset.y + bob) * scale.y, 0f);
        }

        private void SetPointer(bool show)
        {
            if (_pointer != null && _pointer.gameObject.activeSelf != show) _pointer.gameObject.SetActive(show);
        }

        /// <summary>D-113: the welcome gift is a free ten-pull; the reveal shows every card, the toast only the count.</summary>
        private static string RewardText(GachaPullItem[] items) => Strings.Format("tutorial.welcome", items.Length);

        private void SetBanner(bool show, string text)
        {
            if (show && _bannerText != null && text != null && _bannerText.text != text) _bannerText.text = text;
            if (_banner == null || _bannerShown == show) return;
            _bannerShown = show;
            _banner.SetActive(show);
        }
    }
}
