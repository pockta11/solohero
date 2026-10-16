using System.Globalization;
using SoloHero.Core;
using SoloHero.Core.Config;
using SoloHero.Core.Growth;
using SoloHero.Core.Skills;

namespace SoloHero.Game.UI.Common
{
    /// <summary>
    /// The numbers a skill description's placeholders take ({0} damage %, {1} waves, {2}/{3} burn, {4} stun, {5}/{6}
    /// buff, {7} heal, {8}/{9} shield, {10} HP threshold, {11} cooldown, {12}/{13} mark), scaled to a level and stars.
    /// Shared by the skill panel and the D-179 job preview.
    /// </summary>
    public static class SkillText
    {
        public const int ArgCount = 14;

        public static object[] DescArgs(BalanceValues balance, SkillDef def, int level, int stars, object[] into)
        {
            if (into == null || into.Length < ArgCount) into = new object[ArgCount];
            double scale = Formulas.SkillLevelScale(balance, level) * Formulas.SkillAwakenScale(balance, stars);
            into[0] = Num(def.DamageMult * 100d * scale);
            into[1] = def.Waves;
            into[2] = Num(def.DotPercent * scale);
            into[3] = Num(def.DotSeconds);
            into[4] = Num(def.StunSeconds);
            into[5] = Num(def.BuffAmount * scale);
            into[6] = Num(def.BuffSeconds);
            into[7] = Num(def.HealPercent * scale);
            into[8] = Num(def.ShieldPercent * scale);
            into[9] = Num(def.ShieldSeconds);
            into[10] = Num(def.HpThreshold);
            into[11] = Num(def.Cooldown);
            into[12] = Num(def.MarkPercent * scale);
            into[13] = Num(def.MarkSeconds);
            return into;
        }

        public static string Num(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
