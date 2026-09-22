namespace SoloHero.Core.Growth
{
    public readonly struct BuffSet
    {
        public readonly double SumAtk;

        public BuffSet(double sumAtk)
        {
            SumAtk = sumAtk;
        }

        public static BuffSet None => new BuffSet(0d);
    }
}
