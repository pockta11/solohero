using System.Collections.Generic;

namespace SoloHero.Core.Balance
{
    /// <summary>First clear of one global stage.</summary>
    public sealed class SimStageRow
    {
        public int G;
        public bool IsBoss;
        public int Day;
        public double PlaySeconds;
        public double WallHours;
        public int Attempts;
        public int Fails;
        public double ClearSeconds;
        public double StuckPlaySeconds;
        public int HeroLevel;
        public int UpgradeHp;
        public int UpgradeAtk;
        public int UpgradeDef;
        public int UpgradeSpd;
        public int SkillsOwned;
        public int SkillLevelSum;
        public int SkillPulls;
        public int SwordGrade;
        public int HelmGrade;
        public int ArmorGrade;
        public int BootsGrade;
        public int Pulls;
        public double Gold;
        public double EarnedTotal;
        public double SpentTotal;
    }

    /// <summary>Totals for one simulated day (24 h wall clock).</summary>
    public sealed class SimDayRow
    {
        public int Day;
        public int HighestStage;
        public int HeroLevel;
        public double PlaySeconds;
        public double EarnedStage;
        public double EarnedOffline;
        public double EarnedRefund;
        public double EarnedBooster;

        /// <summary>D-111 guide quest gold.</summary>
        public double EarnedQuest;
        public double SpentUpgrade;
        public double SpentGacha;
        public double SpentSkill;

        /// <summary>D-114: pet summons and pet level-ups.</summary>
        public double SpentPet;
        public int PetPulls;
        public int GoldPulls;
        public int GemPulls;
        public int NormalAttempts;
        public int NormalFails;
        public int BossAttempts;
        public int BossFails;

        /// <summary>D-124: boss fails where the hero fell (the rest ran out of time).</summary>
        public int BossDeaths;
        public double GoldEnd;
        public double GemEnd;

        public double Earned => EarnedStage + EarnedOffline + EarnedRefund + EarnedBooster + EarnedQuest;
        public double Spent => SpentUpgrade + SpentGacha + SpentSkill + SpentPet;
    }

    /// <summary>One offline claim at session start.</summary>
    public sealed class SimOfflineClaim
    {
        public int Day;
        public double ElapsedSeconds;
        public double Gold;
        public bool Doubled;
        public double EarnedBefore;
        public double OnlineGoldPerMinute;
    }

    /// <summary>Normal-stage clear durations aggregated per global stage.</summary>
    public sealed class SimClearStat
    {
        public int G;
        public int Count;
        public double Sum;
        public double Min = double.MaxValue;
        public double Max;

        public double Average => Count > 0 ? Sum / Count : 0d;
    }

    public sealed class SimReport
    {
        public string Name;
        public int Seed;
        public readonly List<SimStageRow> Stages = new List<SimStageRow>();
        public readonly List<SimDayRow> Days = new List<SimDayRow>();
        public readonly List<SimOfflineClaim> OfflineClaims = new List<SimOfflineClaim>();
        public readonly SortedDictionary<int, SimClearStat> ClearStats = new SortedDictionary<int, SimClearStat>();

        public int FirstFiveMinutesStage;
        public int FirstFiveMinutesUpgrades;
        public int FirstFiveMinutesPulls;

        public double FirstEpicPlaySeconds = -1d;
        public int FirstEpicDay;
        public double FirstLegendaryPlaySeconds = -1d;
        public int FirstLegendaryDay;

        /// <summary>D-114: pets at the end of the run - owned count, the equipped pet, its level and enhance.</summary>
        public int PetsOwned;
        public string PetEquipped = "";
        public int PetLevel;
        public int PetEnhance;

        /// <summary>D-116: equipment promotions made.</summary>
        public int Promotions;

        /// <summary>D-117: rebirths, Soul earned in total, and the permanent boost levels at the end.</summary>
        public int Rebirths;
        public double SoulEarned;
        public int PermGold;
        public int PermAtk;
        public int PermOffline;

        /// <summary>Median of gold-pull value per gold divided by the best upgrade value per gold.</summary>
        public double PullValueParity;

        public double TotalPlaySeconds;
        public int FinalHighestStage;

        public SimStageRow FindStage(int g)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].G == g) return Stages[i];
            }

            return null;
        }
    }
}
