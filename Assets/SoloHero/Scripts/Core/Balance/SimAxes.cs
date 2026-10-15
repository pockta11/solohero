using System.Collections.Generic;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Jobs;
using SoloHero.Core.Progression;
using SoloHero.Core.Save;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Balance
{
    /// <summary>
    /// D-151 (L-04): the growth axes that end at a cap, and which of them a save has reached. The long-term run records the
    /// first day each one ends: once the capped axes are done only the upgrade lanes carry the curve, and progress slows.
    /// </summary>
    public static class SimAxes
    {
        public const string GearSummon = "gear summon level";
        public const string SkillSummon = "skill summon level";
        public const string PetSummon = "pet summon level";
        public const string Talents = "talent points";
        public const string Job = "job advancement";
        public const string LineSkillsOwned = "line skills owned";
        public const string LineSkillsMax = "line skills max level";
        public const string AchievementPrefix = "achievement ";

        /// <summary>Adds the name of every axis <paramref name="save"/> has finished to <paramref name="into"/>.</summary>
        public static void AddCapped(BalanceValues b, SaveDataV2 save, List<string> into)
        {
            if (SummonLevel.Of(b, save, SummonKind.Gear) >= b.SUMMON_LV_MAX) into.Add(GearSummon);
            if (SummonLevel.Of(b, save, SummonKind.Skill) >= b.SUMMON_LV_MAX) into.Add(SkillSummon);
            if (SummonLevel.Of(b, save, SummonKind.Pet) >= b.SUMMON_LV_MAX) into.Add(PetSummon);
            if (TalentService.Earned(b, save) >= TalentPoints()) into.Add(Talents);
            if (JobService.TierOf(save) >= TopJobTier()) into.Add(Job);

            JobLine line = JobService.LineOf(save);
            if (line != JobLine.None)
            {
                int total = 0, owned = 0, maxed = 0;
                Count(save, b, SkillCatalog.StarterIds, ref total, ref owned, ref maxed);
                for (int g = 0; g < 4; g++)
                {
                    SkillDef[] defs = SkillCatalog.OfLine(line, (Grade)g);
                    for (int i = 0; i < defs.Length; i++) Count(save, b, defs[i].Id, ref total, ref owned, ref maxed);
                }

                if (owned >= total) into.Add(LineSkillsOwned);
                if (maxed >= total) into.Add(LineSkillsMax);
            }

            for (int i = 0; i < AchievementCatalog.Count; i++)
            {
                AchievementDef def = AchievementCatalog.All[i];
                if (AchievementService.Claimed(save, i) >= def.Tiers) into.Add(AchievementPrefix + def.Id);
            }
        }

        /// <summary>Talent points that fill every node (D-158: the shared tree and one job page).</summary>
        public static int TalentPoints() => TalentCatalog.FullPoints();

        public static int TopJobTier()
        {
            int top = 0;
            for (int i = 0; i < JobCatalog.All.Length; i++)
            {
                if (JobCatalog.All[i].Tier > top) top = JobCatalog.All[i].Tier;
            }

            return top;
        }

        private static void Count(SaveDataV2 save, BalanceValues b, string[] ids, ref int total, ref int owned, ref int maxed)
        {
            for (int i = 0; i < ids.Length; i++) Count(save, b, ids[i], ref total, ref owned, ref maxed);
        }

        private static void Count(SaveDataV2 save, BalanceValues b, string id, ref int total, ref int owned, ref int maxed)
        {
            total++;
            int level = SkillBook.GetLevel(save, id);
            if (level > 0) owned++;
            if (level >= b.SKILL_MAX_LEVEL) maxed++;
        }
    }
}
