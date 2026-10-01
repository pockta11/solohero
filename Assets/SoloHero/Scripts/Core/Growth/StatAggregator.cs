using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Growth
{
    public static class StatAggregator
    {
        public static HeroStats Compute(
            BalanceValues balance,
            int heroLevel,
            int upgradeHp,
            int upgradeAtk,
            int upgradeDef,
            int upgradeSpd,
            double swordMult = 1d,
            double armorMult = 1d,
            double helmMult = 1d,
            double bootsSpeedBonus = 0d,
            double bootsCritBonus = 0d,
            BuffSet buffs = default,
            double skillOwnedAtk = 0d,
            TalentEffects talents = null,
            int promotionTier = 0)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            TalentEffects t = talents ?? TalentEffects.None;

            double levelBonus = heroLevel - 1;
            double upg = balance.UPG_STAT_MULT;
            double buffAtk = 1d + buffs.SumAtk;
            double promo = Formulas.PromotionMult(balance, promotionTier);

            double hp = (balance.HP_BASE + balance.LEVEL_HP_GAIN * levelBonus)
                * Math.Pow(upg, upgradeHp)
                * armorMult
                * promo
                * (1d + t.HpPct);

            double atk = (balance.ATK_BASE + balance.LEVEL_ATK_GAIN * levelBonus)
                * Math.Pow(upg, upgradeAtk)
                * swordMult
                * promo
                * (1d + skillOwnedAtk)
                * (1d + t.AtkPct)
                * buffAtk;

            double def = balance.DEF_BASE
                * Math.Pow(upg, upgradeDef)
                * helmMult
                * promo
                * (1d + t.DefPct);

            double atkSpdMax = balance.ATKSPD_BASE + balance.UPG_GAIN_SPD * balance.UPG_MAX_LEVEL_SPD;
            double spd = Math.Min(
                atkSpdMax,
                (balance.ATKSPD_BASE + balance.UPG_GAIN_SPD * upgradeSpd) * (1d + bootsSpeedBonus) * (1d + t.AtkSpdPct));

            double crit = balance.CRIT_RATE_BASE + bootsCritBonus + t.CritPoints;

            return new HeroStats(hp, atk, def, spd, crit, t.CritDamage);
        }

        public static HeroStats Compute(BalanceValues balance, SaveDataV2 data, BuffSet buffs = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Compute(
                balance,
                data.heroLevel,
                data.upgradeHp,
                data.upgradeAtk,
                data.upgradeDef,
                data.upgradeSpd,
                1d,
                1d,
                1d,
                0d,
                0d,
                buffs);
        }
    }
}
