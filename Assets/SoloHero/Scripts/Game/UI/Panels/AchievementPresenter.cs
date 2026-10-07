using SoloHero.Core.Common;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-118 achievements popup (right rail): one row per track with its name and tier, a progress gauge, the gem
    /// reward and a claim button, plus "claim all"; a red dot on the rail while anything can be claimed.
    /// </summary>
    public sealed class AchievementPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popup;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private Text[] _names = new Text[0];
        [SerializeField] private Text[] _progress = new Text[0];
        [SerializeField] private RectTransform[] _fills = new RectTransform[0];
        [SerializeField] private Text[] _rewards = new Text[0];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[0];
        [SerializeField] private Text[] _buttonLabels = new Text[0];
        [SerializeField] private TapGuardButton _claimAll;
        [SerializeField] private GameObject[] _badges = new GameObject[0];

        private AchievementService _service;
        private SaveDataV2 _save;
        private long _shownRevision = -1;
        private float _poll;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        private void OnEnable()
        {
            _service = PanelServices.TryGet<AchievementService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _shownRevision = -1;
        }

        public void Open()
        {
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        public void Claim(int index)
        {
            if (_service == null) return;
            double gems = _service.Gems(index);
            Result result = _service.TryClaim(index);
            if (!result.Ok) return;
            Rewarded(gems);
        }

        public void ClaimAll()
        {
            if (_service == null) return;
            double gems = _service.ClaimAll();
            if (gems > 0d) Rewarded(gems);
        }

        private void Rewarded(double gems)
        {
            PanelServices.TryGet<AudioService>()?.Play(SfxId.GradeEpic);
            if (_toast != null) _toast.Show(Strings.Format("achieve.claimed", BigNumberFormat.Format(gems)));
            Refresh();
        }

        private void LateUpdate()
        {
            if (_service == null || _save == null) return;
            // Kills move every second; a light poll keeps the badge and the open popup current.
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f && _save.saveRevision == _shownRevision) return;
            _poll = 1f;
            _shownRevision = _save.saveRevision;
            Refresh();
        }

        private void Refresh()
        {
            if (_service == null) return;
            bool any = _service.ClaimableCount > 0;
            for (int i = 0; i < _badges.Length; i++)
                if (_badges[i] != null && _badges[i].activeSelf != any) _badges[i].SetActive(any);
            if (_claimAll != null) _claimAll.SetAvailable(any);
            if (!IsOpen) return;

            for (int i = 0; i < AchievementCatalog.Count && i < _names.Length; i++)
            {
                AchievementDef def = AchievementCatalog.All[i];
                bool finished = _service.Finished(i);
                int tier = _service.CurrentTier(i);
                long progress = _service.Progress(i);
                long target = _service.Target(i);
                if (_names[i] != null) _names[i].text = Strings.Format("achieve.tier", Strings.Get(def.NameKey), tier + 1);
                if (_progress[i] != null)
                    _progress[i].text = Strings.Format("achieve.progress", BigNumberFormat.Format(System.Math.Min(progress, target)), BigNumberFormat.Format(target));
                if (_fills[i] != null)
                {
                    float ratio = finished ? 1f : Mathf.Clamp01(target > 0 ? (float)progress / target : 0f);
                    _fills[i].anchorMax = new Vector2(ratio, 1f);
                }

                if (_rewards[i] != null) _rewards[i].text = Strings.Format("achieve.reward", BigNumberFormat.Format(_service.Gems(i)));
                if (_buttons[i] != null) _buttons[i].SetAvailable(_service.Claimable(i));
                if (_buttonLabels[i] != null) _buttonLabels[i].text = Strings.Get(finished ? "achieve.done" : "achieve.claim");
            }
        }
    }
}
