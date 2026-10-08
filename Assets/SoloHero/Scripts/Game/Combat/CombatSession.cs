using System;
using SoloHero.Core.Analytics;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;
using SoloHero.Core.Stage;
using UnityEngine;

namespace SoloHero.Game.Combat
{
    public sealed class CombatSession : MonoBehaviour
    {
        private StageRunner _runner;
        private BalanceValues _balance;
        private SaveDataV2 _save;
        private int _appliedHeroLevel;
        private AdSlotPolicy _ads;
        private GameplayTelemetry _telemetry;

        public StageRunner Runner => _runner;

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

            HeroStats stats = CombatLoadout.ComputeStats(balance, save);

            IRandom random;
            try
            {
                random = Services.Get<IRandom>();
            }
            catch (Exception)
            {
                random = new SystemRandom();
            }

            _balance = balance;
            _save = save;
            try
            {
                _ads = Services.Get<AdSlotPolicy>();
            }
            catch (Exception)
            {
                _ads = null;
            }

            ISaveRequester saveRequester = null;
            try
            {
                saveRequester = Services.Get<ISaveRequester>();
            }
            catch (Exception)
            {
                Log.Warn(LogTag.Combat, "no save requester, stage clears will not save");
            }

            _runner = new StageRunner(balance, random, stats, save, saveRequester);
            // Until attack clips carry the OnHitFrame event (E8-06), the hit lands when the attack starts.
            _runner.Hero.AttackRequested += OnAttackRequested;
            _runner.StateChanged += OnStageStateChanged;
            RefreshLoadout();
            IAnalytics analytics;
            try
            {
                analytics = Services.Get<IAnalytics>();
            }
            catch (Exception)
            {
                analytics = new NullAnalytics();
            }

            // Created before Resume so the first stage start is seen (E6-15).
            _telemetry = new GameplayTelemetry(_runner, save, balance, analytics);
            _runner.Resume(save.farmingStage < 1 ? 1 : save.farmingStage, save.retreatMode);
            // Cold start marker (E1-10): tools/qa/Qa.ps1 waits for / times this line. Log.Info is compiled out of
            // release builds, so the marker goes straight to the Unity log.
            Debug.Log("[Combat] battle ready at stage " + _runner.GlobalStage);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (_telemetry != null)
            {
                _telemetry.Flush();
                _telemetry.Dispose();
            }

            if (_runner == null) return;
            _runner.Hero.AttackRequested -= OnAttackRequested;
            _runner.StateChanged -= OnStageStateChanged;
        }

        private void Update()
        {
            if (!enabled || _runner == null) return;
            _runner.ClearGoldMultiplier = _ads != null ? _ads.StageGoldMultiplier : 1d;
            // D-129: the speed booster scales game time (logic, animation, effects); timers in real time stay real.
            float speed = _ads != null ? _ads.BattleSpeedMultiplier : 1f;
            if (!Mathf.Approximately(Time.timeScale, speed)) Time.timeScale = speed;
            _runner.Tick(Time.deltaTime);
            _telemetry?.Tick(Time.unscaledDeltaTime);
            if (_save.heroLevel != _appliedHeroLevel) RefreshLoadout();
        }

        /// <summary>Call after any growth change (upgrade, equip, skill level) so combat uses the new loadout.</summary>
        public void RefreshLoadout()
        {
            if (_runner == null) return;
            CombatLoadout.Apply(_runner, _balance, _save);
            _appliedHeroLevel = _save.heroLevel;
        }

        private void OnAttackRequested() => _runner.Hero.OnHitFrame(_runner.World);

        private void OnStageStateChanged(StageState state)
        {
            if (state != StageState.Running && state != StageState.Retreat && state != StageState.BossIntro) return;
            _save.farmingStage = _runner.GlobalStage;
            _save.retreatMode = _runner.RetreatMode;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) _telemetry?.Flush();
            if (paused || _runner == null) return;

            SaveDataV2 save;
            try
            {
                save = Services.Get<SaveDataV2>();
            }
            catch (Exception)
            {
                return;
            }

            _runner.Resume(save.farmingStage < 1 ? 1 : save.farmingStage, save.retreatMode);
        }
    }
}
