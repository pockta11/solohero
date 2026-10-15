using SoloHero.Core.Jobs;

namespace SoloHero.Core.Talents
{
    /// <summary>
    /// One node of the talent tree (D-087). Tier 0-3 top to bottom; Column 0 / 1 inside the branch (a capstone uses
    /// column 0 and spans both). Each rank adds <see cref="PerRank"/> of <see cref="Stat"/>.
    /// D-158 (L-07): a node with a <see cref="Line"/> belongs to that line's job talent page, which opens with the
    /// third job; its Tier is the row on the page and each rank costs <see cref="Cost"/> points.
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

        /// <summary>D-158: None = the shared tree; a job line = that line's job talent page.</summary>
        public JobLine Line { get; init; }

        /// <summary>D-158: talent points one rank costs (job talents 2).</summary>
        public int Cost { get; init; } = 1;

        public bool IsJobTalent => Line != JobLine.None;
        public bool IsCapstone => MaxRank == 1;
        public string NameKey => "talent.name." + Id;
        public string DescKey => "talent.desc." + Id;
    }
}
