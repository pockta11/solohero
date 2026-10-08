using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Gacha;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Stage
{
    /// <summary>What clearing one tower floor paid (already added to the save).</summary>
    public readonly struct TowerReward
    {
        public readonly int Floor;
        public readonly double Gems;
        public readonly int GearTickets;
        public readonly int SkillTickets;
        public readonly int PetTickets;

        /// <summary>D-143 breakthrough stones.</summary>
        public readonly int Stones;

        public TowerReward(int floor, double gems, int gearTickets, int skillTickets, int petTickets, int stones = 0)
        {
            Floor = floor;
            Gems = gems;
            GearTickets = gearTickets;
            SkillTickets = skillTickets;
            PetTickets = petTickets;
            Stones = stones;
        }
    }

    /// <summary>
    /// D-130 infinite tower (genre: a second ladder beside the stages). Floor f is the boss of stage
    /// TOWER_G_OFFSET + f against the boss timer; each run starts on the first floor not yet cleared and climbs while
    /// the hero wins. Opens with the pet summon after the first boss. Unlimited tries - the power curve is the gate.
    /// </summary>
    public sealed class TowerService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly ISaveRequester _save;

        public TowerService(BalanceValues balance, SaveDataV2 data, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save;
        }

        public int NextFloor => _data.towerFloor + 1;

        public static bool IsUnlocked(BalanceValues balance, SaveDataV2 data) =>
            balance != null && data != null && data.highestStage >= balance.TOWER_UNLOCK_STAGE;

        public bool Unlocked => IsUnlocked(_balance, _data);

        public static int StageOf(BalanceValues balance, int floor) => balance.TOWER_G_OFFSET + Math.Max(1, floor);

        /// <summary>
        /// The floor's boss is a boss fight at its stage with TOWER_BOSS_HP_MULT x the HP (D-144): the recommended combat
        /// power of a boss there, times that (the HP wall is beaten with damage, and combat power is mostly damage).
        /// </summary>
        public static double RecommendedCp(BalanceValues balance, int floor) =>
            Math.Floor(CombatPower.Recommended(balance, StageOf(balance, floor), true) * Math.Max(1d, balance.TOWER_BOSS_HP_MULT));

        public static double Gems(BalanceValues balance, int floor) =>
            Math.Floor(balance.TOWER_GEM_BASE + balance.TOWER_GEM_PER_FLOOR * Math.Max(1, floor));

        /// <summary>The reward of <paramref name="floor"/> without granting it (for the entry popup).</summary>
        public static TowerReward Preview(BalanceValues balance, int floor)
        {
            bool tickets = balance.TOWER_TICKET_EVERY > 0 && floor % balance.TOWER_TICKET_EVERY == 0;
            bool rare = balance.TOWER_RARE_EVERY > 0 && floor % balance.TOWER_RARE_EVERY == 0;
            return new TowerReward(floor, Gems(balance, floor), tickets ? balance.TOWER_GEAR_TICKETS : 0,
                rare ? balance.TOWER_SKILL_TICKETS : 0, rare ? balance.TOWER_PET_TICKETS : 0,
                balance.TOWER_STONES + (rare ? balance.TOWER_STONES_RARE : 0));
        }

        /// <summary>Records the cleared floor and pays it (gems and tickets). Called by the runner.</summary>
        public static TowerReward Grant(BalanceValues balance, SaveDataV2 data, int floor)
        {
            TowerReward reward = Preview(balance, floor);
            if (floor > data.towerFloor) data.towerFloor = floor;
            data.gem += reward.Gems;
            data.breakStones += reward.Stones;
            ShopService.AddTickets(data, SummonKind.Gear, reward.GearTickets);
            ShopService.AddTickets(data, SummonKind.Skill, reward.SkillTickets);
            ShopService.AddTickets(data, SummonKind.Pet, reward.PetTickets);
            return reward;
        }

        public Result TryEnter(StageRunner runner)
        {
            if (runner == null || !Unlocked) return Result.Fail(FailReason.Locked);
            if (!runner.StartTower(NextFloor)) return Result.Fail(FailReason.Busy);
            _save?.RequestSave();
            return Result.Success;
        }
    }
}
