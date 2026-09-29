namespace SoloHero.Core.Talents
{
    /// <summary>What one talent rank changes (D-087). Fractions unless noted; capstones are on / off.</summary>
    public enum TalentStat
    {
        AtkPct,
        CritPoints,
        CritDamage,
        AtkSpdPct,
        BossDamagePct,
        Execute,
        HpPct,
        DefPct,
        DamageTakenPct,
        HealPct,
        LastStand,
        SkillDamagePct,
        CooldownPct,
        BuffDurationPct,
        DotPct,
        Overload
    }

    public enum TalentBranch
    {
        Might,
        Guard,
        Arcane
    }
}
