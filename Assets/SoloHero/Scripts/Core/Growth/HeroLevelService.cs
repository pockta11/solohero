using System;
using SoloHero.Core.Config;
using SoloHero.Core.Save;

namespace SoloHero.Core.Growth
{
    public sealed class HeroLevelService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;

        public event Action<int> HeroLeveledUp;

        public HeroLevelService(SaveDataV2 data, BalanceValues balance)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        public static double ExpRequired(BalanceValues balance, int heroLevel)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            return balance.EXP_REQ_BASE * Math.Pow(balance.EXP_REQ_GROWTH, heroLevel - 1);
        }

        public int AddExp(double amount)
        {
            if (amount <= 0d) return 0;

            _data.heroExp += amount;
            int gained = 0;
            while (_data.heroExp >= ExpRequired(_balance, _data.heroLevel))
            {
                _data.heroExp -= ExpRequired(_balance, _data.heroLevel);
                _data.heroLevel++;
                gained++;
                HeroLeveledUp?.Invoke(_data.heroLevel);
            }

            return gained;
        }
    }
}
