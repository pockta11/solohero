namespace SoloHero.Core.Common
{
    public enum FailReason
    {
        None,
        NotEnoughGold,
        NotEnoughGem,
        MaxLevel,
        OnCooldown,
        Locked,
        Busy,
        DailyLimit,
        AdUnavailable,
        SlotsFull,
        NoTalentPoints,
        JobLocked,
        NotEnoughTicket,
        NetworkUnavailable,
        CodeInvalid,
        CodeNotFound,
        NoApPoints,
        LimitReached,
        NotEnoughStones,
        NotEnoughTokens,
        EventEnded,
        NotEnoughAwakenStones,

        /// <summary>D-174 / D-177: awakening needs the skill or pet at its max level.</summary>
        NeedMaxLevel,

        /// <summary>D-175: not enough raid medals.</summary>
        NotEnoughMedals,

        /// <summary>D-181: the store is not reachable or not set up (no purchasing package, init failed).</summary>
        StoreUnavailable
    }
}
