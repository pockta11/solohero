namespace SoloHero.Core.Talents
{
    /// <summary>
    /// One node of the talent tree (D-087). Tier 0-3 top to bottom; Column 0 / 1 inside the branch (a capstone uses
    /// column 0 and spans both). Each rank adds <see cref="PerRank"/> of <see cref="Stat"/>.
    /// </summary>
    public sealed class TalentDef
    {
        public string Id { get; init; } = "";
        public TalentBranch Branch { get; init; }
        public int Tier { get; init; }
        public int Column { get; init; }
        public int MaxRank { get; init; } = 5;
        public TalentStat Stat { get; init; }
        public double PerRank { get; init; }

        public bool IsCapstone => MaxRank == 1;
        public string NameKey => "talent.name." + Id;
        public string DescKey => "talent.desc." + Id;
    }
}
