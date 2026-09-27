using System;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    public sealed class KillExp
    {
        public static Result Grant(SaveDataV2 save, BalanceValues balance, int g, bool isBoss = false)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            if (g < 1)
                return Result.Fail(FailReason.Locked);

            double exp = Formulas.EnemyExp(balance, g, isBoss);
            new HeroLevelService(save, balance).AddExp(exp);
            return Result.Success;
        }
    }
}
