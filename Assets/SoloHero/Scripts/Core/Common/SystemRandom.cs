using System;

namespace SoloHero.Core.Common
{
    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom()
            : this(new Random())
        {
        }

        public SystemRandom(Random random)
        {
            _random = random;
        }

        public double NextDouble() => _random.NextDouble();

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
