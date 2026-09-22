namespace SoloHero.Core.Common
{
    public interface IRandom
    {
        double NextDouble();
        int Next(int maxExclusive);
    }
}
