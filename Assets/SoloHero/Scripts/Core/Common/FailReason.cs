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

        /// <summary>D-117: not enough Soul for a permanent rebirth boost.</summary>
        NotEnoughSoul
    }
}
