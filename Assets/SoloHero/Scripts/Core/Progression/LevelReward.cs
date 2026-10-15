using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Progression
{
    /// <summary>What one level reward pays (all zero when the level has none).</summary>
    public readonly struct LevelRewardPay
    {
        public readonly int Gems;
        public readonly int GearTickets;
        public readonly int SkillTickets;
        public readonly int PetTickets;
        public readonly int Stones;

        public LevelRewardPay(int gems, int gearTickets, int skillTickets, int petTickets, int stones)
        {
            Gems = gems;
            GearTickets = gearTickets;
            SkillTickets = skillTickets;
            PetTickets = petTickets;
            Stones = stones;
        }

        public bool Any => Gems > 0 || GearTickets > 0 || SkillTickets > 0 || PetTickets > 0 || Stones > 0;
    }

    /// <summary>
    /// D-154 (L-08) level rewards (genre "growth reward"): every LEVEL_REWARD_EVERY levels pays gems, gear tickets and
    /// breakthrough stones, every LEVEL_REWARD_BIG_EVERY levels more gems and skill and pet tickets. Paid once, the moment
    /// the level is reached (levels never go down, so no save field is needed).
    /// </summary>
    public static class LevelReward
    {
        public static LevelRewardPay For(BalanceValues b, int level)
        {
            if (b == null || level < 2 || b.LEVEL_REWARD_EVERY < 1 || level % b.LEVEL_REWARD_EVERY != 0) return default;
            bool big = b.LEVEL_REWARD_BIG_EVERY > 0 && level % b.LEVEL_REWARD_BIG_EVERY == 0;
            return new LevelRewardPay(
                b.LEVEL_REWARD_GEM + (big ? b.LEVEL_REWARD_BIG_GEM : 0),
                b.LEVEL_REWARD_GEAR_TICKETS,
                big ? b.LEVEL_REWARD_BIG_TICKETS : 0,
                big ? b.LEVEL_REWARD_BIG_TICKETS : 0,
                b.LEVEL_REWARD_STONES);
        }

        /// <summary>Pays the reward of <paramref name="level"/> into the save.</summary>
        public static LevelRewardPay Grant(BalanceValues b, SaveDataV2 data, int level)
        {
            LevelRewardPay pay = For(b, level);
            if (data == null || !pay.Any) return pay;
            data.gem += pay.Gems;
            data.breakStones += pay.Stones;
            ShopService.AddTickets(data, SummonKind.Gear, pay.GearTickets);
            ShopService.AddTickets(data, SummonKind.Skill, pay.SkillTickets);
            ShopService.AddTickets(data, SummonKind.Pet, pay.PetTickets);
            return pay;
        }
    }
}
