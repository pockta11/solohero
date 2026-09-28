using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Skills
{
    /// <summary>
    /// Skill collection rules (D-078): SKILL_SLOT_COUNT slots unlocked by hero level, equip / unequip / auto-equip,
    /// gold level-up per skill, and the owned effect (ATK bonus from every owned skill). Every change is a save
    /// trigger (equip change, skill level-up).
    /// </summary>
    public sealed class SkillService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public event Action<string, int> SkillLeveledUp;

        /// <summary>Equipped slots changed (equip, unequip, auto-equip).</summary>
        public event Action LoadoutChanged;

        public SkillService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
            SkillBook.EnsureStarters(_data, _balance);
        }

        public static int SlotCount(BalanceValues balance) => balance.SKILL_SLOT_COUNT < 1 ? 1 : balance.SKILL_SLOT_COUNT;

        public static int UnlockHeroLevel(BalanceValues balance, int slot)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            switch (slot)
            {
                case 0: return balance.SKILL_UNLOCK_LV_1;
                case 1: return balance.SKILL_UNLOCK_LV_2;
                case 2: return balance.SKILL_UNLOCK_LV_3;
                case 3: return balance.SKILL_UNLOCK_LV_4;
                case 4: return balance.SKILL_UNLOCK_LV_5;
                case 5: return balance.SKILL_UNLOCK_LV_6;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static bool IsSlotUnlocked(BalanceValues balance, int slot, int heroLevel) =>
            slot >= 0 && slot < SlotCount(balance) && heroLevel >= UnlockHeroLevel(balance, slot);

        public static double UpgradeCost(BalanceValues balance, SkillDef def, int level)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            return Formulas.SkillUpgradeCost(balance, def.Grade, level);
        }

        /// <summary>Sum of every owned skill's owned effect, as an ATK fraction (0.12 = +12%).</summary>
        public static double OwnedAtkBonus(BalanceValues balance, SaveDataV2 save)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (save == null) throw new ArgumentNullException(nameof(save));
            double sum = 0d;
            for (int i = 0; i < save.ownedSkills.Count; i++)
            {
                SkillDef def = SkillCatalog.Find(save.ownedSkills[i]);
                if (def == null) continue;
                sum += Formulas.SkillOwnedAtk(balance, def.Grade, SkillBook.GetLevel(save, def.Id));
            }

            return sum;
        }

        public int Level(string id) => SkillBook.GetLevel(_data, id);

        public Result TryLevelUp(string id)
        {
            SkillDef def = SkillCatalog.Find(id);
            if (def == null || !SkillBook.IsOwned(_data, id)) return Result.Fail(FailReason.Locked);

            int current = SkillBook.GetLevel(_data, id);
            if (current >= _balance.SKILL_MAX_LEVEL) return Result.Fail(FailReason.MaxLevel);

            double cost = UpgradeCost(_balance, def, current);
            if (_data.gold < cost) return Result.Fail(FailReason.NotEnoughGold);

            _data.gold -= cost;
            SkillBook.SetLevel(_data, id, current + 1);
            SkillLeveledUp?.Invoke(id, current + 1);
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>
        /// Equips an owned skill. <paramref name="slot"/> -1 takes the first empty unlocked slot (SlotsFull when
        /// none); a given slot replaces what is there. A skill already equipped elsewhere moves (swaps).
        /// </summary>
        public Result TryEquip(string id, int slot = -1)
        {
            if (SkillCatalog.Find(id) == null || !SkillBook.IsOwned(_data, id)) return Result.Fail(FailReason.Locked);

            if (slot < 0)
            {
                if (SkillBook.SlotOf(_data, id) >= 0) return Result.Success;
                slot = FirstEmptyUnlockedSlot();
                if (slot < 0) return Result.Fail(FailReason.SlotsFull);
            }

            if (!IsSlotUnlocked(_balance, slot, _data.heroLevel)) return Result.Fail(FailReason.Locked);

            int from = SkillBook.SlotOf(_data, id);
            if (from == slot) return Result.Success;
            string displaced = SkillBook.EquippedAt(_data, slot);
            SkillBook.SetEquipped(_data, slot, id);
            if (from >= 0) SkillBook.SetEquipped(_data, from, displaced);
            Changed();
            return Result.Success;
        }

        public Result TryUnequip(int slot)
        {
            if (string.IsNullOrEmpty(SkillBook.EquippedAt(_data, slot))) return Result.Fail(FailReason.Busy);
            SkillBook.SetEquipped(_data, slot, "");
            Changed();
            return Result.Success;
        }

        /// <summary>Fills the unlocked slots with the strongest owned skills: grade, then level, then catalog order.</summary>
        public void AutoEquip()
        {
            int slots = SlotCount(_balance);
            int unlocked = 0;
            for (int s = 0; s < slots; s++)
            {
                if (IsSlotUnlocked(_balance, s, _data.heroLevel)) unlocked++;
            }

            var picked = new string[unlocked];
            for (int n = 0; n < unlocked; n++)
            {
                SkillDef best = null;
                int bestLevel = 0;
                for (int i = 0; i < SkillCatalog.All.Length; i++)
                {
                    SkillDef def = SkillCatalog.All[i];
                    if (!SkillBook.IsOwned(_data, def.Id) || Array.IndexOf(picked, def.Id) >= 0) continue;
                    int level = SkillBook.GetLevel(_data, def.Id);
                    if (best == null || def.Grade > best.Grade || (def.Grade == best.Grade && level > bestLevel))
                    {
                        best = def;
                        bestLevel = level;
                    }
                }

                if (best == null) break;
                picked[n] = best.Id;
            }

            int next = 0;
            for (int s = 0; s < slots; s++)
            {
                if (!IsSlotUnlocked(_balance, s, _data.heroLevel))
                {
                    SkillBook.SetEquipped(_data, s, "");
                    continue;
                }

                SkillBook.SetEquipped(_data, s, next < picked.Length && picked[next] != null ? picked[next] : "");
                next++;
            }

            Changed();
        }

        /// <summary>First unlocked empty slot, or -1.</summary>
        public int FirstEmptyUnlockedSlot()
        {
            int slots = SlotCount(_balance);
            for (int s = 0; s < slots; s++)
            {
                if (!IsSlotUnlocked(_balance, s, _data.heroLevel)) continue;
                if (string.IsNullOrEmpty(SkillBook.EquippedAt(_data, s))) return s;
            }

            return -1;
        }

        private void Changed()
        {
            LoadoutChanged?.Invoke();
            _save?.RequestSave();
        }
    }
}
