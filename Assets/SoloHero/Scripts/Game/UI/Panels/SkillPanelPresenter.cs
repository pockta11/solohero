using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Game.Audio;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>Skill panel (E7-08): 3 fixed skills with unlock level, skill level and level-up cost.</summary>
    public sealed class SkillPanelPresenter : MonoBehaviour
    {
        private static readonly string[] SkillNameKeys = { "skill.name.1", "skill.name.2", "skill.name.3" };

        [SerializeField] private Text[] _levelTexts = new Text[3];
        [SerializeField] private Text[] _costTexts = new Text[3];
        [SerializeField] private TapGuardButton[] _buttons = new TapGuardButton[3];
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private UiPunch[] _punches = new UiPunch[3];

        private SkillLevelService _skills;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private AudioService _audio;
        private double _shownGold = -1d;
        private int _shownHeroLevel = -1;

        private void OnEnable()
        {
            _skills = PanelServices.TryGet<SkillLevelService>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _audio = PanelServices.TryGet<AudioService>();
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
            GameAnalytics.Log(AnalyticsEvents.SkillLevel,
                AnalyticsParam.Of(AnalyticsEvents.PSlot, slot),
                AnalyticsParam.Of(AnalyticsEvents.PLevel, SkillLevelService.EffectiveLevel(_skills.GetSavedLevel((SkillSlot)slot))));
            Celebrate(slot);
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
                        ? Strings.Format("skill.level", Strings.Get(SkillNameKeys[i]), level, _balance.SKILL_MAX_LEVEL)
                        : Strings.Format("skill.unlock_at", Strings.Get(SkillNameKeys[i]), SkillLevelService.UnlockHeroLevel(_balance, slot));
                }

                if (i < _costTexts.Length && _costTexts[i] != null)
                    _costTexts[i].text = !unlocked ? Strings.Get("skill.locked") : max ? Strings.Get("char.max") : BigNumberFormat.Format(cost);
                if (i < _buttons.Length && _buttons[i] != null)
                    _buttons[i].SetAvailable(unlocked && !max && _save.gold >= cost);
            }
        }

        /// <summary>E8-11: every successful purchase punches its row and plays the upgrade chime.</summary>
        private void Celebrate(int row)
        {
            if (row >= 0 && row < _punches.Length && _punches[row] != null) _punches[row].Play();
            if (_audio != null) _audio.Play(SfxId.Upgrade);
        }
    }
}
