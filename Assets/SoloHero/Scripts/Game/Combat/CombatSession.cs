using System;
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
            _runner.Resume(save.farmingStage < 1 ? 1 : save.farmingStage, save.retreatMode);
        }

        private void OnDestroy()
        {
            if (_runner == null) return;
            _runner.Hero.AttackRequested -= OnAttackRequested;
            _runner.StateChanged -= OnStageStateChanged;
        }

        private void Update()
        {
            if (!enabled || _runner == null) return;
            _runner.ClearGoldMultiplier = _ads != null ? _ads.StageGoldMultiplier : 1d;
            _runner.Tick(Time.deltaTime);
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
