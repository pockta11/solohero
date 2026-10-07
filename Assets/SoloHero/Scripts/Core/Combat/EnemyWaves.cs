using System;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    /// <summary>
    /// D-110 wave make-up: which role stands at each place of a wave (front to back) and what the role does to the
    /// stage's enemy stats. Chapter 1 teaches the soldier and the tank; from chapter 2 rushers lead and shooters
    /// stay at the back. Role HP is normalised per wave table, so a wave always holds the HP of eight soldiers and
    /// the mix changes how a stage plays, not how long it takes.
    /// </summary>
    public static class EnemyWaves
    {
        private const EnemyRole M = EnemyRole.Melee;
        private const EnemyRole F = EnemyRole.Fast;
        private const EnemyRole T = EnemyRole.Tank;
        private const EnemyRole R = EnemyRole.Ranged;

        private static readonly EnemyRole[] FirstStages = { M, M, M, M, M, M, M, M };
        private static readonly EnemyRole[] Chapter1 = { M, M, M, T, M, M, M, T };
        private static readonly EnemyRole[] Chapter2 = { F, M, M, T, M, M, R, M };
        private static readonly EnemyRole[] Later = { F, F, M, M, T, M, R, R };

        /// <summary>Stages 1-1 and 1-2 are soldiers only, so the first minute stays simple.</summary>
        public const int PlainStages = 2;

        public static EnemyRole RoleFor(int g, int waveSlot, int stagesPerChapter)
        {
            if (waveSlot < 0) waveSlot = 0;
            EnemyRole[] table = TableFor(g, stagesPerChapter);
            return table[waveSlot % table.Length];
        }

        /// <summary>HP multiplier of a role at stage g, normalised so the stage's wave table averages 1.</summary>
        public static double StageHpMult(BalanceValues b, int g, EnemyRole role)
        {
            EnemyRole[] table = TableFor(g, b.STAGES_PER_CHAPTER);
            double sum = 0d;
            for (int i = 0; i < table.Length; i++) sum += HpMult(b, table[i]);
            double mean = sum / table.Length;
            return mean > 0d ? HpMult(b, role) / mean : 1d;
        }

        private static EnemyRole[] TableFor(int g, int stagesPerChapter)
        {
            if (g <= PlainStages) return FirstStages;
            int chapter = (g - 1) / Math.Max(1, stagesPerChapter) + 1;
            return chapter <= 1 ? Chapter1 : chapter == 2 ? Chapter2 : Later;
        }

        public static double HpMult(BalanceValues b, EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Fast: return b.ENEMY_FAST_HP_MULT;
                case EnemyRole.Tank: return b.ENEMY_TANK_HP_MULT;
                case EnemyRole.Ranged: return b.ENEMY_RANGED_HP_MULT;
                default: return 1d;
            }
        }

        public static double AtkMult(BalanceValues b, EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Fast: return b.ENEMY_FAST_ATK_MULT;
                case EnemyRole.Tank: return b.ENEMY_TANK_ATK_MULT;
                case EnemyRole.Ranged: return b.ENEMY_RANGED_ATK_MULT;
                default: return 1d;
            }
        }

        public static double SpeedMult(BalanceValues b, EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Fast: return b.ENEMY_FAST_SPEED_MULT;
                case EnemyRole.Tank: return b.ENEMY_TANK_SPEED_MULT;
                case EnemyRole.Ranged: return b.ENEMY_RANGED_SPEED_MULT;
                default: return 1d;
            }
        }
    }
}
