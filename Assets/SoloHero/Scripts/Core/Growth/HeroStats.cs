namespace SoloHero.Core.Growth
{
    public readonly struct HeroStats
    {
        public readonly double Hp;
        public readonly double Atk;
        public readonly double Def;
        public readonly double AtkSpd;
        public readonly double CritRate;

        /// <summary>Added to CRIT_MULT on a critical hit (D-087 talents; 0.16 = +16 percentage points).</summary>
        public readonly double CritDamageBonus;

        public HeroStats(double hp, double atk, double def, double atkSpd, double critRate, double critDamageBonus = 0d)
        {
            CritDamageBonus = critDamageBonus;
            Hp = hp;
            Atk = atk;
            Def = def;
            AtkSpd = atkSpd;
            CritRate = critRate;
        }
    }
}
