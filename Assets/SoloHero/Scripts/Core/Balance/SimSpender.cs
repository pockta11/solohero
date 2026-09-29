using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Balance
{
    /// <summary>
    /// Greedy player model: every option (4 upgrade lanes, a level-up for each owned skill, one equipment pull, one
    /// skill summon) is scored by expected gain in 2 ln(DPS) + ln(EHP) per gold, and the best one is bought when
    /// affordable. When the best option is not affordable the player saves for it instead of buying a worse one.
    /// After a skill summon the player auto-equips the strongest skills (the panel's auto-equip button).
    /// Talent points (D-087) are spent as soon as they arrive, following <see cref="TalentPlan"/>.
    /// </summary>
    public sealed class SimSpender
    {
        /// <summary>Clear speed counts twice: DPS sets both stage time and survival (fight length), EHP only survival.</summary>
        private const double DpsWeight = 2d;

        private readonly BalanceValues _b;
        private readonly SaveDataV2 _save;
        private readonly GachaService _gacha;
        private readonly SkillSummonService _summon;
        private readonly GachaTableValues _table;
        private readonly UpgradeService _upgrades;
        private readonly SkillService _skills;
        private readonly TalentService _talents;

        /// <summary>A typical first build: damage and survival first, then skills, then the deeper tiers.</summary>
        public static readonly string[] TalentPlan =
        {
            "sharpness", "vitality", "precision", "focus", "ferocity", "iron_hide", "haste", "swiftness",
            "giant_slayer", "endurance", "persistence", "mastery", "execute", "mending", "fortitude",
            "last_stand", "venom", "overload"
        };

        public SimSpender(BalanceValues balance, SaveDataV2 save, GachaService gacha, SkillSummonService summon)
        {
            _b = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _gacha = gacha ?? throw new ArgumentNullException(nameof(gacha));
            _summon = summon ?? throw new ArgumentNullException(nameof(summon));
            _table = GachaTableValues.FromBalance(balance);
            _upgrades = new UpgradeService(save, balance);
            _skills = new SkillService(save, balance);
            _talents = new TalentService(save, balance);
        }

        public double SpentUpgrade { get; private set; }
        public double SpentGacha { get; private set; }

        /// <summary>Skill level-ups and skill summons.</summary>
        public double SpentSkill { get; private set; }
        public double EarnedRefund { get; private set; }
        public int GoldPulls { get; private set; }
        public int GemPulls { get; private set; }
        public int SkillPulls { get; private set; }
        public int Upgrades { get; private set; }
        public int TutorialPulls { get; private set; }

        /// <summary>Gold pull value per gold / best upgrade-lane value per gold, one sample per spending decision.</summary>
        public readonly System.Collections.Generic.List<double> PullToUpgradeValue = new System.Collections.Generic.List<double>();

        public event Action<Grade> GradeObtained;

        private enum Kind { None, Lane, Skill, Pull, SkillPull }

        /// <summary>Spends until the best option is unaffordable. Returns true when anything was bought.</summary>
        public bool Spend(int frontierG, int maxPurchases, double affordableShare = 1d)
        {
            bool bought = false;

            while (_save.gem >= _b.GACHA_COST_TEN_GEM)
            {
                GachaBatchResult r = _gacha.TryPullTenWithGem(_save);
                if (!r.Status.Ok) break;
                GemPulls += r.Items.Length;
                Collect(r);
                bought = true;
            }

            AllocateTalents();

            // New slots open with hero levels; the player fills them.
            if (_skills.FirstEmptyUnlockedSlot() >= 0) _skills.AutoEquip();

            double enemyAtk = Formulas.EnemyAtk(_b, frontierG < 1 ? 1 : frontierG);
            RecordValueParity(enemyAtk);
            for (int n = 0; n < maxPurchases; n++)
            {
                Snapshot now = Snapshot.From(_b, _save);
                double baseScore = Score(now, enemyAtk);

                Kind bestKind = Kind.None;
                string bestId = null;
                int bestIndex = -1;
                double bestRatio = 0d;
                double bestCost = 0d;
                Kind cheapKind = Kind.None;
                string cheapId = null;
                int cheapIndex = -1;
                double cheapRatio = 0d;
                double cheapCost = 0d;

                void Consider(Kind kind, int index, string id, double ratio, double cost)
                {
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestKind = kind;
                        bestIndex = index;
                        bestId = id;
                        bestCost = cost;
                    }

                    if (cost <= _save.gold && ratio > cheapRatio)
                    {
                        cheapRatio = ratio;
                        cheapKind = kind;
                        cheapIndex = index;
                        cheapId = id;
                        cheapCost = cost;
                    }
                }

                for (int lane = 0; lane < 4; lane++)
                {
                    var l = (UpgradeLane)lane;
                    int level = _upgrades.GetLevel(l);
                    if (l == UpgradeLane.Spd && level >= _b.UPG_MAX_LEVEL_SPD) continue;
                    double cost = Formulas.UpgradeCost(_b, l, level);
                    Snapshot next = now;
                    next.AddLane(l);
                    Consider(Kind.Lane, lane, null, (Score(next, enemyAtk) - baseScore) / cost, cost);
                }

                for (int i = 0; i < _save.ownedSkills.Count; i++)
                {
                    SkillDef def = SkillCatalog.Find(_save.ownedSkills[i]);
                    if (def == null) continue;
                    int level = SkillBook.GetLevel(_save, def.Id);
                    if (level >= _b.SKILL_MAX_LEVEL) continue;
                    double cost = SkillService.UpgradeCost(_b, def, level);
                    Snapshot next = now;
                    next.Skill = SimSkillModel.Compute(_b, _save, def.Id, level + 1);
                    Consider(Kind.Skill, -1, def.Id, (Score(next, enemyAtk) - baseScore) / cost, cost);
                }

                Consider(Kind.Pull, -1, null, ExpectedPullRatio(now, baseScore, enemyAtk), _b.GACHA_COST_SINGLE);
                Consider(Kind.SkillPull, -1, null, ExpectedSkillPullRatio(now, baseScore, enemyAtk), _b.SKILL_SUMMON_COST_SINGLE);

                if (bestKind != Kind.None && _save.gold < bestCost && cheapKind != Kind.None
                    && cheapRatio >= bestRatio * affordableShare)
                {
                    bestKind = cheapKind;
                    bestIndex = cheapIndex;
                    bestId = cheapId;
                    bestRatio = cheapRatio;
                    bestCost = cheapCost;
                }

                if (bestKind == Kind.None || _save.gold < bestCost) break;

                switch (bestKind)
                {
                    case Kind.Lane:
                        if (!_upgrades.TryUpgrade((UpgradeLane)bestIndex).Ok) return bought;
                        SpentUpgrade += bestCost;
                        Upgrades++;
                        break;
                    case Kind.Skill:
                        if (!_skills.TryLevelUp(bestId).Ok) return bought;
                        SpentSkill += bestCost;
                        break;
                    case Kind.Pull:
                        if (!PullGold()) return bought;
                        break;
                    case Kind.SkillPull:
                        if (!SummonSkill()) return bought;
                        break;
                }

                bought = true;
            }

            return bought;
        }

        /// <summary>Power score of the current loadout against the frontier enemy.</summary>
        public double CurrentScore(int frontierG) =>
            Score(Snapshot.From(_b, _save), Formulas.EnemyAtk(_b, frontierG < 1 ? 1 : frontierG));

        /// <summary>Records tutorial free pulls (made by TutorialService) without counting them as gold spent.</summary>
        public void CountTutorialPulls(GachaPullItem[] items)
        {
            TutorialPulls += items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                EarnedRefund += items[i].RefundGold;
                GradeObtained?.Invoke(items[i].Grade);
            }
        }

        /// <summary>2 ln(DPS) + ln(EHP) against the frontier enemy. DPS counts basic hits, crit, skills and buffs.</summary>
        /// <summary>Spends every free talent point on the first plan entry that can take a rank.</summary>
        public int AllocateTalents()
        {
            int learned = 0;
            while (_talents.AvailablePoints > 0)
            {
                bool any = false;
                for (int i = 0; i < TalentPlan.Length; i++)
                {
                    if (!_talents.TryLearn(TalentPlan[i]).Ok) continue;
                    learned++;
                    any = true;
                    break;
                }

                if (!any) break;
            }

            return learned;
        }

        public double Score(Snapshot s, double enemyAtk)
        {
            EquipmentBonus eq = EquipmentBonus.FromGrades(_b, s.Sword, s.Helm, s.Armor, s.Boots,
                s.SwordLevel, s.HelmLevel, s.ArmorLevel, s.BootsLevel);
            HeroStats st = StatAggregator.Compute(
                _b, s.HeroLevel, s.UpgHp, s.UpgAtk, s.UpgDef, s.UpgSpd,
                eq.SwordMult, eq.ArmorMult, eq.HelmMult, eq.BootsSpeedBonus, eq.BootsCritBonus,
                default, s.Skill.OwnedAtk);

            double crit = Math.Min(1d, (st.CritRate + s.Skill.CritBuff) / 100d);
            double hitsPerSecond = st.AtkSpd * (1d + s.Skill.SpdBuff) * (1d + crit * (_b.CRIT_MULT - 1d));
            double dps = st.Atk * (hitsPerSecond + s.Skill.Mult) * (1d + s.Skill.AtkBuff);
            double defRef = _b.DEF_REF_MULT * enemyAtk;
            double guard = Math.Min(0.9d, s.Skill.Guard);
            double ehp = st.Hp * (defRef + st.Def) / defRef / (1d - guard);
            return DpsWeight * Math.Log(dps) + Math.Log(ehp);
        }

        private void RecordValueParity(double enemyAtk)
        {
            Snapshot now = Snapshot.From(_b, _save);
            double baseScore = Score(now, enemyAtk);
            double bestLane = 0d;
            for (int lane = 0; lane < 4; lane++)
            {
                var l = (UpgradeLane)lane;
                int level = _upgrades.GetLevel(l);
                if (l == UpgradeLane.Spd && level >= _b.UPG_MAX_LEVEL_SPD) continue;
                Snapshot next = now;
                next.AddLane(l);
                double ratio = (Score(next, enemyAtk) - baseScore) / Formulas.UpgradeCost(_b, l, level);
                if (ratio > bestLane) bestLane = ratio;
            }

            if (bestLane > 0d)
                PullToUpgradeValue.Add(ExpectedPullRatio(now, baseScore, enemyAtk) / bestLane);
        }

        private bool PullGold()
        {
            bool ten = _save.gold >= _b.GACHA_COST_TEN;
            GachaBatchResult r = ten ? _gacha.TryPullTen(_save) : _gacha.TryPull(_save);
            if (!r.Status.Ok) return false;
            SpentGacha += ten ? _b.GACHA_COST_TEN : _b.GACHA_COST_SINGLE;
            GoldPulls += r.Items.Length;
            Collect(r);
            return true;
        }

        private bool SummonSkill()
        {
            bool ten = _save.gold >= _b.SKILL_SUMMON_COST_TEN;
            SkillSummonResult r = ten ? _summon.TryPullTen(_save) : _summon.TryPull(_save);
            if (!r.Status.Ok) return false;
            SpentSkill += ten ? _b.SKILL_SUMMON_COST_TEN : _b.SKILL_SUMMON_COST_SINGLE;
            SkillPulls += r.Items.Length;
            for (int i = 0; i < r.Items.Length; i++) EarnedRefund += r.Items[i].RefundGold;
            _skills.AutoEquip();
            return true;
        }

        private void Collect(GachaBatchResult r)
        {
            for (int i = 0; i < r.Items.Length; i++)
            {
                GachaPullItem item = r.Items[i];
                EarnedRefund += item.RefundGold;
                GradeObtained?.Invoke(item.Grade);
            }
        }

        private double ExpectedPullRatio(Snapshot now, double baseScore, double enemyAtk)
        {
            bool pityNext = _save.pityCount + 1 >= _table.PityCeiling;
            double gain = 0d;
            double refund = 0d;

            for (int s = 0; s < GachaCatalog.SlotCount; s++)
            {
                var slot = (EquipmentSlot)s;
                for (int g = 0; g < GachaCatalog.GradeCount; g++)
                {
                    var grade = (Grade)g;
                    double p = GradeProbability(grade, pityNext) / GachaCatalog.SlotCount;
                    if (p <= 0d) continue;

                    string id = GachaCatalog.IdOf(slot, grade);
                    if (_save.ownedEquipment.Contains(id))
                    {
                        int level = EquipmentLevels.Get(_save, id);
                        if (level >= _b.EQUIP_MAX_LEVEL)
                        {
                            refund += p * GachaCatalog.RefundOf(_b, grade);
                            continue;
                        }

                        // A duplicate enhances the copy; it only adds power now if that copy is equipped.
                        if (g != now.Grade(s)) continue;
                        Snapshot enhanced = now;
                        enhanced.SetLevel(s, level + 1);
                        gain += p * (Score(enhanced, enemyAtk) - baseScore);
                        continue;
                    }

                    if (g <= now.Grade(s)) continue;
                    Snapshot next = now;
                    next.SetGrade(s, g);
                    gain += p * (Score(next, enemyAtk) - baseScore);
                }
            }

            double net = _b.GACHA_COST_SINGLE - refund;
            if (net <= 0d) net = 1d;
            return gain / net;
        }

        /// <summary>Expected score gain of one skill summon per net gold; a new or levelled skill is auto-equipped.</summary>
        private double ExpectedSkillPullRatio(Snapshot now, double baseScore, double enemyAtk)
        {
            bool pityNext = _save.skillPityCount + 1 >= _table.PityCeiling;
            double gain = 0d;
            double refund = 0d;
            for (int g = 0; g < GachaCatalog.GradeCount; g++)
            {
                var grade = (Grade)g;
                SkillDef[] pool = SkillCatalog.OfGrade(grade);
                double p = GradeProbability(grade, pityNext) / pool.Length;
                if (p <= 0d) continue;
                for (int i = 0; i < pool.Length; i++)
                {
                    int level = SkillBook.GetLevel(_save, pool[i].Id);
                    if (level >= _b.SKILL_MAX_LEVEL)
                    {
                        refund += p * Formulas.SkillRefund(_b, grade);
                        continue;
                    }

                    Snapshot next = now;
                    next.Skill = SimSkillModel.Compute(_b, _save, pool[i].Id, level + 1, autoEquip: true);
                    gain += p * (Score(next, enemyAtk) - baseScore);
                }
            }

            double net = _b.SKILL_SUMMON_COST_SINGLE - refund;
            if (net <= 0d) net = 1d;
            return gain / net;
        }

        private double GradeProbability(Grade grade, bool pityNext)
        {
            if (pityNext) return grade == Grade.Legendary ? 1d : 0d;
            switch (grade)
            {
                case Grade.Common: return _table.RateC / 100d;
                case Grade.Rare: return _table.RateR / 100d;
                case Grade.Epic: return _table.RateE / 100d;
                default: return _table.RateL / 100d;
            }
        }

        /// <summary>Value copy of the growth state that <see cref="Score"/> reads.</summary>
        public struct Snapshot
        {
            public int HeroLevel;
            public int UpgHp;
            public int UpgAtk;
            public int UpgDef;
            public int UpgSpd;
            public SkillPower Skill;
            public int Sword;
            public int Helm;
            public int Armor;
            public int Boots;
            public int SwordLevel;
            public int HelmLevel;
            public int ArmorLevel;
            public int BootsLevel;

            public static Snapshot From(BalanceValues b, SaveDataV2 d) => new Snapshot
            {
                SwordLevel = EquipmentLevels.Get(d, d.equippedSword),
                HelmLevel = EquipmentLevels.Get(d, d.equippedHelm),
                ArmorLevel = EquipmentLevels.Get(d, d.equippedArmor),
                BootsLevel = EquipmentLevels.Get(d, d.equippedBoots),
                HeroLevel = d.heroLevel,
                UpgHp = d.upgradeHp,
                UpgAtk = d.upgradeAtk,
                UpgDef = d.upgradeDef,
                UpgSpd = d.upgradeSpd,
                Skill = SimSkillModel.Compute(b, d),
                Sword = EquipmentBonus.GradeOrNone(d.equippedSword, EquipmentSlot.Sword),
                Helm = EquipmentBonus.GradeOrNone(d.equippedHelm, EquipmentSlot.Helm),
                Armor = EquipmentBonus.GradeOrNone(d.equippedArmor, EquipmentSlot.Armor),
                Boots = EquipmentBonus.GradeOrNone(d.equippedBoots, EquipmentSlot.Boots)
            };

            public void AddLane(UpgradeLane lane)
            {
                switch (lane)
                {
                    case UpgradeLane.Hp: UpgHp++; break;
                    case UpgradeLane.Atk: UpgAtk++; break;
                    case UpgradeLane.Def: UpgDef++; break;
                    case UpgradeLane.Spd: UpgSpd++; break;
                }
            }

            public int Grade(int slot)
            {
                switch ((EquipmentSlot)slot)
                {
                    case EquipmentSlot.Sword: return Sword;
                    case EquipmentSlot.Helm: return Helm;
                    case EquipmentSlot.Armor: return Armor;
                    default: return Boots;
                }
            }

            /// <summary>Equips a new copy of <paramref name="grade"/>; a fresh copy starts at level 0.</summary>
            public void SetGrade(int slot, int grade)
            {
                switch ((EquipmentSlot)slot)
                {
                    case EquipmentSlot.Sword: Sword = grade; SwordLevel = 0; break;
                    case EquipmentSlot.Helm: Helm = grade; HelmLevel = 0; break;
                    case EquipmentSlot.Armor: Armor = grade; ArmorLevel = 0; break;
                    default: Boots = grade; BootsLevel = 0; break;
                }
            }

            public void SetLevel(int slot, int level)
            {
                switch ((EquipmentSlot)slot)
                {
                    case EquipmentSlot.Sword: SwordLevel = level; break;
                    case EquipmentSlot.Helm: HelmLevel = level; break;
                    case EquipmentSlot.Armor: ArmorLevel = level; break;
                    default: BootsLevel = level; break;
                }
            }
        }
    }
}
