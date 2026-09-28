using System;
using System.Collections.Generic;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Skills
{
    /// <summary>
    /// Read / write helpers for the skill collection in <see cref="SaveDataV2"/> (D-078): <c>ownedSkills</c> and
    /// <c>ownedSkillLevels</c> are index-aligned, <c>equippedSkills</c> holds one id per slot ("" = empty).
    /// </summary>
    public static class SkillBook
    {
        public static bool IsOwned(SaveDataV2 save, string id) =>
            save != null && !string.IsNullOrEmpty(id) && save.ownedSkills.IndexOf(id) >= 0;

        /// <summary>Level of an owned skill (at least 1); 0 when not owned.</summary>
        public static int GetLevel(SaveDataV2 save, string id)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (string.IsNullOrEmpty(id)) return 0;
            int index = save.ownedSkills.IndexOf(id);
            if (index < 0) return 0;
            Align(save);
            int level = save.ownedSkillLevels[index];
            return level < 1 ? 1 : level;
        }

        public static void SetLevel(SaveDataV2 save, string id, int level)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            int index = save.ownedSkills.IndexOf(id);
            if (index < 0) return;
            Align(save);
            save.ownedSkillLevels[index] = level < 1 ? 1 : level;
        }

        public static void AddOwned(SaveDataV2 save, string id, int level = 1)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (IsOwned(save, id)) return;
            Align(save);
            save.ownedSkills.Add(id);
            save.ownedSkillLevels.Add(level < 1 ? 1 : level);
        }

        public static string EquippedAt(SaveDataV2 save, int slot)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (slot < 0 || slot >= save.equippedSkills.Count) return "";
            return save.equippedSkills[slot] ?? "";
        }

        /// <summary>Slot index holding <paramref name="id"/>, or -1.</summary>
        public static int SlotOf(SaveDataV2 save, string id)
        {
            if (save == null || string.IsNullOrEmpty(id)) return -1;
            return save.equippedSkills.IndexOf(id);
        }

        public static void SetEquipped(SaveDataV2 save, int slot, string id)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (slot < 0) return;
            while (save.equippedSkills.Count <= slot) save.equippedSkills.Add("");
            save.equippedSkills[slot] = id ?? "";
        }

        public static void Align(SaveDataV2 save)
        {
            if (save.ownedSkills == null) save.ownedSkills = new List<string>();
            if (save.ownedSkillLevels == null) save.ownedSkillLevels = new List<int>();
            if (save.equippedSkills == null) save.equippedSkills = new List<string>();
            while (save.ownedSkillLevels.Count < save.ownedSkills.Count) save.ownedSkillLevels.Add(1);
            if (save.ownedSkillLevels.Count > save.ownedSkills.Count)
                save.ownedSkillLevels.RemoveRange(save.ownedSkills.Count, save.ownedSkillLevels.Count - save.ownedSkills.Count);
        }

        /// <summary>
        /// Brings any save to the D-078 shape; returns true when something changed. A save without skills gets the
        /// three starters (keeping the old fixed-skill levels) equipped in slots 1-3. Unknown or duplicated ids are
        /// cleared from the slots, and the slot list is sized to SKILL_SLOT_COUNT.
        /// </summary>
        public static bool EnsureStarters(SaveDataV2 save, BalanceValues balance)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            Align(save);
            bool changed = false;

            if (save.ownedSkills.Count == 0)
            {
                int[] legacy = { save.skillLevel1, save.skillLevel2, save.skillLevel3 };
                for (int i = 0; i < SkillCatalog.StarterIds.Length; i++)
                {
                    int level = legacy[i] < 1 ? 1 : legacy[i];
                    if (level > balance.SKILL_MAX_LEVEL) level = balance.SKILL_MAX_LEVEL;
                    AddOwned(save, SkillCatalog.StarterIds[i], level);
                }

                save.equippedSkills.Clear();
                for (int i = 0; i < SkillCatalog.StarterIds.Length; i++) save.equippedSkills.Add(SkillCatalog.StarterIds[i]);
                changed = true;
            }

            int slots = balance.SKILL_SLOT_COUNT < 1 ? 1 : balance.SKILL_SLOT_COUNT;
            while (save.equippedSkills.Count < slots)
            {
                save.equippedSkills.Add("");
                changed = true;
            }

            if (save.equippedSkills.Count > slots)
            {
                save.equippedSkills.RemoveRange(slots, save.equippedSkills.Count - slots);
                changed = true;
            }

            for (int i = 0; i < save.equippedSkills.Count; i++)
            {
                string id = save.equippedSkills[i];
                if (string.IsNullOrEmpty(id)) continue;
                bool bad = SkillCatalog.Find(id) == null || !IsOwned(save, id) || save.equippedSkills.IndexOf(id) != i;
                if (!bad) continue;
                save.equippedSkills[i] = "";
                changed = true;
            }

            return changed;
        }
    }
}
