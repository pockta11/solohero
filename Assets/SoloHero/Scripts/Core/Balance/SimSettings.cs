namespace SoloHero.Core.Balance
{
    /// <summary>One play session per day: when it starts (local hour) and how long the app stays open.</summary>
    public readonly struct SimSession
    {
        public readonly double StartHour;
        public readonly double Minutes;

        public SimSession(double startHour, double minutes)
        {
            StartHour = startHour;
            Minutes = minutes;
        }
    }

    /// <summary>
    /// Player model for <see cref="BalanceSimulator"/>. Everything that is an assumption about the player
    /// (not a game rule) lives here so the report can state it next to the numbers.
    /// </summary>
    public sealed class SimSettings
    {
        public string Name = "no-ads";
        public int Days = 7;
        public int Seed = 1;
        public float DeltaTime = 1f / 30f;

        /// <summary>GDD persona P-A: 3-5 sessions a day, 2-5 minutes each, one longer sitting. 30 minutes a day.</summary>
        public SimSession[] DailySessions =
        {
            new SimSession(8.0, 8.0),
            new SimSession(12.5, 5.0),
            new SimSession(18.5, 5.0),
            new SimSession(22.0, 12.0)
        };

        /// <summary>Watch every rewarded ad the day allows (offline x2, gem, gold booster).</summary>
        public bool UseAds;

        /// <summary>The tutorial makes the first pull as soon as the player can afford it (GDD first 30 minutes).</summary>
        public bool TutorialFirstPull = true;

        /// <summary>After a boss fail the player retreats and challenges again after this many farming clears.</summary>
        public int BossRetryAfterFarmClears = 4;

        /// <summary>Stop spending decisions after this many purchases in one decision point (safety cap).</summary>
        public int MaxPurchasesPerDecision = 400;

        /// <summary>Unix seconds of day 1, 00:00 local. The simulation treats local time as UTC.</summary>
        public long StartUtc = 1_800_000_000L;

        public static SimSettings NoAds(int seed) => new SimSettings { Name = "no-ads", Seed = seed, UseAds = false };

        public static SimSettings WithAds(int seed) => new SimSettings { Name = "ads", Seed = seed, UseAds = true };
    }
}
