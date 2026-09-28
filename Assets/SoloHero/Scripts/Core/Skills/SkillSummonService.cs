using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Skills
{
    /// <summary>One skill from a summon: new (auto-equipped into an empty slot if any), a level-up, or a refund.</summary>
    public readonly struct SkillPullItem
    {
        public readonly string Id;
        public readonly Grade Grade;
        public readonly bool WasNew;

        /// <summary>Level after the pull: 1 for a new skill, +1 for a duplicate below max, unchanged when refunded.</summary>
        public readonly int Level;
        public readonly double RefundGold;
        public readonly bool AutoEquipped;

        public SkillPullItem(string id, Grade grade, bool wasNew, int level, double refundGold, bool autoEquipped)
        {
            Id = id;
            Grade = grade;
            WasNew = wasNew;
            Level = level;
            RefundGold = refundGold;
            AutoEquipped = autoEquipped;
        }
    }

    public readonly struct SkillSummonResult
    {
        public readonly Result Status;
        public readonly SkillPullItem[] Items;

        public SkillSummonResult(Result status, SkillPullItem[] items)
        {
            Status = status;
            Items = items;
        }

        public static SkillSummonResult Fail(FailReason reason) => new SkillSummonResult(Result.Fail(reason), Array.Empty<SkillPullItem>());
    }

    /// <summary>
    /// Skill summon (D-078, genre "skill gacha"): the equipment rate table and pity rule (GACHA_RATE_*, GACHA_PITY)
    /// with its own pity counter; grade first, then a uniform pick within the grade. A duplicate raises the skill
    /// one level (D-062 rule for gear); at SKILL_MAX_LEVEL it refunds SKILL_REFUND_{grade} gold.
    /// Order is settle -> save (caller) -> animate, as for equipment.
    /// </summary>
    public sealed class SkillSummonService
    {
        private readonly BalanceValues _balance;
        private readonly GachaTableValues _table;
        private readonly IRandom _rng;

        public SkillSummonService(BalanceValues balance, GachaTableValues table, IRandom rng)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public SkillSummonResult TryPull(SaveDataV2 data)
        {
            if (data.gold < _balance.SKILL_SUMMON_COST_SINGLE) return SkillSummonResult.Fail(FailReason.NotEnoughGold);
            data.gold -= _balance.SKILL_SUMMON_COST_SINGLE;
            return Pull(data, 1);
        }

        public SkillSummonResult TryPullTen(SaveDataV2 data)
        {
            if (data.gold < _balance.SKILL_SUMMON_COST_TEN) return SkillSummonResult.Fail(FailReason.NotEnoughGold);
            data.gold -= _balance.SKILL_SUMMON_COST_TEN;
            return Pull(data, 10);
        }

        public SkillSummonResult TryPullTenWithGem(SaveDataV2 data)
        {
            if (data.gem < _balance.SKILL_SUMMON_COST_TEN_GEM) return SkillSummonResult.Fail(FailReason.NotEnoughGem);
            data.gem -= _balance.SKILL_SUMMON_COST_TEN_GEM;
            return Pull(data, 10);
        }

        private SkillSummonResult Pull(SaveDataV2 data, int count)
        {
            SkillBook.EnsureStarters(data, _balance);
            var items = new SkillPullItem[count];
            for (int i = 0; i < count; i++) items[i] = PullOne(data);
            return new SkillSummonResult(Result.Success, items);
        }

        private SkillPullItem PullOne(SaveDataV2 data)
        {
            data.skillPityCount++;
            data.skillPullCount++;

            Grade grade;
            if (data.skillPityCount >= _table.PityCeiling)
            {
                grade = Grade.Legendary;
                data.skillPityCount = 0;
            }
            else
            {
                grade = _table.PickGrade(_rng.NextDouble());
                if (grade == Grade.Legendary && _table.ResetOnLegendary) data.skillPityCount = 0;
            }

            SkillDef[] pool = SkillCatalog.OfGrade(grade);
            SkillDef def = pool[_rng.Next(pool.Length)];
            return Acquire(data, def);
        }

        private SkillPullItem Acquire(SaveDataV2 data, SkillDef def)
        {
            if (SkillBook.IsOwned(data, def.Id))
            {
                int level = SkillBook.GetLevel(data, def.Id);
                if (level < _balance.SKILL_MAX_LEVEL)
                {
                    SkillBook.SetLevel(data, def.Id, level + 1);
                    return new SkillPullItem(def.Id, def.Grade, false, level + 1, 0d, false);
                }

                double refund = Formulas.SkillRefund(_balance, def.Grade);
                data.gold += refund;
                return new SkillPullItem(def.Id, def.Grade, false, level, refund, false);
            }

            SkillBook.AddOwned(data, def.Id);
            bool equipped = false;
            int slots = SkillService.SlotCount(_balance);
            for (int s = 0; s < slots; s++)
            {
                if (!SkillService.IsSlotUnlocked(_balance, s, data.heroLevel)) continue;
                if (!string.IsNullOrEmpty(SkillBook.EquippedAt(data, s))) continue;
                SkillBook.SetEquipped(data, s, def.Id);
                equipped = true;
                break;
            }

            return new SkillPullItem(def.Id, def.Grade, true, 1, 0d, equipped);
        }
    }
}
