namespace SoloHero.Core.Growth
{
    public readonly struct HeroStats
    {
        public readonly double Hp;
        public readonly double Atk;
        public readonly double Def;
        public readonly double AtkSpd;
        public readonly double CritRate;

        public HeroStats(double hp, double atk, double def, double atkSpd, double critRate)
        {
            Hp = hp;
            Atk = atk;
            Def = def;
            AtkSpd = atkSpd;
            CritRate = critRate;
        }
    }
}
