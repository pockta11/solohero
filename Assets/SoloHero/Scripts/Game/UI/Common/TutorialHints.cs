using System.Text;
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
    /// Tutorial hints (E7-13): polls <see cref="TutorialService"/> a few times a second, shows the reward and skill
    /// unlock as toasts and keeps the upgrade hint banner up until the first upgrade. Text comes from the Strings table (E7-17).
    /// </summary>
    public sealed class TutorialHints : MonoBehaviour
    {
        private const float PollSeconds = 0.5f;

        [SerializeField] private GameObject _banner;
        [SerializeField] private Text _bannerText;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private CombatSession _session;

        private readonly StringBuilder _sb = new StringBuilder();
        private TutorialService _tutorial;
        private SaveDataV2 _save;
        private float _poll;
        private bool _bannerShown;

        private void OnEnable()
        {
            _tutorial = PanelServices.TryGet<TutorialService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            SetBanner(false, null);
        }

        private void LateUpdate()
        {
            if (_tutorial == null || _save == null || _save.tutorialStep >= TutorialService.StepDone) return;
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f) return;
            _poll = PollSeconds;

            switch (_tutorial.Tick(_save))
            {
                case TutorialEvent.Rewarded:
                    if (_session != null) _session.RefreshLoadout();
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

        private string RewardText(GachaPullItem[] items)
        {
            _sb.Clear();
            _sb.Append(Strings.Get("tutorial.welcome"));
            for (int i = 0; i < items.Length; i++)
                _sb.Append(i == 0 ? " - " : ", ").Append(PanelServices.GradeName(items[i].Grade)).Append(' ').Append(PanelServices.SlotName(items[i].Slot));
            return _sb.ToString();
        }

        private void SetBanner(bool show, string text)
        {
            if (show && _bannerText != null && text != null && _bannerText.text != text) _bannerText.text = text;
            if (_banner == null || _bannerShown == show) return;
            _bannerShown = show;
            _banner.SetActive(show);
        }
    }
}
