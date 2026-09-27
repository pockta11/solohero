namespace SoloHero.Core.Economy
{
    public enum AdOutcome
    {
        /// <summary>Watched to the reward point: grant and count the slot.</summary>
        Rewarded,

        /// <summary>Closed early: no reward, the daily count is not used (GDD).</summary>
        Closed,

        /// <summary>Not loaded or failed to show: no reward, the normal path stays available.</summary>
        Failed
    }
}
