using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    public sealed class CombatSession : MonoBehaviour
    {
        private StageRunner _runner;

        private void Start()
        {
            BalanceValues balance;
            SaveDataV2 save;
            try
            {
                balance = Services.Get<BalanceValues>();
                save = Services.Get<SaveDataV2>();
            }
            catch (Exception)
            {
                Log.Warn(LogTag.Combat, "combat session missing BalanceValues or SaveDataV2");
                enabled = false;
                return;
            }

            HeroStats stats = StatAggregator.Compute(balance, save);

            IRandom random;
            try
            {
                random = Services.Get<IRandom>();
            }
            catch (Exception)
            {
                random = new SystemRandom();
            }

            _runner = new StageRunner(balance, random, stats, save);
            _runner.Begin(save.farmingStage < 1 ? 1 : save.farmingStage);
        }

        private void Update()
        {
            if (!enabled || _runner == null) return;
            _runner.Tick(Time.deltaTime);
        }
    }
}
