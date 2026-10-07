using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Pets
{
    /// <summary>
    /// D-114 pets (D-102 companions): the pets the save owns, the equipped one, gold levels and duplicate enhance
    /// levels. Pets are obtained from the pet summon; equip and level-up change the combat loadout (the caller
    /// refreshes it) and request a save.
    /// </summary>
    public sealed class PetService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public PetService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public event Action Changed;

        public bool IsOwned(int index) => IsOwned(_data, index);

        public static bool IsOwned(SaveDataV2 data, int index) =>
            data != null && data.petOwned != null && index >= 0 && index < PetCatalog.Count
            && data.petOwned.Contains(PetCatalog.All[index].Id);

        public int OwnedCount => _data.petOwned != null ? _data.petOwned.Count : 0;

        public int Level(int index) => Level(_data, index);

        public static int Level(SaveDataV2 data, int index)
        {
            if (data == null || data.companionLevels == null || index < 0 || index >= data.companionLevels.Count) return 1;
            int lv = data.companionLevels[index];
            return lv < 1 ? 1 : lv;
        }

        public int Enhance(int index) => Enhance(_data, index);

        public static int Enhance(SaveDataV2 data, int index)
        {
            if (data == null || data.petEnhance == null || index < 0 || index >= data.petEnhance.Count) return 0;
            return Math.Max(0, data.petEnhance[index]);
        }

        public static void SetEnhance(SaveDataV2 data, int index, int enhance)
        {
            if (data.petEnhance == null) data.petEnhance = new List<int>();
            while (data.petEnhance.Count <= index) data.petEnhance.Add(0);
            data.petEnhance[index] = enhance;
        }

        public int EquippedIndex => PetCatalog.IndexOf(_data.companionEquipped);

        public bool IsMax(int index) => Level(index) >= _balance.PET_MAX_LEVEL;

        public double LevelCost(int index) => Formulas.PetLevelCost(_balance, PetCatalog.All[index].Grade, Level(index));

        /// <summary>Damage multiplier of a pet from its level and enhance (x its AttackMult per hit).</summary>
        public static double AttackScale(BalanceValues balance, SaveDataV2 data, int index) =>
            Formulas.PetLevelScale(balance, Level(data, index)) * Formulas.PetEnhanceMult(balance, Enhance(data, index));

        /// <summary>Damage per second of the equipped pet in hero ATK multiples (0 without one).</summary>
        public static double EquippedRate(BalanceValues balance, SaveDataV2 data)
        {
            int index = data != null ? PetCatalog.IndexOf(data.companionEquipped) : -1;
            if (!IsOwned(data, index)) return 0d;
            return PetCatalog.All[index].DamagePerSecond * AttackScale(balance, data, index);
        }

        public Result TryEquip(int index)
        {
            if (!IsOwned(index)) return Result.Fail(FailReason.Locked);
            _data.companionEquipped = PetCatalog.All[index].Id;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        public Result TryLevelUp(int index)
        {
            if (!IsOwned(index)) return Result.Fail(FailReason.Locked);
            if (IsMax(index)) return Result.Fail(FailReason.MaxLevel);
            double cost = LevelCost(index);
            if (_data.gold < cost) return Result.Fail(FailReason.NotEnoughGold);
            _data.gold -= cost;
            while (_data.companionLevels.Count <= index) _data.companionLevels.Add(1);
            _data.companionLevels[index] = Level(index) + 1;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>One owned pet's owned effect in percent: PET_OWNED_ATK_{grade} x enhance x level.</summary>
        public static double OwnedAtkPercent(BalanceValues balance, GearGrade grade, int enhance, int level)
        {
            double percent;
            switch (grade)
            {
                case GearGrade.Common: percent = balance.PET_OWNED_ATK_C; break;
                case GearGrade.Uncommon: percent = balance.PET_OWNED_ATK_U; break;
                case GearGrade.Rare: percent = balance.PET_OWNED_ATK_R; break;
                case GearGrade.Epic: percent = balance.PET_OWNED_ATK_E; break;
                case GearGrade.Legendary: percent = balance.PET_OWNED_ATK_L; break;
                case GearGrade.Mythic: percent = balance.PET_OWNED_ATK_M; break;
                default: percent = balance.PET_OWNED_ATK_A; break;
            }

            return percent * Formulas.PetEnhanceMult(balance, enhance) * Formulas.PetLevelOwnedMult(balance, level);
        }

        /// <summary>Owned effect of every owned pet as an ATK fraction (0.05 = +5%), equipped or not.</summary>
        public static double OwnedAtkBonus(BalanceValues balance, SaveDataV2 data)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (data == null || data.petOwned == null) return 0d;
            double sum = 0d;
            for (int i = 0; i < PetCatalog.Count; i++)
            {
                if (!data.petOwned.Contains(PetCatalog.All[i].Id)) continue;
                sum += OwnedAtkPercent(balance, PetCatalog.All[i].Grade, Enhance(data, i), Level(data, i));
            }

            return sum / 100d;
        }

        /// <summary>
        /// Gives a save its pets once: an older save (no pets recorded) gets every D-102 companion its cleared stages had
        /// unlocked, a new one the slime. Equips the slime when the equipped id is not an owned pet.
        /// </summary>
        public static void EnsureOwned(SaveDataV2 data)
        {
            if (data == null) return;
            if (data.petOwned == null) data.petOwned = new List<string>();
            if (data.petEnhance == null) data.petEnhance = new List<int>();
            if (data.companionLevels == null) data.companionLevels = new List<int>();
            if (data.petOwned.Count == 0)
            {
                for (int i = 0; i < PetCatalog.Count; i++)
                {
                    PetDef def = PetCatalog.All[i];
                    if (def.LegacyUnlockStage >= 0 && data.highestStage >= def.LegacyUnlockStage) data.petOwned.Add(def.Id);
                }
            }

            if (!IsOwned(data, PetCatalog.IndexOf(data.companionEquipped))) data.companionEquipped = PetCatalog.All[0].Id;
            if (!data.petOwned.Contains(data.companionEquipped)) data.petOwned.Add(data.companionEquipped);
        }
    }
}
