using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    public sealed class KillExp
    {
        public static Result Grant(SaveDataV2 save, BalanceValues balance, int g)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (g < 1)
                return Result.Fail(FailReason.Locked);

            double exp = balance.ENEMY_EXP_BASE * Math.Pow(balance.ENEMY_EXP_GROWTH, g - 1);
            new HeroLevelService(save, balance).AddExp(exp);
            return Result.Success;
        }
    }
}
