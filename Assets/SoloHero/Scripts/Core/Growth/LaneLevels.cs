using System;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    /// <summary>Levels of the six gold stat lanes (value copy; the save holds them as separate fields).</summary>
    public struct LaneLevels
    {
        public int Hp;
        public int Atk;
        public int Def;
        public int Spd;
        public int Crit;
        public int CritDmg;

        public static LaneLevels From(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new LaneLevels
            {
                Hp = data.upgradeHp,
                Atk = data.upgradeAtk,
                Def = data.upgradeDef,
                Spd = data.upgradeSpd,
                Crit = data.upgradeCrit,
                CritDmg = data.upgradeCritDmg
            };
        }

        public int Get(UpgradeLane lane)
        {
            switch (lane)
            {
                case UpgradeLane.Hp: return Hp;
                case UpgradeLane.Atk: return Atk;
                case UpgradeLane.Def: return Def;
                case UpgradeLane.Spd: return Spd;
                case UpgradeLane.Crit: return Crit;
                case UpgradeLane.CritDmg: return CritDmg;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }

        public void Set(UpgradeLane lane, int level)
        {
            switch (lane)
            {
                case UpgradeLane.Hp: Hp = level; break;
                case UpgradeLane.Atk: Atk = level; break;
                case UpgradeLane.Def: Def = level; break;
                case UpgradeLane.Spd: Spd = level; break;
                case UpgradeLane.Crit: Crit = level; break;
                case UpgradeLane.CritDmg: CritDmg = level; break;
                default: throw new ArgumentOutOfRangeException(nameof(lane));
            }
        }

        /// <summary>A copy with one more level in <paramref name="lane"/>.</summary>
        public LaneLevels Plus(UpgradeLane lane)
        {
            LaneLevels next = this;
            next.Set(lane, Get(lane) + 1);
            return next;
        }

        public void WriteTo(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.upgradeHp = Hp;
            data.upgradeAtk = Atk;
            data.upgradeDef = Def;
            data.upgradeSpd = Spd;
            data.upgradeCrit = Crit;
            data.upgradeCritDmg = CritDmg;
        }
    }
}
