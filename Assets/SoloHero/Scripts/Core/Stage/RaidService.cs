using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Stage
{
    /// <summary>D-175: one raid shop item - medals for a reward, bought up to Limit times a week.</summary>
    public sealed class RaidShopItem
    {
        public string Id { get; init; } = "";
        public int Cost { get; init; }
        public int Limit { get; init; } = 1;
        public RewardBundle Reward { get; init; } = new RewardBundle();

        public string NameKey => "raid.shop." + Id;
    }

    /// <summary>What one raid run paid.</summary>
    public readonly struct RaidResult
    {
        public readonly int Medals;
        public readonly int FirstClearGems;
        public readonly int Permille;
        public readonly bool Killed;

        public RaidResult(int medals, int firstClearGems, int permille, bool killed)
        {
            Medals = medals;
            FirstClearGems = firstClearGems;
            Permille = permille;
            Killed = killed;
        }
    }

    /// <summary>
    /// D-175 (L-14) the weekly boss raid (Soul Strike's boss raid, no ranking): RAID_WEEKLY_TICKETS entries a week (Monday
    /// 0:00 local, DayKey weeks), RAID_LEVELS levels - the next opens when one is beaten - each a stronger boss with
    /// patterns that fights back. A run pays medals by the share of the boss's HP dealt (more on a kill); a level's first
    /// kill pays gems. Medals buy from the weekly raid shop. A beaten level can be swept for its kill reward.
    /// </summary>
    public sealed class RaidService
    {
        public static readonly RaidShopItem[] Shop =
        {
            new RaidShopItem { Id = "awaken10", Cost = 40, Limit = 5, Reward = new RewardBundle { AwakenStones = 10 } },
            new RaidShopItem { Id = "stone50", Cost = 40, Limit = 3, Reward = new RewardBundle { Stones = 50 } },
            new RaidShopItem { Id = "gear10", Cost = 50, Limit = 3, Reward = new RewardBundle { GearTickets = 10 } },
            new RaidShopItem { Id = "skill5", Cost = 50, Limit = 2, Reward = new RewardBundle { SkillTickets = 5 } },
            new RaidShopItem { Id = "pet5", Cost = 50, Limit = 2, Reward = new RewardBundle { PetTickets = 5 } },
            new RaidShopItem { Id = "gem300", Cost = 80, Limit = 1, Reward = new RewardBundle { Gems = 300 } },
        };

        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;

        public RaidService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        /// <summary>Raised after a purchase or a sweep.</summary>
        public event Action Changed;

        public static bool IsUnlocked(BalanceValues balance, SaveDataV2 data) =>
            balance != null && data != null && data.highestStage >= balance.RAID_UNLOCK_STAGE;

        public bool Unlocked => IsUnlocked(_balance, _data);

        /// <summary>The boss stage of raid level <paramref name="level"/> (1-based).</summary>
        public static int StageOf(BalanceValues balance, int level) =>
            balance.RAID_BASE_STAGE + balance.RAID_LEVEL_STEP * (Math.Max(1, level) - 1);

        /// <summary>Recommended combat power for level <paramref name="level"/>: its stage boss's, x RAID_REC_MULT.</summary>
        public static double RecommendedCp(BalanceValues balance, int level) =>
            CombatPower.Recommended(balance, StageOf(balance, level), true) * balance.RAID_REC_MULT;

        /// <summary>The highest level that can be entered: one past the best kill, up to RAID_LEVELS.</summary>
        public int MaxLevel => Math.Min(_balance.RAID_LEVELS, _data.raidCleared + 1);

        public int Cleared => _data.raidCleared;

        public int Medals => _data.raidMedals;

        public int Remaining
        {
            get
            {
                RollWeek();
                return Math.Max(0, _balance.RAID_WEEKLY_TICKETS - _data.raidUsed);
            }
        }

        /// <summary>Best share of a level's boss HP dealt, in permille (0 before a run).</summary>
        public int BestPermille(int level)
        {
            int i = level - 1;
            return i >= 0 && i < _data.raidBest.Count ? _data.raidBest[i] : 0;
        }

        /// <summary>Medals for dealing <paramref name="permille"/> of level <paramref name="level"/>'s HP (and killing it).</summary>
        public static int MedalsFor(BalanceValues balance, int level, int permille, bool killed)
        {
            int share = Math.Max(0, Math.Min(1000, permille));
            int medals = (int)Math.Floor(balance.RAID_MEDALS_PER_LEVEL * level * share / 1000d);
            return medals + (killed ? balance.RAID_CLEAR_MEDALS * level : 0);
        }

        /// <summary>
        /// Pays a finished run (called by the runner): medals, the best share, and on a first kill the next level and its
        /// gems.
        /// </summary>
        public static RaidResult Settle(BalanceValues balance, SaveDataV2 data, int level, double share, bool killed)
        {
            int permille = killed ? 1000 : (int)Math.Floor(Math.Max(0d, Math.Min(1d, share)) * 1000d);
            int medals = MedalsFor(balance, level, permille, killed);
            data.raidMedals += medals;
            while (data.raidBest.Count < level) data.raidBest.Add(0);
            if (permille > data.raidBest[level - 1]) data.raidBest[level - 1] = permille;
            int gems = 0;
            if (killed && level > data.raidCleared)
            {
                data.raidCleared = level;
                gems = balance.RAID_FIRST_GEM * level;
                data.gem += gems;
            }

            return new RaidResult(medals, gems, permille, killed);
        }

        public Result TryEnter(int level, StageRunner runner)
        {
            if (runner == null || !Unlocked || level < 1 || level > MaxLevel) return Result.Fail(FailReason.Locked);
            if (Remaining <= 0) return Result.Fail(FailReason.DailyLimit);
            if (!runner.StartRaid(level)) return Result.Fail(FailReason.Busy);
            _data.raidUsed++;
            _save?.RequestSave();
            return Result.Success;
        }

        public bool CanSweep(int level) => Unlocked && level >= 1 && level <= _data.raidCleared && Remaining > 0;

        /// <summary>A beaten level's kill reward at once, for an entry.</summary>
        public Result TrySweep(int level, out int medals)
        {
            medals = 0;
            if (!Unlocked || level < 1 || level > _data.raidCleared) return Result.Fail(FailReason.Locked);
            if (Remaining <= 0) return Result.Fail(FailReason.DailyLimit);
            medals = MedalsFor(_balance, level, 1000, true);
            _data.raidMedals += medals;
            _data.raidUsed++;
            Done();
            return Result.Success;
        }

        public int Bought(int index)
        {
            RollWeek();
            return index >= 0 && index < _data.raidShopBought.Count ? _data.raidShopBought[index] : 0;
        }

        public bool CanBuy(int index) =>
            index >= 0 && index < Shop.Length && Bought(index) < Shop[index].Limit && _data.raidMedals >= Shop[index].Cost;

        public Result TryBuy(int index)
        {
            if (index < 0 || index >= Shop.Length) return Result.Fail(FailReason.Locked);
            RaidShopItem item = Shop[index];
            if (Bought(index) >= item.Limit) return Result.Fail(FailReason.MaxLevel);
            if (_data.raidMedals < item.Cost) return Result.Fail(FailReason.NotEnoughMedals);
            _data.raidMedals -= item.Cost;
            while (_data.raidShopBought.Count <= index) _data.raidShopBought.Add(0);
            _data.raidShopBought[index]++;
            item.Reward.PayInto(_balance, _data);
            Done();
            return Result.Success;
        }

        /// <summary>Seconds until the weekly reset (next Monday 0:00 local).</summary>
        public long SecondsToReset
        {
            get
            {
                DateTime now = _clock.LocalNow;
                DateTime monday = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7)).AddDays(7);
                return Math.Max(0L, (long)(monday - now).TotalSeconds);
            }
        }

        private void Done()
        {
            Changed?.Invoke();
            _save?.RequestSave();
        }

        /// <summary>A new week clears the entries and the shop limits (medals carry over).</summary>
        private void RollWeek()
        {
            string week = DayKey.ThisWeek(_clock);
            if (!DayKey.IsNewDay(_data.raidWeek, week)) return;
            _data.raidWeek = week;
            _data.raidUsed = 0;
            _data.raidShopBought.Clear();
        }
    }
}
