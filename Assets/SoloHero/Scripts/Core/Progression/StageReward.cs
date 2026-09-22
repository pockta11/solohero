using System;
using SoloHero.Core;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;

namespace SoloHero.Core.Progression
{
    public sealed class StageReward
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public StageReward(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        public Result ApplyClear(int g)
        {
            if (g < 1)
                return Result.Fail(FailReason.Locked);

            _data.gold += Formulas.StageGold(_balance, g);
            // BalanceValues has no exp-per-stage field; grant 0 hero exp.

            if (StageIndex.IsBoss(g, _balance.STAGES_PER_CHAPTER))
            {
                StageIndex.FromGlobal(g, _balance.STAGES_PER_CHAPTER, out int chapter, out _);
                int index = chapter - 1;
                EnsureFlagSlot(index);
                if (!_data.chapterFirstClearFlags[index])
                {
                    _data.gem += _balance.CHAPTER_CLEAR_GEM;
                    _data.chapterFirstClearFlags[index] = true;
                }
            }

            if (g > _data.highestStage)
                _data.highestStage = g;

            _save?.RequestSave();
            return Result.Success;
        }

        private void EnsureFlagSlot(int index)
        {
            while (_data.chapterFirstClearFlags.Count <= index)
                _data.chapterFirstClearFlags.Add(false);
        }
    }
}
