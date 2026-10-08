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

        /// <summary>D-104: the skill belongs to another job line (or the hero has no job yet).</summary>
        JobLocked,

        /// <summary>D-128: no summon tickets of that kind.</summary>
        NotEnoughTicket,

        /// <summary>D-134: the account tools need the server and it did not answer.</summary>
        NetworkUnavailable,

        /// <summary>D-134: a transfer code that is not 12 characters of the code alphabet.</summary>
        CodeInvalid,

        /// <summary>D-134: no transfer under that code, or it expired.</summary>
        CodeNotFound,

        /// <summary>D-141: no unspent AP (or nothing to reset).</summary>
        NoApPoints,

        /// <summary>D-143: the lane is at its limit and needs a limit break first.</summary>
        LimitReached,

        /// <summary>D-143: not enough breakthrough stones for the limit break.</summary>
        NotEnoughStones
    }
}
