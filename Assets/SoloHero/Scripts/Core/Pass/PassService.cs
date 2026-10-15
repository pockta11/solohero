using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Daily;
using SoloHero.Core.Economy;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Pass
{
    /// <summary>
    /// D-163 (L-17) free season pass. Seasons are PASS_SEASON_DAYS long, counted from PASS_EPOCH_UTC on the trusted
    /// clock, so moving the device clock does not end a season. Pass XP comes from claimed daily (PASS_XP_DAILY) and
    /// weekly (PASS_XP_WEEKLY) missions and from kills (one XP per PASS_KILLS_PER_XP). Every PASS_XP_PER_LEVEL XP is a
    /// level of <see cref="PassCatalog"/>; reached levels are claimed together. When a season ends its reached but
    /// unclaimed rewards are paid at the rollover, then XP and claims start over.
    /// </summary>
    public sealed class PassService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;

        public PassService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        /// <summary>Raised when XP moves, a level is claimed or a season rolls over.</summary>
        public event Action Changed;

        private long SeasonSeconds => Math.Max(1L, (long)_balance.PASS_SEASON_DAYS * 86400L);

        /// <summary>The season number at the clock's time (0 = the one starting at PASS_EPOCH_UTC).</summary>
        public int Season => SeasonAt(_balance, _clock.UtcNowSeconds);

        public static int SeasonAt(BalanceValues balance, long utcSeconds)
        {
            long length = Math.Max(1L, (long)balance.PASS_SEASON_DAYS * 86400L);
            long since = utcSeconds - balance.PASS_EPOCH_UTC;
            return since <= 0 ? 0 : (int)(since / length);
        }

        public long SecondsLeft
        {
            get
            {
                long end = _balance.PASS_EPOCH_UTC + (Season + 1L) * SeasonSeconds;
                return Math.Max(0L, end - _clock.UtcNowSeconds);
            }
        }

        public int MaxXp => PassCatalog.Levels * Math.Max(1, _balance.PASS_XP_PER_LEVEL);

        public int Xp
        {
            get
            {
                Sync();
                return _data.passXp;
            }
        }

        /// <summary>Levels reached this season (0..Levels).</summary>
        public int Level => Math.Min(PassCatalog.Levels, Xp / Math.Max(1, _balance.PASS_XP_PER_LEVEL));

        /// <summary>XP inside the level being filled (full once the last level is reached).</summary>
        public int XpIntoLevel => Level >= PassCatalog.Levels ? _balance.PASS_XP_PER_LEVEL : Xp % Math.Max(1, _balance.PASS_XP_PER_LEVEL);

        public int Claimed
        {
            get
            {
                Sync();
                return Math.Min(_data.passClaimed, PassCatalog.Levels);
            }
        }

        public int ClaimableCount => Math.Max(0, Level - Claimed);

        public bool IsClaimed(int level) => level >= 1 && level <= Claimed;

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Sync();
            int before = _data.passXp;
            _data.passXp = Math.Min(MaxXp, before + amount);
            if (_data.passXp != before) Changed?.Invoke();
        }

        /// <summary>Counts recorded actions (DailyService.Recorded): kills turn into XP, the rest is carried in kills.</summary>
        public void Record(MissionKind kind, int amount)
        {
            if (kind != MissionKind.Kill || amount <= 0) return;
            Sync();
            int per = Math.Max(1, _balance.PASS_KILLS_PER_XP);
            long carry = (long)_data.passKillCarry + amount;
            int xp = (int)Math.Min(int.MaxValue, carry / per);
            _data.passKillCarry = (int)(carry % per);
            AddXp(xp);
        }

        /// <summary>Daily and weekly mission claims (DailyService.MissionClaimed).</summary>
        public void OnMissionClaimed(bool weekly) => AddXp(weekly ? _balance.PASS_XP_WEEKLY : _balance.PASS_XP_DAILY);

        /// <summary>Pays every reached level not yet claimed; Locked when there is none. <paramref name="paid"/> sums them.</summary>
        public Result TryClaimAll(out PassPayout paid)
        {
            paid = default;
            Sync();
            int level = Level;
            if (level <= _data.passClaimed) return Result.Fail(FailReason.Locked);
            paid = PayRange(_data.passClaimed + 1, level);
            _data.passClaimed = level;
            Changed?.Invoke();
            _save?.RequestSave();
            return Result.Success;
        }

        private PassPayout PayRange(int from, int to)
        {
            var total = new PassPayout();
            for (int level = from; level <= to; level++)
            {
                RewardBundle reward = PassCatalog.At(level);
                if (reward == null) continue;
                double gold = reward.GoldFor(_balance, _data);
                reward.PayInto(_balance, _data);
                total.Add(reward, gold);
            }

            return total;
        }

        /// <summary>A new season pays the old one's reached, unclaimed levels and starts over.</summary>
        private void Sync()
        {
            int season = Season;
            if (_data.passSeason == season) return;
            int reached = Math.Min(PassCatalog.Levels, _data.passXp / Math.Max(1, _balance.PASS_XP_PER_LEVEL));
            if (reached > _data.passClaimed) PayRange(_data.passClaimed + 1, reached);
            _data.passSeason = season;
            _data.passXp = 0;
            _data.passClaimed = 0;
            _data.passKillCarry = 0;
            Changed?.Invoke();
        }
    }

    /// <summary>What one claim (or a rollover) paid, summed over the levels.</summary>
    public struct PassPayout
    {
        public int Gems;
        public double Gold;
        public int GearTickets;
        public int SkillTickets;
        public int PetTickets;
        public int Stones;

        public void Add(RewardBundle reward, double gold)
        {
            Gems += reward.Gems;
            Gold += gold;
            GearTickets += reward.GearTickets;
            SkillTickets += reward.SkillTickets;
            PetTickets += reward.PetTickets;
            Stones += reward.Stones;
        }
    }
}
