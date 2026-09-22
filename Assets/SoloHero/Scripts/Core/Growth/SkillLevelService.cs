using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    public sealed class SkillLevelService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public event Action<SkillSlot, int> SkillLeveledUp;

        public SkillLevelService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public static int EffectiveLevel(int savedLevel) => savedLevel < 1 ? 1 : savedLevel;

        public static int UnlockHeroLevel(BalanceValues balance, SkillSlot slot)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            switch (slot)
            {
                case SkillSlot.Slot1: return balance.SKILL_UNLOCK_LV_1;
                case SkillSlot.Slot2: return balance.SKILL_UNLOCK_LV_2;
                case SkillSlot.Slot3: return balance.SKILL_UNLOCK_LV_3;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static bool IsUnlocked(BalanceValues balance, SkillSlot slot, int heroLevel) =>
            heroLevel >= UnlockHeroLevel(balance, slot);

        public static double UpgradeCost(BalanceValues balance, SkillSlot slot, int currentLevel)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            int level = EffectiveLevel(currentLevel);
            double baseCost;
            switch (slot)
            {
                case SkillSlot.Slot1: baseCost = balance.SKILL_UPG_BASE_1; break;
                case SkillSlot.Slot2: baseCost = balance.SKILL_UPG_BASE_2; break;
                case SkillSlot.Slot3: baseCost = balance.SKILL_UPG_BASE_3; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return baseCost * Math.Pow(balance.SKILL_UPG_COST_GROWTH, level - 1);
        }

        public static double DamageMultiplier(BalanceValues balance, SkillSlot slot, int savedLevel)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            double baseMult;
            switch (slot)
            {
                case SkillSlot.Slot1: baseMult = balance.SKILL_MULT_1; break;
                case SkillSlot.Slot2: baseMult = balance.SKILL_MULT_2; break;
                case SkillSlot.Slot3: return 0d;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }

            int level = EffectiveLevel(savedLevel);
            return baseMult * (1d + balance.SKILL_LEVEL_GAIN / 100d * (level - 1));
        }

        public static double BattleCryAtkBuffFraction(BalanceValues balance, int savedLevel)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            int level = EffectiveLevel(savedLevel);
            double percent = balance.BATTLECRY_ATK_BUFF
                * (1d + balance.SKILL_LEVEL_GAIN / 100d * (level - 1));
            return percent / 100d;
        }

        public Result TryLevelUp(SkillSlot slot)
        {
            if (!IsUnlocked(_balance, slot, _data.heroLevel))
                return Result.Fail(FailReason.Locked);

            int current = EffectiveLevel(GetSavedLevel(slot));
            if (current >= _balance.SKILL_MAX_LEVEL)
                return Result.Fail(FailReason.MaxLevel);

            double cost = UpgradeCost(_balance, slot, current);
            if (_data.gold < cost)
                return Result.Fail(FailReason.NotEnoughGold);

            _data.gold -= cost;
            int next = current + 1;
            SetSavedLevel(slot, next);
            SkillLeveledUp?.Invoke(slot, next);
            _save?.RequestSave();
            return Result.Success;
        }

        public int GetSavedLevel(SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Slot1: return _data.skillLevel1;
                case SkillSlot.Slot2: return _data.skillLevel2;
                case SkillSlot.Slot3: return _data.skillLevel3;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        private void SetSavedLevel(SkillSlot slot, int level)
        {
            switch (slot)
            {
                case SkillSlot.Slot1: _data.skillLevel1 = level; break;
                case SkillSlot.Slot2: _data.skillLevel2 = level; break;
                case SkillSlot.Slot3: _data.skillLevel3 = level; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }
    }
}
