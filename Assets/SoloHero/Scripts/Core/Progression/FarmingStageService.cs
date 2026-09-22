using System;
using SoloHero.Core.Common;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    public sealed class FarmingStageService
    {
        private readonly ISaveRequester _save;

        public FarmingStageService(ISaveRequester save = null)
        {
            _save = save;
        }

        public Result TrySet(SaveDataV2 save, int stage)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            if (stage < 1 || stage > save.highestStage)
                return Result.Fail(FailReason.Locked);

            if (save.farmingStage == stage)
                return Result.Success;

            save.farmingStage = stage;
            _save?.RequestSave();
            return Result.Success;
        }
    }
}
