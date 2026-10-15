using System.Collections.Generic;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using SoloHero.Core.Pets;
using SoloHero.Core.Skills;

namespace SoloHero.Core.Events
{
    /// <summary>
    /// D-173 (L-19) pickup banners shipped in the app; the schedule (RTDB config/events, template = a banner id) opens
    /// them, next to the running event. Pets: one banner per Epic or better pet that shares its grade (a pet alone in
    /// its grade has nothing to win over; <c>pickup_pet_{id}</c>). Skills: the
    /// k-th Legendary skill of every job line together (<c>pickup_skill_{k}</c>, k from 1), so the banner fits whatever
    /// line the hero follows.
    /// </summary>
    public static class PickupCatalog
    {
        public const string Prefix = "pickup_";

        public static readonly PickupBanner[] All = Build();

        public static PickupBanner Find(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith(Prefix, System.StringComparison.Ordinal)) return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }

            return null;
        }

        private static PickupBanner[] Build()
        {
            var list = new List<PickupBanner>();
            for (int i = 0; i < PetCatalog.Count; i++)
            {
                PetDef pet = PetCatalog.All[i];
                if (pet.Grade < GearGrade.Epic || PetCatalog.OfGrade(pet.Grade).Length < 2) continue;
                list.Add(new PickupBanner { Id = Prefix + "pet_" + pet.Id, Kind = SummonKind.Pet, Targets = new[] { pet.Id } });
            }

            // The base (first and second job) Legendary skills of each line, k-th of each line together.
            JobLine[] lines = { JobLine.Warrior, JobLine.Mage, JobLine.Archer };
            var perLine = new List<string>[lines.Length];
            int most = 0;
            for (int l = 0; l < lines.Length; l++)
            {
                perLine[l] = new List<string>();
                foreach (SkillDef def in SkillCatalog.OfLine(lines[l], Grade.Legendary))
                    if (def.JobTier < 3) perLine[l].Add(def.Id);
                if (perLine[l].Count > most) most = perLine[l].Count;
            }

            for (int k = 0; k < most; k++)
            {
                var targets = new List<string>();
                for (int l = 0; l < lines.Length; l++)
                    if (k < perLine[l].Count) targets.Add(perLine[l][k]);
                list.Add(new PickupBanner { Id = Prefix + "skill_" + (k + 1), Kind = SummonKind.Skill, Targets = targets.ToArray() });
            }

            return list.ToArray();
        }
    }
}
