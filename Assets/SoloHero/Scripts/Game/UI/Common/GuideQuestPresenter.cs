using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using SoloHero.Game.Audio;
using SoloHero.Game.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// D-111 guide quest bar (genre main-quest strip): the current quest, its progress and reward. Done, it turns gold
    /// and pulses; a tap claims it and the next quest slides in. Not done, a tap takes the player where the quest is
    /// made (upgrades, summon, skills, talents, the job button) or says to keep fighting. Polls a few times a second;
    /// text is rebuilt only when the quest or its progress changes.
    /// </summary>
    public sealed class GuideQuestPresenter : MonoBehaviour
    {
        private const float PollSeconds = 0.2f;

        [SerializeField] private Text _title;
        [SerializeField] private Text _progress;
        [SerializeField] private Text _reward;
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private Sprite _gemSprite;
        [SerializeField] private Sprite _goldSprite;
        [SerializeField] private Image _background;
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite _readySprite;
        [SerializeField] private UiPulse _pulse;
        [SerializeField] private PanelHost _panels;
        [SerializeField] private GachaPanelPresenter _gacha;
        [SerializeField] private ToastQueue _toast;

        [Tooltip("Tabs: character, equipment, summon, skill, talent.")]
        [SerializeField] private int _characterTab;
        [SerializeField] private int _summonTab = 2;
        [SerializeField] private int _skillTab = 3;
        [SerializeField] private int _talentTab = 4;

        private GuideQuestService _service;
        private SaveDataV2 _save;
        private BalanceValues _balance;
        private int _shownIndex = -1;
        private int _shownProgress = -1;
        private int _shownFarm = -1;
        private bool _shownReady;
        private float _poll;

        private void OnEnable()
        {
            _service = PanelServices.TryGet<GuideQuestService>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _shownIndex = -1;
            _poll = 0f;
        }

        private void LateUpdate()
        {
            if (_service == null || _save == null) return;
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f) return;
            _poll = PollSeconds;

            int index = _service.Index;
            int progress = _service.Progress;
            bool ready = _service.IsComplete;
            if (index == _shownIndex && progress == _shownProgress && ready == _shownReady && _save.farmingStage == _shownFarm) return;
            _shownIndex = index;
            _shownProgress = progress;
            _shownReady = ready;
            _shownFarm = _save.farmingStage;
            Draw(_service.Current, progress, ready);
        }

        public void OnTap()
        {
            if (_service == null) return;
            if (_service.IsComplete)
            {
                double gold = _service.RewardGold;
                double gems = _service.Current.Gems;
                if (!_service.TryClaim().Ok) return;
                PanelServices.TryGet<AudioService>()?.Play(gems > 0d ? SfxId.GradeEpic : SfxId.Gold);
                if (_toast != null)
                    _toast.Show(gems > 0d
                        ? Strings.Format("guide.claimed_gem", BigNumberFormat.Format(gems))
                        : Strings.Format("guide.claimed_gold", BigNumberFormat.Format(gold)));
                _shownIndex = -1;
                _poll = 0f;
                return;
            }

            Navigate(_service.Current.Kind);
        }

        private void Navigate(GuideKind kind)
        {
            switch (kind)
            {
                case GuideKind.UpgradeTotal:
                case GuideKind.JobTier:
                    Open(_characterTab);
                    break;
                case GuideKind.GearPulls:
                case GuideKind.OwnedGear:
                    Open(_summonTab);
                    if (_gacha != null) _gacha.SetMode(GachaPanelPresenter.ModeGear);
                    break;
                case GuideKind.SkillPulls:
                    Open(_summonTab);
                    if (_gacha != null) _gacha.SetMode(GachaPanelPresenter.ModeSkill);
                    break;
                case GuideKind.EquipSkills:
                    Open(_skillTab);
                    break;
                case GuideKind.Talents:
                    Open(_talentTab);
                    break;
                default:
                    if (_toast != null) _toast.Show(Strings.Get("guide.keep_fighting"));
                    break;
            }
        }

        private void Open(int tab)
        {
            if (_panels != null) _panels.Open(tab);
        }

        private void Draw(GuideQuestDef def, int progress, bool ready)
        {
            if (_title != null) _title.text = Strings.Format(KeyOf(def.Kind), TargetText(def));
            if (_progress != null)
            {
                _progress.text = ready
                    ? Strings.Get("guide.claim")
                    : Strings.Format("guide.progress", ProgressText(def, progress), TargetText(def));
            }

            bool gems = def.Gems > 0d;
            if (_reward != null)
                _reward.text = BigNumberFormat.Format(gems ? def.Gems : GuideQuestService.RewardGoldOf(_balance, _save, def));
            if (_rewardIcon != null) _rewardIcon.sprite = gems ? _gemSprite : _goldSprite;
            if (_background != null && _idleSprite != null) _background.sprite = ready && _readySprite != null ? _readySprite : _idleSprite;
            if (_pulse != null) _pulse.enabled = ready;
        }

        private string TargetText(GuideQuestDef def) =>
            def.Kind == GuideKind.ClearStage ? StageLabel(def.Target) : def.Target.ToString();

        private string ProgressText(GuideQuestDef def, int progress)
        {
            if (def.Kind != GuideKind.ClearStage) return (progress > def.Target ? def.Target : progress).ToString();
            return StageLabel(progress > def.Target ? def.Target : progress);
        }

        private string StageLabel(int g)
        {
            int per = _balance != null ? _balance.STAGES_PER_CHAPTER : 10;
            StageIndex.FromGlobal(g < 1 ? 1 : g, per, out int chapter, out int stage);
            return chapter + "-" + stage;
        }

        private static string KeyOf(GuideKind kind)
        {
            switch (kind)
            {
                case GuideKind.UpgradeTotal: return "guide.upgrade";
                case GuideKind.ClearStage: return "guide.stage";
                case GuideKind.HeroLevel: return "guide.level";
                case GuideKind.GearPulls: return "guide.gear_pull";
                case GuideKind.SkillPulls: return "guide.skill_pull";
                case GuideKind.EquipSkills: return "guide.equip_skill";
                case GuideKind.JobTier: return "guide.job";
                case GuideKind.Talents: return "guide.talent";
                default: return "guide.gear_owned";
            }
        }
    }
}
