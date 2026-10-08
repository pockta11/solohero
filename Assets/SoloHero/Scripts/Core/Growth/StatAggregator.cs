using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Growth
{
    public static class StatAggregator
    {
        /// <summary>
        /// Hero stats from growth. D-141: AP replaces the flat per-level gains: the main stat adds AP_MAIN_ATK to base ATK
        /// a point, vitality AP_VIT_HP to base HP. D-142: the crit lanes add crit rate and crit damage.
        /// </summary>
        public static HeroStats Compute(
            BalanceValues balance,
            ApPoints ap,
            LaneLevels lanes,
            double swordMult = 1d,
            double armorMult = 1d,
            double helmMult = 1d,
            double bootsSpeedBonus = 0d,
            double bootsCritBonus = 0d,
            BuffSet buffs = default,
            double skillOwnedAtk = 0d,
            TalentEffects talents = null,
            int jobTier = 0)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            TalentEffects t = talents ?? TalentEffects.None;

            double upg = balance.UPG_STAT_MULT;
            double buffAtk = 1d + buffs.SumAtk;
            double promo = Formulas.JobStatMult(balance, jobTier);

            double hp = (balance.HP_BASE + balance.AP_VIT_HP * ap.Vit)
                * Math.Pow(upg, lanes.Hp)
                * armorMult
                * promo
                * (1d + t.HpPct);

            double atk = (balance.ATK_BASE + balance.AP_MAIN_ATK * ap.Main)
                * Math.Pow(upg, lanes.Atk)
                * swordMult
                * promo
                * (1d + skillOwnedAtk)
                * (1d + t.AtkPct)
                * buffAtk;

            double def = balance.DEF_BASE
                * Math.Pow(upg, lanes.Def)
                * helmMult
                * promo
                * (1d + t.DefPct);

            double atkSpdMax = balance.ATKSPD_BASE + balance.UPG_GAIN_SPD * balance.UPG_MAX_LEVEL_SPD;
            double spd = Math.Min(
                atkSpdMax,
                (balance.ATKSPD_BASE + balance.UPG_GAIN_SPD * lanes.Spd) * (1d + bootsSpeedBonus) * (1d + t.AtkSpdPct));

            double crit = balance.CRIT_RATE_BASE + balance.UPG_GAIN_CRIT * lanes.Crit + bootsCritBonus + t.CritPoints;
            double critDamage = t.CritDamage + balance.UPG_GAIN_CRITDMG * lanes.CritDmg;

            return new HeroStats(hp, atk, def, spd, crit, critDamage);
        }

        /// <summary>Growth only (AP and lanes, no gear, talents or job) of a save.</summary>
        public static HeroStats Compute(BalanceValues balance, SaveDataV2 data, BuffSet buffs = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Compute(balance, ApPoints.From(data), LaneLevels.From(data), buffs: buffs);
        }
    }
}
