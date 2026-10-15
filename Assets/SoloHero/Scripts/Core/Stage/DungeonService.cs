using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Save;

namespace SoloHero.Core.Stage
{
    /// <summary>
    /// D-100 daily dungeon entries: DUNGEON_DAILY_TICKETS per dungeon per device-local day (like the ad counters).
    /// An entry is spent only when the runner actually starts the run, and that spend is saved at once. D-143: they
    /// open once stage DUNGEON_UNLOCK_STAGE is cleared, and every finished run pays DUNGEON_STONES breakthrough stones.
    /// D-161 (L-12) sweep: once a dungeon has a record, an entry can be spent without fighting for the record's kills
    /// at today's reward per kill, plus the stones.
    /// </summary>
    public sealed class DungeonService
    {
        private readonly BalanceValues _balance;
        private readonly SaveDataV2 _data;
        private readonly IClock _clock;
        private readonly ISaveRequester _save;

        public DungeonService(BalanceValues balance, SaveDataV2 data, IClock clock, ISaveRequester save = null)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _save = save;
        }

        public int DailyTickets => _balance.DUNGEON_DAILY_TICKETS;

        public static bool IsUnlocked(BalanceValues balance, SaveDataV2 data) =>
            balance != null && data != null && data.highestStage >= balance.DUNGEON_UNLOCK_STAGE;

        public bool Unlocked => IsUnlocked(_balance, _data);

        /// <summary>D-174: whether this dungeon is open (the material dungeon opens later than the others).</summary>
        public bool IsOpen(DungeonKind kind) => IsOpen(_balance, _data, kind);

        public static bool IsOpen(BalanceValues balance, SaveDataV2 data, DungeonKind kind) =>
            IsUnlocked(balance, data) && (kind != DungeonKind.Material || data.highestStage >= balance.MATERIAL_UNLOCK_STAGE);

        /// <summary>D-174: the stage that opens a dungeon.</summary>
        public int UnlockStage(DungeonKind kind) => kind == DungeonKind.Material ? _balance.MATERIAL_UNLOCK_STAGE : _balance.DUNGEON_UNLOCK_STAGE;

        public int Remaining(DungeonKind kind)
        {
            RollDay();
            int left = DailyTicketsOf(kind) - Used(kind);
            return left < 0 ? 0 : left;
        }

        /// <summary>D-165: entries a day for each dungeon (the score dungeon has its own count).</summary>
        public int DailyTicketsOf(DungeonKind kind) =>
            kind == DungeonKind.Score ? _balance.SCORE_DAILY_TICKETS : kind == DungeonKind.Material ? _balance.MATERIAL_DAILY_TICKETS : _balance.DUNGEON_DAILY_TICKETS;

        /// <summary>D-174: awakening stones a material run of <paramref name="kills"/> kills pays.</summary>
        public static int MaterialStones(BalanceValues balance, int kills) =>
            balance.MATERIAL_KILLS_PER_STONE <= 0 || kills <= 0 ? 0 : kills / balance.MATERIAL_KILLS_PER_STONE;

        /// <summary>D-165: the tier of a score run that dealt <paramref name="ratio"/> chapter-boss HP of damage.</summary>
        public static int ScoreTier(BalanceValues balance, double ratio)
        {
            if (balance.SCORE_TIER_BASE <= 0d || !(ratio >= balance.SCORE_TIER_BASE)) return 0;
            int tier = 1 + (int)Math.Floor(Math.Log(ratio / balance.SCORE_TIER_BASE, 2d) + 1e-9);
            return Math.Max(1, Math.Min(balance.SCORE_TIER_MAX, tier));
        }

        /// <summary>D-165: breakthrough stones a score run of <paramref name="tier"/> pays.</summary>
        public static int ScoreStones(BalanceValues balance, int tier) =>
            tier <= 0 ? balance.DUNGEON_STONES : balance.SCORE_STONES_BASE + balance.SCORE_STONES_PER_TIER * (tier - 1);

        /// <summary>D-165: the damage ratio where <paramref name="tier"/> starts.</summary>
        public static double ScoreTierStart(BalanceValues balance, int tier) =>
            tier <= 0 ? 0d : balance.SCORE_TIER_BASE * Math.Pow(2d, tier - 1);

        /// <summary>What one kill pays right now (gold or EXP) at the farming stage, for the entry card.</summary>
        public double RewardPerKill(DungeonKind kind)
        {
            int g = _data.farmingStage < 1 ? 1 : _data.farmingStage;
            return kind == DungeonKind.Gold ? Formulas.DungeonGoldPerKill(_balance, g) : Formulas.DungeonExpPerKill(_balance, g);
        }

        /// <summary>D-161: the most kills of one run of <paramref name="kind"/> (0 until the first run ends).</summary>
        public int Best(DungeonKind kind) => BestOf(_data, kind);

        public static int BestOf(SaveDataV2 data, DungeonKind kind)
        {
            switch (kind)
            {
                case DungeonKind.Gold: return data.dungeonGoldBest;
                case DungeonKind.Exp: return data.dungeonExpBest;
                case DungeonKind.Score: return data.dungeonScoreBest;
                case DungeonKind.Material: return data.dungeonMaterialBest;
                default: return 0;
            }
        }

        /// <summary>
        /// D-161: keeps the run's result when it beats the record - kills for gold and EXP, the tier for the D-165 score
        /// dungeon. Called by the runner when a run ends.
        /// </summary>
        public static void RecordRun(SaveDataV2 data, DungeonKind kind, int result)
        {
            if (data == null) return;
            if (kind == DungeonKind.Gold && result > data.dungeonGoldBest) data.dungeonGoldBest = result;
            else if (kind == DungeonKind.Exp && result > data.dungeonExpBest) data.dungeonExpBest = result;
            else if (kind == DungeonKind.Score && result > data.dungeonScoreBest) data.dungeonScoreBest = result;
            else if (kind == DungeonKind.Material && result > data.dungeonMaterialBest) data.dungeonMaterialBest = result;
        }

        /// <summary>
        /// D-161: what a sweep pays now - the record's kills at the current reward per kill; for the score dungeon the
        /// best tier's stones (paid as stones, not here).
        /// </summary>
        public double SweepReward(DungeonKind kind) =>
            kind == DungeonKind.Score ? ScoreStones(_balance, Best(kind))
            : kind == DungeonKind.Material ? MaterialStones(_balance, Best(kind))
            : Best(kind) * RewardPerKill(kind);

        public bool CanSweep(DungeonKind kind) =>
            (kind == DungeonKind.Gold || kind == DungeonKind.Exp || kind == DungeonKind.Score || kind == DungeonKind.Material)
            && IsOpen(kind) && Best(kind) > 0 && Remaining(kind) > 0;

        /// <summary>
        /// D-161: spends an entry and pays the record at once (gold, or EXP through the hero level), plus the stones.
        /// No fight: refused while a dungeon or the tower is running only by the caller's busy check.
        /// </summary>
        public Result TrySweep(DungeonKind kind, out double earned)
        {
            earned = 0d;
            if (kind != DungeonKind.Gold && kind != DungeonKind.Exp && kind != DungeonKind.Score && kind != DungeonKind.Material)
                return Result.Fail(FailReason.Locked);
            if (!IsOpen(kind) || Best(kind) <= 0) return Result.Fail(FailReason.Locked);
            if (Remaining(kind) <= 0) return Result.Fail(FailReason.DailyLimit);
            earned = SweepReward(kind);
            if (kind == DungeonKind.Material)
            {
                // D-174: the record's awakening stones plus the run's breakthrough stones.
                _data.awakenStones += (int)earned;
                _data.breakStones += _balance.DUNGEON_STONES;
                _data.dungeonMaterialUsed++;
                _save?.RequestSave();
                return Result.Success;
            }

            if (kind == DungeonKind.Score)
            {
                // D-165: the best tier's stones instead of the flat dungeon stones.
                _data.breakStones += (int)earned;
                _data.dungeonScoreUsed++;
                _save?.RequestSave();
                return Result.Success;
            }

            if (kind == DungeonKind.Gold)
            {
                _data.gold += earned;
                _data.dungeonGoldUsed++;
            }
            else
            {
                new HeroLevelService(_data, _balance).AddExp(earned);
                _data.dungeonExpUsed++;
            }

            _data.breakStones += _balance.DUNGEON_STONES;
            _save?.RequestSave();
            return Result.Success;
        }

        public Result TryEnter(DungeonKind kind, StageRunner runner)
        {
            if (kind == DungeonKind.None || runner == null || !IsOpen(kind)) return Result.Fail(FailReason.Locked);
            if (Remaining(kind) <= 0) return Result.Fail(FailReason.DailyLimit);
            if (kind == DungeonKind.Tower) return Result.Fail(FailReason.Locked);
            if (!runner.StartDungeon(kind)) return Result.Fail(FailReason.Busy);
            if (kind == DungeonKind.Gold) _data.dungeonGoldUsed++;
            else if (kind == DungeonKind.Score) _data.dungeonScoreUsed++;
            else if (kind == DungeonKind.Material) _data.dungeonMaterialUsed++;
            else _data.dungeonExpUsed++;
            _save?.RequestSave();
            return Result.Success;
        }

        private int Used(DungeonKind kind) =>
            kind == DungeonKind.Gold ? _data.dungeonGoldUsed : kind == DungeonKind.Score ? _data.dungeonScoreUsed
            : kind == DungeonKind.Material ? _data.dungeonMaterialUsed : _data.dungeonExpUsed;

        private void RollDay()
        {
            string today = DayKey.Today(_clock);
            if (!DayKey.IsNewDay(_data.dungeonDate, today)) return;
            _data.dungeonDate = today;
            _data.dungeonGoldUsed = 0;
            _data.dungeonExpUsed = 0;
            _data.dungeonScoreUsed = 0;
            _data.dungeonMaterialUsed = 0;
        }
    }
}
