using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
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
    /// D-107: only the hero's own job line is in the pool, so summoning opens with the first job (JobLocked before).
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
            if (JobService.LineOf(data) == JobLine.None) return SkillSummonResult.Fail(FailReason.JobLocked);
            if (data.gold < _balance.SKILL_SUMMON_COST_SINGLE) return SkillSummonResult.Fail(FailReason.NotEnoughGold);
            data.gold -= _balance.SKILL_SUMMON_COST_SINGLE;
            return Pull(data, 1);
        }

        public SkillSummonResult TryPullTen(SaveDataV2 data)
        {
            if (JobService.LineOf(data) == JobLine.None) return SkillSummonResult.Fail(FailReason.JobLocked);
            if (data.gold < _balance.SKILL_SUMMON_COST_TEN) return SkillSummonResult.Fail(FailReason.NotEnoughGold);
            data.gold -= _balance.SKILL_SUMMON_COST_TEN;
            return Pull(data, 10);
        }

        /// <summary>D-128: spends up to <paramref name="max"/> skill tickets on as many pulls (after the first job).</summary>
        public SkillSummonResult TryPullTickets(SaveDataV2 data, int max)
        {
            if (JobService.LineOf(data) == JobLine.None) return SkillSummonResult.Fail(FailReason.JobLocked);
            int count = Math.Min(max, data.skillTickets);
            if (count < 1) return SkillSummonResult.Fail(FailReason.NotEnoughTicket);
            data.skillTickets -= count;
            return Pull(data, count);
        }

        public SkillSummonResult TryPullTenWithGem(SaveDataV2 data)
        {
            if (JobService.LineOf(data) == JobLine.None) return SkillSummonResult.Fail(FailReason.JobLocked);
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
            // D-115: the summon level before this pull picks the rate table.
            GachaTableValues table = _table.AtLevel(SummonLevel.Of(_balance, data, SummonKind.Skill));
            // D-123: the pity starts counting once the summon level has opened its grade.
            bool pity = table.PityOpen;
            if (pity) data.skillPityCount++;
            data.skillPullCount++;

            Grade grade;
            if (pity && data.skillPityCount >= table.PityCeiling)
            {
                grade = Grade.Legendary;
                data.skillPityCount = 0;
            }
            else
            {
                grade = table.PickGrade(_rng.NextDouble());
                if (grade == Grade.Legendary && table.ResetOnLegendary) data.skillPityCount = 0;
            }

            SkillDef[] pool = SkillCatalog.OfLine(JobService.LineOf(data), grade);
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
