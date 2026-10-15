using System;
using SoloHero.Core.Config;
using SoloHero.Core.Gacha;
using SoloHero.Core.Save;

namespace SoloHero.Core.Economy
{
    /// <summary>
    /// D-156: one reward of gems, gold, summon tickets, breakthrough stones and event tokens. Gold is given in clear-gold
    /// multiples of the farming stage, so the same table stays worth something at any point of the curve.
    /// </summary>
    public sealed class RewardBundle
    {
        public int Gems { get; init; }

        /// <summary>Gold = the farming stage's clear gold x this.</summary>
        public double GoldStages { get; init; }
        public int GearTickets { get; init; }
        public int SkillTickets { get; init; }
        public int PetTickets { get; init; }
        public int Stones { get; init; }
        public int Tokens { get; init; }

        /// <summary>D-174: skill awakening stones.</summary>
        public int AwakenStones { get; init; }

        public double GoldFor(BalanceValues b, SaveDataV2 data)
        {
            if (b == null || data == null || GoldStages <= 0d) return 0d;
            int stage = data.farmingStage < 1 ? 1 : data.farmingStage;
            return Math.Floor(Formulas.StageGold(b, stage) * GoldStages);
        }

        public void PayInto(BalanceValues b, SaveDataV2 data)
        {
            if (data == null) return;
            data.gem += Gems;
            data.gold += GoldFor(b, data);
            ShopService.AddTickets(data, SummonKind.Gear, GearTickets);
            ShopService.AddTickets(data, SummonKind.Skill, SkillTickets);
            ShopService.AddTickets(data, SummonKind.Pet, PetTickets);
            data.breakStones += Stones;
            data.eventTokens += Tokens;
            data.awakenStones += AwakenStones;
        }
    }
}
