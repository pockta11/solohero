using System;
using SoloHero.Core.Common;
using SoloHero.Core.Config;
using SoloHero.Core.Skills;
using SoloHero.Core.Talents;

namespace SoloHero.Core.Combat
{
    /// <summary>
    /// Runs the equipped skills in combat (D-078): up to SKILL_SLOT_COUNT slots cast automatically in slot order
    /// when off cooldown and their condition holds (enemy in reach, or HP under the heal threshold), with
    /// SKILL_SEQUENCE_GAP between casts. A cast lands its first wave at once; later waves follow every
    /// WaveInterval. Buffs last per slot and are summed into the hero every tick. Allocation free while ticking.
    /// With <see cref="AutoEnabled"/> off (D-085 manual mode) only <see cref="TryCast"/> starts casts; cooldowns, buffs
    /// and pending waves keep running.
    /// </summary>
    public sealed class SkillAutoCaster
    {
        private const int MaxPending = 8;

        private readonly BalanceValues _balance;
        private readonly SkillDef[] _defs;
        private readonly int[] _levels;
        private readonly float[] _cooldown;
        private readonly float[] _buffRemaining;

        private readonly SkillDef[] _pendingDef = new SkillDef[MaxPending];
        private readonly double[] _pendingDamage = new double[MaxPending];
        private readonly double[] _pendingDot = new double[MaxPending];
        private readonly int[] _pendingWaves = new int[MaxPending];
        private readonly float[] _pendingTimer = new float[MaxPending];

        private float _sequenceGap;
        private TalentEffects _talents = TalentEffects.None;
        private int _damageCasts;
        private HeroBrain _castHero;

        public SkillAutoCaster(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            // D-104: one extra slot after the book slots holds the second job's ultimate.
            int slots = (balance.SKILL_SLOT_COUNT < 1 ? 1 : balance.SKILL_SLOT_COUNT) + 1;
            _defs = new SkillDef[slots];
            _levels = new int[slots];
            _cooldown = new float[slots];
            _buffRemaining = new float[slots];
            for (int i = 0; i < slots; i++) _levels[i] = 1;
        }

        public int SlotCount => _defs.Length;

        /// <summary>D-104: the job ultimate's slot (the last one); it auto-casts even in manual mode.</summary>
        public int UltimateSlot => _defs.Length - 1;

        /// <summary>D-085: false in manual mode, where slots cast only when tapped.</summary>
        public bool AutoEnabled { get; set; } = true;

        /// <summary>Raised when a cast starts (auto or manual): slot, skill. Views play the cast effect and name.</summary>
        public event Action<int, SkillDef> SkillCast;

        /// <summary>Raised for every impact the skill's VfxAt asks for: skill, world X.</summary>
        public event Action<SkillDef, double> SkillImpact;

        public double BuffAtk => BuffSum(SkillBuff.Atk) / 100d;
        public double BuffAtkSpd => BuffSum(SkillBuff.AtkSpd) / 100d;
        public double BuffCrit => BuffSum(SkillBuff.Crit);
        public double BuffGuard => BuffSum(SkillBuff.Guard) / 100d;

        /// <summary>Kept for the ATK-only callers: the summed ATK buff fraction.</summary>
        public double AtkBuffSum => BuffAtk;

        /// <summary>Puts a skill (or null for an empty / locked slot) into a slot and resets that slot's timers.</summary>
        public void SetSlot(int slot, SkillDef def, int level)
        {
            if (slot < 0 || slot >= _defs.Length) return;
            if (level < 1) level = 1;
            if (level > _balance.SKILL_MAX_LEVEL) level = _balance.SKILL_MAX_LEVEL;
            if (_defs[slot] != def)
            {
                _cooldown[slot] = 0f;
                _buffRemaining[slot] = 0f;
            }

            _defs[slot] = def;
            _levels[slot] = level;
        }

        public SkillDef DefAt(int slot) => slot >= 0 && slot < _defs.Length ? _defs[slot] : null;

        public int LevelAt(int slot) => slot >= 0 && slot < _levels.Length ? _levels[slot] : 1;

        public bool IsReady(int slot) => DefAt(slot) != null && _cooldown[slot] <= 0f;

        public float CooldownRemaining(int slot) => slot >= 0 && slot < _cooldown.Length ? _cooldown[slot] : 0f;

        public float CooldownTotal(int slot)
        {
            SkillDef def = DefAt(slot);
            return def != null ? Cooldown(def) : 0f;
        }

        /// <summary>D-087 skill talents: damage, cooldown, buff duration, DoT, heal, Overload.</summary>
        public void SetTalents(TalentEffects talents) => _talents = talents ?? TalentEffects.None;

        /// <summary>Cooldown after the Haste talent (at most -50%).</summary>
        public float Cooldown(SkillDef def) => (float)(def.Cooldown * (1d - Math.Min(0.5d, _talents.CooldownPct)));

        /// <summary>Seconds left on the buff this slot gave (0 when none is running).</summary>
        public float BuffRemaining(int slot) => slot >= 0 && slot < _buffRemaining.Length ? _buffRemaining[slot] : 0f;

        public void SetCooldown(int slot, float seconds)
        {
            if (slot < 0 || slot >= _cooldown.Length) return;
            _cooldown[slot] = seconds < 0f ? 0f : seconds;
        }

        public void ResetCooldowns()
        {
            for (int i = 0; i < _cooldown.Length; i++)
            {
                _cooldown[i] = 0f;
                _buffRemaining[i] = 0f;
            }

            for (int i = 0; i < MaxPending; i++) ClearPending(i);
            _sequenceGap = 0f;
            _damageCasts = 0;
        }

        public void Tick(float dt, HeroBrain hero, ICombatWorld world, bool isBossFight)
        {
            for (int i = 0; i < _cooldown.Length; i++)
            {
                if (_cooldown[i] > 0f) _cooldown[i] -= dt;
                if (_buffRemaining[i] > 0f)
                {
                    _buffRemaining[i] -= dt;
                    if (_buffRemaining[i] < 0f) _buffRemaining[i] = 0f;
                }
            }

            if (_sequenceGap > 0f) _sequenceGap -= dt;
            if (hero == null || world == null) return;

            if (hero.State == HeroState.Dead)
            {
                for (int i = 0; i < MaxPending; i++) ClearPending(i);
                return;
            }

            TickPending(dt, world);

            if (hero.State == HeroState.Skill) return;
            if (_sequenceGap > 0f) return;

            for (int i = 0; i < _defs.Length; i++)
            {
                if (!AutoEnabled && i != UltimateSlot) continue;
                SkillDef def = _defs[i];
                if (def == null || _cooldown[i] > 0f) continue;
                if (!MeetsAutoCondition(def, hero, world, isBossFight)) continue;
                if (TryCast(i, hero, world).Ok) return;
            }
        }

        /// <summary>Manual tap on a HUD skill button: casts if ready, whatever the auto condition says.</summary>
        public Result TryCast(int slot, HeroBrain hero, ICombatWorld world)
        {
            if (hero == null || world == null) return Result.Fail(FailReason.Busy);
            if (hero.State == HeroState.Dead || hero.State == HeroState.Skill) return Result.Fail(FailReason.Busy);

            SkillDef def = DefAt(slot);
            if (def == null) return Result.Fail(FailReason.Locked);
            if (_cooldown[slot] > 0f) return Result.Fail(FailReason.OnCooldown);
            if (def.DealsDamage && !world.HasEnemyInRange(def.Range)) return Result.Fail(FailReason.Busy);

            Result begin = hero.TryBeginSkill();
            if (!begin.Ok) return begin;

            SkillCast?.Invoke(slot, def);
            Apply(slot, def, hero, world);
            _cooldown[slot] = Cooldown(def);
            _sequenceGap = _balance.SKILL_SEQUENCE_GAP;
            hero.EndSkill(world);
            return Result.Success;
        }

        private bool MeetsAutoCondition(SkillDef def, HeroBrain hero, ICombatWorld world, bool isBossFight)
        {
            if (def.HpThreshold > 0d) return hero.Hp <= hero.MaxHp * def.HpThreshold / 100d;
            switch (def.Kind)
            {
                case SkillKind.Strike:
                case SkillKind.Area:
                    return world.HasEnemyInRange(def.Range);
                case SkillKind.Buff:
                    return world.HasEnemyInRange(def.Range) || (isBossFight && world.AliveCount > 0);
                case SkillKind.Heal:
                    return hero.Hp < hero.MaxHp;
                default:
                    return false;
            }
        }

        private void Apply(int slot, SkillDef def, HeroBrain hero, ICombatWorld world)
        {
            double scale = Formulas.SkillLevelScale(_balance, _levels[slot]);

            double heal = 1d + _talents.HealPct;
            if (def.HealPercent > 0d) hero.Heal(hero.MaxHp * def.HealPercent / 100d * scale * heal);
            if (def.ShieldPercent > 0d) hero.AddShield(hero.MaxHp * def.ShieldPercent / 100d * scale * heal, def.ShieldSeconds);
            if (def.Buff != SkillBuff.None && def.BuffSeconds > 0f)
                _buffRemaining[slot] = (float)(def.BuffSeconds * (1d + _talents.BuffDurationPct));

            if (!def.DealsDamage) return;

            // ATK buffs (this skill's own included) raise skill hits too (GDD stat order: buffs last).
            // Talents: skill damage for hits and DoT, DoT bonus, Overload doubles every Nth damaging cast.
            _damageCasts++;
            double overload = _talents.Overload && _damageCasts % TalentCatalog.OverloadEvery == 0 ? TalentCatalog.OverloadMult : 1d;
            double buff = (1d + BuffAtk) * (1d + _talents.SkillDamagePct) * overload;
            double damage = DamageCalc.SkillHit(hero.Stats, def.DamageMult * scale) * buff;
            double dot = def.DotPercent > 0d ? DamageCalc.SkillDot(hero.Stats, def.DotPercent, scale) * buff * (1d + _talents.DotPct) : 0d;
            _castHero = hero;
            Wave(def, damage, dot, world);
            if (def.Waves > 1) Enqueue(def, damage, dot, def.Waves - 1);
        }

        private void Enqueue(SkillDef def, double damage, double dot, int waves)
        {
            for (int i = 0; i < MaxPending; i++)
            {
                if (_pendingWaves[i] > 0) continue;
                _pendingDef[i] = def;
                _pendingDamage[i] = damage;
                _pendingDot[i] = dot;
                _pendingWaves[i] = waves;
                _pendingTimer[i] = def.WaveInterval;
                return;
            }
        }

        private void TickPending(float dt, ICombatWorld world)
        {
            for (int i = 0; i < MaxPending; i++)
            {
                if (_pendingWaves[i] <= 0) continue;
                _pendingTimer[i] -= dt;
                while (_pendingWaves[i] > 0 && _pendingTimer[i] <= 0f)
                {
                    SkillDef def = _pendingDef[i];
                    Wave(def, _pendingDamage[i], _pendingDot[i], world);
                    _pendingWaves[i]--;
                    _pendingTimer[i] += def.WaveInterval > 0f ? def.WaveInterval : 0.1f;
                }

                if (_pendingWaves[i] <= 0) ClearPending(i);
            }
        }

        private void ClearPending(int i)
        {
            _pendingDef[i] = null;
            _pendingWaves[i] = 0;
            _pendingTimer[i] = 0f;
            _pendingDamage[i] = 0d;
            _pendingDot[i] = 0d;
        }

        /// <summary>One wave: Strike hits the nearest enemy in range, Area every enemy from the hero to the range.</summary>
        private void Wave(SkillDef def, double damage, double dot, ICombatWorld world)
        {
            if (def.Kind == SkillKind.Strike)
            {
                EnemyBrain target = world.NearestEnemyInRange(def.Range);
                if (target == null) return;
                Hit(def, target, damage, dot, world);
                if (def.VfxAt == SkillVfxAt.Impact || def.VfxAt == SkillVfxAt.EachTarget) SkillImpact?.Invoke(def, target.X);
                return;
            }

            bool first = true;
            int slots = world.SlotCount;
            for (int s = 0; s < slots; s++)
            {
                EnemyBrain e = world.GetSlot(s);
                if (e == null || !e.IsAlive) continue;
                if (e.X < world.HeroX || e.X - world.HeroX > def.Range) continue;
                double x = e.X;
                Hit(def, e, damage, dot, world);
                if (def.VfxAt == SkillVfxAt.EachTarget || (first && def.VfxAt == SkillVfxAt.Impact)) SkillImpact?.Invoke(def, x);
                first = false;
            }
        }

        private void Hit(SkillDef def, EnemyBrain target, double damage, double dot, ICombatWorld world)
        {
            if (_castHero != null) damage *= _castHero.TalentHitMult(target);
            // The combo reads the state from earlier hits; this skill's own stun / DoT applies after it.
            double combo = DamageCalc.SkillCombo(_balance, target.IsStunned, target.HasDot);
            damage *= combo;
            target.TakeDamage(damage);
            world.ReportHit(target, damage, combo > 1d ? HitKind.Combo : HitKind.Skill);
            if (dot > 0d) target.ApplyDot(dot, def.DotSeconds);
            if (def.StunSeconds > 0f) target.Stun(def.StunSeconds);
        }

        private double BuffSum(SkillBuff stat)
        {
            double sum = 0d;
            for (int i = 0; i < _defs.Length; i++)
            {
                SkillDef def = _defs[i];
                if (def == null || def.Buff != stat || _buffRemaining[i] <= 0f) continue;
                sum += def.BuffAmount * Formulas.SkillLevelScale(_balance, _levels[i]);
            }

            return sum;
        }
    }
}
