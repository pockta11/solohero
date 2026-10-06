using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;

namespace SoloHero.Core.Balance
{
    /// <summary>Average combat value of a skill loadout, as factors the power score multiplies in.</summary>
    public struct SkillPower
    {
        /// <summary>Skill damage per second in ATK multiples (hits x targets + burn).</summary>
        public double Mult;

        /// <summary>Uptime-weighted buffs: ATK and attack speed fractions, crit points, guard fraction.</summary>
        public double AtkBuff;
        public double SpdBuff;
        public double CritBuff;
        public double Guard;

        /// <summary>D-109: uptime-weighted cooldown speed-up (Haste buffs) and damage amplifier (marks) as fractions.</summary>
        public double Haste;
        public double Mark;

        /// <summary>Skill damage per second after Haste.</summary>
        public double HastedMult => Mult * (1d + Haste);

        /// <summary>Owned effect of the whole collection (ATK fraction).</summary>
        public double OwnedAtk;
    }

    /// <summary>
    /// Player-model estimate of what the skill collection is worth (D-078) for <see cref="SimSpender"/>. Area skills
    /// are assumed to hit <see cref="AreaTargets"/> enemies; heals, shields and stuns are not scored.
    /// </summary>
    public static class SimSkillModel
    {
        public const double AreaTargets = 2d;

        /// <summary>
        /// Power of the saved collection, optionally with one skill at another level (<paramref name="changedLevel"/>
        /// 1 adds an unowned one). <paramref name="autoEquip"/> re-picks the slots the way SkillService.AutoEquip does.
        /// </summary>
        public static SkillPower Compute(BalanceValues b, SaveDataV2 save, string changedId = null, int changedLevel = 0,
            bool autoEquip = false)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (save == null) throw new ArgumentNullException(nameof(save));
            var power = new SkillPower();

            for (int i = 0; i < SkillCatalog.All.Length; i++)
            {
                SkillDef def = SkillCatalog.All[i];
                int level = LevelOf(save, def.Id, changedId, changedLevel);
                if (level > 0) power.OwnedAtk += Formulas.SkillOwnedAtk(b, def.Grade, level);
            }

            int slots = SkillService.SlotCount(b);
            if (!autoEquip)
            {
                for (int s = 0; s < slots; s++)
                {
                    if (!SkillService.IsSlotUnlocked(b, s, save.heroLevel)) continue;
                    SkillDef def = SkillCatalog.Find(SkillBook.EquippedAt(save, s));
                    if (def != null) Add(b, ref power, def, LevelOf(save, def.Id, changedId, changedLevel));
                }

                return power;
            }

            int unlocked = 0;
            for (int s = 0; s < slots; s++)
            {
                if (SkillService.IsSlotUnlocked(b, s, save.heroLevel)) unlocked++;
            }

            var used = new bool[SkillCatalog.All.Length];
            for (int n = 0; n < unlocked; n++)
            {
                int best = -1;
                int bestLevel = 0;
                for (int i = 0; i < SkillCatalog.All.Length; i++)
                {
                    if (used[i]) continue;
                    int level = LevelOf(save, SkillCatalog.All[i].Id, changedId, changedLevel);
                    if (level <= 0) continue;
                    if (best < 0 || SkillCatalog.All[i].Grade > SkillCatalog.All[best].Grade
                        || (SkillCatalog.All[i].Grade == SkillCatalog.All[best].Grade && level > bestLevel))
                    {
                        best = i;
                        bestLevel = level;
                    }
                }

                if (best < 0) break;
                used[best] = true;
                Add(b, ref power, SkillCatalog.All[best], bestLevel);
            }

            return power;
        }

        private static int LevelOf(SaveDataV2 save, string id, string changedId, int changedLevel) =>
            changedId != null && id == changedId ? changedLevel : SkillBook.GetLevel(save, id);

        private static void Add(BalanceValues b, ref SkillPower power, SkillDef def, int level)
        {
            if (def.Cooldown <= 0f) return;
            double scale = Formulas.SkillLevelScale(b, level);
            if (def.DealsDamage)
            {
                double targets = def.Kind == SkillKind.Area ? AreaTargets : 1d;
                double perCast = def.DamageMult * scale * def.Waves * targets
                    + def.DotPercent / 100d * scale * def.DotSeconds * targets;
                power.Mult += perCast / def.Cooldown;
                // A mark on the targets amplifies every hit while it lasts; a boss or a big pack is marked often enough.
                if (def.MarkPercent > 0d && def.MarkSeconds > 0f)
                    power.Mark += def.MarkPercent / 100d * scale * Math.Min(1d, def.MarkSeconds / def.Cooldown);
            }

            if (def.Buff == SkillBuff.None || def.BuffSeconds <= 0f) return;
            double uptime = Math.Min(1d, def.BuffSeconds / def.Cooldown);
            double amount = def.BuffAmount * scale * uptime;
            switch (def.Buff)
            {
                case SkillBuff.Atk: power.AtkBuff += amount / 100d; break;
                case SkillBuff.AtkSpd: power.SpdBuff += amount / 100d; break;
                case SkillBuff.Crit: power.CritBuff += amount; break;
                case SkillBuff.Guard: power.Guard += amount / 100d; break;
                case SkillBuff.Haste: power.Haste += amount / 100d; break;
            }
        }
    }
}
