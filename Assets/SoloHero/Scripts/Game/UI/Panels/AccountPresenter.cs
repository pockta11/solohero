using System;
using System.Threading.Tasks;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Infrastructure;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// D-134 account window, opened from the settings: the account id (copy), device transfer (issue a 24 h code /
    /// enter a code from the old device), Google's privacy options where UMP requires them (D-135) and deleting all
    /// data. Taking a transfer and deleting both ask first in a confirm box and restart the app when done.
    /// </summary>
    public sealed class AccountPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popup;
        [SerializeField] private Text _idText;
        [SerializeField] private Text _codeText;
        [SerializeField] private Text _codeNote;
        [SerializeField] private InputField _codeInput;
        [SerializeField] private Button[] _actions = new Button[0];
        [SerializeField] private GameObject _privacyButton;
        [SerializeField] private GameObject _confirm;
        [SerializeField] private Text _confirmTitle;
        [SerializeField] private Text _confirmBody;
        [SerializeField] private ToastQueue _toast;

        private AccountService _account;
        private Func<Task> _confirmed;
        private bool _busy;

        public bool IsOpen => _popup != null && _popup.activeSelf;

        private void Awake()
        {
            if (_popup != null) _popup.SetActive(false);
            if (_confirm != null) _confirm.SetActive(false);
        }

        public void Open()
        {
            _account = PanelServices.TryGet<AccountService>();
            if (_confirm != null) _confirm.SetActive(false);
            if (_popup != null) _popup.SetActive(true);
            Refresh();
        }

        /// <summary>Back key and the close button: the confirm box first, then the window (not while a request runs).</summary>
        public void Close()
        {
            if (_busy) return;
            if (_confirm != null && _confirm.activeSelf)
            {
                _confirm.SetActive(false);
                _confirmed = null;
                return;
            }

            if (_popup != null) _popup.SetActive(false);
        }

        public void CopyId()
        {
            if (_account == null) return;
            GUIUtility.systemCopyBuffer = _account.UserId;
            _toast?.Show(Strings.Get("account.copied"));
        }

        public void CopyCode()
        {
            string code = _account != null ? _account.IssuedCode : null;
            if (string.IsNullOrEmpty(code)) return;
            GUIUtility.systemCopyBuffer = TransferCode.Format(code);
            _toast?.Show(Strings.Get("account.copied"));
        }

        public async void IssueCode()
        {
            if (_busy || _account == null) return;
            SetBusy(true);
            (Result result, string code) = await _account.IssueTransferCodeAsync();
            SetBusy(false);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            GameAnalytics.Log(AnalyticsEvents.Account, AnalyticsParam.Of(AnalyticsEvents.PAction, "issue"));
            _toast?.Show(Strings.Get("account.code_issued"));
            Refresh();
            if (!string.IsNullOrEmpty(code)) GUIUtility.systemCopyBuffer = TransferCode.Format(code);
        }

        public async void RedeemCode()
        {
            if (_busy || _account == null) return;
            string code = TransferCode.Normalize(_codeInput != null ? _codeInput.text : "");
            if (code == null)
            {
                _toast?.ShowFailure(FailReason.CodeInvalid);
                return;
            }

            if (code == _account.IssuedCode)
            {
                _toast?.Show(Strings.Get("account.own_code"));
                return;
            }

            SetBusy(true);
            (Result result, SaveDataV2 incoming) = await _account.FindTransferAsync(code);
            SetBusy(false);
            if (!result.Ok)
            {
                _toast?.ShowFailure(result.Reason);
                return;
            }

            Ask("account.import_title",
                Strings.Format("account.import_body", StageLabel(incoming.highestStage), incoming.heroLevel),
                async () =>
                {
                    GameAnalytics.Log(AnalyticsEvents.Account, AnalyticsParam.Of(AnalyticsEvents.PAction, "redeem"));
                    Result taken = await _account.AcceptTransferAsync(code, incoming);
                    if (!taken.Ok) _toast?.ShowFailure(taken.Reason);
                });
        }

        public void AskDelete()
        {
            if (_busy || _account == null) return;
            Ask("account.delete_title", Strings.Get(_account.IsOnline ? "account.delete_body" : "account.delete_body_local"), async () =>
            {
                GameAnalytics.Log(AnalyticsEvents.Account, AnalyticsParam.Of(AnalyticsEvents.PAction, "delete"));
                Result deleted = await _account.DeleteAllDataAsync();
                if (!deleted.Ok) _toast?.ShowFailure(deleted.Reason);
            });
        }

        public void ShowPrivacyOptions()
        {
            if (_busy) return;
            AdConsent.ShowPrivacyOptions(shown =>
            {
                if (!shown) _toast?.ShowFailure(FailReason.NetworkUnavailable);
                Refresh();
            });
        }

        public async void ConfirmYes()
        {
            if (_busy || _confirmed == null) return;
            Func<Task> action = _confirmed;
            _confirmed = null;
            SetBusy(true);
            try
            {
                await action();
            }
            finally
            {
                SetBusy(false);
                if (_confirm != null) _confirm.SetActive(false);
            }
        }

        public void ConfirmNo()
        {
            if (_busy) return;
            _confirmed = null;
            if (_confirm != null) _confirm.SetActive(false);
        }

        private void Ask(string titleKey, string body, Func<Task> action)
        {
            _confirmed = action;
            if (_confirmTitle != null) _confirmTitle.text = Strings.Get(titleKey);
            if (_confirmBody != null) _confirmBody.text = body;
            if (_confirm != null) _confirm.SetActive(true);
        }

        private void Refresh()
        {
            if (_account == null) _account = PanelServices.TryGet<AccountService>();
            bool online = _account != null && _account.IsOnline;
            if (_idText != null)
                _idText.text = _account == null ? "" : online ? _account.UserId : Strings.Get("account.local_mode");
            string code = _account != null ? _account.IssuedCode : null;
            if (_codeText != null) _codeText.text = string.IsNullOrEmpty(code) ? Strings.Get("account.no_code") : TransferCode.Format(code);
            if (_codeNote != null)
                _codeNote.text = string.IsNullOrEmpty(code)
                    ? Strings.Get(online ? "account.transfer_help" : "account.transfer_offline")
                    : Strings.Format("account.code_left", _account.IssuedHoursLeft);
            if (_privacyButton != null) _privacyButton.SetActive(AdConsent.PrivacyOptionsRequired);
            SetBusy(_busy);
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            bool online = _account != null && _account.IsOnline;
            bool hasCode = _account != null && !string.IsNullOrEmpty(_account.IssuedCode);
            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i] == null) continue;
                // 0 copy id, 1 issue, 2 copy code, 3 redeem, 4 delete: transfers need the server.
                bool needsServer = i == 1 || i == 2 || i == 3;
                _actions[i].interactable = !busy && (!needsServer || online) && (i != 2 || hasCode);
            }
        }

        /// <summary>"3-7" style chapter-stage label for the global stage index.</summary>
        private static string StageLabel(int globalStage)
        {
            BalanceValues balance = PanelServices.TryGet<BalanceValues>() ?? new BalanceValues();
            StageIndex.FromGlobal(globalStage < 1 ? 1 : globalStage, balance.STAGES_PER_CHAPTER, out int chapter, out int stage);
            return chapter + "-" + stage;
        }
    }
}
