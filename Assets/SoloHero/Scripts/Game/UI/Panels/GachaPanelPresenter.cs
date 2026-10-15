using System.Globalization;
using System.Text;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Game.Combat;
using SoloHero.Game.UI.Common;
using SoloHero.Game.View;
using UnityEngine;
using UnityEngine.UI;

namespace SoloHero.Game.UI.Panels
{
    /// <summary>
    /// Summon panel (E7-07, D-078, D-114): equipment / skill / pet tabs on top (genre summon screen), then 10 / 100
    /// (D-172) / gem 10 pulls, the pity gauge of the shown tab, rate disclosure and the last result. The pet page opens after
    /// the first boss. Order is confirm -> save -> show:
    /// the service settles gold and items, then the save is requested, then the card reveal (E5-11) plays.
    /// </summary>
    public sealed class GachaPanelPresenter : MonoBehaviour
    {
        public const int ModeGear = 0;
        public const int ModeSkill = 1;
        public const int ModePet = 2;

        [SerializeField] private Text _pityText;
        [SerializeField] private Image _pityFill;
        [SerializeField] private Text _rateText;
        [SerializeField] private Text _resultText;
        [Tooltip("D-172: the x100 pull (was the single pull).")]
        [SerializeField] private Text _bulkCostText;
        [SerializeField] private Text _tenCostText;
        [SerializeField] private Text _gemCostText;
        [SerializeField] private TapGuardButton _bulkButton;
        [SerializeField] private TapGuardButton _tenButton;
        [SerializeField] private TapGuardButton _gemButton;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
        [SerializeField] private RewardFx _rewardFx;
        [SerializeField] private GachaRevealView _reveal;
        [Tooltip("D-128: spend up to ten tickets of the shown summon.")]
        [SerializeField] private TapGuardButton _ticketButton;
        [SerializeField] private Text _ticketLabel;
        [Tooltip("D-120: watch an ad for a free ten-pull (gear and pet pages).")]
        [SerializeField] private TapGuardButton _adButton;
        [SerializeField] private Text _adLabel;

        [Header("Tabs (D-078)")]
        [SerializeField] private Image[] _tabImages = new Image[3];
        [SerializeField] private Sprite _tabIdle;
        [SerializeField] private Sprite _tabActive;
        [SerializeField] private EquipmentIconSet _equipmentIcons;
        [SerializeField] private SkillIconSet _skillIcons;

        [Header("D-173 pickup banner")]
        [SerializeField] private GameObject _pickupRoot;
        [SerializeField] private Image _pickupIcon;
        [SerializeField] private Text _pickupText;

        [Tooltip("D-114 pet looks, index-aligned with PetCatalog (the card shows the first idle frame).")]
        [SerializeField] private CharacterArt[] _petArts = new CharacterArt[0];

        private readonly StringBuilder _sb = new StringBuilder();
        private GearTableValues _gearTable;
        private GachaTableValues _skillTable;
        private GachaService _gacha;
        private SkillSummonService _skillSummon;
        private PetSummonService _petSummon;
        private AdSlotPolicy _adPolicy;
        private IAdGateway _ads;
        private bool _adBusy;
        private int _shownFarmingStage = -1;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private ISaveRequester _requester;
        private double _shownGold = -1d;
        private double _shownGem = -1d;
        private int _mode = ModeGear;
        private int _shownHighest = -1;
        private SoloHero.Core.Events.EventService _events;
        private float _pickupClock;

        private void OnEnable()
        {
            _gacha = PanelServices.TryGet<GachaService>();
            _skillSummon = PanelServices.TryGet<SkillSummonService>();
            _petSummon = PanelServices.TryGet<PetSummonService>();
            _adPolicy = PanelServices.TryGet<AdSlotPolicy>();
            _ads = PanelServices.TryGet<IAdGateway>();
            _balance = PanelServices.TryGet<BalanceValues>();
            _save = PanelServices.TryGet<SaveDataV2>();
            _requester = PanelServices.TryGet<ISaveRequester>();
            _events = PanelServices.TryGet<SoloHero.Core.Events.EventService>();
            _shownGold = -1d;
            DrawStatic();
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null) return;
            // D-173: the pickup's time left (and its start or end) ticks once a second.
            _pickupClock -= Time.unscaledDeltaTime;
            if (_pickupClock <= 0f)
            {
                _pickupClock = 1f;
                bool was = _pickupRoot != null && _pickupRoot.activeSelf;
                DrawPickup();
                if (_pickupRoot != null && was != _pickupRoot.activeSelf) DrawStatic();
            }

            if (_save.gold == _shownGold && _save.gem == _shownGem && _save.farmingStage == _shownFarmingStage
                && _save.highestStage == _shownHighest) return;
            Refresh();
        }

        /// <summary>D-111: plays the card reveal for gear pulled elsewhere (the welcome gift's free summon).</summary>
        public void ShowGearReveal(GachaPullItem[] items)
        {
            if (_reveal != null && items != null && items.Length > 0) _reveal.Show(Cards(items));
        }

        public void SetMode(int mode)
        {
            if (mode != ModeGear && mode != ModeSkill && mode != ModePet) return;
            if (_mode != mode && _resultText != null) _resultText.text = "";
            _mode = mode;
            DrawStatic();
            DrawPickup();
            Refresh();
        }

        /// <summary>D-115: which summon level the shown page uses.</summary>
        private SummonKind Kind => _mode == ModePet ? SummonKind.Pet : _mode == ModeSkill ? SummonKind.Skill : SummonKind.Gear;

        private int CurrentLevel => _balance != null && _save != null ? SummonLevel.Of(_balance, _save, Kind) : 1;

        /// <summary>
        /// D-115: a toast when the pull just raised the summon level (and the rate table with it); D-121: it names the
        /// grade that level opened.
        /// </summary>
        private void AnnounceLevel(int before)
        {
            int after = CurrentLevel;
            if (after <= before) return;
            int opened = -1;
            for (int level = before + 1; level <= after; level++)
            {
                int g = SummonLevel.GradeOpeningAt(OpenLevels(_mode), level);
                if (g >= 0) opened = g;
            }

            // D-159: the late levels paid tickets of this summon (SummonLevel.PayLevelRewards in the service).
            int tickets = 0;
            for (int level = before + 1; level <= after; level++) tickets += SummonLevel.RewardTickets(_balance, Kind, level);
            // D-171: the new level rises over the reveal and its tickets fly to the summon tab.
            if (_rewardFx != null)
            {
                var top = new Vector2(Screen.width * 0.5f, Screen.height * 0.84f);
                _rewardFx.Label(Strings.Format("fx.summon_level", Strings.Get(ModeKeys[_mode]), after), top, new Color(1f, 0.85f, 0.4f, 1f));
                if (tickets > 0) _rewardFx.Play(RewardKind.Ticket, tickets, top - new Vector2(0f, 90f), 1);
            }

            if (_toast != null)
            {
                string summon = Strings.Get(ModeKeys[_mode]);
                if (tickets > 0)
                    _toast.Show(Strings.Format("gacha.level_up_reward", summon, after, Strings.Format(TicketRewardKeys[_mode], tickets)));
                else
                    _toast.Show(opened >= 0
                        ? Strings.Format("gacha.level_up_open", summon, after, PanelServices.GradeName(LadderGrade(_mode, opened)))
                        : Strings.Format("gacha.level_up", summon, after));
            }

            DrawStatic();
        }

        private GearTableValues GearTable => _gearTable ??= GearTableValues.FromBalance(_balance);

        private GachaTableValues SkillTable => _skillTable ??= GachaTableValues.FromBalance(_balance);

        /// <summary>D-121: open levels of the page's grades (the gear ladder for gear and pets, four grades for skills).</summary>
        private int[] OpenLevels(int mode) => mode == ModeSkill ? SkillTable.OpenLevels : GearTable.OpenLevels;

        /// <summary>D-123: whether the page's summon level already opened the pity grade (Legendary).</summary>
        private bool PityOpen(int mode)
        {
            SummonKind kind = mode == ModePet ? SummonKind.Pet : mode == ModeSkill ? SummonKind.Skill : SummonKind.Gear;
            int level = SummonLevel.Of(_balance, _save, kind);
            return mode == ModeSkill ? SkillTable.AtLevel(level).PityOpen : GearTable.AtLevel(level).PityOpen;
        }

        /// <summary>The ladder grade (name and colour) of a grade index of the page's table.</summary>
        private static GearGrade LadderGrade(int mode, int index) => mode == ModeSkill ? ((Grade)index).ToGearGrade() : (GearGrade)index;

        private static readonly string[] ModeKeys = { "gacha.mode_gear", "gacha.mode_skill", "gacha.mode_pet" };

        /// <summary>D-159: the ticket reward line of each page's summon.</summary>
        private static readonly string[] TicketRewardKeys = { "reward.gear", "reward.skill", "reward.pet" };

        /// <summary>D-172: SUMMON_BULK_PULLS pulls (ten ten-pulls) in one tap.</summary>
        public void PullBulk()
        {
            int before = CurrentLevel;
            PullBulkNow();
            AnnounceLevel(before);
        }

        public void PullTen()
        {
            int before = CurrentLevel;
            PullTenNow();
            AnnounceLevel(before);
        }

        public void PullTenWithGem()
        {
            int before = CurrentLevel;
            PullTenWithGemNow();
            AnnounceLevel(before);
        }

        private int BulkTens => _balance != null ? System.Math.Max(1, _balance.SUMMON_BULK_PULLS / 10) : 10;

        private void PullBulkNow()
        {
            int tens = BulkTens;
            if (_mode == ModePet) ApplyPet(_petSummon != null && _save != null ? _petSummon.TryPullTens(_save, tens) : default, "pet_gold_bulk");
            else if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPullTens(_save, tens) : default, "skill_gold_bulk");
            else Apply(_gacha != null && _save != null ? _gacha.TryPullTens(_save, tens) : default, "gold_bulk");
        }

        private void PullTenNow()
        {
            if (_mode == ModePet) ApplyPet(_petSummon != null && _save != null ? _petSummon.TryPullTen(_save) : default, "pet_gold_ten");
            else if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPullTen(_save) : default, "skill_gold_ten");
            else Apply(_gacha != null && _save != null ? _gacha.TryPullTen(_save) : default, "gold_ten");
        }

        private void PullTenWithGemNow()
        {
            if (_mode == ModePet) ApplyPet(_petSummon != null && _save != null ? _petSummon.TryPullTenWithGem(_save) : default, "pet_gem_ten");
            else if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPullTenWithGem(_save) : default, "skill_gem_ten");
            else Apply(_gacha != null && _save != null ? _gacha.TryPullTenWithGem(_save) : default, "gem_ten");
        }

        /// <summary>D-120: an ad for a free ten-pull of the shown page (gear or pet).</summary>
        public void WatchAdSummon()
        {
            if (_adPolicy == null || _ads == null || _save == null || _adBusy || _mode == ModeSkill) return;
            AdSlot slot = _mode == ModePet ? AdSlot.FreePetSummon : AdSlot.FreeGearSummon;
            Result can = _adPolicy.CanUse(slot);
            if (can.Ok && _mode == ModePet && !PetSummonService.IsUnlocked(_balance, _save)) can = Result.Fail(FailReason.Locked);
            if (!can.Ok)
            {
                if (_toast != null) _toast.ShowFailure(can.Reason);
                return;
            }

            int mode = _mode;
            _adBusy = true;
            _ads.Show(outcome =>
            {
                _adBusy = false;
                Result r = _adPolicy.Complete(slot, outcome);
                GameAnalytics.Log(r.Ok ? AnalyticsEvents.AdReward : AnalyticsEvents.AdFail, AnalyticsParam.Of(AnalyticsEvents.PSlot, slot.ToString()));
                if (!r.Ok)
                {
                    if (_toast != null) _toast.ShowFailure(r.Reason);
                    Refresh();
                    return;
                }

                int before = CurrentLevel;
                int pulls = _balance.AD_FREE_SUMMON_PULLS;
                if (mode == ModePet) ApplyPet(_petSummon != null ? _petSummon.TryPullFree(_save, pulls) : default, "pet_ad_free");
                else Apply(_gacha != null ? _gacha.PullFree(_save, pulls) : default, "ad_free");
                AnnounceLevel(before);
            });
        }

        /// <summary>D-128: pulls of the shown summon paid with its tickets; D-172: up to SUMMON_TICKET_MAX a tap.</summary>
        public void UseTickets()
        {
            if (_save == null || _balance == null) return;
            int before = CurrentLevel;
            int max = TicketBatch;
            if (_mode == ModePet) ApplyPet(_petSummon != null ? _petSummon.TryPullTickets(_save, max) : default, "pet_ticket");
            else if (_mode == ModeSkill) ApplySkill(_skillSummon != null ? _skillSummon.TryPullTickets(_save, max) : default, "skill_ticket");
            else Apply(_gacha != null ? _gacha.TryPullTickets(_save, max) : default, "ticket");
            AnnounceLevel(before);
        }

        private void Apply(GachaBatchResult result, string kind)
        {
            if (!result.Status.Ok || result.Items == null)
            {
                if (_toast != null && result.Status.Reason != FailReason.None) _toast.ShowFailure(result.Status.Reason);
                return;
            }

            if (_requester != null) _requester.RequestSave();
            if (_session != null) _session.RefreshLoadout();
            LogPull(AnalyticsEvents.GachaPull, (long)BestGrade(result.Items), result.Items.Length, kind, _save.pityCount);
            DrawResult(result.Items);
            if (_reveal != null) _reveal.Show(Cards(result.Items));
            Refresh();
        }

        private void ApplySkill(SkillSummonResult result, string kind)
        {
            if (!result.Status.Ok || result.Items == null)
            {
                if (_toast != null && result.Status.Reason != FailReason.None) _toast.ShowFailure(result.Status.Reason);
                return;
            }

            if (_requester != null) _requester.RequestSave();
            if (_session != null) _session.RefreshLoadout();
            Grade best = Grade.Common;
            for (int i = 0; i < result.Items.Length; i++)
            {
                if (result.Items[i].Grade > best) best = result.Items[i].Grade;
            }

            LogPull(AnalyticsEvents.SkillSummon, (long)best, result.Items.Length, kind, _save.skillPityCount);
            DrawSkillResult(result.Items);
            if (_reveal != null) _reveal.Show(Cards(result.Items));
            Refresh();
        }

        /// <summary>D-114: settle -> save -> reveal, as for the other summons.</summary>
        private void ApplyPet(PetSummonResult result, string kind)
        {
            if (!result.Status.Ok || result.Items == null)
            {
                if (_toast != null && result.Status.Reason != FailReason.None) _toast.ShowFailure(result.Status.Reason);
                return;
            }

            if (_requester != null) _requester.RequestSave();
            if (_session != null) _session.RefreshLoadout();
            GearGrade best = GearGrade.Common;
            for (int i = 0; i < result.Items.Length; i++)
            {
                if (result.Items[i].Grade > best) best = result.Items[i].Grade;
            }

            LogPull(AnalyticsEvents.PetSummon, (long)best, result.Items.Length, kind, _save.petPityCount);
            DrawPetResult(result.Items);
            if (_reveal != null) _reveal.Show(Cards(result.Items));
            Refresh();
        }

        private RevealCard[] Cards(PetPullItem[] items)
        {
            var cards = new RevealCard[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                PetPullItem item = items[i];
                int index = PetCatalog.IndexOf(item.Id);
                CharacterArt art = index >= 0 && index < _petArts.Length ? _petArts[index] : null;
                Sprite icon = art != null && art.idle.Length > 0 ? art.idle[0] : null;
                PetDef def = PetCatalog.Find(item.Id);
                cards[i] = new RevealCard(item.Grade, def != null ? Strings.Get(def.NameKey) : item.Id, PetNote(item), icon, PetIconScale, item.WasNew);
            }

            return cards;
        }

        /// <summary>Ink-cel pet frames are trimmed to the creature (D-168); a little larger than the card's icon box.</summary>
        private const float PetIconScale = 1.1f;

        private static string PetNote(PetPullItem item)
        {
            if (item.WasNew) return Strings.Get(item.AutoEquipped ? "gacha.pet_new_equipped" : "gacha.skill_new");
            if (item.RefundGold > 0d) return Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold));
            return Strings.Format("gacha.enhanced", item.EnhancedLevel);
        }

        private RevealCard[] Cards(GachaPullItem[] items)
        {
            var cards = new RevealCard[items.Length];
            // D-140: a weapon shows as the hero's line draws it (sword, staff or bow).
            SoloHero.Core.Jobs.WeaponKind weapon = SoloHero.Core.Jobs.JobTerms.WeaponOf(SoloHero.Core.Jobs.JobService.LineOf(_save));
            for (int i = 0; i < items.Length; i++)
            {
                GachaPullItem item = items[i];
                Sprite icon = _equipmentIcons != null ? _equipmentIcons.Get(item.Slot, item.Grade, weapon) : null;
                cards[i] = new RevealCard(item.Grade, PanelServices.SlotName(item.Slot), PanelServices.PullNote(item), icon, 1f, !item.WasDuplicate);
            }

            return cards;
        }

        private RevealCard[] Cards(SkillPullItem[] items)
        {
            var cards = new RevealCard[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                SkillPullItem item = items[i];
                SkillDef def = SkillCatalog.Find(item.Id);
                Sprite icon = _skillIcons != null ? _skillIcons.Get(item.Id) : null;
                cards[i] = new RevealCard(item.Grade.ToGearGrade(), def != null ? Strings.Get(def.NameKey) : item.Id, SkillNote(item), icon, 1f, item.WasNew);
            }

            return cards;
        }

        private static string SkillNote(SkillPullItem item)
        {
            if (item.WasNew) return Strings.Get("gacha.skill_new");
            if (item.RefundGold > 0d) return Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold));
            return Strings.Format("gacha.skill_up", item.Level);
        }

        private int TicketBatch => System.Math.Max(1, _balance.SUMMON_TICKET_MAX);

        private double BulkCost => TenCost * BulkTens;

        private double TenCost => _mode == ModePet ? _balance.PET_SUMMON_COST_TEN
            : _mode == ModeSkill ? _balance.SKILL_SUMMON_COST_TEN : _balance.GACHA_COST_TEN;

        private int GemCost => _mode == ModePet ? _balance.PET_SUMMON_COST_TEN_GEM
            : _mode == ModeSkill ? _balance.SKILL_SUMMON_COST_TEN_GEM : _balance.GACHA_COST_TEN_GEM;

        private void DrawStatic()
        {
            if (_balance == null) return;
            if (_rateText != null) _rateText.text = RatesText(_mode);
            if (_bulkCostText != null) _bulkCostText.text = Strings.Format("gacha.cost_bulk", BulkTens * 10, Price(BulkCost));
            if (_tenCostText != null) _tenCostText.text = Strings.Format("gacha.cost_ten", Price(TenCost));
            if (_gemCostText != null) _gemCostText.text = Strings.Format("gacha.cost_gem", GemCost);
            for (int i = 0; i < _tabImages.Length; i++)
            {
                if (_tabImages[i] == null) continue;
                Sprite sprite = i == _mode ? _tabActive : _tabIdle;
                if (sprite != null) _tabImages[i].sprite = sprite;
                // D-108: the idle tab has its own gray plate; only dim when there is no separate sprite.
                _tabImages[i].color = i == _mode || (_tabIdle != null && _tabIdle != _tabActive) ? Color.white : new Color(0.55f, 0.55f, 0.62f, 1f);
            }
        }

        private void Refresh()
        {
            if (_balance == null || _save == null) return;
            _shownGold = _save.gold;
            _shownGem = _save.gem;
            _shownHighest = _save.highestStage;
            bool skill = _mode == ModeSkill;
            bool pet = _mode == ModePet;
            // D-114: the pet page waits for the first boss.
            bool locked = pet && !PetSummonService.IsUnlocked(_balance, _save);

            int pity = skill ? _balance.GACHA_PITY : _balance.GEAR_PITY;
            int count = skill ? _save.skillPityCount : pet ? _save.petPityCount : _save.pityCount;
            int left = pity - count;
            float ratio = pity > 0 ? Mathf.Clamp01((float)count / pity) : 0f;
            string top;
            if (locked)
            {
                top = Strings.Format("gacha.pet_locked", StageLabel(_balance.PET_UNLOCK_STAGE));
                ratio = 0f;
            }
            else if (PityOpen(_mode))
            {
                top = Strings.Format("gacha.pity", left, count, pity);
            }
            else
            {
                // D-123: before the pity grade opens, the gauge is the summon level (MapleStory Idle style).
                SummonKind kind = Kind;
                int level = CurrentLevel;
                int from = SummonLevel.PullsFor(_balance, kind, level);
                int to = SummonLevel.PullsFor(_balance, kind, level + 1);
                int pulls = SummonLevel.Pulls(_save, kind);
                top = Strings.Format("gacha.level_progress", level, BigNumberFormat.Format(to - pulls));
                ratio = to > from ? Mathf.Clamp01((float)(pulls - from) / (to - from)) : 1f;
            }

            if (_pityText != null) _pityText.text = top;
            // Width by anchor: a Filled image without a sprite ignores fillAmount.
            if (_pityFill != null) _pityFill.rectTransform.anchorMax = new Vector2(ratio, 1f);

            if (_bulkButton != null) _bulkButton.SetAvailable(!locked && _save.gold >= BulkCost);
            if (_tenButton != null) _tenButton.SetAvailable(!locked && _save.gold >= TenCost);
            if (_gemButton != null) _gemButton.SetAvailable(!locked && _save.gem >= GemCost);
            _shownFarmingStage = _save.farmingStage;
            int tickets = ShopService.TicketsOf(_save, Kind);
            if (_ticketLabel != null)
                _ticketLabel.text = Strings.Format("gacha.tickets", System.Math.Max(1, System.Math.Min(TicketBatch, tickets)), BigNumberFormat.Format(tickets));
            if (_ticketButton != null) _ticketButton.SetAvailable(!locked && tickets > 0);
            DrawAdButton(locked);
        }

        /// <summary>
        /// The disclosure, drawn from the same table the draw uses (GDD rate disclosure rule): every grade in its colour,
        /// four to a line, then the pity rule. Gear has seven grades (D-113); skills keep their four.
        /// </summary>
        private string RatesText(int mode)
        {
            _sb.Clear();
            // D-115: the summon level and the pulls to the next one head the table it selects.
            SummonKind kind = mode == ModePet ? SummonKind.Pet : mode == ModeSkill ? SummonKind.Skill : SummonKind.Gear;
            int level = SummonLevel.Of(_balance, _save, kind);
            int[] open = OpenLevels(mode);
            bool pityOpen = PityOpen(mode);
            int opening = level < _balance.SUMMON_LV_MAX ? SummonLevel.GradeOpeningAt(open, level + 1) : -1;
            if (level >= _balance.SUMMON_LV_MAX)
            {
                _sb.Append(Strings.Format("gacha.level_max", level));
            }
            else if (!pityOpen && opening >= 0)
            {
                // D-123: the gauge above already counts to the next level; this line names what it opens.
                _sb.Append(Strings.Format("gacha.next_open", level + 1, PanelServices.GradeName(LadderGrade(mode, opening))));
            }
            else
            {
                // D-121: when the next level opens a grade, the line says which; D-159: a late level names its tickets.
                int left = SummonLevel.PullsFor(_balance, kind, level + 1) - SummonLevel.Pulls(_save, kind);
                int reward = SummonLevel.RewardTickets(_balance, kind, level + 1);
                _sb.Append(opening >= 0
                    ? Strings.Format("gacha.level_open", level, BigNumberFormat.Format(left), PanelServices.GradeName(LadderGrade(mode, opening)))
                    : reward > 0
                        ? Strings.Format("gacha.level_reward", level, BigNumberFormat.Format(left), Strings.Format(TicketRewardKeys[mode], reward))
                        : Strings.Format("gacha.level", level, BigNumberFormat.Format(left)));
            }

            _sb.Append('\n');
            if (mode == ModeSkill)
            {
                GachaTableValues table = SkillTable.AtLevel(level);
                double[] rates = { table.RateC, table.RateR, table.RateE, table.RateL };
                for (int g = 0; g < rates.Length; g++) AppendRate(g, LadderGrade(mode, g), rates[g], open[g] > level ? open[g] : 0);
                _sb.Append('\n').Append(pityOpen
                    ? Strings.Format("gacha.rates_pity_skill", table.PityCeiling)
                    : Strings.Format("gacha.rates_pity_locked", SkillTable.OpenLevel(Grade.Legendary), table.PityCeiling));
                if (TryPickup(mode, out string skillName, out GearGrade skillGrade, out int skillPool, out _))
                    AppendPickupRate(skillName, skillGrade, rates[SkillIndex(skillGrade)], open[SkillIndex(skillGrade)] > level, skillPool);
            }
            else
            {
                GearTableValues table = GearTable.AtLevel(level);
                for (int g = 0; g < table.Rates.Length; g++) AppendRate(g, (GearGrade)g, table.Rates[g], open[g] > level ? open[g] : 0);
                _sb.Append('\n').Append(pityOpen
                    ? Strings.Format(mode == ModePet ? "gacha.rates_pity_pet" : "gacha.rates_pity_gear", table.PityCeiling)
                    : Strings.Format("gacha.rates_pity_locked", GearTable.OpenLevel(GearTableValues.PityGrade), table.PityCeiling));
                if (TryPickup(mode, out string petName, out GearGrade petGrade, out int petPool, out _))
                    AppendPickupRate(petName, petGrade, table.Rates[(int)petGrade], open[(int)petGrade] > level, petPool);
            }

            return _sb.ToString();
        }

        /// <summary>
        /// D-173: the running pickup of the page (pets, skills): the boosted item's name and grade, the size of its grade's
        /// pool and the seconds left. A skill banner counts only when its target of the hero's line is in the pool.
        /// </summary>
        private bool TryPickup(int mode, out string name, out GearGrade grade, out int poolSize, out long secondsLeft)
        {
            name = "";
            grade = GearGrade.Common;
            poolSize = 0;
            secondsLeft = 0L;
            if (_save == null) return false;
            if (mode == ModePet)
            {
                PickupBanner banner = _petSummon?.Pickup;
                PetDef pet = banner != null && banner.Targets.Length > 0 ? PetCatalog.Find(banner.Targets[0]) : null;
                if (pet == null) return false;
                name = Strings.Get(pet.NameKey);
                grade = pet.Grade;
                poolSize = PetCatalog.OfGrade(pet.Grade).Length;
                secondsLeft = _events != null ? _events.PickupSecondsLeft(SummonKind.Pet) : 0L;
                return true;
            }

            if (mode == ModeSkill)
            {
                SkillDef def = PickupSkill();
                if (def == null) return false;
                name = Strings.Get(def.NameKey);
                grade = def.Grade.ToGearGrade();
                poolSize = SkillCatalog.OfLine(SoloHero.Core.Jobs.JobService.LineOf(_save), def.Grade, SoloHero.Core.Jobs.JobService.TierOf(_save)).Length;
                secondsLeft = _events != null ? _events.PickupSecondsLeft(SummonKind.Skill) : 0L;
                return true;
            }

            return false;
        }

        /// <summary>D-173: the skill banner's target in the hero's pool, or null.</summary>
        private SkillDef PickupSkill()
        {
            PickupBanner banner = _skillSummon?.Pickup;
            if (banner == null || _save == null) return null;
            SoloHero.Core.Jobs.JobLine line = SoloHero.Core.Jobs.JobService.LineOf(_save);
            if (line == SoloHero.Core.Jobs.JobLine.None) return null;
            int tier = SoloHero.Core.Jobs.JobService.TierOf(_save);
            for (int i = 0; i < banner.Targets.Length; i++)
            {
                SkillDef def = SkillCatalog.Find(banner.Targets[i]);
                if (def == null || def.Line != line) continue;
                SkillDef[] pool = SkillCatalog.OfLine(line, def.Grade, tier);
                if (System.Array.IndexOf(pool, def) >= 0) return def;
            }

            return null;
        }

        /// <summary>D-173 disclosure: the boosted item's rate and each other item of its grade (or when the grade opens).</summary>
        private void AppendPickupRate(string name, GearGrade grade, double gradeRate, bool locked, int poolSize)
        {
            _sb.Append('\n').Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiPalette.GradeInk(grade))).Append('>');
            if (locked)
            {
                _sb.Append(Strings.Format("pickup.rates_locked", name, PanelServices.GradeName(grade)));
            }
            else
            {
                double share = _balance.PICKUP_SHARE;
                _sb.Append(Strings.Format("pickup.rates", name,
                    Pct(Pickup.TargetRate(gradeRate, share)), PanelServices.GradeName(grade), Pct(Pickup.OtherRate(gradeRate, share, poolSize))));
            }

            _sb.Append("</color>");
        }

        /// <summary>The four-grade index (skill table) of a gear-ladder grade a skill maps to.</summary>
        private static int SkillIndex(GearGrade grade) =>
            grade >= GearGrade.Legendary ? (int)Grade.Legendary : grade >= GearGrade.Epic ? (int)Grade.Epic : grade >= GearGrade.Rare ? (int)Grade.Rare : (int)Grade.Common;

        private static string Pct(double percent) => System.Math.Round(percent, 4).ToString("0.####", CultureInfo.InvariantCulture);

        /// <summary>D-173: the pickup band over the summon circle - the boosted item, its share and the time left.</summary>
        private void DrawPickup()
        {
            if (_pickupRoot == null) return;
            bool show = TryPickup(_mode, out string name, out GearGrade grade, out _, out long left);
            if (_pickupRoot.activeSelf != show) _pickupRoot.SetActive(show);
            if (!show) return;
            if (_pickupText != null)
                _pickupText.text = Strings.Format("pickup.banner", name) + "\n"
                    + Strings.Format("pickup.banner_sub", PanelServices.GradeName(grade), _balance.PICKUP_SHARE, TimeLeft(left));
            if (_pickupIcon != null)
            {
                Sprite icon = PickupIcon();
                _pickupIcon.sprite = icon;
                _pickupIcon.enabled = icon != null;
            }
        }

        private Sprite PickupIcon()
        {
            if (_mode == ModePet)
            {
                PickupBanner banner = _petSummon?.Pickup;
                int index = banner != null && banner.Targets.Length > 0 ? PetCatalog.IndexOf(banner.Targets[0]) : -1;
                CharacterArt art = index >= 0 && index < _petArts.Length ? _petArts[index] : null;
                return art != null && art.idle.Length > 0 ? art.idle[0] : null;
            }

            SkillDef def = PickupSkill();
            return def != null && _skillIcons != null ? _skillIcons.Get(def.Id) : null;
        }

        private static string TimeLeft(long seconds)
        {
            long days = seconds / 86400;
            long hours = seconds % 86400 / 3600;
            long minutes = seconds % 3600 / 60;
            return days > 0 ? Strings.Format("event.time_days", days, hours) : Strings.Format("event.time_hours", hours, minutes);
        }

        /// <summary>One disclosed grade: its rate in its colour, or (D-121) the summon level that opens it, dimmed.</summary>
        private void AppendRate(int index, GearGrade grade, double percent, int opensAt)
        {
            if (index > 0) _sb.Append(index % 4 == 0 ? "\n" : "  ");
            if (opensAt > 0)
            {
                _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiPalette.InkMuted)).Append('>')
                    .Append(Strings.Format("gacha.rate_locked", PanelServices.GradeName(grade), opensAt))
                    .Append("</color>");
                return;
            }

            percent = System.Math.Round(percent, 4);
            _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiPalette.GradeInk(grade))).Append('>')
                .Append(Strings.Format("gacha.rate_item", PanelServices.GradeName(grade), percent.ToString("0.###", CultureInfo.InvariantCulture)))
                .Append("</color>");
        }

        /// <summary>D-127: prices shorten like every other number (1.35K, 45K).</summary>
        private static string Price(double gold) => BigNumberFormat.Format(gold);

        private static GearGrade BestGrade(GachaPullItem[] items)
        {
            GearGrade best = GearGrade.Common;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Grade > best) best = items[i].Grade;
            }

            return best;
        }

        /// <summary>best_grade is the gear ladder index (0 Common .. 6 Ancient) for gear and the skill grade for skills.</summary>
        private void LogPull(string eventName, long best, int count, string kind, int pity)
        {
            // D-173: the pickup banner of the page when it ran ("none" otherwise).
            PickupBanner banner = _mode == ModePet ? _petSummon?.Pickup : _mode == ModeSkill ? _skillSummon?.Pickup : null;
            GameAnalytics.Log(eventName,
                AnalyticsParam.Of(AnalyticsEvents.PKind, kind),
                AnalyticsParam.Of(AnalyticsEvents.PCount, count),
                AnalyticsParam.Of(AnalyticsEvents.PBestGrade, best),
                AnalyticsParam.Of(AnalyticsEvents.PPity, pity),
                AnalyticsParam.Of(AnalyticsEvents.PPickup, banner != null ? banner.Id : "none"));
        }

        /// <summary>
        /// The last pull at a glance (the card reveal shows each item): grade counts in grade colours, then what
        /// happened to the items - equipped, enhanced, stored, refunded.
        /// </summary>
        private void DrawResult(GachaPullItem[] items)
        {
            if (_resultText == null) return;
            var counts = new int[GachaCatalog.GradeCount];
            int equipped = 0, enhanced = 0, stored = 0, refunded = 0;
            for (int i = 0; i < items.Length; i++)
            {
                GachaPullItem item = items[i];
                counts[(int)item.Grade]++;
                if (item.EnhancedLevel > 0) enhanced++;
                else if (item.WasDuplicate) refunded++;
                else if (item.AutoEquipped) equipped++;
                else stored++;
            }

            AppendGradeCounts(counts);
            _sb.Append('\n').Append(Strings.Format("gacha.summary", equipped, enhanced, stored, refunded));
            _resultText.text = _sb.ToString();
        }

        private void DrawPetResult(PetPullItem[] items)
        {
            if (_resultText == null) return;
            var counts = new int[GachaCatalog.GradeCount];
            int fresh = 0, up = 0, refunded = 0;
            for (int i = 0; i < items.Length; i++)
            {
                counts[(int)items[i].Grade]++;
                if (items[i].WasNew) fresh++;
                else if (items[i].RefundGold > 0d) refunded++;
                else up++;
            }

            AppendGradeCounts(counts);
            _sb.Append('\n').Append(Strings.Format("gacha.pet_summary", fresh, up, refunded));
            _resultText.text = _sb.ToString();
        }

        private void DrawAdButton(bool locked)
        {
            if (_adButton == null) return;
            bool show = _mode != ModeSkill && _adPolicy != null;
            if (_adButton.gameObject.activeSelf != show) _adButton.gameObject.SetActive(show);
            if (!show) return;
            AdSlot slot = _mode == ModePet ? AdSlot.FreePetSummon : AdSlot.FreeGearSummon;
            int left = _adPolicy.Remaining(slot);
            if (_adLabel != null) _adLabel.text = Strings.Format("ad.free_summon", _balance.AD_FREE_SUMMON_PULLS, left, _adPolicy.DailyLimit(slot));
            _adButton.SetAvailable(!locked && !_adBusy && left > 0);
        }

        /// <summary>"c-s" label of a global stage index.</summary>
        private string StageLabel(int g)
        {
            int per = _balance.STAGES_PER_CHAPTER;
            return ((g - 1) / per + 1) + "-" + ((g - 1) % per + 1);
        }

        private void DrawSkillResult(SkillPullItem[] items)
        {
            if (_resultText == null) return;
            var counts = new int[GachaCatalog.GradeCount];
            int fresh = 0, up = 0, refunded = 0;
            for (int i = 0; i < items.Length; i++)
            {
                counts[(int)items[i].Grade.ToGearGrade()]++;
                if (items[i].WasNew) fresh++;
                else if (items[i].RefundGold > 0d) refunded++;
                else up++;
            }

            AppendGradeCounts(counts);
            _sb.Append('\n').Append(Strings.Format("gacha.skill_summary", fresh, up, refunded));
            _resultText.text = _sb.ToString();
        }

        private void AppendGradeCounts(int[] counts)
        {
            _sb.Clear();
            for (int g = GachaCatalog.GradeCount - 1; g >= 0; g--)
            {
                if (counts[g] == 0) continue;
                if (_sb.Length > 0) _sb.Append("   ");
                _sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(UiPalette.GradeInk((GearGrade)g))).Append('>')
                    .Append(PanelServices.GradeName((GearGrade)g)).Append(' ').Append(counts[g]).Append("</color>");
            }
        }
    }
}
