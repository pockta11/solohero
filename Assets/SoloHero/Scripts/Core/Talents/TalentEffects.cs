namespace SoloHero.Core.Talents
{
    /// <summary>
    /// Summed talent ranks (D-087). Stat parts feed <c>StatAggregator</c>; combat parts reach the hero and the skill
    /// caster through <c>StageRunner.SetTalents</c>. All zero = no talents.
    /// </summary>
    public sealed class TalentEffects
    {
        public static readonly TalentEffects None = new TalentEffects();

        public double AtkPct;
        public double CritPoints;
        public double CritDamage;
        public double AtkSpdPct;
        public double BossDamagePct;
        public bool Execute;
        public double HpPct;
        public double DefPct;
        public double DamageTakenPct;
        public double HealPct;
        public bool LastStand;
        public double SkillDamagePct;
        public double CooldownPct;
        public double BuffDurationPct;
        public double DotPct;
        public bool Overload;

        public void Add(TalentStat stat, double amount)
        {
            switch (stat)
            {
                case TalentStat.AtkPct: AtkPct += amount; break;
                case TalentStat.CritPoints: CritPoints += amount; break;
                case TalentStat.CritDamage: CritDamage += amount; break;
                case TalentStat.AtkSpdPct: AtkSpdPct += amount; break;
                case TalentStat.BossDamagePct: BossDamagePct += amount; break;
                case TalentStat.Execute: Execute = amount > 0d; break;
                case TalentStat.HpPct: HpPct += amount; break;
                case TalentStat.DefPct: DefPct += amount; break;
                case TalentStat.DamageTakenPct: DamageTakenPct += amount; break;
                case TalentStat.HealPct: HealPct += amount; break;
                case TalentStat.LastStand: LastStand = amount > 0d; break;
                case TalentStat.SkillDamagePct: SkillDamagePct += amount; break;
                case TalentStat.CooldownPct: CooldownPct += amount; break;
                case TalentStat.BuffDurationPct: BuffDurationPct += amount; break;
                case TalentStat.DotPct: DotPct += amount; break;
                case TalentStat.Overload: Overload = amount > 0d; break;
            }
        }
    }
}
