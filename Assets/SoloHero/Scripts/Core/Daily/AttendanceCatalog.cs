using SoloHero.Core.Economy;

namespace SoloHero.Core.Daily
{
    /// <summary>A reward for reaching a total of <see cref="Days"/> attendance days.</summary>
    public sealed class AttendanceMilestone
    {
        public int Days { get; init; }
        public RewardBundle Reward { get; init; }
    }

    /// <summary>
    /// D-166 (L-11) monthly attendance: a 28-day board (four weeks of seven) whose week ends (days 7, 14, 21, 28) pay
    /// more, and rewards for the total days attended (30, 60, 100, 200, 365). A missed day never resets anything.
    /// The board's gems and gold are BalanceValues (ATTENDANCE_*); the tickets on the week ends and the milestones are
    /// here.
    /// </summary>
    public static class AttendanceCatalog
    {
        public const int BoardDays = 28;
        public const int WeekDays = 7;

        /// <summary>Tickets of a week-end day on top of its gems; null for the other days.</summary>
        public static RewardBundle WeekEndExtra(int boardDay)
        {
            switch (boardDay)
            {
                case 14: return new RewardBundle { GearTickets = 10 };
                case 21: return new RewardBundle { SkillTickets = 2, PetTickets = 2 };
                case 28: return new RewardBundle { GearTickets = 20, SkillTickets = 5, PetTickets = 5 };
                default: return null;
            }
        }

        public static readonly AttendanceMilestone[] Milestones =
        {
            new AttendanceMilestone { Days = 30, Reward = new RewardBundle { Gems = 300, GearTickets = 10 } },
            new AttendanceMilestone { Days = 60, Reward = new RewardBundle { Gems = 500, GearTickets = 20 } },
            new AttendanceMilestone { Days = 100, Reward = new RewardBundle { Gems = 800, GearTickets = 30, SkillTickets = 5, PetTickets = 5 } },
            new AttendanceMilestone { Days = 200, Reward = new RewardBundle { Gems = 1500, GearTickets = 50, SkillTickets = 10, PetTickets = 10 } },
            new AttendanceMilestone { Days = 365, Reward = new RewardBundle { Gems = 3000, GearTickets = 100, SkillTickets = 20, PetTickets = 20 } },
        };

        /// <summary>The milestone reached exactly at <paramref name="totalDays"/>, or null.</summary>
        public static AttendanceMilestone At(int totalDays)
        {
            foreach (AttendanceMilestone m in Milestones)
                if (m.Days == totalDays) return m;
            return null;
        }

        /// <summary>The first milestone above <paramref name="totalDays"/>, or null after the last.</summary>
        public static AttendanceMilestone Next(int totalDays)
        {
            foreach (AttendanceMilestone m in Milestones)
                if (m.Days > totalDays) return m;
            return null;
        }
    }
}
