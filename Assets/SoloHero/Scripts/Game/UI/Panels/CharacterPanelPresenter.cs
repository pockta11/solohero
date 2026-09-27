using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>Character panel (E7-05): 4 upgrade lanes with level, cost and MAX state, plus the stat summary.</summary>
    public sealed class CharacterPanelPresenter : MonoBehaviour
    {
        private static readonly string[] LaneKeys = { "stat.hp", "stat.atk", "stat.def", "stat.atkspd" };

        [SerializeField] private Text[] _levelTexts = new Text[4];
        [SerializeField] private Text[] _costTexts = new Text[4];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[4];
        [SerializeField] private Text _summaryText;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;

        private UpgradeService _upgrade;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private double _shownGold = -1d;

        private void OnEnable()
        {
            _upgrade = PanelServices.TryGet<UpgradeService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _shownGold = -1d;
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null || _save.gold == _shownGold) return;
            Refresh();
        }

        public void Upgrade(int lane)
        {
            if (_upgrade == null) return;
            Result result = _upgrade.TryUpgrade((UpgradeLane)lane);
            if (!result.Ok)
            {
                if (_toast != null) _toast.ShowFailure(result.Reason);
                return;
            }

            if (_session != null) _session.RefreshLoadout();
            Refresh();
        }

        private void Refresh()
        {
            if (_upgrade == null || _balance == null || _save == null) return;
            _shownGold = _save.gold;

            for (int i = 0; i < 4; i++)
            {
                var lane = (UpgradeLane)i;
                int level = _upgrade.GetLevel(lane);
                bool max = lane == UpgradeLane.Spd && level >= _balance.UPG_MAX_LEVEL_SPD;
                double cost = max ? 0d : Formulas.UpgradeCost(_balance, lane, level);

                if (i < _levelTexts.Length && _levelTexts[i] != null)
                    _levelTexts[i].text = Strings.Format("char.lane", Strings.Get(LaneKeys[i]), level);
                if (i < _costTexts.Length && _costTexts[i] != null)
                    _costTexts[i].text = max ? Strings.Get("char.max") : BigNumberFormat.Format(cost);
                if (i < _buttons.Length && _buttons[i] != null)
                    _buttons[i].SetAvailable(!max && _save.gold >= cost);
            }

            if (_summaryText == null) return;
            HeroStats s = CombatLoadout.ComputeStats(_balance, _save);
            _summaryText.text = Strings.Format("char.summary",
                _save.heroLevel,
                BigNumberFormat.Format(s.Hp),
                BigNumberFormat.Format(s.Atk),
                BigNumberFormat.Format(s.Def),
                s.AtkSpd.ToString("0.00"),
                s.CritRate.ToString("0"));
        }
    }
}
