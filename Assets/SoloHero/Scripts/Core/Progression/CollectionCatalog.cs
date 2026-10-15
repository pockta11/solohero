using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using SoloHero.Core.Pets;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Progression
{
    public enum CollectionKind
    {
        Gear = 0,
        Pet = 1,
        Skill = 2
    }

    /// <summary>One set of the collection: every item of a kind and grade, and the stat it adds once complete.</summary>
    public sealed class CollectionSet
    {
        public string Id { get; init; } = "";
        public CollectionKind Kind { get; init; }

        /// <summary>The grade index: the seven-grade ladder for gear and pets, the four-grade <see cref="Grade"/> for skills.</summary>
        public int Grade { get; init; }

        public TalentStat Stat { get; init; }
        public double Amount { get; init; }
    }

    /// <summary>
    /// D-164 (L-18) collection codex (genre: set bonuses for collecting). Every grade of gear (all eight slots), of pets
    /// and of the hero's own line of skills is a set; a complete set adds its stat for good, through the talent channel.
    /// New content joins its grade's set, so updates raise the collection's worth on their own. Ids are for strings.
    /// </summary>
    public static class CollectionCatalog
    {
        public static readonly CollectionSet[] All =
        {
            Set("gear_c", CollectionKind.Gear, 0, TalentStat.AtkPct, 0.01),
            Set("gear_u", CollectionKind.Gear, 1, TalentStat.AtkPct, 0.02),
            Set("gear_r", CollectionKind.Gear, 2, TalentStat.AtkPct, 0.03),
            Set("gear_e", CollectionKind.Gear, 3, TalentStat.AtkPct, 0.05),
            Set("gear_l", CollectionKind.Gear, 4, TalentStat.AtkPct, 0.08),
            Set("gear_m", CollectionKind.Gear, 5, TalentStat.AtkPct, 0.12),
            Set("gear_a", CollectionKind.Gear, 6, TalentStat.AtkPct, 0.2),

            Set("pet_c", CollectionKind.Pet, 0, TalentStat.HpPct, 0.03),
            Set("pet_u", CollectionKind.Pet, 1, TalentStat.HpPct, 0.04),
            Set("pet_r", CollectionKind.Pet, 2, TalentStat.HpPct, 0.05),
            Set("pet_e", CollectionKind.Pet, 3, TalentStat.HpPct, 0.08),
            Set("pet_l", CollectionKind.Pet, 4, TalentStat.HpPct, 0.12),
            Set("pet_m", CollectionKind.Pet, 5, TalentStat.HpPct, 0.15),
            Set("pet_a", CollectionKind.Pet, 6, TalentStat.HpPct, 0.2),

            Set("skill_c", CollectionKind.Skill, 0, TalentStat.SkillDamagePct, 0.03),
            Set("skill_r", CollectionKind.Skill, 1, TalentStat.SkillDamagePct, 0.05),
            Set("skill_e", CollectionKind.Skill, 2, TalentStat.SkillDamagePct, 0.08),
            Set("skill_l", CollectionKind.Skill, 3, TalentStat.SkillDamagePct, 0.12),
        };

        public static int Count => All.Length;

        private static CollectionSet Set(string id, CollectionKind kind, int grade, TalentStat stat, double amount) =>
            new CollectionSet { Id = id, Kind = kind, Grade = grade, Stat = stat, Amount = amount };

        /// <summary>How many members of <paramref name="set"/> the save owns, and how many there are.</summary>
        public static void Progress(SaveDataV2 data, CollectionSet set, out int owned, out int total)
        {
            owned = 0;
            total = 0;
            if (data == null || set == null) return;
            switch (set.Kind)
            {
                case CollectionKind.Gear:
                    for (int s = 0; s < GachaCatalog.SlotCount; s++)
                    {
                        total++;
                        if (data.ownedEquipment.Contains(GachaCatalog.IdOf((EquipmentSlot)s, (GearGrade)set.Grade))) owned++;
                    }

                    break;
                case CollectionKind.Pet:
                    for (int p = 0; p < PetCatalog.Count; p++)
                    {
                        if ((int)PetCatalog.All[p].Grade != set.Grade) continue;
                        total++;
                        if (PetService.IsOwned(data, p)) owned++;
                    }

                    break;
                default:
                    // The hero's own line, third-job skills included; the beginner has no skill sets yet.
                    JobLine line = JobService.LineOf(data);
                    if (line == JobLine.None) return;
                    foreach (SkillDef def in SkillCatalog.OfLine(line, (Grade)set.Grade))
                    {
                        total++;
                        if (SkillBook.IsOwned(data, def.Id)) owned++;
                    }

                    break;
            }
        }

        public static bool IsComplete(SaveDataV2 data, CollectionSet set)
        {
            Progress(data, set, out int owned, out int total);
            return total > 0 && owned >= total;
        }

        public static int CompleteCount(SaveDataV2 data)
        {
            int n = 0;
            foreach (CollectionSet set in All)
                if (IsComplete(data, set)) n++;
            return n;
        }

        /// <summary>Adds every complete set's stat to <paramref name="effects"/> (CombatLoadout's talent channel).</summary>
        public static void AddTo(TalentEffects effects, SaveDataV2 data)
        {
            if (effects == null || data == null) return;
            foreach (CollectionSet set in All)
                if (IsComplete(data, set)) effects.Add(set.Stat, set.Amount);
        }

        /// <summary>The summed bonus of one stat over the complete sets (for the codex header).</summary>
        public static double Total(SaveDataV2 data, TalentStat stat)
        {
            double sum = 0d;
            foreach (CollectionSet set in All)
                if (set.Stat == stat && IsComplete(data, set)) sum += set.Amount;
            return sum;
        }
    }
}
