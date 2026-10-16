namespace SoloHero.Core.Analytics
{
    /// <summary>
    /// Event and parameter names (E6-15). Firebase rules: snake_case, at most 40 characters, no reserved prefixes.
    /// Retention, sessions and session length come from Firebase's automatic events and are not logged here.
    /// The metric each event serves is in _bmad-output/implementation-artifacts/6-15-analytics-events.md.
    /// </summary>
    public static class AnalyticsEvents
    {
        public const string StageReach = "stage_reach";
        public const string StageClear = "stage_clear";
        public const string StageFail = "stage_fail";
        public const string RetreatEnter = "retreat_enter";
        public const string Challenge = "challenge";
        public const string TutorialStep = "tutorial_step";
        public const string LevelUp = "hero_level_up";
        public const string GoldSession = "gold_session";
        public const string GachaPull = "gacha_pull";
        public const string Upgrade = "upgrade";

        /// <summary>D-143: a lane's limit break (lane, breaks after it).</summary>
        public const string LimitBreak = "limit_break";
        public const string SkillLevel = "skill_level";
        /// <summary>D-174: a skill awakening star (skill, level = the new stars).</summary>
        public const string SkillAwaken = "skill_awaken";

        /// <summary>D-177: a pet awakening star (pet, level = stars).</summary>
        public const string PetAwaken = "pet_awaken";
        public const string SkillSummon = "skill_summon";

        /// <summary>D-114 pet summon; same params as gacha_pull.</summary>
        public const string PetSummon = "pet_summon";
        public const string AdReward = "ad_reward";
        public const string AdFail = "ad_fail";
        public const string OfflineClaim = "offline_claim";

        /// <summary>
        /// P1-2: a C# exception or Log.Error in the player (they do not crash the app, so Play's vitals never see them).
        /// Params: kind (Exception / Error / Assert), message (first line, 100 chars), where (first stack frame).
        /// </summary>
        public const string AppException = "app_exception";

        /// <summary>D-133: account tools in the settings (transfer code issued / redeemed, data deleted).</summary>
        public const string Account = "account_action";

        /// <summary>
        /// D-170 (roadmap section 6): the frontier has not moved for a day or more. Once per local day.
        /// Params: stage (the next frontier stage), chapter, tier, boss, days (since the frontier last moved).
        /// </summary>
        public const string StageStall = "stage_stall";

        /// <summary>D-170: a growth axis with an upper limit reached it (once per save). Params: axis, level, stage.</summary>
        public const string AxisCap = "axis_cap";

        /// <summary>D-170: the frontier entered a new difficulty tier (hard, hell, ...). Params: tier, stage.</summary>
        public const string TierEnter = "tier_enter";

        public const string UserHighestChapter = "highest_chapter";
        public const string UserDifficultyTier = "difficulty_tier";

        public const string PStage = "stage";
        public const string PChapter = "chapter";
        public const string PBoss = "boss";
        public const string PSeconds = "seconds";
        public const string PAttempt = "attempt";
        public const string PStep = "step";
        public const string PLevel = "level";
        public const string PEarned = "earned";
        public const string PSpent = "spent";
        public const string PKind = "kind";
        public const string PCount = "count";
        public const string PBestGrade = "best_grade";
        public const string PPity = "pity";
        /// <summary>D-173: the pickup banner running at a pet or skill summon, or "none".</summary>
        public const string PPickup = "pickup";
        public const string PLane = "lane";
        public const string PSlot = "slot";
        public const string PSkill = "skill";

        /// <summary>D-177: the pet id of a pet awakening.</summary>
        public const string PPet = "pet";
        public const string PGold = "gold";
        public const string PDoubled = "doubled";
        public const string PMessage = "message";
        public const string PWhere = "where";
        public const string PAction = "action";
        public const string PTier = "tier";
        public const string PDays = "days";
        public const string PAxis = "axis";

        /// <summary>D-170: axis_cap values.</summary>
        public const string AxisGearSummon = "summon_gear";
        public const string AxisSkillSummon = "summon_skill";
        public const string AxisPetSummon = "summon_pet";
        public const string AxisFinalJob = "job_final";

        /// <summary>D-179: a job advancement (which job, which tier, at what level).</summary>
        public const string JobAdvance = "job_advance";

        /// <summary>D-181: a store purchase granted (product id, lifetime count).</summary>
        public const string Purchase = "iap_purchase";

        public const string PJob = "job";
        public const string PProduct = "product";
    }
}
