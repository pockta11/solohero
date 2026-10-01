using System;
using SoloHero.Core.Companions;
using SoloHero.Core.Config;

namespace SoloHero.Core.Combat
{
    /// <summary>
    /// D-102: the equipped companion attacks the nearest enemy in its range every Interval seconds for hero ATK x
    /// AttackMult x level scale, with the D-098 combo bonus, and applies its burn / stun. Ticks with the stage.
    /// </summary>
    public sealed class CompanionCaster
    {
        private readonly BalanceValues _balance;
        private CompanionDef _def;
        private double _scale = 1d;
        private float _timer;

        public CompanionCaster(BalanceValues balance)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        public CompanionDef Def => _def;

        /// <summary>The companion struck: target, damage dealt.</summary>
        public event Action<EnemyBrain, double> Attacked;

        public void Set(CompanionDef def, int level)
        {
            if (def != _def) _timer = def != null ? def.Interval : 0f;
            _def = def;
            _scale = Formulas.CompanionLevelScale(_balance, level);
        }

        public void Reset() => _timer = _def != null ? _def.Interval : 0f;

        public void Tick(float dt, HeroBrain hero, ICombatWorld world)
        {
            if (_def == null || hero == null || world == null || hero.State == HeroState.Dead) return;
            _timer -= dt;
            if (_timer > 0f) return;
            EnemyBrain target = world.NearestEnemyInRange(_def.Range);
            if (target == null)
            {
                _timer = 0f;
                return;
            }

            _timer = _def.Interval;
            double damage = DamageCalc.SkillHit(hero.Stats, _def.AttackMult * _scale)
                * DamageCalc.SkillCombo(_balance, target.IsStunned, target.HasDot);
            target.TakeDamage(damage);
            world.ReportHit(target, damage, HitKind.Companion);
            if (_def.DotPercent > 0d) target.ApplyDot(DamageCalc.SkillDot(hero.Stats, _def.DotPercent, _scale), _def.DotSeconds);
            if (_def.StunSeconds > 0f) target.Stun(_def.StunSeconds);
            Attacked?.Invoke(target, damage);
        }
    }
}
