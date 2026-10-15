using SoloHero.Core.Economy;

namespace SoloHero.Core.Pass
{
    /// <summary>
    /// D-163 (L-17) free season pass: the reward of every pass level, the same table each season (a later season can
    /// swap it as data). Level 10, 20 and 30 are the big ones. Gold is in clear-gold multiples of the farming stage.
    /// </summary>
    public static class PassCatalog
    {
        public static readonly RewardBundle[] Free =
        {
            new RewardBundle { GoldStages = 10 },
            new RewardBundle { GearTickets = 5 },
            new RewardBundle { Gems = 50 },
            new RewardBundle { Stones = 10 },
            new RewardBundle { GearTickets = 10, SkillTickets = 1 },
            new RewardBundle { GoldStages = 15 },
            new RewardBundle { PetTickets = 2 },
            new RewardBundle { Gems = 50 },
            new RewardBundle { Stones = 15 },
            new RewardBundle { Gems = 100, GearTickets = 10, SkillTickets = 2, PetTickets = 2 },

            new RewardBundle { GoldStages = 20 },
            new RewardBundle { GearTickets = 8 },
            new RewardBundle { Gems = 80 },
            new RewardBundle { Stones = 15 },
            new RewardBundle { GearTickets = 15, SkillTickets = 2 },
            new RewardBundle { GoldStages = 25 },
            new RewardBundle { PetTickets = 3 },
            new RewardBundle { Gems = 80 },
            new RewardBundle { Stones = 20 },
            new RewardBundle { Gems = 200, GearTickets = 20, SkillTickets = 3, PetTickets = 3 },

            new RewardBundle { GoldStages = 30 },
            new RewardBundle { GearTickets = 10 },
            new RewardBundle { Gems = 100 },
            new RewardBundle { Stones = 25 },
            new RewardBundle { GearTickets = 20, SkillTickets = 3 },
            new RewardBundle { GoldStages = 40 },
            new RewardBundle { PetTickets = 4 },
            new RewardBundle { Gems = 100 },
            new RewardBundle { Stones = 30 },
            new RewardBundle { Gems = 300, GearTickets = 30, SkillTickets = 5, PetTickets = 5, Stones = 50 },
        };

        public static int Levels => Free.Length;

        /// <summary>The reward of pass level <paramref name="level"/> (1-based); null outside the table.</summary>
        public static RewardBundle At(int level) => level >= 1 && level <= Free.Length ? Free[level - 1] : null;

        /// <summary>The big levels (every tenth) get the highlighted cell.</summary>
        public static bool IsBig(int level) => level > 0 && level % 10 == 0;
    }
}
