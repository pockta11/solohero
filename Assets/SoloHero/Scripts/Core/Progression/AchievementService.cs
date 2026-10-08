using System;
using System.Collections.Generic;
using SoloHero.Core.Common;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    /// <summary>
    /// D-118 achievements: progress is read from lifetime totals in the save (kills, bosses, best stage and level, summon
    /// counters, collections), so nothing is tracked twice; the save only remembers how many tiers of each track were
    /// claimed. Claims are one tier at a time, in order.
    /// </summary>
    public sealed class AchievementService
    {
        private readonly SaveDataV2 _data;
        private readonly ISaveRequester _save;

        public AchievementService(SaveDataV2 data, ISaveRequester save = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save;
        }

        public event Action Changed;

        public static long Progress(SaveDataV2 data, AchievementKind kind)
        {
            if (data == null) return 0;
            switch (kind)
            {
                case AchievementKind.Kills: return data.totalKills;
                case AchievementKind.BossKills: return data.bossKills;
                case AchievementKind.BestStage: return data.highestStage;
                case AchievementKind.HeroLevel: return data.heroLevel;
                case AchievementKind.GearPulls: return data.totalPullCount;
                case AchievementKind.SkillPulls: return data.skillPullCount;
                case AchievementKind.PetPulls: return data.petPullCount;
                case AchievementKind.GearOwned: return data.ownedEquipment != null ? data.ownedEquipment.Count : 0;
                case AchievementKind.PetsOwned: return data.petOwned != null ? data.petOwned.Count : 0;
                case AchievementKind.SkillsOwned: return data.ownedSkills != null ? data.ownedSkills.Count : 0;
                default: return 0;
            }
        }

        /// <summary>Tiers of a track claimed so far.</summary>
        public static int Claimed(SaveDataV2 data, int index)
        {
            if (data == null || data.achievementTiers == null || index < 0 || index >= data.achievementTiers.Count) return 0;
            return Math.Max(0, data.achievementTiers[index]);
        }

        public long Progress(int index) => Progress(_data, AchievementCatalog.All[index].Kind);

        public int Claimed(int index) => Claimed(_data, index);

        public bool Finished(int index) => Claimed(index) >= AchievementCatalog.All[index].Tiers;

        /// <summary>The tier the track works on (the next to claim), or the last when finished.</summary>
        public int CurrentTier(int index) => Math.Min(Claimed(index), AchievementCatalog.All[index].Tiers - 1);

        public long Target(int index) => AchievementCatalog.All[index].Targets[CurrentTier(index)];

        public double Gems(int index) => AchievementCatalog.All[index].Gems[CurrentTier(index)];

        public bool Claimable(int index) => !Finished(index) && Progress(index) >= Target(index);

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < AchievementCatalog.Count; i++) if (Claimable(i)) n++;
                return n;
            }
        }

        public Result TryClaim(int index)
        {
            if (index < 0 || index >= AchievementCatalog.Count) return Result.Fail(FailReason.Locked);
            if (!Claimable(index)) return Result.Fail(FailReason.Locked);
            _data.gem += Gems(index);
            if (_data.achievementTiers == null) _data.achievementTiers = new List<int>();
            while (_data.achievementTiers.Count <= index) _data.achievementTiers.Add(0);
            _data.achievementTiers[index] = Claimed(index) + 1;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        /// <summary>Claims every completed tier of every track; returns the gems earned.</summary>
        public double ClaimAll()
        {
            double gems = 0d;
            for (int i = 0; i < AchievementCatalog.Count; i++)
            {
                for (int guard = 0; guard < 32 && Claimable(i); guard++)
                {
                    double g = Gems(i);
                    if (!TryClaim(i).Ok) break;
                    gems += g;
                }
            }

            return gems;
        }
    }
}
