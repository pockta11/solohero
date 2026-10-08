using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Pets
{
    /// <summary>One pet from a summon: new (equipped when it outranks the equipped pet), an enhance, or a refund.</summary>
    public readonly struct PetPullItem
    {
        public readonly string Id;
        public readonly GearGrade Grade;
        public readonly bool WasNew;

        /// <summary>Enhance level a duplicate raised the pet to; 0 when it was new or refunded.</summary>
        public readonly int EnhancedLevel;
        public readonly double RefundGold;
        public readonly bool AutoEquipped;

        public PetPullItem(string id, GearGrade grade, bool wasNew, int enhancedLevel, double refundGold, bool autoEquipped)
        {
            Id = id;
            Grade = grade;
            WasNew = wasNew;
            EnhancedLevel = enhancedLevel;
            RefundGold = refundGold;
            AutoEquipped = autoEquipped;
        }
    }

    public readonly struct PetSummonResult
    {
        public readonly Result Status;
        public readonly PetPullItem[] Items;

        public PetSummonResult(Result status, PetPullItem[] items)
        {
            Status = status;
            Items = items;
        }

        public static PetSummonResult Fail(FailReason reason) => new PetSummonResult(Result.Fail(reason), Array.Empty<PetPullItem>());
    }

    /// <summary>
    /// D-114 pet summon (genre "pet gacha"): the gear rate table and pity ceiling (GEAR_RATE_*, GEAR_PITY: a Legendary,
    /// reset by a Legendary or better) on its own counter; grade first, then a uniform pick within the grade. A
    /// duplicate enhances the pet up to PET_MAX_ENHANCE, then refunds PET_REFUND_{grade} gold. A new pet that outranks
    /// the equipped one is equipped. Order is settle -> save (caller) -> animate, as for equipment. The summon opens
    /// after the first boss (PET_UNLOCK_STAGE).
    /// </summary>
    public sealed class PetSummonService
    {
        private readonly BalanceValues _balance;
        private readonly GearTableValues _table;
        private readonly IRandom _rng;

        public PetSummonService(BalanceValues balance, GearTableValues table, IRandom rng)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public bool IsUnlocked(SaveDataV2 data) => IsUnlocked(_balance, data);

        public static bool IsUnlocked(BalanceValues balance, SaveDataV2 data) =>
            data != null && balance != null && data.highestStage >= balance.PET_UNLOCK_STAGE;

        public PetSummonResult TryPull(SaveDataV2 data)
        {
            if (!IsUnlocked(data)) return PetSummonResult.Fail(FailReason.Locked);
            if (data.gold < _balance.PET_SUMMON_COST_SINGLE) return PetSummonResult.Fail(FailReason.NotEnoughGold);
            data.gold -= _balance.PET_SUMMON_COST_SINGLE;
            return Pull(data, 1);
        }

        public PetSummonResult TryPullTen(SaveDataV2 data)
        {
            if (!IsUnlocked(data)) return PetSummonResult.Fail(FailReason.Locked);
            if (data.gold < _balance.PET_SUMMON_COST_TEN) return PetSummonResult.Fail(FailReason.NotEnoughGold);
            data.gold -= _balance.PET_SUMMON_COST_TEN;
            return Pull(data, 10);
        }

        /// <summary>D-120: free pulls (an ad summon); same rates, pity and counters as paid ones.</summary>
        public PetSummonResult TryPullFree(SaveDataV2 data, int count)
        {
            if (!IsUnlocked(data)) return PetSummonResult.Fail(FailReason.Locked);
            return Pull(data, count < 1 ? 1 : count);
        }

        public PetSummonResult TryPullTenWithGem(SaveDataV2 data)
        {
            if (!IsUnlocked(data)) return PetSummonResult.Fail(FailReason.Locked);
            if (data.gem < _balance.PET_SUMMON_COST_TEN_GEM) return PetSummonResult.Fail(FailReason.NotEnoughGem);
            data.gem -= _balance.PET_SUMMON_COST_TEN_GEM;
            return Pull(data, 10);
        }

        private PetSummonResult Pull(SaveDataV2 data, int count)
        {
            PetService.EnsureOwned(data);
            var items = new PetPullItem[count];
            for (int i = 0; i < count; i++) items[i] = PullOne(data);
            return new PetSummonResult(Result.Success, items);
        }

        private PetPullItem PullOne(SaveDataV2 data)
        {
            // D-115: the summon level before this pull picks the rate table.
            GearTableValues table = _table.AtLevel(SummonLevel.Of(_balance, data, SummonKind.Pet));
            // D-123: the pity starts counting once the summon level has opened its grade.
            bool pity = table.PityOpen;
            if (pity) data.petPityCount++;
            data.petPullCount++;

            GearGrade grade;
            if (pity && data.petPityCount >= table.PityCeiling)
            {
                grade = GearTableValues.PityGrade;
                data.petPityCount = 0;
            }
            else
            {
                grade = table.PickGrade(_rng.NextDouble());
                if (grade >= GearTableValues.PityGrade && table.ResetOnPityGrade) data.petPityCount = 0;
            }

            PetDef[] pool = PetCatalog.OfGrade(grade);
            return Acquire(data, pool[_rng.Next(pool.Length)]);
        }

        private PetPullItem Acquire(SaveDataV2 data, PetDef def)
        {
            int index = PetCatalog.IndexOf(def.Id);
            if (data.petOwned.Contains(def.Id))
            {
                int enhance = PetService.Enhance(data, index);
                if (enhance < _balance.PET_MAX_ENHANCE)
                {
                    PetService.SetEnhance(data, index, enhance + 1);
                    return new PetPullItem(def.Id, def.Grade, false, enhance + 1, 0d, false);
                }

                double refund = Formulas.PetRefund(_balance, def.Grade);
                data.gold += refund;
                return new PetPullItem(def.Id, def.Grade, false, 0, refund, false);
            }

            data.petOwned.Add(def.Id);
            PetDef equipped = PetCatalog.Find(data.companionEquipped);
            bool equip = equipped == null || !data.petOwned.Contains(equipped.Id) || def.Grade > equipped.Grade;
            if (equip) data.companionEquipped = def.Id;
            return new PetPullItem(def.Id, def.Grade, true, 0, 0d, equip);
        }
    }
}
