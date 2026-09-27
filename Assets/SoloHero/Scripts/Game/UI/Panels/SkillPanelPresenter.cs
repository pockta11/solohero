using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>Skill panel (E7-08): 3 fixed skills with unlock level, skill level and level-up cost.</summary>
    public sealed class SkillPanelPresenter : MonoBehaviour
    {
        private static readonly string[] SkillNames = { "Power Strike", "Whirlwind", "Battle Cry" };

        [SerializeField] private Text[] _levelTexts = new Text[3];
        [SerializeField] private Text[] _costTexts = new Text[3];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[3];
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;

        private SkillLevelService _skills;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private double _shownGold = -1d;
        private int _shownHeroLevel = -1;

        private void OnEnable()
        {
            _skills = PanelServices.TryGet<SkillLevelService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _shownGold = -1d;
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            if (_save.gold == _shownGold && _save.heroLevel == _shownHeroLevel) return;
            Refresh();
        }

        public void LevelUp(int slot)
        {
            if (_skills == null) return;
            Result result = _skills.TryLevelUp((SkillSlot)slot);
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
            if (_skills == null || _balance == null || _save == null) return;
            _shownGold = _save.gold;
            _shownHeroLevel = _save.heroLevel;

            for (int i = 0; i < 3; i++)
            {
                var slot = (SkillSlot)i;
                bool unlocked = SkillLevelService.IsUnlocked(_balance, slot, _save.heroLevel);
                int level = SkillLevelService.EffectiveLevel(_skills.GetSavedLevel(slot));
                bool max = level >= _balance.SKILL_MAX_LEVEL;
                double cost = SkillLevelService.UpgradeCost(_balance, slot, level);

                if (i < _levelTexts.Length && _levelTexts[i] != null)
                {
                    _levelTexts[i].text = unlocked
                        ? SkillNames[i] + "  Lv " + level + "/" + _balance.SKILL_MAX_LEVEL
                        : SkillNames[i] + "  (hero Lv " + SkillLevelService.UnlockHeroLevel(_balance, slot) + ")";
                }

                if (i < _costTexts.Length && _costTexts[i] != null)
                    _costTexts[i].text = !unlocked ? "Locked" : max ? "MAX" : BigNumberFormat.Format(cost);
                if (i < _buttons.Length && _buttons[i] != null)
                    _buttons[i].SetAvailable(unlocked && !max && _save.gold >= cost);
            }
        }
    }
}
