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
    /// Summon panel (E7-07, D-078, D-114): equipment / skill / pet tabs on top (genre summon screen), then single / 10 /
    /// gem 10 pulls, the pity gauge of the shown tab, rate disclosure and the last result. The pet page opens after
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
        [SerializeField] private Text _singleCostText;
        [SerializeField] private Text _tenCostText;
        [SerializeField] private Text _gemCostText;
        [SerializeField] private TapGuardButton _singleButton;
        [SerializeField] private TapGuardButton _tenButton;
        [SerializeField] private TapGuardButton _gemButton;
        [SerializeField] private CombatSession _session;
        [SerializeField] private ToastQueue _toast;
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
            _shownGold = -1d;
            DrawStatic();
            Refresh();
        }

        private void LateUpdate()
        {
            if (_save == null) return;
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

            if (_toast != null)
            {
                string summon = Strings.Get(ModeKeys[_mode]);
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

        public void PullSingle()
        {
            int before = CurrentLevel;
            PullSingleNow();
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

        private void PullSingleNow()
        {
            if (_mode == ModePet) ApplyPet(_petSummon != null && _save != null ? _petSummon.TryPull(_save) : default, "pet_gold_single");
            else if (_mode == ModeSkill) ApplySkill(_skillSummon != null && _save != null ? _skillSummon.TryPull(_save) : default, "skill_gold_single");
            else Apply(_gacha != null && _save != null ? _gacha.TryPull(_save) : default, "gold_single");
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

        /// <summary>D-128: up to ten pulls of the shown summon, paid with its tickets.</summary>
        public void UseTickets()
        {
            if (_save == null) return;
            int before = CurrentLevel;
            if (_mode == ModePet) ApplyPet(_petSummon != null ? _petSummon.TryPullTickets(_save, 10) : default, "pet_ticket");
            else if (_mode == ModeSkill) ApplySkill(_skillSummon != null ? _skillSummon.TryPullTickets(_save, 10) : default, "skill_ticket");
            else Apply(_gacha != null ? _gacha.TryPullTickets(_save, 10) : default, "ticket");
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
                cards[i] = new RevealCard(item.Grade, def != null ? Strings.Get(def.NameKey) : item.Id, PetNote(item), icon, PetIconScale);
            }

            return cards;
        }

        /// <summary>Pet sheets are 48 px frames with the creature in the lower middle; scale them up on the card.</summary>
        private const float PetIconScale = 1.9f;

        private static string PetNote(PetPullItem item)
        {
            if (item.WasNew) return Strings.Get(item.AutoEquipped ? "gacha.pet_new_equipped" : "gacha.skill_new");
            if (item.RefundGold > 0d) return Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold));
            return Strings.Format("gacha.enhanced", item.EnhancedLevel);
        }

        private RevealCard[] Cards(GachaPullItem[] items)
        {
            var cards = new RevealCard[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                GachaPullItem item = items[i];
                Sprite icon = _equipmentIcons != null ? _equipmentIcons.Get(item.Slot, item.Grade) : null;
                cards[i] = new RevealCard(item.Grade, PanelServices.SlotName(item.Slot), PanelServices.PullNote(item), icon);
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
                cards[i] = new RevealCard(item.Grade.ToGearGrade(), def != null ? Strings.Get(def.NameKey) : item.Id, SkillNote(item), icon);
            }

            return cards;
        }

        private static string SkillNote(SkillPullItem item)
        {
            if (item.WasNew) return Strings.Get("gacha.skill_new");
            if (item.RefundGold > 0d) return Strings.Format("gacha.refund", BigNumberFormat.Format(item.RefundGold));
            return Strings.Format("gacha.skill_up", item.Level);
        }

        private double SingleCost => _mode == ModePet ? _balance.PET_SUMMON_COST_SINGLE
            : _mode == ModeSkill ? _balance.SKILL_SUMMON_COST_SINGLE : _balance.GACHA_COST_SINGLE;

        private double TenCost => _mode == ModePet ? _balance.PET_SUMMON_COST_TEN
            : _mode == ModeSkill ? _balance.SKILL_SUMMON_COST_TEN : _balance.GACHA_COST_TEN;

        private int GemCost => _mode == ModePet ? _balance.PET_SUMMON_COST_TEN_GEM
            : _mode == ModeSkill ? _balance.SKILL_SUMMON_COST_TEN_GEM : _balance.GACHA_COST_TEN_GEM;

        private void DrawStatic()
        {
            if (_balance == null) return;
            if (_rateText != null) _rateText.text = RatesText(_mode);
            if (_singleCostText != null) _singleCostText.text = Strings.Format("gacha.cost_single", Price(SingleCost));
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

            if (_singleButton != null) _singleButton.SetAvailable(!locked && _save.gold >= SingleCost);
            if (_tenButton != null) _tenButton.SetAvailable(!locked && _save.gold >= TenCost);
            if (_gemButton != null) _gemButton.SetAvailable(!locked && _save.gem >= GemCost);
            _shownFarmingStage = _save.farmingStage;
            int tickets = ShopService.TicketsOf(_save, Kind);
            if (_ticketLabel != null)
                _ticketLabel.text = Strings.Format("gacha.tickets", System.Math.Min(10, tickets), BigNumberFormat.Format(tickets));
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
                // D-121: when the next level opens a grade, the line says which.
                int left = SummonLevel.PullsFor(_balance, kind, level + 1) - SummonLevel.Pulls(_save, kind);
                _sb.Append(opening >= 0
                    ? Strings.Format("gacha.level_open", level, BigNumberFormat.Format(left), PanelServices.GradeName(LadderGrade(mode, opening)))
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
            }
            else
            {
                GearTableValues table = GearTable.AtLevel(level);
                for (int g = 0; g < table.Rates.Length; g++) AppendRate(g, (GearGrade)g, table.Rates[g], open[g] > level ? open[g] : 0);
                _sb.Append('\n').Append(pityOpen
                    ? Strings.Format(mode == ModePet ? "gacha.rates_pity_pet" : "gacha.rates_pity_gear", table.PityCeiling)
                    : Strings.Format("gacha.rates_pity_locked", GearTable.OpenLevel(GearTableValues.PityGrade), table.PityCeiling));
            }

            return _sb.ToString();
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
        private static void LogPull(string eventName, long best, int count, string kind, int pity)
        {
            GameAnalytics.Log(eventName,
                AnalyticsParam.Of(AnalyticsEvents.PKind, kind),
                AnalyticsParam.Of(AnalyticsEvents.PCount, count),
                AnalyticsParam.Of(AnalyticsEvents.PBestGrade, best),
                AnalyticsParam.Of(AnalyticsEvents.PPity, pity));
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
