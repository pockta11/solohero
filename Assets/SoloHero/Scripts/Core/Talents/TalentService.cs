using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Jobs;
using SoloHero.Core.Save;

namespace SoloHero.Core.Talents
{
    /// <summary>
    /// Talent tree rules (D-087): TALENT_POINTS_PER_LEVEL points per hero level after the first, spent one rank at a
    /// time; tier t of a branch opens after t x TALENT_TIER_STEP points spent in that branch. A reset returns every
    /// point for TALENT_RESET_GEM gems. Learning and resetting are player decisions and request a save.
    /// Ranks live in <see cref="SaveDataV2.talentIds"/> / <see cref="SaveDataV2.talentRanks"/> (index-aligned).
    /// D-158 (L-07): the job talent page of the hero's line opens with the third job and its ranks cost
    /// <see cref="TalentDef.Cost"/> points from the same pool.
    /// </summary>
    public sealed class TalentService
    {
        private readonly SaveDataV2 _data;
        private readonly BalanceValues _balance;
        private readonly ISaveRequester _save;

        public TalentService(SaveDataV2 data, BalanceValues balance, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _save = save;
        }

        /// <summary>Ranks changed (learn or reset).</summary>
        public event Action Changed;

        public int AvailablePoints => Available(_balance, _data);

        public int RankOf(string id) => Rank(_data, id);

        public static int Earned(BalanceValues balance, SaveDataV2 data)
        {
            int levels = data.heroLevel - 1;
            return levels > 0 ? levels * balance.TALENT_POINTS_PER_LEVEL : 0;
        }

        /// <summary>Points in learned ranks (D-158: a job talent rank costs its <see cref="TalentDef.Cost"/>).</summary>
        public static int Spent(SaveDataV2 data)
        {
            int sum = 0;
            for (int i = 0; i < data.talentRanks.Count; i++)
            {
                TalentDef def = i < data.talentIds.Count ? TalentCatalog.Find(data.talentIds[i]) : null;
                sum += data.talentRanks[i] * (def != null ? def.Cost : 1);
            }

            return sum;
        }

        public static int Available(BalanceValues balance, SaveDataV2 data)
        {
            int left = Earned(balance, data) - Spent(data);
            return left > 0 ? left : 0;
        }

        public static int Rank(SaveDataV2 data, string id)
        {
            int index = data.talentIds.IndexOf(id);
            return index >= 0 && index < data.talentRanks.Count ? data.talentRanks[index] : 0;
        }

        public static int BranchSpent(SaveDataV2 data, TalentBranch branch)
        {
            int sum = 0;
            for (int i = 0; i < data.talentIds.Count && i < data.talentRanks.Count; i++)
            {
                TalentDef def = TalentCatalog.Find(data.talentIds[i]);
                if (def != null && !def.IsJobTalent && def.Branch == branch) sum += data.talentRanks[i];
            }

            return sum;
        }

        /// <summary>Points that must be spent in the branch before a node of this tier can take ranks.</summary>
        public static int TierRequirement(BalanceValues balance, int tier) => tier * balance.TALENT_TIER_STEP;

        public static bool IsTierOpen(BalanceValues balance, SaveDataV2 data, TalentDef def) =>
            BranchSpent(data, def.Branch) >= TierRequirement(balance, def.Tier);

        /// <summary>D-158: a job talent opens with the third job of its own line.</summary>
        public static bool IsJobPageOpen(SaveDataV2 data, TalentDef def) =>
            def.Line == JobService.LineOf(data) && JobService.TierOf(data) >= TalentCatalog.JobPageTier;

        /// <summary>The node may take ranks now (tier opened in its branch, or its job page opened).</summary>
        public static bool IsOpen(BalanceValues balance, SaveDataV2 data, TalentDef def) =>
            def.IsJobTalent ? IsJobPageOpen(data, def) : IsTierOpen(balance, data, def);

        public Result CanLearn(string id) => CanLearn(_balance, _data, id);

        public static Result CanLearn(BalanceValues balance, SaveDataV2 data, string id)
        {
            TalentDef def = TalentCatalog.Find(id);
            if (def == null) return Result.Fail(FailReason.Locked);
            if (Rank(data, id) >= def.MaxRank) return Result.Fail(FailReason.MaxLevel);
            if (!IsOpen(balance, data, def)) return Result.Fail(def.IsJobTalent ? FailReason.JobLocked : FailReason.Locked);
            if (Available(balance, data) < def.Cost) return Result.Fail(FailReason.NoTalentPoints);
            return Result.Success;
        }

        /// <summary>True when some node of the tree or of the hero's job page can take a rank now (the tab badge).</summary>
        public static bool HasLearnable(BalanceValues balance, SaveDataV2 data)
        {
            if (Available(balance, data) < 1) return false;
            foreach (TalentDef def in TalentCatalog.All)
                if (CanLearn(balance, data, def.Id).Ok) return true;
            foreach (TalentDef def in TalentCatalog.JobNodes(JobService.LineOf(data)))
                if (CanLearn(balance, data, def.Id).Ok) return true;
            return false;
        }

        public Result TryLearn(string id)
        {
            Result check = CanLearn(id);
            if (!check.Ok) return check;

            Align();
            int index = _data.talentIds.IndexOf(id);
            if (index < 0)
            {
                _data.talentIds.Add(id);
                _data.talentRanks.Add(1);
            }
            else
            {
                _data.talentRanks[index]++;
            }

            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>Returns every point for TALENT_RESET_GEM gems. Nothing learned is Busy.</summary>
        public Result TryReset()
        {
            if (Spent(_data) == 0) return Result.Fail(FailReason.Busy);
            if (_data.gem < _balance.TALENT_RESET_GEM) return Result.Fail(FailReason.NotEnoughGem);

            _data.gem -= _balance.TALENT_RESET_GEM;
            _data.talentIds.Clear();
            _data.talentRanks.Clear();
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>Sum of every learned rank (ranks above a node's max are ignored).</summary>
        public static TalentEffects Effects(SaveDataV2 data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.talentIds.Count == 0) return TalentEffects.None;
            var effects = new TalentEffects();
            AddTo(effects, data);
            return effects;
        }

        /// <summary>
        /// Adds the learned talents to <paramref name="effects"/> (D-104: job mastery is added on top; D-158: job talents
        /// count only for their own line).
        /// </summary>
        public static void AddTo(TalentEffects effects, SaveDataV2 data)
        {
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            if (data == null) throw new ArgumentNullException(nameof(data));
            JobLine line = JobService.LineOf(data);
            for (int i = 0; i < data.talentIds.Count && i < data.talentRanks.Count; i++)
            {
                TalentDef def = TalentCatalog.Find(data.talentIds[i]);
                if (def == null || (def.IsJobTalent && def.Line != line)) continue;
                int rank = Math.Min(data.talentRanks[i], def.MaxRank);
                if (rank > 0) effects.Add(def.Stat, def.PerRank * rank);
            }
        }

        private void Align()
        {
            while (_data.talentRanks.Count < _data.talentIds.Count) _data.talentRanks.Add(0);
            while (_data.talentRanks.Count > _data.talentIds.Count) _data.talentRanks.RemoveAt(_data.talentRanks.Count - 1);
        }
    }
}
