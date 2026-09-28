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
        public const string SkillLevel = "skill_level";
        public const string SkillSummon = "skill_summon";
        public const string AdReward = "ad_reward";
        public const string AdFail = "ad_fail";
        public const string OfflineClaim = "offline_claim";

        public const string UserHighestChapter = "highest_chapter";

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
        public const string PLane = "lane";
        public const string PSlot = "slot";
        public const string PSkill = "skill";
        public const string PGold = "gold";
        public const string PDoubled = "doubled";
    }
}
