using System;
using SoloHero.Core.Config;
using SoloHero.Core.Equipment;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Balance
{
    /// <summary>
    /// Greedy player model: every option (4 upgrade lanes, 3 skills, one gold pull) is scored by
    /// expected gain in 2 ln(DPS) + ln(EHP) per gold, and the best one is bought when affordable.
    /// When the best option is not affordable the player saves for it instead of buying a worse one.
    /// </summary>
    public sealed class SimSpender
    {
        private const int WhirlwindAssumedTargets = 2;

        /// <summary>Clear speed counts twice: DPS sets both stage time and survival (fight length), EHP only survival.</summary>
        private const double DpsWeight = 2d;

        private readonly BalanceValues _b;
        private readonly SaveDataV2 _save;
        private readonly GachaService _gacha;
        private readonly GachaTableValues _table;
        private readonly UpgradeService _upgrades;
        private readonly SkillLevelService _skills;

        public SimSpender(BalanceValues balance, SaveDataV2 save, GachaService gacha)
        {
            _b = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _gacha = gacha ?? throw new ArgumentNullException(nameof(gacha));
            _table = GachaTableValues.FromBalance(balance);
            _upgrades = new UpgradeService(save, balance);
            _skills = new SkillLevelService(save, balance);
        }

        public double SpentUpgrade { get; private set; }
        public double SpentGacha { get; private set; }
        public double SpentSkill { get; private set; }
        public double EarnedRefund { get; private set; }
        public int GoldPulls { get; private set; }
        public int GemPulls { get; private set; }
        public int Upgrades { get; private set; }
        public int TutorialPulls { get; private set; }

        public event Action<Grade> GradeObtained;

        private enum Kind { None, Lane, Skill, Pull }

        /// <summary>Spends until the best option is unaffordable. Returns true when anything was bought.</summary>
        public bool Spend(int frontierG, int maxPurchases)
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

            double enemyAtk = Formulas.EnemyAtk(_b, frontierG < 1 ? 1 : frontierG);
            for (int n = 0; n < maxPurchases; n++)
            {
                Snapshot now = Snapshot.From(_save);
                double baseScore = Score(now, enemyAtk);

                Kind bestKind = Kind.None;
                int bestIndex = -1;
                double bestRatio = 0d;
                double bestCost = 0d;

                for (int lane = 0; lane < 4; lane++)
                {
                    var l = (UpgradeLane)lane;
                    int level = _upgrades.GetLevel(l);
                    if (l == UpgradeLane.Spd && level >= _b.UPG_MAX_LEVEL_SPD) continue;
                    double cost = Formulas.UpgradeCost(_b, l, level);
                    Snapshot next = now;
                    next.AddLane(l);
                    double ratio = (Score(next, enemyAtk) - baseScore) / cost;
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestKind = Kind.Lane;
                        bestIndex = lane;
                        bestCost = cost;
                    }
                }

                for (int s = 0; s < 3; s++)
                {
                    var slot = (SkillSlot)s;
                    if (!SkillLevelService.IsUnlocked(_b, slot, _save.heroLevel)) continue;
                    int level = SkillLevelService.EffectiveLevel(_skills.GetSavedLevel(slot));
                    if (level >= _b.SKILL_MAX_LEVEL) continue;
                    double cost = SkillLevelService.UpgradeCost(_b, slot, level);
                    Snapshot next = now;
                    next.SetSkill(s, level + 1);
                    double ratio = (Score(next, enemyAtk) - baseScore) / cost;
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        bestKind = Kind.Skill;
                        bestIndex = s;
                        bestCost = cost;
                    }
                }

                double pullRatio = ExpectedPullRatio(now, baseScore, enemyAtk);
                if (pullRatio > bestRatio)
                {
                    bestRatio = pullRatio;
                    bestKind = Kind.Pull;
                    bestCost = _b.GACHA_COST_SINGLE;
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
                        if (!_skills.TryLevelUp((SkillSlot)bestIndex).Ok) return bought;
                        SpentSkill += bestCost;
                        break;
                    case Kind.Pull:
                        if (!PullGold()) return bought;
                        break;
                }

                bought = true;
            }

            return bought;
        }

        /// <summary>Tutorial gift: pulls that cost the player nothing. Not counted as gold earned or spent.</summary>
        public void FreePulls(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _save.gold += _b.GACHA_COST_SINGLE;
                GachaBatchResult r = _gacha.TryPull(_save);
                if (!r.Status.Ok) return;
                TutorialPulls += r.Items.Length;
                Collect(r);
            }
        }

        /// <summary>2 ln(DPS) + ln(EHP) against the frontier enemy. DPS counts basic hits, crit and skills.</summary>
        public double Score(Snapshot s, double enemyAtk)
        {
            EquipmentBonus eq = EquipmentBonus.FromGrades(_b, s.Sword, s.Helm, s.Armor, s.Boots);
            HeroStats st = StatAggregator.Compute(
                _b, s.HeroLevel, s.UpgHp, s.UpgAtk, s.UpgDef, s.UpgSpd,
                eq.SwordMult, eq.ArmorMult, eq.HelmMult, eq.BootsSpeedBonus, eq.BootsCritBonus);

            double crit = Math.Min(1d, st.CritRate / 100d);
            double hitsPerSecond = st.AtkSpd * (1d + crit * (_b.CRIT_MULT - 1d));
            double skills = SkillLevelService.DamageMultiplier(_b, SkillSlot.Slot1, s.Skill1) / _b.SKILL_CD_1;
            if (SkillLevelService.IsUnlocked(_b, SkillSlot.Slot2, s.HeroLevel))
                skills += SkillLevelService.DamageMultiplier(_b, SkillSlot.Slot2, s.Skill2) * WhirlwindAssumedTargets / _b.SKILL_CD_2;
            double buff = 1d;
            if (SkillLevelService.IsUnlocked(_b, SkillSlot.Slot3, s.HeroLevel))
                buff += SkillLevelService.BattleCryAtkBuffFraction(_b, s.Skill3) * Math.Min(1d, _b.BATTLECRY_DURATION / _b.SKILL_CD_3);

            double dps = st.Atk * (hitsPerSecond + skills) * buff;
            double defRef = _b.DEF_REF_MULT * enemyAtk;
            double ehp = st.Hp * (defRef + st.Def) / defRef;
            return DpsWeight * Math.Log(dps) + Math.Log(ehp);
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

                    if (_save.ownedEquipment.Contains(GachaCatalog.IdOf(slot, grade)))
                    {
                        refund += p * GachaCatalog.RefundOf(_b, grade);
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
            public int Skill1;
            public int Skill2;
            public int Skill3;
            public int Sword;
            public int Helm;
            public int Armor;
            public int Boots;

            public static Snapshot From(SaveDataV2 d) => new Snapshot
            {
                HeroLevel = d.heroLevel,
                UpgHp = d.upgradeHp,
                UpgAtk = d.upgradeAtk,
                UpgDef = d.upgradeDef,
                UpgSpd = d.upgradeSpd,
                Skill1 = SkillLevelService.EffectiveLevel(d.skillLevel1),
                Skill2 = SkillLevelService.EffectiveLevel(d.skillLevel2),
                Skill3 = SkillLevelService.EffectiveLevel(d.skillLevel3),
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

            public void SetSkill(int index, int level)
            {
                if (index == 0) Skill1 = level;
                else if (index == 1) Skill2 = level;
                else Skill3 = level;
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

            public void SetGrade(int slot, int grade)
            {
                switch ((EquipmentSlot)slot)
                {
                    case EquipmentSlot.Sword: Sword = grade; break;
                    case EquipmentSlot.Helm: Helm = grade; break;
                    case EquipmentSlot.Armor: Armor = grade; break;
                    default: Boots = grade; break;
                }
            }
        }
    }
}
