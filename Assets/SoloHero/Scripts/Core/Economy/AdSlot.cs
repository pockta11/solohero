namespace SoloHero.Core.Economy
{
    /// <summary>GDD rewarded ad slots: A-1 offline x2, A-2 gems, A-3 gold booster.</summary>
    public enum AdSlot
    {
        OfflineDouble = 0,
        Gem = 1,
        GoldBooster = 2,

        /// <summary>D-120: a free gear ten-pull for an ad.</summary>
        FreeGearSummon = 3,

        /// <summary>D-120: a free pet ten-pull for an ad (after the pet summon opens).</summary>
        FreePetSummon = 4,

        /// <summary>D-129: the battle runs faster for a while (AD_SPEED_MULT for AD_SPEED_SECONDS).</summary>
        BattleSpeed = 5
    }
}
